using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace DiscordDeck;
static class LanTest {
 public static void Run(){using var form=new Form();Exception? failure=null;
 form.Shown+=async(_,_)=>{try{
  var settings=new Settings();var played=-1;using var server=new LanServer(settings,(command,slot)=>{if(command=="play")played=slot;return new{ok=true,slots=Enumerable.Range(1,100).Select(i=>new{id=i,name=$"Sound {i}",assigned=false})};});
  using var http=new HttpClient{BaseAddress=new Uri("http://127.0.0.1:47836")};
  void Check(bool ok,string why){if(!ok)throw new Exception(why);}
  Check((await http.GetAsync("/status")).StatusCode==HttpStatusCode.Forbidden,"unpaired status allowed");
  var pin=server.BeginPairing();var bad=await http.PostAsync("/pair",new StringContent("{\"pin\":\"000000\"}",Encoding.UTF8,"application/json"));Check(bad.StatusCode==HttpStatusCode.Forbidden,"bad PIN allowed");
  var pair=await http.PostAsync("/pair",new StringContent(JsonSerializer.Serialize(new{pin}),Encoding.UTF8,"application/json"));var paired=JsonDocument.Parse(await pair.Content.ReadAsStringAsync());Check(paired.RootElement.GetProperty("key").GetString()==settings.MobileToken,"wrong pairing key");
  http.DefaultRequestHeaders.Add("X-DiscordDeck-Key",settings.MobileToken);
  var status=JsonDocument.Parse(await http.GetStringAsync("/status"));Check(status.RootElement.GetProperty("slots").GetArrayLength()==100,"slots lost");
  Check((await http.PostAsync("/play/100",null)).IsSuccessStatusCode&&played==99,"slot 100 routing failed");
  Check((await http.PostAsync("/play/101",null)).StatusCode==HttpStatusCode.BadRequest,"slot 101 allowed");
  Check((await http.PostAsync("/show",null)).StatusCode==HttpStatusCode.NotFound,"LAN can activate PC UI");
  var reused=await http.PostAsync("/pair",new StringContent(JsonSerializer.Serialize(new{pin}),Encoding.UTF8,"application/json"));Check(reused.StatusCode==HttpStatusCode.Forbidden,"pair code reusable");
  using var udp=new UdpClient();var message=Encoding.UTF8.GetBytes("DISCOVER_DISCORDDECK_1");await udp.SendAsync(message,new IPEndPoint(IPAddress.Loopback,LanServer.DiscoveryPort));var answer=await udp.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(3));var identity=JsonDocument.Parse(answer.Buffer);Check(identity.RootElement.GetProperty("id").GetString()==settings.ServerId,"discovery mismatch");Check(!Encoding.UTF8.GetString(answer.Buffer).Contains(settings.MobileToken),"discovery leaks key");
  Check(!LanServer.IsLocal(IPAddress.Parse("8.8.8.8"))&&LanServer.IsLocal(IPAddress.Parse("192.168.0.20")),"IP filtering failed");
  bool blocked=false;try{MyInstants.Validate("https://www.myinstants.com.evil.test/instant/x");}catch(InvalidDataException){blocked=true;}Check(blocked,"spoofed source accepted");
  var imported=await MyInstants.Import("https://www.myinstants.com/en/instant/vine-boom-sound-70972/");Check(File.Exists(imported.File)&&new FileInfo(imported.File).Length>1000,"live MyInstants import failed");
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"lan-test.json"),JsonSerializer.Serialize(new{ok=true,slots=100,pairing=true,unauthorizedRejected=true,discovery=true,slot100=true,sourceImport=imported.Name,audioPlaybackTested=false}));
 }catch(Exception e){failure=e;File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"lan-test.json"),JsonSerializer.Serialize(new{ok=false,error=e.ToString()}));}finally{form.Close();}};
 Application.Run(form);if(failure!=null)Environment.ExitCode=1;
 }
}
