using Ghostbox.Core;
using System.Buffers.Binary;
using System.Text.Json;

string folder=Path.Combine(Path.GetTempPath(),"whispers-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
string input=Path.Combine(folder,"original.wav");var pcm=new byte[96000*4];
for(int i=0;i<pcm.Length/2;i++)BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i*2,2),(short)(7000*Math.Sin(2*Math.PI*1000*i/48000)+2000*Math.Sin(2*Math.PI*50*i/48000)));
using(var wave=new PcmWaveWriter(input))wave.Write(pcm);
string hash=await Investigation.HashAsync(input);
var endpoint=new SilenceStop(-38,1.5);var quietPcm=new byte[3840];var talkingPcm=pcm.AsSpan(0,3840).ToArray();
Check(!endpoint.Add(quietPcm)&&!endpoint.HeardActivity,"mic endpoint ignores initial silence");
for(int i=0;i<6;i++)Check(!endpoint.Add(talkingPcm),"mic stays open during activity");
Check(endpoint.HeardActivity,"mic endpoint requires sustained activity");
for(int i=0;i<37;i++)Check(!endpoint.Add(quietPcm),"mic allows the configured pause");
Check(endpoint.Add(quietPcm),"mic stops after 1.5 seconds quiet");
var spike=new SilenceStop(-38,1.5);spike.Add(talkingPcm);for(int i=0;i<38;i++)spike.Add(quietPcm);
Check(!spike.HeardActivity,"isolated short noise does not arm mic endpoint");
var waiting=new SilenceStop(-38,1.5,1);bool finished=false;for(int i=0;i<26;i++)finished|=waiting.Add(quietPcm);
Check(finished&&waiting.TimedOut,"mic endpoint times out when no activity arrives");
ProcessingRecipe Recipe(string key,double value)=>new(){Controls=new(){{key,new(true,value)}}};
async Task<string> Render(string name,ProcessingRecipe recipe,bool creative=false){string file=Path.Combine(folder,name+".wav");await Investigation.RenderAsync(input,file,recipe,0,4,creative,default);return file;}
double Duration(string path)=>ConversationFile.Inspect(path).Length/96000.0;
AudioAnalysis Analyze(string path){var wave=ConversationFile.Inspect(path);using var file=File.OpenRead(path);file.Position=wave.Offset+96000;var bytes=new byte[12288];int n=file.Read(bytes);return new AudioAnalyzer().Add(bytes[..n])!;}
string dry=await Render("dry",new());Check(File.ReadAllBytes(dry).AsSpan((int)ConversationFile.Inspect(dry).Offset).Slice(0,pcm.Length).SequenceEqual(pcm),"bypass export preserves every PCM sample");
foreach(string key in new[]{"echo","reverb","distortion","clicks","sweep"})
{
    string zero=await Render("zero-"+key,Recipe(key,0),true);var zeroWave=ConversationFile.Inspect(zero);
    Check(zeroWave.Length==pcm.Length&&File.ReadAllBytes(zero).AsSpan((int)zeroWave.Offset,pcm.Length).SequenceEqual(pcm),key+" zero amount is exact bypass");
}
foreach(double speed in new[]{.25,.5,.75,1,1.25,1.5,2})
{
    var p=Recipe("speed",speed);string path=await Render("pitch-"+speed,p);Check(Math.Abs(Duration(path)-4/speed)<.12,$"pitch-preserving {speed}x duration");Check(Math.Abs(Analyze(path).DominantHz-1000)<30,$"pitch-preserving {speed}x pitch");
    path=await Render("tape-"+speed,p with {PreservePitch=false});Check(Math.Abs(Duration(path)-4/speed)<.03,$"tape {speed}x duration");Check(Math.Abs(Analyze(path).DominantHz-1000*speed)<30,$"tape {speed}x pitch");
}
string reverse=await Render("reverse",new(){Reverse=true});var reversed=File.ReadAllBytes(reverse);var reversedWave=ConversationFile.Inspect(reverse);
Check(BinaryPrimitives.ReadInt16LittleEndian(reversed.AsSpan((int)reversedWave.Offset,2))==BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(pcm.Length-2,2)),"reverse starts with original final sample");
string regionReverse=Path.Combine(folder,"reverse-range.wav");await Investigation.RenderAsync(input,regionReverse,new(){Reverse=true},1,2,false,default);
var regionBytes=File.ReadAllBytes(regionReverse);var regionWave=ConversationFile.Inspect(regionReverse);
Check(Math.Abs(Duration(regionReverse)-1)<.001&&BinaryPrimitives.ReadInt16LittleEndian(regionBytes.AsSpan((int)regionWave.Offset,2))==BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(192000-2,2)),"reverse selection exports only the chosen range in reverse order");
foreach(var control in new[]{("hum",50d),("harmonics",1d),("hiss",6d),("highpass",180d),("lowpass",4200d),("clicks",25d),("voice",3d),("attenuate",6d),("bass",3d),("treble",-3d),("compressor",2d),("gain",3d),("normalize",-20d),("limiter",-1.5),("white",.1),("echo",.25),("reverb",.25),("distortion",.2),("chop",4d),("sweep",1d)})
{
    string path=await Render("control-"+control.Item1,Recipe(control.Item1,control.Item2),true);Check(new FileInfo(path).Length>44,"engine executes "+control.Item1);
}
string learned=await Render("profile",Recipe("denoise",12) with {NoiseSource=input,NoiseStart=0,NoiseEnd=.75});Check(Math.Abs(Duration(learned)-4)<.05,"noise-profile prefix omitted from processed recording");
foreach(string format in new[]{"mp3","flac"})
{
    string encoded=Path.Combine(folder,"import."+format),decoded=Path.Combine(folder,"decoded-"+format+".wav");
    await Investigation.RunAsync(["-v","error","-nostdin","-n","-i",input,encoded],default);string beforeHash=await Investigation.HashAsync(encoded);
    await Investigation.DecodeAsync(encoded,decoded,default);Check(Math.Abs(Duration(decoded)-4)<.05&&await Investigation.HashAsync(encoded)==beforeHash,"import "+format+" decodes and preserves original");
}
string stereo=await Render("stereo",new(){Channels=2});Check(BitConverter.ToInt16(File.ReadAllBytes(stereo),22)==2,"stereo export has two channels");
string shareMp3=Path.Combine(folder,"share-export.mp3"),shareDecoded=Path.Combine(folder,"share-export-decoded.wav");
await Investigation.RunAsync(["-hide_banner","-loglevel","error","-nostdin","-n","-i",dry,"-map","0:a:0","-vn","-c:a","libmp3lame","-b:a","192k",shareMp3],default);
await Investigation.DecodeAsync(shareMp3,shareDecoded,default);
Check(Math.Abs(Duration(shareDecoded)-4)<.05&&new FileInfo(shareMp3).Length>1000&&await Investigation.HashAsync(input)==hash,"192 kbps share MP3 decodes to expected duration and preserves original");
var multi=new ProcessingRecipe{Controls=new(){{"hum",new(true,60)},{"harmonics",new(true,1)},{"echo",new(true,.2)},{"delay",new(true,350)},{"feedback",new(true,.3)},{"limiter",new(true,-1.5)}}};
Check(new FileInfo(await Render("multi",multi,true)).Length>44,"combined harmonics, adjustable echo delay/feedback and limiter execute");
string oldOutputHash=await Investigation.HashAsync(dry);
bool refused=false;try{await Investigation.RenderAsync(input,dry,new(),0,4,false,default);}catch(IOException){refused=true;}Check(refused&&await Investigation.HashAsync(input)==hash&&await Investigation.HashAsync(dry)==oldOutputHash,"existing output refused and original/output both preserved");
refused=false;try{await Render("creative-refused",Recipe("white",.1));}catch(ArgumentException){refused=true;}Check(refused,"synthetic effects require explicit creative opt-in");
var session=new InvestigationSession{Name="Test",OriginalFile=input,OriginalSha256=hash,Start=.5,End=2,Recipe=Recipe("denoise",6),Markers=[new(1,"test note","denoise=6")]};
var restored=JsonSerializer.Deserialize<InvestigationSession>(JsonSerializer.Serialize(session))!;Check(restored.Markers[0].Note=="test note"&&restored.Recipe.Get("denoise")==6&&restored.OriginalSha256==hash,"session roundtrip preserves markers, recipe and original hash");
if(args.Length>0)
{
    string originalHash=await Investigation.HashAsync(args[0]),canonical=Path.Combine(folder,"real.wav");await Investigation.DecodeAsync(args[0],canonical,default);
    string target=Path.Combine(folder,"real-clean.wav");await Investigation.RenderAsync(args[0],target,Recipe("denoise",6),0,Math.Min(10,Duration(canonical)),false,default);
    Check(await Investigation.HashAsync(args[0])==originalHash,"genuine radio clip remains hash-identical after cleanup");
    Check(Analyze(target).ClippedPercent==0,"genuine radio cleanup fixture does not clip sampled frame");
}
Check(await Investigation.HashAsync(input)==hash,"all transforms preserve original file hash");
Console.WriteLine("All investigation checks passed. Fixtures: "+folder);

