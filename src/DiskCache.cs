using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace RetroWindows {
static class DiskCache {
 internal static bool Enabled { get { return Environment.GetEnvironmentVariable("RETRO_DUET_CACHE")!="0"; } }
 internal static string FileName(string key){string root=Environment.GetEnvironmentVariable("RETRO_DUET_CACHE_DIR");if(string.IsNullOrEmpty(root))root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RetroDuetSaver","Cache");return Path.Combine(root,Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString("N"),key+".bin");}
 internal static byte[] Read(string key){if(!Enabled)return null;try{string path=FileName(key);if(!File.Exists(path)||new FileInfo(path).Length>20000000)return null;return File.ReadAllBytes(path);}catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}}
 internal static void Write(string key,byte[] bytes){if(!Enabled)return;string temp=null;try{string path=FileName(key);Directory.CreateDirectory(Path.GetDirectoryName(path));temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Delete(path);File.Move(temp,path);}catch(IOException){}catch(UnauthorizedAccessException){}finally{if(temp!=null)try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}}}
 internal static BitmapSource Image(string key,int width,int height){byte[] bytes=Read(key);if(bytes==null)return null;try{using(var input=new MemoryStream(bytes))using(var zip=new DeflateStream(input,CompressionMode.Decompress))using(var r=new BinaryReader(zip)){if(r.ReadInt32()!=0x52444331||r.ReadInt32()!=width||r.ReadInt32()!=height)return null;double dx=r.ReadDouble(),dy=r.ReadDouble();if(dx<=0||dy<=0||double.IsNaN(dx)||double.IsNaN(dy)||double.IsInfinity(dx)||double.IsInfinity(dy))return null;byte[] pixels=r.ReadBytes(width*height*4);if(pixels.Length!=width*height*4)return null;var image=BitmapSource.Create(width,height,dx,dy,PixelFormats.Pbgra32,null,pixels,width*4);image.Freeze();return image;}}catch(IOException){return null;}catch(ArgumentException){return null;}}
 internal static void SaveImage(string key,BitmapSource image){if(!Enabled)return;try{BitmapSource source=image;if(source.Format!=PixelFormats.Pbgra32){source=new FormatConvertedBitmap(source,PixelFormats.Pbgra32,null,0);source.Freeze();}byte[] pixels=new byte[source.PixelWidth*source.PixelHeight*4];source.CopyPixels(pixels,source.PixelWidth*4,0);using(var output=new MemoryStream()){using(var zip=new DeflateStream(output,CompressionMode.Compress,true))using(var w=new BinaryWriter(zip)){w.Write(0x52444331);w.Write(source.PixelWidth);w.Write(source.PixelHeight);w.Write(source.DpiX);w.Write(source.DpiY);w.Write(pixels);}Write(key,output.ToArray());}}catch(IOException){}catch(UnauthorizedAccessException){}}
}
}
