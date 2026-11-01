using System;using System.IO;using System.Collections.Generic;using System.Windows;
namespace RetroWindows {static partial class ReferenceMotion {
 const int MotionSteps=2400;
 static readonly Dictionary<int,Sample[]> motionCache=new Dictionary<int,Sample[]>();
 static void RectWrite(BinaryWriter w,Rect r){w.Write(r.IsEmpty);if(!r.IsEmpty){w.Write(r.X);w.Write(r.Y);w.Write(r.Width);w.Write(r.Height);}}
 static Rect RectRead(BinaryReader r){return r.ReadBoolean()?Rect.Empty:new Rect(r.ReadDouble(),r.ReadDouble(),r.ReadDouble(),r.ReadDouble());}
 static Sample[] LoadMotion(int owner,int scene){string key="motion-"+owner+"-"+scene;var bytes=DiskCache.Read(key);if(bytes!=null)try{using(var reader=new BinaryReader(new MemoryStream(bytes))){if(reader.ReadInt32()!=MotionSteps)throw new IOException();var result=new Sample[MotionSteps+1];for(int i=0;i<result.Length;i++){result[i]=new Sample{Pointer=new Point(reader.ReadDouble(),reader.ReadDouble()),Button=RectRead(reader),Selection=RectRead(reader),Down=reader.ReadBoolean(),Show=reader.ReadBoolean(),Digits=reader.ReadInt32(),ViewState=reader.ReadInt32(),PressedKey=reader.ReadInt32(),SliderX=reader.ReadDouble(),SliderY=reader.ReadDouble(),DrawProgress=reader.ReadDouble()};}return result;}}catch(IOException){}catch(ArgumentException){}
 var samples=new Sample[MotionSteps+1];for(int i=0;i<samples.Length;i++)samples[i]=Evaluate(owner,scene,i*24.0/MotionSteps,24);
 using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream)){writer.Write(MotionSteps);foreach(var s in samples){writer.Write(s.Pointer.X);writer.Write(s.Pointer.Y);RectWrite(writer,s.Button);RectWrite(writer,s.Selection);writer.Write(s.Down);writer.Write(s.Show);writer.Write(s.Digits);writer.Write(s.ViewState);writer.Write(s.PressedKey);writer.Write(s.SliderX);writer.Write(s.SliderY);writer.Write(s.DrawProgress);}writer.Flush();DiskCache.Write(key,stream.ToArray());}}
 return samples;
 }
 public static Sample At(int owner,int scene,double age,double duration){if(!DiskCache.Enabled||scene<7)return Evaluate(owner,scene,age,duration);int key=owner*100+scene;Sample[] samples;if(!motionCache.TryGetValue(key,out samples)){samples=LoadMotion(owner,scene);motionCache[key]=samples;}double position=Math.Max(0,Math.Min(MotionSteps,age/duration*MotionSteps));int index=(int)position;if(index==MotionSteps)return samples[index];var a=samples[index];var b=samples[index+1];double u=position-index;
 // Keep clicks/releases and text states exact, including events between samples.
 if(a.Down!=b.Down||a.ViewState!=b.ViewState||a.Digits!=b.Digits||a.PressedKey!=b.PressedKey||a.Button!=b.Button||a.Selection!=b.Selection)return Evaluate(owner,scene,age,duration);
 a.Pointer=new Point(a.Pointer.X+(b.Pointer.X-a.Pointer.X)*u,a.Pointer.Y+(b.Pointer.Y-a.Pointer.Y)*u);a.SliderX+=(b.SliderX-a.SliderX)*u;a.SliderY+=(b.SliderY-a.SliderY)*u;a.DrawProgress+=(b.DrawProgress-a.DrawProgress)*u;return a;
 }
}}
