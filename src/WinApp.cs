using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Globalization;using System.IO;using System.Windows.Forms;using System.Xml.Serialization;
namespace RetroWindows {
public class Options {
 public int FrameRate=144; public bool Cinematic=true; public int Palette=0,Speed=100; public bool Clock=false,Seconds=false,Chinese=true,Animate=true;
 public static string FileName { get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RetroDuetSaver","settings.xml");} }
 public static Options Load(){try{using(var s=File.OpenRead(FileName)){var o=(Options)new XmlSerializer(typeof(Options)).Deserialize(s);o.Palette=Math.Max(0,Math.Min(16,o.Palette));o.Speed=Math.Max(25,Math.Min(200,o.Speed));return o;}}catch{return new Options();}}
 public void Save(){Directory.CreateDirectory(Path.GetDirectoryName(FileName));using(var s=File.Create(FileName))new XmlSerializer(typeof(Options)).Serialize(s,this);}
}
sealed class Settings:Form {
 Options o=Options.Load();ComboBox frameRate; CheckBox cinematic;ComboBox palette;TrackBar speed;CheckBox animate;
 public Settings(){Text="Mac × Windows · 屏保设置";ClientSize=new Size(470,535);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(245,244,239);
 Controls.Add(new Label{Text="Mac × Windows",Font=new Font("Segoe UI",26,FontStyle.Bold),Bounds=new Rectangle(26,18,410,48)});Controls.Add(new Label{Text="经典界面 · 消散转场 · 逐字签名",Bounds=new Rectangle(28,70,420,28),ForeColor=Color.DimGray});Controls.Add(new Label{Text="配色",Bounds=new Rectangle(28,116,90,28)});
 palette=new ComboBox{Bounds=new Rectangle(128,112,300,32),DropDownStyle=ComboBoxStyle.DropDownList};palette.Items.AddRange(new object[]{"单色背景缓慢变色 / 约 3 分钟一圈","琥珀黄","酒红","紫罗兰","青绿","橄榄绿","经典灰","橙","朱红","洋红","紫红","靛蓝","蓝","湖蓝","草绿","柠檬黄","金黄"});palette.SelectedIndex=o.Palette;Controls.Add(palette);
 Controls.Add(new Label{Text="动画速度",Bounds=new Rectangle(28,168,96,28)});speed=new TrackBar{Bounds=new Rectangle(122,158,310,45),Minimum=25,Maximum=200,TickFrequency=25,Value=o.Speed};Controls.Add(speed);
 animate=Check("缓慢镜头漫游",28,214,o.Animate);Controls.Add(new Label{Text="日期时间仅保留在背景像素面板中",Bounds=new Rectangle(28,260,405,28),ForeColor=Color.DimGray});
 Controls.Add(new Label{Text="F11 全屏 · 空格换色 · S 设置 · F2 帧率\nA 自动鼠标 · P 暂停 · Esc 退出",Bounds=new Rectangle(28,394,420,56),ForeColor=Color.DimGray});Controls.Add(new Label{Text="目标帧率",Bounds=new Rectangle(28,309,90,28)}); frameRate=new ComboBox{Bounds=new Rectangle(128,305,300,32),DropDownStyle=ComboBoxStyle.DropDownList};frameRate.Items.AddRange(new object[]{"60 FPS","120 FPS","144 FPS"});frameRate.SelectedIndex=o.FrameRate>=144?2:o.FrameRate>=120?1:0;Controls.Add(frameRate);cinematic=Check("自动鼠标与拖拽演出",28,350,o.Cinematic);var save=new Button{Text="保存设置",Bounds=new Rectangle(296,466,132,38)};save.Click+=delegate{o.FrameRate=new[]{60,120,144}[frameRate.SelectedIndex];o.Cinematic=cinematic.Checked;o.Palette=palette.SelectedIndex;o.Speed=speed.Value;o.Clock=false;o.Animate=animate.Checked;try{o.Save();DialogResult=DialogResult.OK;Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,"无法保存设置");}};Controls.Add(save);AcceptButton=save;
 }
 CheckBox Check(string t,int x,int y,bool v){var c=new CheckBox{Text=t,Bounds=new Rectangle(x,y,205,30),Checked=v};Controls.Add(c);return c;}
}

sealed class Art:IDisposable {
 public const int P=3,SoundTrackX=760,SoundTrackY=1170,SoundTrackHeight=180,SoundTravel=159;
 public const int MineX=452,MineY=1894,MineCell=48,MineSize=384;
 static readonly Color Silver=Color.White,Gray=Color.FromArgb(145,145,145),White=Color.White,Black=Color.Black;
 readonly Dictionary<int,SolidBrush> brushes=new Dictionary<int,SolidBrush>();
 SolidBrush Brush(Color c){SolidBrush b;if(!brushes.TryGetValue(c.ToArgb(),out b))brushes[c.ToArgb()]=b=new SolidBrush(c);return b;}
 void Fill(Graphics g,Color c,float x,float y,float w,float h){if(w>0&&h>0)g.FillRectangle(Brush(c),x,y,w,h);}
 void Cell(Graphics g,int x,int y,int p,Color c){if(c==White){Fill(g,c,x,y,p,p);return;}Fill(g,Color.FromArgb(30,0,0,0),x+.6f,y+.9f,p-.2f,p-.2f);Fill(g,c,x+.225f,y+.225f,p-.55f,p-.55f);Fill(g,Color.FromArgb(65,255,255,255),x+.225f,y+.225f,p-.55f,.35f);Fill(g,Color.FromArgb(65,255,255,255),x+.225f,y+.575f,.25f,p-.9f);}
 void Sprite(Graphics g,string[] rows,int x,int y,int p,Color c){for(int j=0;j<rows.Length;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]=='1')Cell(g,x+i*p,y+j*p,p,c);}
 void Text(Graphics g,string s,int x,int y,int p,Color c){foreach(char ch in s){Sprite(g,WindowsPixels.Glyph(ch),x,y,p,c);x+=WindowsPixels.Advance(ch)*p;}}
 void Bold(Graphics g,string s,int x,int y,Color c){foreach(char ch in s){Sprite(g,WindowsPixels.BoldGlyph(ch),x,y,P,c);x+=WindowsPixels.BoldAdvance(ch)*P;}}
 int Width(string s){int w=0;foreach(char c in s)w+=WindowsPixels.Advance(c)*P;return w;}
 void Label(Graphics g,string s,int x,int y,int w,int h,bool bold,bool center){var b=WindowsPixels.Bounds(s,bold);int tx=center?x+(w-b.Width*P)/2-b.X*P:x;int ty=y+(h-b.Height*P)/2-b.Y*P;if(bold)Bold(g,s,tx,ty,Black);else Text(g,s,tx,ty,P,Black);}
 void Line(Graphics g,int x,int y,int xx,int yy,Color c){if(y==yy)for(int n=x;n<xx;n+=P)Cell(g,n,y,P,c);else for(int n=y;n<yy;n+=P)Cell(g,x,n,P,c);}
 void Edge(Graphics g,int x,int y,int w,int h,Color top,Color bottom){Line(g,x,y,x+w,y,top);Line(g,x,y,x,y+h,top);Line(g,x,y+h-P,x+w,y+h-P,bottom);Line(g,x+w-P,y,x+w-P,y+h,bottom);}
 void Bevel(Graphics g,int x,int y,int w,int h,bool inset,Color face){Fill(g,face,x,y,w,h);Edge(g,x,y,w,h,inset?Gray:Color.FromArgb(210,210,210),inset?Color.FromArgb(210,210,210):Black);Edge(g,x+P,y+P,w-2*P,h-2*P,inset?Black:Silver,inset?Silver:Gray);}
 void Frame(Graphics g,int x,int y,int w,int h){Edge(g,x,y,w,h,Gray,White);Edge(g,x+P,y+P,w-2*P,h-2*P,White,Gray);}
 void Button(Graphics g,int x,int y,int w,int h,string s){Bevel(g,x,y,w,h,false,Silver);Label(g,s,x+9,y+6,w-18,h-12,false,true);}
 void Group(Graphics g,int x,int y,int w,int h,string s){Frame(g,x,y+15,w,h-15);Fill(g,White,x+21,y,Width(s)+18,36);Text(g,s,x+30,y,P,Black);}
 void Arrow(Graphics g,int x,int y,bool down){Sprite(g,down?new[]{"1111111","0111110","0011100","0001000"}:new[]{"0001000","0011100","0111110","1111111"},x,y,P,Black);}
 void Icon(Graphics g,int x,int y,string title){
 if(title.Contains("Notepad")||title.Contains("WordPad")){Bevel(g,x,y,36,42,false,White);for(int i=0;i<4;i++)Line(g,x+9,y+9+i*6,x+30,y+9+i*6,Black);return;}
 if(title.Contains("Paint")){Sprite(g,new[]{"001110000000","011111000001","110001100010","100000100100","110001101000","011111010000","001110100000","000001000000","000010000000","000100000000","001000000000"},x,y,P,Black);return;}
 if(title.Contains("Mines")){Sprite(g,new[]{"00000100000","00100100100","00011111000","00111111100","11111111111","00111111100","00011111000","00100100100","00000100000"},x,y+3,P,Black);return;}
 Sprite(g,new[]{"11111011111","11111011111","11111011111","11111011111","00000000000","11111011111","11111011111","11111011111","11111011111"},x,y+3,P,Black);
 }
 // Shared Win95 metrics: three-pixel frame, 18-pixel caption, 16x14 caption buttons.
 void Window(Graphics g,int x,int y,int w,int h,string title,int kind){
 Fill(g,Color.FromArgb(50,0,0,0),x+9,y+12,w,h);Bevel(g,x,y,w,h,false,Silver);
 for(int yy=y+9;yy<y+63;yy+=P)for(int xx=x+9;xx<x+w-9;xx+=P)Cell(g,xx,yy,P,Black);
 Fill(g,White,x+15,y+15,42,42);Icon(g,x+18,y+15,title);Bold(g,title,x+66,y+18,White);
 int close=x+w-60;Button(g,close,y+15,48,42,"");Sprite(g,new[]{"1100011","0110110","0011100","0001000","0011100","0110110","1100011"},close+12,y+24,P,Black);
 if(kind==2){Button(g,close-51,y+15,48,42,"?");}
 if(kind==0){Button(g,close-102,y+15,48,42,"");Line(g,close-90,y+45,close-66,y+45,Black);Button(g,close-51,y+15,48,42,"");Edge(g,close-42,y+24,30,24,Black,Black);Line(g,close-42,y+27,close-12,y+27,Black);}
 }
 void Menu(Graphics g,int x,int y,params string[] items){foreach(string s in items){int w=Width(s)+30;Label(g,s,x+15,y,w-30,54,false,false);x+=w;}}
 void Tabs(Graphics g,int x,int y,int w,int h,string a,string b){Frame(g,x,y+54,w,h-54);int aw=Width(a)+48;Bevel(g,x,y,aw,60,false,Silver);Fill(g,White,x+6,y+54,aw-12,9);Label(g,a,x+15,y+3,aw-30,48,false,true);int bw=Width(b)+48;Bevel(g,x+aw,y+6,bw,48,false,Silver);Label(g,b,x+aw+12,y+6,bw-24,45,false,true);}
 void Actions(Graphics g,int x,int y,int w){Button(g,x+w-738,y,225,69,"OK");Button(g,x+w-495,y,225,69,"Cancel");Button(g,x+w-252,y,225,69,"Apply");}
 void Slider(Graphics g,int x,int y,int w,double value){Bevel(g,x,y+21,w,12,true,White);for(int i=0;i<7;i++)Line(g,x+i*(w-3)/6,y+42,x+i*(w-3)/6,y+54,Black);Bevel(g,x+(int)((w-33)*value),y,33,54,false,White);}
 void Track(Graphics g,int x,int y,int h){Bevel(g,x-6,y,12,h,true,White);for(int n=0;n<7;n++)Line(g,x-36,y+n*(h-3)/6,x-18,y+n*(h-3)/6,Black);}
 void Analog(Graphics g,int cx,int cy,int r){var used=new HashSet<string>();for(int n=0;n<180;n++){double a=n*Math.PI*2/180;int x=(int)Math.Round((cx+r*Math.Cos(a))/P)*P,y=(int)Math.Round((cy+r*Math.Sin(a))/P)*P;if(used.Add(x+":"+y))Cell(g,x,y,P,Black);}for(int n=0;n<12;n++){double a=n*Math.PI/6;Sprite(g,new[]{"11","11"},(int)(cx+(r-15)*Math.Cos(a)),(int)(cy+(r-15)*Math.Sin(a)),P,Black);}}
 void Calendar(Graphics g){
 var d=DateTime.Today;Bevel(g,145,285,225,57,true,White);Label(g,d.ToString("MMMM",CultureInfo.InvariantCulture),157,288,165,48,false,false);Button(g,322,291,42,42,"");Arrow(g,331,306,true);
 Bevel(g,388,285,120,57,true,White);Label(g,d.Year.ToString(),397,288,84,48,false,false);Button(g,478,288,24,24,"");Arrow(g,481,294,false);Button(g,478,312,24,24,"");Arrow(g,481,318,true);
 Bevel(g,145,360,363,270,true,White);string[] days={"Su","Mo","Tu","We","Th","Fr","Sa"};for(int i=0;i<7;i++)Label(g,days[i],151+i*50,369,48,33,false,true);Line(g,151,408,499,408,Black);
 int offset=(int)new DateTime(d.Year,d.Month,1).DayOfWeek;for(int n=1;n<=DateTime.DaysInMonth(d.Year,d.Month);n++){int x=151+((n-1+offset)%7)*50,y=414+((n-1+offset)/7)*33;if(n==d.Day)Edge(g,x,y,48,33,Black,Black);Label(g,n.ToString(),x,y,48,33,false,true);}
 }
 void Desktop(Graphics g){
 // Match the Macintosh desktop grid across x=2200, including its pixel phase.
 for(int y=0;y<3040;y+=5)for(int x=0;x<2200;x+=5)if((x/5+y/5)%2==0)Cell(g,x,y,5,Black);
 Window(g,70,65,930,720,"Date/Time Properties",2);Tabs(g,103,155,864,507,"Date & Time","Time Zone");Group(g,124,237,402,411,"Date");Calendar(g);Group(g,550,237,387,411,"Time");Analog(g,742,382,102);Bevel(g,601,534,282,63,true,White);Button(g,850,540,27,24,"");Arrow(g,853,546,false);Button(g,850,567,27,24,"");Arrow(g,853,573,true);Actions(g,70,695,930);
 Window(g,1065,65,1010,720,"Keyboard Properties",2);Tabs(g,1098,155,944,507,"Speed","Language");
 Group(g,1125,237,888,300,"Character repeat");Text(g,"Repeat delay:",1155,282,P,Black);Text(g,"Long",1425,282,P,Black);Text(g,"Short",1885,282,P,Black);Slider(g,1430,327,500,.65);
 Text(g,"Repeat rate:",1155,393,P,Black);Text(g,"Slow",1425,393,P,Black);Text(g,"Fast",1895,393,P,Black);Slider(g,1430,438,500,.7);
 Group(g,1125,552,888,93,"Cursor blink rate");Text(g,"None",1155,594,P,Black);Text(g,"Fast",1785,594,P,Black);Bevel(g,1320,616,450,12,true,White);Bevel(g,1900,579,75,57,true,White);Actions(g,1065,695,1010);
 Window(g,70,855,930,660,"Volume Control",0);Menu(g,94,927,"Options","Help");
 string[] names={"Volume Control","Wave","CD Audio"};for(int i=0;i<3;i++){int x=112+i*280;Label(g,names[i],x,990,255,48,false,true);Text(g,"Balance",x+54,1053,P,Black);Slider(g,x+45,1100,174,.5);Text(g,"L",x+12,1104,P,Black);Text(g,"R",x+228,1104,P,Black);int center=200+i*280;Track(g,center,SoundTrackY,SoundTrackHeight);if(i<2)Bevel(g,center-19,1230-i*30,39,21,false,White);Bevel(g,x+12,1410,39,39,true,White);Label(g,i==0?"Mute all":"Mute",x+66,1407,183,48,false,false);if(i<2)Line(g,x+267,990,x+267,1464,Gray);}
 Mines(g);if(!FluidApp.WithoutPaint)Paint(g);Notepad(g);Taskbar(g);
 }
 void Mines(Graphics g){Window(g,380,1645,528,660,"Minesweeper",0);Menu(g,398,1717,"Game","Help");Bevel(g,440,1780,408,96,true,White);Counter(g,452,1792,"010");Counter(g,719,1792,"000");Bevel(g,608,1792,72,72,false,White);Sprite(g,new[]{"001111100","010000010","100101001","100000001","101000101","100111001","010000010","001111100"},624,1807,4,Black);Frame(g,MineX-9,MineY-9,MineSize+18,MineSize+18);}
 void Counter(Graphics g,int x,int y,string digits){Fill(g,Black,x,y,123,72);int[] bits={63,6,91,79,102,109,125,7,127,111};for(int i=0;i<3;i++){int m=bits[digits[i]-'0'],xx=x+9+i*36;int[,] seg={{3,0,24,6},{27,6,6,24},{27,36,6,24},{3,60,24,6},{0,36,6,24},{0,6,6,24},{3,30,24,6}};for(int j=0;j<7;j++)if((m&(1<<j))!=0)Fill(g,White,xx+seg[j,0],y+3+seg[j,1],seg[j,2],seg[j,3]);}}
 static bool MineAt(int x,int y){return (x==1&&y==1)||(x==5&&y==1)||(x==7&&y==3)||(x==2&&y==5)||(x==6&&y==6)||(x==1&&y==7)||(x==7&&y==7)||(x==0&&y==4)||(x==4&&y==7)||(x==6&&y==0);}
 static int Neighbors(int x,int y){int n=0;for(int yy=y-1;yy<=y+1;yy++)for(int xx=x-1;xx<=x+1;xx++)if(MineAt(xx,yy))n++;return n;}
 void Board(Graphics g,int state){for(int y=0;y<8;y++)for(int x=0;x<8;x++){int px=x*MineCell,py=y*MineCell;bool open=((state&1)!=0&&x>=3&&x<=4&&y>=3&&y<=4)||((state&2)!=0&&x==5&&y==5);if(open){Fill(g,White,px,py,MineCell,MineCell);Edge(g,px,py,MineCell,MineCell,Gray,Gray);int n=Neighbors(x,y);if(n>0)Label(g,n.ToString(),px,py,MineCell,MineCell,true,true);}else Bevel(g,px,py,MineCell,MineCell,false,White);}}
 public static Bitmap MineTexture(int state){using(var a=new Art()){var b=new Bitmap(MineSize*2,MineSize*2,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.ScaleTransform(2,2);a.Board(g,state);}return b;}}
 static readonly string[][] Tools={
 new[]{"0001110","0110001","1000001","1000010","0100100","0011000","0000000"},
 new[]{"1010101","0000000","1000001","0000000","1000001","0000000","1010101"},
 new[]{"0000000","0011110","0111111","1111110","0111100","0011000","0000000"},
 new[]{"0001000","0011100","0111110","1111111","0111110","0000010","0000011"},
 new[]{"0000011","0000111","0001110","0011100","0111000","1110000","1000000"},
 new[]{"0011100","0100010","1000001","1000001","0100010","0011110","0000011"},
 new[]{"0000010","0000111","0001110","0011100","0111000","1110000","1000000"},
 new[]{"0000110","0001100","0011000","0110000","1111000","1111000","0110000"},
 new[]{"1001000","0100100","1010010","0101001","1010100","0100010","1001001"},
 new[]{"0011100","0100010","1000001","1000001","1111111","1000001","1000001"},
 new[]{"0000001","0000010","0000100","0001000","0010000","0100000","1000000"},
 new[]{"0000011","0000100","0001000","0011000","0100000","0100000","1000000"},
 new[]{"1111111","1000001","1000001","1000001","1000001","1000001","1111111"},
 new[]{"0011000","0100100","1000010","1000001","1000001","1000001","1111111"},
 new[]{"0011100","0100010","1000001","1000001","1000001","0100010","0011100"},
 new[]{"0111110","1000001","1000001","1000001","1000001","1000001","0111110"}};
 void Paint(Graphics g){Window(g,1065,1645,1010,660,"untitled - Paint",0);Menu(g,1083,1717,"File","Edit","View","Image","Colors","Help");for(int i=0;i<16;i++){int x=1083+i%2*54,y=1774+i/2*54;Bevel(g,x,y,54,54,i==6,White);Sprite(g,Tools[i],x+12,y+12,4,Black);}Bevel(g,1215,1780,825,360,true,White);Scroll(g,1992,1789,48,342,true);Scroll(g,1224,2092,768,48,false);
 for(int i=0;i<28;i++){int x=1221+i%14*48,y=2170+i/14*48;Bevel(g,x,y,45,45,true,White);for(int yy=0;yy<10;yy++)for(int xx=0;xx<10;xx++)if((xx+yy*i)%(1+i%7)==0)Cell(g,x+7+xx*3,y+7+yy*3,3,Black);}Frame(g,1083,2272,974,24);}
 void Scroll(Graphics g,int x,int y,int w,int h,bool vertical){Fill(g,Color.FromArgb(225,225,225),x,y,w,h);Bevel(g,x,y,48,48,false,White);if(vertical){Arrow(g,x+15,y+15,false);Bevel(g,x,y+h-48,48,48,false,White);Arrow(g,x+15,y+h-33,true);Bevel(g,x,y+51,48,90,false,White);}else{Sprite(g,new[]{"0001","0011","0111","1111","0111","0011","0001"},x+15,y+12,P,Black);Bevel(g,x+w-48,y,48,48,false,White);Sprite(g,new[]{"1000","1100","1110","1111","1110","1100","1000"},x+w-30,y+12,P,Black);Bevel(g,x+51,y,150,48,false,White);}}
 void Notepad(Graphics g){Window(g,620,2430,1242,500,"Untitled - Notepad",0);Menu(g,638,2502,"File","Edit","Search","Help");Bevel(g,629,2556,1224,365,true,White);Scroll(g,1800,2565,48,303,true);Scroll(g,638,2868,1162,48,false);Bevel(g,1800,2868,48,48,false,White);}
 void Taskbar(Graphics g){Bevel(g,70,2320,2005,84,false,White);Bevel(g,82,2329,192,66,false,White);Icon(g,97,2341,"Windows");Label(g,"Start",145,2335,114,54,true,true);string[] names={"Notepad","Minesweeper","Paint"};int[] widths={375,441,345};int x=298;for(int i=0;i<3;i++){Bevel(g,x,2329,widths[i],66,false,White);Icon(g,x+12,2341,names[i]);Label(g,names[i],x+63,2335,widths[i]-75,54,false,false);x+=widths[i]+12;}Bevel(g,1855,2329,204,66,true,White);Label(g,"EN",1870,2335,69,54,false,true);}
 void Dialog(Graphics g){Window(g,1065,855,1010,660,"Windows",1);Sprite(g,new[]{"001111100","011111110","110000011","111000111","111101111","111000111","110000011","011111110","001111100"},1128,1035,9,Black);Wrap(g,"This program has performed an illegal operation and will be shut down.",1254,1035,735);Wrap(g,"If the problem persists, contact the program vendor.",1254,1206,735);Button(g,1563,1410,225,69,"Close");Button(g,1812,1410,225,69,"Details >>");}
 void Wrap(Graphics g,string s,int x,int y,int width){string line="";foreach(string word in s.Split(' ')){string next=line.Length==0?word:line+" "+word;if(Width(next)>width&&line.Length>0){Text(g,line,x,y,P,Black);y+=54;line=word;}else line=next;}Text(g,line,x,y,P,Black);}
 public static void ReferenceDialog(Graphics g,int x,int y,int width,int height,string title,bool help){using(var a=new Art()){a.Window(g,x,y,width,height,title,help?2:1);}}
 public static void ReferencePaint(Graphics g){using(var a=new Art()){var state=g.Save();g.TranslateTransform(95-1065,70-1645);a.Paint(g);g.Restore(state);}}
 public static void ReferenceWindow(Graphics g,int x,int y,int width,int height,string title,string[] menus){using(var a=new Art()){a.Window(g,x,y,width,height,title,0);a.Menu(g,x+24,y+72,menus);a.Line(g,x+9,y+123,x+width-9,y+123,Gray);}}
 public static void ReferenceFrame(Graphics g,string title,string[] menus){using(var a=new Art()){a.Window(g,40,55,1120,690,title,0);a.Menu(g,64,127,menus);a.Line(g,49,178,1151,178,Gray);}}
 public static Bitmap KnobTexture(int width,int height){using(var a=new Art()){var b=new Bitmap(width*2,height*2,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.ScaleTransform(2,2);a.Bevel(g,0,0,width,height,false,White);}return b;}}
 public static Bitmap PaintMenuTexture(){using(var a=new Art()){var b=new Bitmap(930,828,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.ScaleTransform(2,2);a.Bevel(g,0,0,465,414,false,White);string[] names={"New","Open...","Save","Save As...","Page Setup...","Print...","Exit"};for(int i=0;i<names.Length;i++){int y=9+i*54;a.Label(g,names[i],27,y,285,54,false,false);if(i<3)a.Label(g,new[]{"Ctrl+N","Ctrl+O","Ctrl+S"}[i],312,y,138,54,false,false);}a.Line(g,12,228,450,228,Gray);a.Line(g,12,336,450,336,Gray);}return b;}}
 public static string[] Glyph(char c){return WindowsPixels.Glyph(c);}public static int Advance(char c){return WindowsPixels.Advance(c);}
 public static Bitmap Texture(bool dialog){using(var a=new Art()){var b=new Bitmap((dialog?1030:2200)*2,(dialog?684:3040)*2,PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.ScaleTransform(2,2);if(dialog){g.TranslateTransform(-1065,-855);a.Dialog(g);}else a.Desktop(g);}return b;}}
 public void Dispose(){foreach(var b in brushes.Values)b.Dispose();}
}
}
