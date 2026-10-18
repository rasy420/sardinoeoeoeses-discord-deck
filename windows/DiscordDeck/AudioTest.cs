using NAudio.CoreAudioApi;
using NAudio.Wave;
namespace DiscordDeck;
public static class AudioTest {
 public static object Run(){
 void Stage(string stage)=>File.AppendAllText(Path.Combine(AppContext.BaseDirectory,"audio-test-stages.log"),DateTime.Now+" "+stage+Environment.NewLine);Stage("enumerate");
 var devices=AudioEngine.Devices();var output=devices.FirstOrDefault(d=>d.Direction=="Render"&&d.Name.StartsWith("CABLE Input",StringComparison.OrdinalIgnoreCase))??devices.FirstOrDefault(d=>d.Direction=="Render"&&d.Name.Contains("Steam Streaming Microphone"))??throw new Exception("Kein virtuelles Audiopaar aktiv");var input=devices.FirstOrDefault(d=>d.Direction=="Capture"&&d.Name.StartsWith("CABLE Output",StringComparison.OrdinalIgnoreCase))??devices.FirstOrDefault(d=>d.Direction=="Capture"&&d.Name.Contains("Steam Streaming Microphone"))??throw new Exception("Kein virtueller Eingang aktiv");
 var dir=Path.Combine(AppContext.BaseDirectory,"TestAudio");Directory.CreateDirectory(dir);var tone=Path.Combine(dir,"Testton.wav");
 using(var wav=new WaveFileWriter(tone,new WaveFormat(44100,16,1))){for(int i=0;i<44100;i++){double t=i/44100.0;float x=(float)(.3*Math.Sin(2*Math.PI*997*t)*Math.Min(1,t*100)*Math.Min(1,(1-t)*100));wav.WriteSample(x);}}
 Stage("open capture");using var en=new MMDeviceEnumerator();using var device=en.GetDevice(input.Id);using var capture=new WasapiCapture(device);var memory=new MemoryStream();var writer=new WaveFileWriter(new NAudio.Utils.IgnoreDisposeStream(memory),capture.WaveFormat);var recordingEnded=new ManualResetEventSlim();capture.DataAvailable+=(_,e)=>writer.Write(e.Buffer,0,e.BytesRecorded);capture.RecordingStopped+=(_,e)=>recordingEnded.Set();
 var s=new Settings{OutputId=output.Id,MixMicrophone=false,MonitorEnabled=false,MasterVolume=.7f};using var engine=new AudioEngine();string? error=null;engine.Failed+=e=>error=e;Stage("start engine");engine.Start(s);Stage("start capture");capture.StartRecording();Stage("play tone");Thread.Sleep(300);engine.Play(new SoundSlot{File=tone,Volume=1});Thread.Sleep(1400);engine.StopSounds();Thread.Sleep(300);Stage("stop capture");capture.StopRecording();if(!recordingEnded.Wait(2000))throw new Exception("Capture stop timeout");Stage("read samples");writer.Dispose();
 memory.Position=0;using var reader=new WaveFileReader(memory);var sample=reader.ToSampleProvider();float[] buffer=new float[(int)(reader.TotalTime.TotalSeconds*reader.WaveFormat.SampleRate*reader.WaveFormat.Channels)+10000];int total=0,n;while(total<buffer.Length&&(n=sample.Read(buffer,total,buffer.Length-total))>0)total+=n;Stage("samples read "+total);
 double re=0,im=0,energy=0;int channels=sample.WaveFormat.Channels,rate=sample.WaveFormat.SampleRate;int frames=total/channels;for(int i=0;i<frames;i++){double v=buffer[i*channels];re+=v*Math.Cos(2*Math.PI*997*i/rate);im+=v*Math.Sin(2*Math.PI*997*i/rate);energy+=v*v;}
 double amplitude=2*Math.Sqrt(re*re+im*im)/Math.Max(1,frames);double rms=Math.Sqrt(energy/Math.Max(1,frames));Stage("check missing file");bool missingRejected=false;try{engine.Play(new SoundSlot{File=Path.Combine(dir,"missing-cocoa-test.wav")});}catch(FileNotFoundException){missingRejected=true;}
 Stage("restart engine");engine.Stop();engine.Start(s);engine.Play(new SoundSlot{File=tone});engine.StopSounds();engine.Stop();if(error!=null)throw new Exception(error);
 bool passed=amplitude>.015&&amplitude<.4&&rms>.02&&rms<.4&&missingRejected;if(!passed)throw new Exception($"Audio-Test fehlgeschlagen: tone={amplitude:F4}, rms={rms:F4}, missing={missingRejected}");
 Stage("passed");return new{ok=true,output=output.Name,input=input.Name,measured997HzAmplitude=amplitude,rms,recordedFrames=frames,missingFileRejected=missingRejected,restartAndStop=true,discordReceptionVerified=false};
 }
}






