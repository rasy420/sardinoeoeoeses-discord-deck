using System.Net;
using System.Text;
using System.Text.Json;
namespace DiscordDeck;
public sealed class LocalApi:IDisposable {
 readonly HttpListener listener=new();readonly string token;readonly Func<string,int,object> dispatch;readonly SynchronizationContext ui;
 public LocalApi(string token,Func<string,int,object> dispatch){this.token=token;this.dispatch=dispatch;ui=SynchronizationContext.Current??throw new InvalidOperationException("UI context missing");listener.Prefixes.Add("http://127.0.0.1:47835/");listener.Start();_ = Loop();}
 async Task Loop(){while(listener.IsListening){HttpListenerContext c;try{c=await listener.GetContextAsync().ConfigureAwait(false);}catch(HttpListenerException){break;}catch(ObjectDisposedException){break;}_ = Handle(c);}}
 async Task Handle(HttpListenerContext c){try{
 c.Response.Headers["Access-Control-Allow-Origin"]="*";c.Response.Headers["Access-Control-Allow-Headers"]="Content-Type, X-DiscordDeck-Key";c.Response.Headers["Access-Control-Allow-Methods"]="GET, POST, OPTIONS";
 if(c.Request.HttpMethod=="OPTIONS"){c.Response.StatusCode=204;return;}
 if(c.Request.Headers["X-DiscordDeck-Key"]!=token){c.Response.StatusCode=403;return;}
 var path=c.Request.Url!.AbsolutePath;bool status=path=="/status";
 if((status&&c.Request.HttpMethod!="GET")||(!status&&c.Request.HttpMethod!="POST")){c.Response.StatusCode=405;return;}
 int slot=-1;string command=status?"status":path=="/stop"?"stop":path=="/show"?"show":path.StartsWith("/volume/delta/")&&int.TryParse(path[14..],out slot)&&slot>=-100&&slot<=100?"volume":path.StartsWith("/play/")&&int.TryParse(path[6..],out slot)&&slot>=1&&slot<=100?"play":"invalid";
 if(command=="invalid"){c.Response.StatusCode=404;return;}
 var tcs=new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
 ui.Post(_=>{try{tcs.SetResult(dispatch(command,command=="volume"?slot:slot-1));}catch(Exception e){tcs.SetResult(new{ok=false,error=e.Message});}},null);
 var result=await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));byte[] data=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));c.Response.ContentType="application/json";await c.Response.OutputStream.WriteAsync(data);
 }catch(Exception){try{c.Response.StatusCode=500;}catch{}}finally{c.Response.Close();}}
 public void Dispose(){listener.Stop();listener.Close();}
}


