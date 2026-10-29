using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
namespace DiscordDeck;

public sealed class LanServer:IDisposable {
 public const int Port=47836,DiscoveryPort=47837;
 readonly int serverPort;readonly Settings settings; readonly Func<string,int,object> dispatch;readonly SynchronizationContext ui;
 readonly WebApplication web;readonly UdpClient udp;readonly CancellationTokenSource stop=new();
 readonly object gate=new();string pin="";DateTime pairingUntil;int attempts;
 public string BeginPairing(){lock(gate){pin=RandomNumberGenerator.GetInt32(100000,1000000).ToString();pairingUntil=DateTime.UtcNow.AddMinutes(2);attempts=0;return pin;}}
 public object Identity()=>new{protocol="DiscordDeck/1",id=settings.ServerId,name=Environment.MachineName,port=serverPort,slots=100};
 public LanServer(Settings settings,Func<string,int,object> dispatch,int port=Port,int discoveryPort=DiscoveryPort){
  serverPort=port;
  this.settings=settings;this.dispatch=dispatch;ui=SynchronizationContext.Current??throw new InvalidOperationException("UI context missing");
  var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls($"http://0.0.0.0:{port}");web=builder.Build();
  web.Use(async(ctx,next)=>{if(!IsLocal(ctx.Connection.RemoteIpAddress)){ctx.Response.StatusCode=403;return;}await next(ctx);});
  web.MapGet("/discover",()=>Identity());
  web.MapGet("/download/android",()=>{var path=Path.Combine(AppContext.BaseDirectory,"DiscordDeck-Android.apk");return File.Exists(path)?Results.File(path,"application/vnd.android.package-archive","SSB-Android.apk"):Results.NotFound();});
  web.MapPost("/pair",async(HttpContext ctx)=>{
   if(ctx.Request.ContentLength is null or >1024)return Results.BadRequest();
   PairRequest? request;try{request=await ctx.Request.ReadFromJsonAsync<PairRequest>();}catch{return Results.BadRequest();}
   lock(gate){if(DateTime.UtcNow>pairingUntil||attempts>=5||string.IsNullOrEmpty(pin))return Results.StatusCode(403);attempts++;
    if(request?.pin!=pin)return Results.StatusCode(403);
    pin="";return Results.Json(new{ok=true,key=settings.MobileToken,id=settings.ServerId,name=Environment.MachineName});}
  });
  web.MapGet("/status",async(HttpContext ctx)=>Authorized(ctx)?Results.Json(await OnUi("phone-status",0)):Results.StatusCode(403));
  web.MapPost("/play/{slot:int}",async(HttpContext ctx,int slot)=>!Authorized(ctx)?Results.StatusCode(403):slot<1||slot>100?Results.BadRequest():Results.Json(await OnUi("play",slot-1)));
  foreach(var action in new[]{"play","stop"}){var command=action;web.MapPost("/"+action+"-sound/{id}",async(HttpContext ctx,string id)=>!Authorized(ctx)?Results.StatusCode(403):!Guid.TryParse(id,out _)?Results.BadRequest():Results.Json(await OnUi(()=>{var index=settings.Slots.FindIndex(s=>s.Id==id);if(index<0)return new{ok=false,error="Sound no longer exists"};return dispatch(command=="play"?"play":"stop-slot",index);})));}
  web.MapPost("/gain/{slot:int}/{percent:int}",async(HttpContext ctx,int slot,int percent)=>!Authorized(ctx)?Results.StatusCode(403):slot<1||slot>100||percent<0||percent>400?Results.BadRequest():Results.Json(await OnUi("gain",(slot-1)*401+percent)));web.MapPost("/stop",async(HttpContext ctx)=>Authorized(ctx)?Results.Json(await OnUi("stop",0)):Results.StatusCode(403));
  // Kachelfarben vom Handy: Farbnummer 0 entfernt, 1 bis 10 ist die Palette. Der PC speichert, das Handy liest es im Status zurück.
  web.MapPost("/tile-color/{id}/{color:int}",async(HttpContext ctx,string id,int color)=>!Authorized(ctx)?Results.StatusCode(403):!Guid.TryParse(id,out _)||color<0||color>TilePalette.Colors.Length?Results.BadRequest():Results.Json(await OnUi(()=>{var index=settings.Slots.FindIndex(s=>s.Id==id);if(index<0)return new{ok=false,error="Sound no longer exists"};return dispatch("tile-color",index*16+color);})));
  web.MapPost("/tile-fill/{on:int}",async(HttpContext ctx,int on)=>!Authorized(ctx)?Results.StatusCode(403):on is not (0 or 1)?Results.BadRequest():Results.Json(await OnUi("tile-fill",on)));
  udp=new UdpClient(new IPEndPoint(IPAddress.Any,discoveryPort));
  try{Task.Run(()=>web.StartAsync()).GetAwaiter().GetResult();_ = DiscoverLoop();}catch{udp.Dispose();throw;}
 }
 public static bool IsLocal(IPAddress? address){if(address==null)return false;if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();if(IPAddress.IsLoopback(address))return true;var b=address.GetAddressBytes();return b.Length==4&&(b[0]==10||b[0]==192&&b[1]==168||b[0]==172&&b[1]>=16&&b[1]<=31);}
 bool Authorized(HttpContext ctx){var supplied=Encoding.UTF8.GetBytes(ctx.Request.Headers["X-DiscordDeck-Key"].ToString());return CryptographicOperations.FixedTimeEquals(supplied,Encoding.UTF8.GetBytes(settings.MobileToken));}
 Task<object> OnUi(string command,int slot)=>OnUi(()=>dispatch(command,slot));
 async Task<object> OnUi(Func<object> action){var done=new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);ui.Post(_=>{try{done.SetResult(action());}catch(Exception e){done.SetResult(new{ok=false,error=e.Message});}},null);return await done.Task.WaitAsync(TimeSpan.FromSeconds(5));}
 async Task DiscoverLoop(){DateTime last=DateTime.MinValue;while(!stop.IsCancellationRequested){try{var packet=await udp.ReceiveAsync(stop.Token);if(!IsLocal(packet.RemoteEndPoint.Address)||packet.Buffer.Length>100||Encoding.UTF8.GetString(packet.Buffer)!="DISCOVER_DISCORDDECK_1"||DateTime.UtcNow-last<TimeSpan.FromMilliseconds(100))continue;last=DateTime.UtcNow;var data=JsonSerializer.SerializeToUtf8Bytes(Identity());await udp.SendAsync(data,packet.RemoteEndPoint,stop.Token);}catch(OperationCanceledException){break;}catch(ObjectDisposedException){break;}catch(SocketException)when(stop.IsCancellationRequested){break;}}}
 public void Dispose(){stop.Cancel();udp.Dispose();web.DisposeAsync().AsTask().GetAwaiter().GetResult();stop.Dispose();}
 sealed record PairRequest(string pin);
}
