using System;using System.Diagnostics;using System.IO;using System.Windows.Media.Imaging;
namespace RetroWindows {static class CacheChecks {
 [STAThread]static void Main(){
 NativeChecks();
 var watch=Stopwatch.StartNew();var image=ReferenceContent.Image(0,19);Console.WriteLine("panel_ms="+watch.Elapsed.TotalMilliseconds.ToString("F1"));
 watch.Restart();ReferenceContent.Image(1,20);Console.WriteLine("display_ms="+watch.Elapsed.TotalMilliseconds.ToString("F1"));
 watch.Restart();ReferenceContent.Interaction(0,19,1,true);Console.WriteLine("state_ms="+watch.Elapsed.TotalMilliseconds.ToString("F1"));
 double max=0;int checks=0;var random=new Random(42);
 for(int owner=0;owner<2;owner++)for(int scene=7;scene<=22;scene++)for(int i=0;i<3000;i++){
 double t=random.NextDouble()*24;var a=ReferenceMotion.Evaluate(owner,scene,t,24);var b=ReferenceMotion.At(owner,scene,t,24);
 if(a.Down!=b.Down||a.Digits!=b.Digits||a.ViewState!=b.ViewState||a.Show!=b.Show||a.PressedKey!=b.PressedKey||a.Button!=b.Button||a.Selection!=b.Selection)throw new Exception("State mismatch "+owner+"/"+scene+" at "+t);
 double error=(a.Pointer-b.Pointer).Length;max=Math.Max(error,max);if(error>.1||Math.Abs(a.SliderX-b.SliderX)>.1||Math.Abs(a.SliderY-b.SliderY)>.1)throw new Exception("Motion error "+owner+"/"+scene+" at "+t+": "+error);checks++;
 }
 Console.WriteLine("motion_checks="+checks+" max_pointer_error="+max.ToString("F6"));
 DiskCache.SaveImage("roundtrip",image);var loaded=DiskCache.Image("roundtrip",2400,1600);var aPixels=new byte[2400*1600*4];var bPixels=new byte[aPixels.Length];image.CopyPixels(aPixels,9600,0);loaded.CopyPixels(bPixels,9600,0);for(int i=0;i<aPixels.Length;i++)if(aPixels[i]!=bPixels[i])throw new Exception("Pixel mismatch");Console.WriteLine("lossless_roundtrip=PASS");
 DiskCache.Write("broken",new byte[]{1,2,3});if(DiskCache.Image("broken",2400,1600)!=null)throw new Exception("Corrupt cache accepted");Console.WriteLine("corrupt_cache_fallback=PASS");
 }
 static void NativeChecks(){
 var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
 int checkedPrefixes=0;
 foreach(var type in new[]{typeof(Macintosh.FluidView),typeof(FluidView)}){
 bool mac=type.Namespace=="Macintosh";
 foreach(string line in new[]{"Author","Motto"}){
 string text=(string)type.GetField("Signature"+line,flags).GetRawConstantValue();
 var sequence=(SignatureCache)type.GetField(line.ToLowerInvariant()+"Sequence",flags).GetValue(null);
 double pitch=mac?(line=="Author"?7:5):(double)type.GetField("Signature"+line+"Pitch",flags).GetValue(null);
 double x=650,y=line=="Author"?2620:2740;
 if(!mac){var ink=(System.Drawing.Rectangle)type.GetField("Signature"+line+"Ink",flags).GetValue(null);x=700-ink.X*pitch;y-=ink.Y*pitch;}
 double deadline=0;
 for(int count=0;count<=text.Length;count++){
 var original=(System.Windows.Media.Geometry)type.GetMethod("PixelText",flags).Invoke(null,mac?new object[]{text.Substring(0,count),x,y,pitch,false}:new object[]{text.Substring(0,count),x,y,pitch,false,true});
 var cached=sequence.Prefix(count);
 if(original.ToString(System.Globalization.CultureInfo.InvariantCulture)!=cached.ToString(System.Globalization.CultureInfo.InvariantCulture))throw new Exception("Signature geometry mismatch "+type+line+count);
 if(!object.ReferenceEquals(cached,sequence.Prefix(count)))throw new Exception("Geometry not reused");
 if(count>0){int i=count-1;deadline+=.09+((i*7+3)%5)*.022+(text[i]==' '?.085:0);if(sequence.Count(deadline)!=count||sequence.Count(deadline-1e-9)!=count-1)throw new Exception("Typing deadline mismatch");}
 checkedPrefixes++;
 }
 }
 }
 var win=typeof(FluidView);var originalRoute=(Array)win.GetMethod("MakePaintRoute",flags).Invoke(null,null);var cachedRoute=(Array)win.GetField("PaintRoute",flags).GetValue(null);if(originalRoute.Length!=cachedRoute.Length)throw new Exception("Route length");for(int i=0;i<originalRoute.Length;i++)if(!originalRoute.GetValue(i).Equals(cachedRoute.GetValue(i)))throw new Exception("Paint route mismatch");
 Console.WriteLine("signature_prefixes="+checkedPrefixes+" typing_boundaries=PASS native_paint_route=PASS");
 }

}}
