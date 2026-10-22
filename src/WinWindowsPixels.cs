using System;using System.Collections.Generic;using System.Drawing;using System.Runtime.InteropServices;
namespace RetroWindows {
static class WindowsPixels {
 sealed class Set {public Dictionary<char,string[]> Glyphs=new Dictionary<char,string[]>();public Dictionary<char,int> Advances=new Dictionary<char,int>();public int Height;public string Face;}
 [StructLayout(LayoutKind.Sequential)]struct Size{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct Metrics {public int Height,Ascent,Descent,InternalLeading,ExternalLeading,AveWidth,MaxWidth,Weight,Overhang,DigitizedX,DigitizedY;public char First,Last,Default,Break;public byte Italic,Underlined,StrikeOut,Pitch,CharSet;}
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateFont(int h,int w,int e,int o,int weight,uint italic,uint underline,uint strike,uint charset,uint output,uint clip,uint quality,uint pitch,string face);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")]static extern int SetBkMode(IntPtr dc,int mode);
 [DllImport("gdi32.dll")]static extern uint SetTextColor(IntPtr dc,uint color);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern bool TextOut(IntPtr dc,int x,int y,string text,int count);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern bool GetTextExtentPoint32(IntPtr dc,string s,int n,out Size size);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern bool GetTextMetrics(IntPtr dc,out Metrics m);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern int GetTextFace(IntPtr dc,int count,System.Text.StringBuilder name);
 public static string FontInfo{get{return Body.Face+" "+Body.Height+"px; Bold "+Bold.Face+" "+Bold.Height+"px; Mono "+Mono.Face+" "+Mono.Height+"px";}}
 static readonly Set Body=Build("MS Sans Serif",-11,400),Bold=Build("MS Sans Serif",-11,700),Mono=Build("Fixedsys",-12,400);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern int AddFontResourceEx(string file,uint flags,IntPtr unused);
 static Set Build(string name,int height,int weight){
 string fontFile=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Fonts",name=="Fixedsys"?"vgafix.fon":"sserife.fon");AddFontResourceEx(fontFile,16,IntPtr.Zero);
 var set=new Set();
 using(var b=new Bitmap(80,48))using(var g=Graphics.FromImage(b)){
 var font=CreateFont(height,0,0,0,weight,0,0,0,0,0,0,3,0,name);
 try{for(int n=32;n<256;n++){
 char ch=CharacterCode(n);g.Clear(Color.White);var dc=g.GetHdc();Size size;Metrics metrics;IntPtr previous=SelectObject(dc,font);
 try{var faceName=new System.Text.StringBuilder(100);GetTextFace(dc,100,faceName);set.Face=faceName.ToString();SetBkMode(dc,1);SetTextColor(dc,0);GetTextMetrics(dc,out metrics);GetTextExtentPoint32(dc,ch.ToString(),1,out size);TextOut(dc,2,0,ch.ToString(),1);}finally{SelectObject(dc,previous);g.ReleaseHdc(dc);}
 int width=Math.Max(1,size.X);set.Height=metrics.Height;set.Advances[ch]=width;
 var rows=new string[set.Height];
 for(int y=0;y<rows.Length;y++){var row=new char[width+metrics.Overhang];for(int x=0;x<row.Length;x++)row[x]=b.GetPixel(x+2,y).R<128?'1':'0';rows[y]=new string(row);}
 set.Glyphs[ch]=rows;
 }}finally{DeleteObject(font);}
 }return set;
 }
 public static char CharacterCode(int n){return System.Text.Encoding.GetEncoding(1252).GetString(new byte[]{(byte)n})[0];}
 public static string[] BoldGlyph(char c){return Bold.Glyphs.ContainsKey(c)?Bold.Glyphs[c]:Bold.Glyphs['?'];}
 public static int BoldAdvance(char c){return Bold.Advances.ContainsKey(c)?Bold.Advances[c]:Bold.Advances['?'];}
 public static int Height{get{return Body.Height;}}
 public static Rectangle Bounds(string s,bool bold){int x=0,l=9999,t=9999,r=-1,b=-1;foreach(char c in s){var rows=bold?BoldGlyph(c):Glyph(c);for(int y=0;y<rows.Length;y++)for(int i=0;i<rows[y].Length;i++)if(rows[y][i]=='1'){l=Math.Min(l,x+i);r=Math.Max(r,x+i);t=Math.Min(t,y);b=Math.Max(b,y);}x+=bold?BoldAdvance(c):Advance(c);}return r<0?Rectangle.Empty:new Rectangle(l,t,r-l+1,b-t+1);}
 public static string[] Glyph(char c){string[] rows;return Body.Glyphs.TryGetValue(c,out rows)?rows:Body.Glyphs['?'];}
 public static string[] MonoGlyph(char c){string[] rows;return Mono.Glyphs.TryGetValue(c,out rows)?rows:Mono.Glyphs['?'];}
 public static string[] Digit(char c){return Glyph(c);}
 public static int Advance(char c){int n;return Body.Advances.TryGetValue(c,out n)?n:Body.Advances['?'];}
 public static int MonoAdvance(char c){int n;return Mono.Advances.TryGetValue(c,out n)?n:Mono.Advances['?'];}
 public static Rectangle MonoBounds(string text){int advance=0,left=9999,top=9999,right=-1,bottom=-1;foreach(char ch in text){var rows=MonoGlyph(ch);for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++)if(rows[y][x]=='1'){left=Math.Min(left,advance+x);right=Math.Max(right,advance+x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}advance+=MonoAdvance(ch);}return right<0?Rectangle.Empty:new Rectangle(left,top,right-left+1,bottom-top+1);}
 public static int MonoHeight{get{return Mono.Height;}}
}
}
