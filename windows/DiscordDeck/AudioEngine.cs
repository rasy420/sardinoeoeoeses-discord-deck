using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using static DiscordDeck.Lang;
namespace DiscordDeck;
public record AudioDevice(string Id,string Name,string Direction,string State);
public sealed class AudioEngine:IDisposable {
 readonly MixingSampleProvider mixer=new(WaveFormat.CreateIeeeFloatWaveFormat(48000,2)){ReadFully=true};
 readonly MixingSampleProvider sounds=new(WaveFormat.CreateIeeeFloatWaveFormat(48000,2)){ReadFully=true};
 readonly List<Clip> clips=new();readonly object gate=new();
 WasapiOut? monitor; BufferedWaveProvider? monitorBuffer; readonly TapProvider soundTap;
 long mirroredNonzero,renderedNonzero;
 public bool Monitoring=>monitor?.PlaybackState==PlaybackState.Playing;
 public long MirroredNonzero=>Interlocked.Read(ref mirroredNonzero);
 public long RenderedNonzero=>Interlocked.Read(ref renderedNonzero);
 static int CountNonzero(float[] samples,int offset,int count){int n=0;for(int i=offset;i<offset+count;i++)if(Math.Abs(samples[i])>0.00001f)n++;return n;}
 WasapiOut? output;WasapiCapture? mic;BufferedWaveProvider? micBuffer;VolumeSampleProvider? micGain;readonly VolumeSampleProvider master;VolumeSampleProvider? routeGain;
 public static bool IsVirtualOutput(string name)=>name.Contains("CABLE",StringComparison.OrdinalIgnoreCase)||name.Contains("Steam Streaming Microphone",StringComparison.OrdinalIgnoreCase);
 public bool Running=>output?.PlaybackState==PlaybackState.Playing;
 public event Action<string>? Failed;
 public AudioEngine(){master=new VolumeSampleProvider(sounds);soundTap=new TapProvider(master,MirrorSound);mixer.AddMixerInput(soundTap);sounds.MixerInputEnded+=(_,e)=>{if(e.SampleProvider is Clip c){lock(gate)clips.Remove(c);c.Dispose();}};}
 void MirrorSound(float[] samples,int offset,int count){var target=monitorBuffer;if(target==null)return;var bytes=new byte[count*4];Buffer.BlockCopy(samples,offset*4,bytes,0,bytes.Length);target.AddSamples(bytes,0,bytes.Length);Interlocked.Add(ref mirroredNonzero,CountNonzero(samples,offset,count));}
 public static List<AudioDevice> Devices(bool all=false){using var en=new MMDeviceEnumerator();return new[]{DataFlow.Render,DataFlow.Capture}.SelectMany(flow=>en.EnumerateAudioEndPoints(flow,all?DeviceState.All:DeviceState.Active).Select(d=>{using(d)return new AudioDevice(d.ID,d.FriendlyName,flow.ToString(),d.State.ToString());})).ToList();}
 public void Start(Settings s){
 Stop();using var en=new MMDeviceEnumerator();using var device=en.GetDevice(s.OutputId);
 if(!IsVirtualOutput(device.FriendlyName))throw new InvalidOperationException(T("Bitte einen virtuellen CABLE-Eingang wählen.","Please choose a virtual CABLE input."));
 try {SetVolume(s.MasterVolume,s.MicrophoneVolume);
 if(s.MonitorEnabled){using var headphones=string.IsNullOrEmpty(s.MonitorId)?en.GetDefaultAudioEndpoint(DataFlow.Render,Role.Communications):en.GetDevice(s.MonitorId);if(IsVirtualOutput(headphones.FriendlyName))throw new InvalidOperationException(T("Zum Mithören echte Kopfhörer auswählen.","Choose real headphones for listening."));monitorBuffer=new BufferedWaveProvider(master.WaveFormat){BufferDuration=TimeSpan.FromMilliseconds(200),DiscardOnBufferOverflow=true,ReadFully=true};monitor=new WasapiOut(headphones,AudioClientShareMode.Shared,true,40);monitor.PlaybackStopped+=OutputStopped;monitor.Init(new TapProvider(monitorBuffer.ToSampleProvider(),(b,o,n)=>Interlocked.Add(ref renderedNonzero,CountNonzero(b,o,n))));monitor.Play();}
 if(s.MixMicrophone){using var md=en.GetDevice(s.MicrophoneId);if(md.FriendlyName.Contains("CABLE",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(T("Als Mikrofon bitte dein echtes Mikrofon wählen (Rückkopplungsschutz).","Please choose your real microphone as the microphone (feedback protection)."));
 mic=new WasapiCapture(md);micBuffer=new BufferedWaveProvider(mic.WaveFormat){BufferDuration=TimeSpan.FromMilliseconds(400),DiscardOnBufferOverflow=true,ReadFully=true};
 micGain=new VolumeSampleProvider(Convert(micBuffer.ToSampleProvider())){Volume=s.MicrophoneVolume};mixer.AddMixerInput(micGain);mic.DataAvailable+=MicData;mic.RecordingStopped+=MicStopped;mic.StartRecording();}
 output=new WasapiOut(device,AudioClientShareMode.Shared,true,40);output.PlaybackStopped+=OutputStopped;routeGain=new VolumeSampleProvider(mixer){Volume=device.FriendlyName.Contains("Steam Streaming Microphone",StringComparison.OrdinalIgnoreCase)?0.03162278f:1f};output.Init(routeGain);output.Play();
 }catch{Stop();throw;}}
 void MicData(object? sender,WaveInEventArgs e){micBuffer?.AddSamples(e.Buffer,0,e.BytesRecorded);}
 void MicStopped(object? sender,StoppedEventArgs e){if(e.Exception!=null)Failed?.Invoke(T("Mikrofon getrennt: ","Microphone disconnected: ")+e.Exception.Message);}
 void OutputStopped(object? sender,StoppedEventArgs e){if(e.Exception!=null)Failed?.Invoke(T("Audioausgabe unterbrochen: ","Audio output interrupted: ")+e.Exception.Message);}
 public void SetVolume(float volume,float microphone){master.Volume=Math.Clamp(volume,0,1);if(micGain!=null)micGain.Volume=Math.Clamp(microphone,0,2);}
 public void Play(SoundSlot slot){if(!Running)throw new InvalidOperationException(T("Audio zuerst starten.","Start audio first."));if(!File.Exists(slot.File))throw new FileNotFoundException(T("Sounddatei fehlt. Bitte neu zuweisen.","Sound file is missing. Please assign it again."),slot.File);var c=new Clip(slot.File,slot.Volume,slot);lock(gate){if(clips.Count>=16){c.Dispose();throw new InvalidOperationException(T("Maximal 16 gleichzeitige Sounds. Mit Stopp beenden.","At most 16 sounds at once. Use Stop to end them."));}clips.Add(c);}try{sounds.AddMixerInput(c);}catch{lock(gate)clips.Remove(c);c.Dispose();throw;}}
 public int Playing{get{lock(gate)return clips.Count;}}
 public string PlayingSignature{get{lock(gate)return string.Join(",",clips.Select(c=>c.Slot?.Id).Distinct().Order());}}
 public HashSet<string> PlayingIds(){lock(gate)return clips.Where(c=>c.Slot!=null).Select(c=>c.Slot!.Id).ToHashSet();}
 public bool IsPlaying(SoundSlot slot){lock(gate)return clips.Any(c=>ReferenceEquals(c.Slot,slot));}
 public void StopSlot(SoundSlot slot){Clip[] stopped;lock(gate){stopped=clips.Where(c=>ReferenceEquals(c.Slot,slot)).ToArray();foreach(var c in stopped)clips.Remove(c);}foreach(var c in stopped){sounds.RemoveMixerInput(c);c.Dispose();}}
 public void SetSlotVolume(SoundSlot slot){lock(gate)foreach(var clip in clips)if(ReferenceEquals(clip.Slot,slot))clip.SetVolume(slot.Volume);}
 public void StopSounds(){Clip[] all;lock(gate){all=clips.ToArray();clips.Clear();}foreach(var c in all){sounds.RemoveMixerInput(c);c.Dispose();}monitorBuffer?.ClearBuffer();}
 public void Stop(){if(output!=null){output.PlaybackStopped-=OutputStopped;output.Stop();output.Dispose();output=null;}if(monitor!=null){monitor.PlaybackStopped-=OutputStopped;monitor.Stop();monitor.Dispose();monitor=null;}monitorBuffer=null;if(mic!=null){mic.DataAvailable-=MicData;mic.RecordingStopped-=MicStopped;mic.StopRecording();mic.Dispose();mic=null;}mixer.RemoveAllMixerInputs();mixer.AddMixerInput(soundTap);StopSounds();micBuffer=null;micGain=null;}
 public void Dispose()=>Stop();
 internal sealed class TapProvider:ISampleProvider {
 readonly ISampleProvider source;readonly Action<float[],int,int> tap;
 public TapProvider(ISampleProvider source,Action<float[],int,int> tap){this.source=source;this.tap=tap;}
 public WaveFormat WaveFormat=>source.WaveFormat;
 public int Read(float[] buffer,int offset,int count){int n=source.Read(buffer,offset,count);if(n>0)tap(buffer,offset,n);return n;}
 }
 internal static ISampleProvider Convert(ISampleProvider s){if(s.WaveFormat.SampleRate!=48000)s=new WdlResamplingSampleProvider(s,48000);if(s.WaveFormat.Channels==1)s=new MonoToStereoSampleProvider(s);if(s.WaveFormat.Channels!=2)throw new InvalidDataException(T("Bitte Mono- oder Stereo-Audiodateien verwenden.","Please use mono or stereo audio files."));return s;}
 internal sealed class Clip:ISampleProvider,IDisposable {
 readonly AudioFileReader file;readonly VolumeSampleProvider source;public SoundSlot? Slot {get;} readonly object sync=new();bool disposed;
 public Clip(string path,float volume,SoundSlot? slot=null):this(path,volume,slot,slot?.TrimStart??0,slot?.TrimEnd??0){}
 public Clip(string path,float volume,SoundSlot? slot,double trimStart,double trimEnd){Slot=slot;file=new AudioFileReader(path);try{var (start,end)=Range(trimStart,trimEnd,file.TotalTime.TotalSeconds);ISampleProvider s=file;if(start>0)file.CurrentTime=TimeSpan.FromSeconds(start);if(end<file.TotalTime.TotalSeconds)s=new OffsetSampleProvider(file){Take=TimeSpan.FromSeconds(end-start)};source=new VolumeSampleProvider(Convert(s)){Volume=Math.Clamp(volume,0,4)};}catch{file.Dispose();throw;}}
 // Gültiger Abspielbereich; ungültige oder leere Bereiche spielen den ganzen Sound.
 public static (double start,double end) Range(double start,double end,double total){if(!double.IsFinite(start)||start<0||start>=total)start=0;if(!double.IsFinite(end)||end<=0||end>total)end=total;if(end<=start){start=0;end=total;}return (start,end);}
 public WaveFormat WaveFormat=>source.WaveFormat;
 public void SetVolume(float volume){lock(sync)source.Volume=Math.Clamp(volume,0,4);}
 public int Read(float[] buffer,int offset,int count){lock(sync)return disposed?0:source.Read(buffer,offset,count);}
 public void Dispose(){lock(sync){if(disposed)return;disposed=true;file.Dispose();}}
 }
}





