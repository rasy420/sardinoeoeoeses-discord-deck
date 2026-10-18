using NAudio.Wave;
namespace DiscordDeck;
static class GainTest {
 public static void Run(){var path=Path.GetTempFileName();try{using(var w=new WaveFileWriter(path,WaveFormat.CreateIeeeFloatWaveFormat(48000,2))){for(int i=0;i<960;i++)w.WriteSample(.05f);}float Read(float gain){using var clip=new AudioEngine.Clip(path,gain);var b=new float[32];if(clip.Read(b,0,b.Length)!=b.Length)throw new Exception("short read");return b[0];}var normal=Read(1);var boost=Read(4);if(Math.Abs(boost/normal-4)>.001)throw new Exception("gain mismatch");using var live=new AudioEngine.Clip(path,1);live.SetVolume(3);var samples=new float[16];live.Read(samples,0,16);if(Math.Abs(samples[0]-.15f)>.001)throw new Exception("live gain mismatch");File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"gain-test.txt"),"PASS: 100%=0.05; 400%=0.20; live 300%=0.15. No audio devices opened.");}finally{File.Delete(path);}}
}
