using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Windows.Media;using System.Windows.Media.Imaging;
using Brush=System.Drawing.Brush;
namespace RetroWindows {
static partial class ReferenceContent {
 static readonly Dictionary<int,BitmapSource> cache=new Dictionary<int,BitmapSource>();static readonly Queue<int> order=new Queue<int>();
 public static BitmapSource Image(int owner,int scene){int key=owner*100+scene;BitmapSource value;if(cache.TryGetValue(key,out value))return value;
 using(var art=new Painter(owner==0))using(var bitmap=new Bitmap(2400,1600,System.Drawing.Imaging.PixelFormat.Format32bppPArgb)){using(var g=Graphics.FromImage(bitmap)){g.Clear(System.Drawing.Color.White);g.ScaleTransform(2,2);art.Draw(g,scene);}
 var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
 try{var raw=new byte[data.Stride*bitmap.Height];Marshal.Copy(data.Scan0,raw,0,raw.Length);for(int i=0;i<raw.Length;i+=4){byte a=(byte)(255-raw[i]);raw[i]=raw[i+1]=raw[i+2]=0;raw[i+3]=a;}value=BitmapSource.Create(bitmap.Width,bitmap.Height,192,192,PixelFormats.Pbgra32,null,raw,data.Stride);value.Freeze();value=Macintosh.FluidView.BakeInk(value);}finally{bitmap.UnlockBits(data);}}
 // A bounded two-card cache avoids retaining a second full desktop for every new scene.
 while(order.Count>=2)cache.Remove(order.Dequeue());cache[key]=value;order.Enqueue(key);return value;
 }
 sealed partial class Painter:IDisposable {
 readonly bool mac;readonly SolidBrush black=new SolidBrush(System.Drawing.Color.Black),paper=new SolidBrush(System.Drawing.Color.White),gray=new SolidBrush(System.Drawing.Color.FromArgb(145,145,145)),light=new SolidBrush(System.Drawing.Color.FromArgb(215,215,215)),rim=new SolidBrush(System.Drawing.Color.FromArgb(65,255,255,255)),shadow=new SolidBrush(System.Drawing.Color.FromArgb(40,0,0,0));
 public Painter(bool mac){this.mac=mac;}
 void Fill(Graphics g,Brush b,float x,float y,float w,float h){g.FillRectangle(b,x,y,w,h);}
 void Cell(Graphics g,int x,int y,int p,Brush b){if(b==paper){Fill(g,b,x,y,p,p);return;}Fill(g,shadow,x+1,y+1.5f,p-.3f,p-.3f);Fill(g,b,x+.225f,y+.225f,p-.55f,p-.55f);Fill(g,rim,x+.225f,y+.225f,p-.55f,.35f);Fill(g,rim,x+.225f,y+.575f,.25f,p-.9f);}
 void Sprite(Graphics g,string[] rows,int x,int y,int p,Brush b){for(int j=0;j<rows.Length;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]=='1')Cell(g,x+i*p,y+j*p,p,b);}
 string[] Glyph(char c,bool mono){return mac?Macintosh.ReferenceFonts.Glyph(c):mono?WindowsPixels.MonoGlyph(c):WindowsPixels.Glyph(c);}
 int Advance(char c,bool mono){return mac?Macintosh.ReferenceFonts.Advance(c):mono?WindowsPixels.MonoAdvance(c):WindowsPixels.Advance(c);}
 int Width(string text,int p,bool mono){int w=0;foreach(char c in text)w+=Advance(c,mono)*p;return w;}
 void Text(Graphics g,string text,int x,int y,int p,bool mono,Brush brush){foreach(char c in text){Sprite(g,Glyph(c,mono),x,y,p,brush);x+=Advance(c,mono)*p;}}
 void Center(Graphics g,string text,int y,int p,bool mono){Text(g,text,600-Width(text,p,mono)/2,y,p,mono,black);}
 void Line(Graphics g,int x,int y,int w,Brush b){for(int i=0;i<w;i+=3)Cell(g,x+i,y,3,b);}
 void Edge(Graphics g,int x,int y,int w,int h,Brush top,Brush bottom){Fill(g,top,x,y,w,3);Fill(g,top,x,y,3,h);Fill(g,bottom,x,y+h-3,w,3);Fill(g,bottom,x+w-3,y,3,h);}
 void Box(Graphics g,int x,int y,int w,int h,bool inset){Fill(g,paper,x,y,w,h);Edge(g,x,y,w,h,inset?gray:light,inset?light:black);Edge(g,x+3,y+3,w-6,h-6,inset?black:paper,inset?paper:gray);}
 void Frame(Graphics g,string title,string menus){if(!mac){Art.ReferenceFrame(g,title,menus.Split(new[]{"    "},StringSplitOptions.RemoveEmptyEntries));return;}
 Box(g,40,55,1120,690,false);
 if(mac){Edge(g,46,61,1108,678,black,black);for(int k=0;k<5;k++)Line(g,61,73+k*9,1075,black);int tw=Width(title,5,false);Fill(g,paper,600-tw/2-18,64,tw+36,47);Text(g,title,600-tw/2,70,5,false,black);Fill(g,paper,61,73,30,30);Edge(g,61,73,30,30,black,black);}
 else{Fill(g,black,49,64,1102,54);Text(g,title,67,73,3,false,paper);for(int i=0;i<3;i++)Box(g,986+i*51,70,48,42,false);Line(g,998,100,21,black);Edge(g,1048,79,27,24,black,black);Sprite(g,new[]{"10001","01010","00100","01010","10001"},1097,79,4,black);}
 Text(g,menus,64,133,4,false,black);Line(g,49,178,1102,gray);
 }
 static readonly string[] Happy={"0011111111111100","0110000000000110","1100000000000011","1000000000000001","1000100000010001","1000100000010001","1000000100000001","1000000100000001","1001000000001001","1000111111110001","1000000000000001","1111111111111111","1000000000000001","1011111111111101","1000000000000001","1111111111111111"};
 static readonly string[] OldComputer={"1111111111111","1000000000001","1000000000001","1000000000001","1000000000001","1000000000001","1111111111111","0000011100000","0000011100000","0011111111100"};
 static readonly string[] Floppy={"1111111111111","1001111111001","1001000011001","1001000011001","1001111111001","1000000000001","1001111111001","1001000001001","1001000001001","1001000001001","1001111111001","1000000000001","1111111111111"};
 static readonly string[] Folder={"0111110000000","1100011111110","1000000000011","1000000000001","1000000000001","1000000000001","1000000000001","1000000000001","1111111111111"};
 static readonly string[] File={"1111111110000","1000000011000","1000000010100","1000000010010","1000000011111","1000000000001","1011111111101","1000000000001","1011111111101","1000000000001","1011111111101","1000000000001","1111111111111"};
 static readonly string[] Heart={"00110001100","01111011110","11111111111","11111111111","01111111110","00111111100","00011111000","00001110000","00000100000"};
 static readonly string[] Clock={"00011111000","00100000100","01000100010","10000100001","10000100001","10000111001","10000000001","01000000010","00100000100","00011111000"};
 static readonly string[] Mouse={"00000100000","00000100000","00001110000","00110001100","01000100010","10000100001","10000100001","10000000001","10000000001","10000000001","01000000010","00111111100"};
 static readonly string[] BrushIcon={"0000000111","0000001110","0000011100","0000111000","0001110000","0011100000","0111110000","1111110000","1111110000","0111100000"};
 static readonly string[] Printer={"00111111100","00100000100","00111111100","11111111111","10000000001","10111111101","10000000001","11111111111","00100000100","00111111100"};
 static readonly string[] Keyboard={"111111111111111","100000000000001","101010101010101","100000000000001","101010101010101","100000000000001","101111111111101","100000000000001","111111111111111"};
 void Desktop(Graphics g){int[,] bayer={{0,8,2,10},{12,4,14,6},{3,11,1,9},{15,7,13,5}};for(int y=0;y<800;y+=5)for(int x=0;x<1200;x+=5){double edge=Math.Max(0,Math.Min(1,Math.Min(Math.Min(x/180.0,(1200-x)/180.0),Math.Min(y/120.0,(800-y)/120.0))));if((x/5+y/5)%2==0&&bayer[(x/5)%4,(y/5)%4]<edge*16)Cell(g,x,y,5,black);}}
 void Flag(Graphics g,int x,int y,int p){for(int row=0;row<10;row++)for(int col=0;col<18;col++){bool on=col<7||col>9;if(on&&row!=4&&row!=5){int wave=(int)Math.Round(1.5*Math.Sin(col*.3));Cell(g,x+col*p,y+(row+wave)*p,p,black);}}}

 // Pixel paths retain the same cell pitch and relief as the desktop typography.
 void Path(Graphics g,int x,int y,int p,int[] points){for(int k=0;k<points.Length-2;k+=2){int ax=points[k],ay=points[k+1],bx=points[k+2],by=points[k+3],steps=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));for(int n=0;n<=steps;n++){double t=steps==0?0:(double)n/steps;int xx=(int)Math.Round(ax+(bx-ax)*t),yy=(int)Math.Round(ay+(by-ay)*t);for(int j=0;j<2;j++)for(int i=0;i<2;i++)Cell(g,x+(xx+i)*p,y+(yy+j)*p,p,black);}}}
 void Clippy(Graphics g,int x,int y,int p,bool blink){Path(g,x,y,p,new[]{7,33,3,28,2,9,3,4,6,1,13,0,20,2,24,7,25,29,23,36,19,40,13,41,8,38,6,33,6,14,8,9,11,7,15,8,18,11,19,28,17,32,13,33,10,30,10,17});
 Fill(g,paper,x+5*p,y+7*p,9*p,10*p);Fill(g,paper,x+15*p,y+6*p,9*p,10*p);if(blink){Line(g,x+6*p,y+12*p,6*p,black);Line(g,x+16*p,y+11*p,6*p,black);}else{Sprite(g,new[]{"00111100","01100110","11000011","11000011","11011011","11011011","01100110","00111100"},x+5*p,y+7*p,p,black);Sprite(g,new[]{"00111100","01100110","11000011","11000011","11011011","11011011","01100110","00111100"},x+15*p,y+6*p,p,black);};}
 void Assistant(Graphics g,bool blink){Frame(g,"Document1 - Microsoft Word","File    Edit    View    Insert    Format    Tools    Table    Help");
 Box(g,60,190,1080,66,false);Box(g,76,201,352,44,true);Text(g,"MS Sans Serif",88,205,2,false,black);Sprite(g,new[]{"1111111","0111110","0011100","0001000"},398,218,2,black);Box(g,444,201,112,44,true);Text(g,"12",457,205,2,false,black);Sprite(g,new[]{"1111111","0111110","0011100","0001000"},527,218,2,black);
 for(int i=0;i<3;i++){Box(g,586+i*62,200,52,46,false);string label=new[]{"B","I","U"}[i];Text(g,label,600+i*62,206,2,false,black);}for(int i=0;i<4;i++){int xx=801+i*76;for(int j=0;j<4;j++)Line(g,xx,208+j*8,j%2==0?42:32,black);}
 Edge(g,76,280,650,422,gray,light);for(int i=0;i<17;i++){int xx=98+i*35;Fill(g,gray,xx,286,2,i%4==0?15:7);}Text(g,"Across the Great Wall,",112,329,3,false,black);Text(g,"we can reach every",112,420,3,false,black);Text(g,"corner in the world.",112,474,3,false,black);Text(g,"By RylanTu",112,596,3,false,black);
 // Pixel-rounded Office Assistant balloon with a tail aimed at the clip.
 Fill(g,paper,764,292,340,168);Edge(g,770,292,328,168,black,black);Fill(g,paper,764,298,340,156);Fill(g,black,764,298,3,156);Fill(g,black,1101,298,3,156);Text(g,"It looks like",784,311,2,false,black);Text(g,"you're writing",784,350,2,false,black);Text(g,"a letter.",784,389,2,false,black);Path(g,873,457,3,new[]{0,0,5,8,12,0});Clippy(g,859,506,4,blink);
 }
 static readonly string[] OldBin={"0011111111100","0111111111110","0001000001000","0011111111100","0010101010100","0010101010100","0010101010100","0010101010100","0010101010100","0001111111000"};
 static readonly string[] OldDrive={"0011111111100","0110000000110","1100000000011","1111111111111","1000000010101","1111111111111"};
 static readonly string[] Panel={"111111111111111","100000000000001","101110101110101","101010100100101","101110101110101","100000000000001","101010101010101","100000000000001","111111111111111"};
 static readonly string[] Explorer={"000011111100000","001110000111000","011000000001100","110001111000110","100011001100010","100111111110010","100110000000010","110011000001110","011001111111000","001110000110000","000011111100000"};

 static readonly string[] Computer={"000111111111111111110000","001100000000000000011000","001011111111111111101000","001010000000000000101000","001010000000000000101000","001010000000000000101000","001010000000000000101000","001010000000000000101000","001010000000000000101000","001010000000000000101000","001011111111111111101000","001000000000000000001000","001111111111111111111000","000000000111100000000000","000000001100110000000000","000001111111111111000000","001111111111111111111100","011000000000000000000110","110111111111111001100011","100000000000000000000001","111111111111111111111111"};
 static readonly string[] Bin={"00000000111111000000","00000001100001100000","00011111111111111000","00110000000000001100","01111111111111111110","00010000000000010000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00010100100100110000","00001111111111100000"};
 static readonly string[] Drive={"000011111111111111110000","000110000000000000011000","001100000000000000001100","011000000000000000000110","110000000000000000000011","111111111111111111111111","100000000000000000000001","101111111111000001101101","100000000000000000000001","111111111111111111111111"};
 void Caption(Graphics g,string text,int cx,int y,int p){foreach(string line in text.Split('\n')){Text(g,line,cx-Width(line,p,false)/2,y,p,false,black);y+=mac?42:34;}}

 static void ILine(bool[,] a,int x,int y,int xx,int yy,bool ink){int n=Math.Max(Math.Abs(xx-x),Math.Abs(yy-y));for(int i=0;i<=n;i++){double u=n==0?0:(double)i/n;int px=(int)Math.Round(x+(xx-x)*u),py=(int)Math.Round(y+(yy-y)*u);if(px>=0&&px<24&&py>=0&&py<24)a[px,py]=ink;}}
 static void IBox(bool[,] a,int x,int y,int w,int h){ILine(a,x,y,x+w-1,y,true);ILine(a,x,y,x,y+h-1,true);ILine(a,x,y+h-1,x+w-1,y+h-1,true);ILine(a,x+w-1,y,x+w-1,y+h-1,true);}
 static void IEllipse(bool[,] a,int x,int y,int rx,int ry){for(int i=0;i<120;i++){double t=i*Math.PI/60;int px=x+(int)Math.Round(Math.Cos(t)*rx),py=y+(int)Math.Round(Math.Sin(t)*ry);if(px>=0&&px<24&&py>=0&&py<24)a[px,py]=true;}}
 static string[] WinIcon(int kind){var a=new bool[24,24];
 if(kind==1){IBox(a,2,1,20,22);IBox(a,6,1,12,8);IBox(a,14,2,3,5);IBox(a,5,13,14,10);for(int y=16;y<=20;y+=2)ILine(a,8,y,16,y,true);}
 if(kind==3){ILine(a,2,6,8,6,true);ILine(a,8,6,11,9,true);ILine(a,11,9,21,9,true);ILine(a,2,6,2,19,true);ILine(a,2,19,19,19,true);ILine(a,19,19,23,11,true);ILine(a,23,11,6,11,true);ILine(a,6,11,2,19,true);ILine(a,21,9,21,11,true);}
 if(kind==5){IEllipse(a,12,12,10,10);for(int k=0;k<12;k++){double t=k*Math.PI/6;int x=12+(int)Math.Round(Math.Sin(t)*8),y=12+(int)Math.Round(Math.Cos(t)*8);ILine(a,x,y,x,y,true);}ILine(a,12,12,12,5,true);ILine(a,12,12,17,14,true);IBox(a,11,11,3,3);}
 if(kind==6){IBox(a,1,3,22,18);ILine(a,2,6,21,6,true);for(int col=0;col<3;col++){int x=5+col*7;ILine(a,x,9,x,18,true);IBox(a,x-2,10+col*2,5,3);}}
 if(kind==7){IEllipse(a,10,14,8,7);IEllipse(a,6,12,1,1);IEllipse(a,9,9,1,1);IEllipse(a,13,10,1,1);IEllipse(a,5,16,1,1);ILine(a,11,19,21,3,true);ILine(a,13,20,23,4,true);ILine(a,21,3,23,4,true);ILine(a,10,20,13,20,true);}
 if(kind==8){IBox(a,6,1,13,9);for(int y=4;y<8;y+=2)ILine(a,9,y,16,y,true);IBox(a,2,9,21,10);ILine(a,3,12,21,12,true);IBox(a,6,15,13,8);ILine(a,9,18,16,18,true);ILine(a,9,20,16,20,true);a[19,11]=true;}
 if(kind==9){IBox(a,0,5,24,14);for(int r=0;r<3;r++)for(int c=0;c<7;c++)IBox(a,2+c*3,7+r*3,2,2);IBox(a,5,16,13,2);}
 if(kind==10){IEllipse(a,12,12,8,9);IEllipse(a,12,12,6,7);for(int y=6;y<12;y++)for(int x=9;x<17;x++)a[x,y]=false;ILine(a,5,12,19,12,true);ILine(a,5,14,18,14,true);ILine(a,19,12,19,14,true);ILine(a,1,18,22,4,true);ILine(a,1,19,22,5,true);ILine(a,1,18,3,21,true);ILine(a,3,21,11,19,true);}
 if(kind==11){ILine(a,4,1,15,1,true);ILine(a,15,1,21,7,true);ILine(a,21,7,21,23,true);ILine(a,21,23,4,23,true);ILine(a,4,23,4,1,true);ILine(a,15,1,15,7,true);ILine(a,15,7,21,7,true);for(int y=10;y<21;y+=3)ILine(a,7,y,18,y,true);}
 var rows=new string[24];for(int y=0;y<24;y++){var row=new char[24];for(int x=0;x<24;x++)row[x]=a[x,y]?'1':'0';rows[y]=new string(row);}return rows;}
 void Gallery(Graphics g){Frame(g,mac?"Scrapbook":"Desktop",mac?"File    Edit":"File    Edit    View    Help");
 string[][] icons=mac?new[]{Happy,Floppy,Folder,File,Heart,Clock,Mouse,BrushIcon,Printer,Keyboard}:new[]{Computer,WinIcon(1),Drive,WinIcon(3),Bin,WinIcon(5),WinIcon(6),WinIcon(7),WinIcon(8),WinIcon(9),WinIcon(10),WinIcon(11)};
 string[] names=mac?new[]{"Macintosh","Disk","Folder","Document","Favorites","Clock","Mouse","MacPaint","Printer","Keyboard"}:new[]{"My Computer","Floppy (A:)","Local Disk (C:)","My Documents","Recycle Bin","Date / Time","Control Panel","Paint","Printers","Keyboard","Internet\nExplorer","Readme.txt"};
 for(int i=0;i<icons.Length;i++){int cx=198+(i%4)*268,top=210+(i/4)*166;string[] icon=icons[i];int pitch=mac?5:4;int widest=0;foreach(string row in icon)widest=Math.Max(widest,row.Length);int x=cx-widest*pitch/2,y=top+(94-icon.Length*pitch)/2;Sprite(g,icon,x,y,pitch,black);Caption(g,names[i],cx,top+108,mac?3:2);}
 // Explorer's status line gives the icon field a defined lower edge.
 if(!mac){Line(g,53,715,1094,gray);Text(g,icons.Length+" object(s)",70,722,1,false,black);}
 }
 public void Draw(Graphics g,int scene){Desktop(g);if(DrawExtended(g,scene))return;if(scene==12||scene==13){Assistant(g,scene==13);return;}
 if(scene==7||scene==11){if(mac){Fill(g,paper,520,285,160,160);if(scene==11){Sprite(g,Floppy,522,285,12,black);Fill(g,paper,560,340,80,66);Text(g,"?",578,340,6,false,black);}else Sprite(g,Happy,520,285,10,black);}else if(scene==11){Fill(g,paper,180,330,840,100);Center(g,"Starting MS-DOS...",350,4,true);}else{Flag(g,490,285,13);Fill(g,paper,230,460,740,70);Center(g,"Starting Windows 95...",470,3,true);}return;}
 if(scene==10){Frame(g,"Welcome to Macintosh","File    Edit    View    Special");Sprite(g,Happy,520,245,10,black);Center(g,"Welcome to Macintosh.",485,5,false);Center(g,"January 24, 1984",585,3,false);return;}
 if(scene==8){Frame(g,mac?"Untitled - SimpleText":"Document - WordPad",mac?"File    Edit    Search    Font    Style":"File    Edit    View    Insert    Format    Help");
 if(mac){Text(g,"DON'T PANIC",100,300,8,false,black);Text(g,"The answer is 42.",100,455,4,false,black);Box(g,735,183,300,390,false);string[] sizes={"9 Point","10 Point","12 Point","14 Point","18 Point","24 Point","36 Point"};for(int i=0;i<sizes.Length;i++)Text(g,sizes[i],765,203+i*49,4,false,black);}
 else{Box(g,70,192,420,57,true);Text(g,"MS Sans Serif",85,204,3,false,black);Box(g,515,192,180,57,true);Text(g,"18",533,204,3,false,black);Text(g,"Windows 95",100,325,4,true,black);Text(g,"Passion for a better world",100,480,3,true,black);Box(g,515,249,180,345,false);int[] sizes={8,9,10,11,12,14,18,24};for(int i=0;i<sizes.Length;i++)Text(g,sizes[i].ToString(),544,258+i*40,3,false,black);}
 for(int i=0;i<20;i++){int x=82+i*52;Fill(g,black,x,678,3,i%5==0?30:15);}return;}
 if(scene==9){Gallery(g);return;}
 }
 public void Dispose(){black.Dispose();paper.Dispose();gray.Dispose();light.Dispose();rim.Dispose();shadow.Dispose();}
 }
}
}
