using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
namespace RetroWindows {
// Persist glyph cells, pen positions and typing deadlines, not rendered animation frames.
internal sealed class SignatureCache {
 readonly Rect[][] cells;
 readonly double[] deadlines, positions;
 readonly Geometry[] prefixes;
 SignatureCache(Rect[][] cells,double[] deadlines,double[] positions){this.cells=cells;this.deadlines=deadlines;this.positions=positions;prefixes=new Geometry[cells.Length+1];}
 internal double Duration {get{return deadlines.Length==0?0:deadlines[deadlines.Length-1];}}
 internal int Count(double elapsed){int lo=0,hi=deadlines.Length;while(lo<hi){int mid=(lo+hi)/2;if(elapsed>=deadlines[mid])lo=mid+1;else hi=mid;}return lo;}
 internal double Pen(int count){return positions[count];}
 internal Geometry Prefix(int count){if(prefixes[count]!=null)return prefixes[count];var geometry=new StreamGeometry{FillRule=FillRule.Nonzero};using(var c=geometry.Open())for(int i=0;i<count;i++)foreach(var r in cells[i]){c.BeginFigure(r.TopLeft,true,true);c.LineTo(r.TopRight,true,false);c.LineTo(r.BottomRight,true,false);c.LineTo(r.BottomLeft,true,false);}geometry.Freeze();return prefixes[count]=geometry;}
 internal static SignatureCache Open(string key,string text,double x,double y,double pitch,Func<char,string[]> glyph,Func<char,int> advance){
 byte[] data=DiskCache.Read(key);if(data!=null)try{using(var r=new BinaryReader(new MemoryStream(data))){if(r.ReadInt32()!=0x53494731||r.ReadString()!=text||r.ReadDouble()!=x||r.ReadDouble()!=y||r.ReadDouble()!=pitch)throw new IOException();var cells=new Rect[text.Length][];var deadlines=new double[text.Length];var positions=new double[text.Length+1];for(int i=0;i<positions.Length;i++){positions[i]=r.ReadDouble();if(!Finite(positions[i]))throw new IOException();}for(int i=0;i<cells.Length;i++){deadlines[i]=r.ReadDouble();if(!Finite(deadlines[i])||deadlines[i]<=(i==0?0:deadlines[i-1]))throw new IOException();int n=r.ReadInt32();if(n<0||n>4096)throw new IOException();cells[i]=new Rect[n];for(int j=0;j<n;j++){double a=r.ReadDouble(),b=r.ReadDouble(),w=r.ReadDouble(),h=r.ReadDouble();if(!Finite(a)||!Finite(b)||!Finite(w)||!Finite(h)||w<0||h<0)throw new IOException();cells[i][j]=new Rect(a,b,w,h);}}if(r.BaseStream.Position!=r.BaseStream.Length)throw new IOException();return new SignatureCache(cells,deadlines,positions);}}catch(IOException){}catch(ArgumentException){}
 var built=new Rect[text.Length][];var times=new double[text.Length];var pens=new double[text.Length+1];double pen=x,time=0,gap=Math.Min(.55,pitch*.11);pens[0]=x;
 for(int k=0;k<text.Length;k++){var rows=glyph(text[k]);var list=new List<Rect>();for(int j=0;j<rows.Length;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]=='1')list.Add(new Rect(pen+i*pitch+gap*.5,y+j*pitch+gap*.5,pitch-gap,pitch-gap));built[k]=list.ToArray();pen+=advance(text[k])*pitch;pens[k+1]=pen;time+=.09+((k*7+3)%5)*.022+(text[k]==' '?.085:0);times[k]=time;}
 using(var stream=new MemoryStream()){using(var w=new BinaryWriter(stream)){w.Write(0x53494731);w.Write(text);w.Write(x);w.Write(y);w.Write(pitch);foreach(double p in pens)w.Write(p);for(int i=0;i<built.Length;i++){w.Write(times[i]);w.Write(built[i].Length);foreach(var r in built[i]){w.Write(r.X);w.Write(r.Y);w.Write(r.Width);w.Write(r.Height);}}w.Flush();DiskCache.Write(key,stream.ToArray());}}
 return new SignatureCache(built,times,pens);
 }
 static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
}
}
