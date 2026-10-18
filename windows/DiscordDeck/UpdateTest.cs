using System.Text.Json;
using System.Security.Cryptography;
namespace DiscordDeck;
internal static class UpdateTest {
 internal static void Run(){
  var log=new List<string>();void Check(bool value,string label){if(!value)throw new Exception(label);log.Add(label);}
  string Fixture(string tag="v1.3.0",string host="github.com",string digest="sha256:"+"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",bool draft=false,bool pre=false,string name="SSB-Setup.exe")=>JsonSerializer.Serialize(new{tag_name=tag,draft,prerelease=pre,assets=new[]{new{name,browser_download_url="https://"+host+"/"+Updater.Repository+"/releases/download/"+tag+"/SSB-Setup.exe",digest}}});
  void Reject(string json,string label){try{Updater.Parse(json);}catch{log.Add(label);return;}throw new Exception(label);}
  Check(Updater.Parse(Fixture())?.Version==new Version(1,3,0),"Valid latest release parsed");
  Check(Updater.Parse(Fixture("v1.0.0"))!.Version<Updater.Current,"Older release does not trigger update");
  Check(Updater.Parse(Fixture(draft:true))==null,"Draft ignored");Check(Updater.Parse(Fixture(pre:true))==null,"Prerelease ignored");
  Reject(Fixture(host:"example.com"),"External download rejected");Reject(Fixture(digest:""),"Missing checksum rejected");Reject(Fixture(tag:"bad"),"Invalid version rejected");Reject(Fixture(name:"other.exe"),"Missing installer rejected");Reject("{}","Malformed response rejected");
  var path=Path.GetTempFileName();try{File.WriteAllText(path,"test installer");var hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));Check(Updater.Matches(path,hash),"Correct SHA256 accepted");File.AppendAllText(path,"tampered");Check(!Updater.Matches(path,hash),"Tampered download rejected");}finally{File.Delete(path);}
  File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"update-test.json"),JsonSerializer.Serialize(new{ok=true,log},new JsonSerializerOptions{WriteIndented=true}));
 }
}
