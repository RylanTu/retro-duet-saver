using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;
namespace Macintosh {
public class Options {
 public int FrameRate=144; public bool Cinematic=true; public int Palette=0,Speed=100; public bool Clock=false,Seconds=false,Chinese=true,Animate=true;
 public static string FileName { get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MacintoshSaver","settings.xml");} }
 public static Options Load(){try{using(var s=File.OpenRead(FileName)){var o=(Options)new XmlSerializer(typeof(Options)).Deserialize(s);o.Palette=Math.Max(0,Math.Min(16,o.Palette));o.Speed=Math.Max(25,Math.Min(200,o.Speed));return o;}}catch{return new Options();}}
 public void Save(){Directory.CreateDirectory(Path.GetDirectoryName(FileName));using(var s=File.Create(FileName))new XmlSerializer(typeof(Options)).Serialize(s,this);}
}
static class App {
 [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
 [STAThread] static void Main(string[] args){
 SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
 string m=args.Length==0?"":args[0].ToLowerInvariant();
 if(m.StartsWith("/c")||m=="--settings"){Application.Run(new Settings());return;}
 if(m=="--render"){using(var art=new Art())using(var b=new Bitmap(1600,1000))using(var g=Graphics.FromImage(b)){art.Paint(g,b.Size,6,Options.Load(),false);b.Save(args.Length>1?args[1]:"preview.png",ImageFormat.Png);}return;}
 if(m.StartsWith("/p")){long h;string raw=m.Contains(":")?m.Substring(m.IndexOf(':')+1):args.Length>1?args[1]:"0";if(long.TryParse(raw,out h)&&h!=0)Application.Run(new Canvas(new IntPtr(h)));return;}
 if(m.StartsWith("/s")){using(var ctx=new SaverContext())Application.Run(ctx);return;}
 Application.Run(new Canvas());
 }
}
sealed class SaverContext:ApplicationContext {
 List<Canvas> forms=new List<Canvas>();bool closing;
 public SaverContext(){foreach(var s in Screen.AllScreens){var f=new Canvas(s.Bounds);forms.Add(f);f.FormClosed+=Closed;f.Show();}}
 void Closed(object s,FormClosedEventArgs e){if(closing)return;closing=true;foreach(var f in forms)if(!f.IsDisposed)f.Close();ExitThread();}
}
sealed class Canvas:Form {
 Art art=new Art();Timer timer=new Timer();Stopwatch watch=Stopwatch.StartNew();Options opts=Options.Load();bool saver,embedded,full,hidden;Point mouse;Rectangle old;FormWindowState oldState;
 [DllImport("user32.dll")]static extern IntPtr SetParent(IntPtr a,IntPtr b);
 [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int n,int v);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out RECT r);
 [StructLayout(LayoutKind.Sequential)]struct RECT{public int left,top,right,bottom;}
 public Canvas(){Text="Macintosh · 复古动态屏保";ClientSize=new Size(1120,700);MinimumSize=new Size(640,400);StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;KeyPreview=true;AutoScaleMode=AutoScaleMode.None;BackColor=Color.Black;timer.Interval=33;timer.Tick+=delegate{if(Visible&&WindowState!=FormWindowState.Minimized)Invalidate();};timer.Start();KeyDown+=KeysDown;MouseMove+=delegate{if(saver&&watch.Elapsed.TotalSeconds>1.5&&(Math.Abs(Cursor.Position.X-mouse.X)>8||Math.Abs(Cursor.Position.Y-mouse.Y)>8))Close();};MouseDown+=delegate{if(saver&&watch.Elapsed.TotalSeconds>1.5)Close();};}
 public Canvas(Rectangle r):this(){saver=full=true;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=r;TopMost=true;ShowInTaskbar=false;mouse=Cursor.Position;}
 public Canvas(IntPtr p):this(){embedded=true;MinimumSize=Size.Empty;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;SetParent(Handle,p);SetWindowLong(Handle,-16,0x50000000);RECT r;if(GetClientRect(p,out r))Bounds=new Rectangle(0,0,r.right-r.left,r.bottom-r.top);timer.Interval=100;}
 protected override void OnShown(EventArgs e){base.OnShown(e);if(saver){Cursor.Hide();hidden=true;}}
 protected override void OnPaint(PaintEventArgs e){art.Paint(e.Graphics,ClientSize,watch.Elapsed.TotalSeconds,opts,!saver&&!embedded&&!full);}
 void KeysDown(object s,KeyEventArgs e){if(embedded)return;if(saver||e.KeyCode==Keys.Escape){Close();return;}if(e.KeyCode==Keys.F11||e.KeyCode==Keys.Enter){if(!full){old=Bounds;oldState=WindowState;WindowState=FormWindowState.Normal;FormBorderStyle=FormBorderStyle.None;Bounds=Screen.FromControl(this).Bounds;full=true;Cursor.Hide();hidden=true;}else{full=false;FormBorderStyle=FormBorderStyle.Sizable;Bounds=old;WindowState=oldState;if(hidden){Cursor.Show();hidden=false;}}}if(e.KeyCode==Keys.Space){opts.Palette=(opts.Palette+1)%7;art.Dirty();}if(e.KeyCode==Keys.C)opts.Clock=!opts.Clock;if(e.KeyCode==Keys.P)opts.Animate=!opts.Animate;if(e.KeyCode==Keys.S){using(var f=new Settings())f.ShowDialog(this);opts=Options.Load();art.Dirty();}}
 protected override void Dispose(bool disposing){if(disposing){timer.Dispose();art.Dispose();if(hidden){Cursor.Show();hidden=false;}}base.Dispose(disposing);}
}
sealed class Settings:Form {
 Options o=Options.Load();ComboBox frameRate; CheckBox cinematic;ComboBox palette;TrackBar speed;CheckBox animate;
 public Settings(){Text="Macintosh · 屏保设置";ClientSize=new Size(470,535);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(245,244,239);
 Controls.Add(new Label{Text="Macintosh",Font=new Font("Segoe UI",26,FontStyle.Bold),Bounds=new Rectangle(26,18,410,48)});Controls.Add(new Label{Text="复古像素界面 · 实时时钟 · 缓慢镜头漫游",Bounds=new Rectangle(28,70,420,28),ForeColor=Color.DimGray});Controls.Add(new Label{Text="配色",Bounds=new Rectangle(28,116,90,28)});
 palette=new ComboBox{Bounds=new Rectangle(128,112,300,32),DropDownStyle=ComboBoxStyle.DropDownList};palette.Items.AddRange(new object[]{"原版连续色谱 / 约 3 分钟一圈","琥珀黄","酒红","紫罗兰","青绿","橄榄绿","经典灰","橙","朱红","洋红","紫红","靛蓝","蓝","湖蓝","草绿","柠檬黄","金黄"});palette.SelectedIndex=o.Palette;Controls.Add(palette);
 Controls.Add(new Label{Text="动画速度",Bounds=new Rectangle(28,168,96,28)});speed=new TrackBar{Bounds=new Rectangle(122,158,310,45),Minimum=25,Maximum=200,TickFrequency=25,Value=o.Speed};Controls.Add(speed);
 animate=Check("连续缓慢漫游",28,214,o.Animate);Controls.Add(new Label{Text="日期时间仅保留在背景像素面板中",Bounds=new Rectangle(28,260,405,28),ForeColor=Color.DimGray});
 Controls.Add(new Label{Text="F11 全屏 · 空格换色 · S 设置 · F2 帧率\nA 自动鼠标 · P 暂停 · Esc 退出",Bounds=new Rectangle(28,394,420,56),ForeColor=Color.DimGray});Controls.Add(new Label{Text="目标帧率",Bounds=new Rectangle(28,309,90,28)}); frameRate=new ComboBox{Bounds=new Rectangle(128,305,300,32),DropDownStyle=ComboBoxStyle.DropDownList};frameRate.Items.AddRange(new object[]{"60 FPS","120 FPS","144 FPS"});frameRate.SelectedIndex=o.FrameRate>=144?2:o.FrameRate>=120?1:0;Controls.Add(frameRate);cinematic=Check("自动鼠标与拖拽演出",28,350,o.Cinematic);var save=new Button{Text="保存设置",Bounds=new Rectangle(296,466,132,38)};save.Click+=delegate{o.FrameRate=new[]{60,120,144}[frameRate.SelectedIndex];o.Cinematic=cinematic.Checked;o.Palette=palette.SelectedIndex;o.Speed=speed.Value;o.Clock=false;o.Animate=animate.Checked;try{o.Save();DialogResult=DialogResult.OK;Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,"无法保存设置");}};Controls.Add(save);AcceptButton=save;
 }
 CheckBox Check(string t,int x,int y,bool v){var c=new CheckBox{Text=t,Bounds=new Rectangle(x,y,205,30),Checked=v};Controls.Add(c);return c;}
}
sealed class Art:IDisposable {
 public const int SoundTrackX=700,SoundTrackY=1015,SoundTrackHeight=360,SoundTravel=318;
 static readonly Color[] bg={Color.FromArgb(171,115,30),Color.FromArgb(160,39,76),Color.FromArgb(113,66,157),Color.FromArgb(42,130,139),Color.FromArgb(144,149,49),Color.FromArgb(173,175,167)};
 static readonly Color[] fg={Color.FromArgb(255,226,149),Color.FromArgb(255,201,216),Color.FromArgb(235,207,255),Color.FromArgb(191,243,236),Color.FromArgb(249,251,157),Color.FromArgb(248,247,235)};
 bool textureOnly; double motion=6,lastElapsed=-1; Bitmap cached;string key="";DateTime now;Color ink,paper,light,shade;SolidBrush inkBrush,shadowBrush,rimBrush;
 static Color Mix(Color a,Color b,double t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
 public void Dirty(){key="";}
 public void Paint(Graphics g,Size size,double elapsed,Options o,bool hint){if(size.Width<1||size.Height<1)return;if(lastElapsed<0)motion=o.Animate?elapsed*o.Speed/100.0:6;else if(o.Animate)motion+=(elapsed-lastElapsed)*o.Speed/100.0;lastElapsed=elapsed;double t=motion;int idx=o.Palette==0?(int)(t/14)%5:o.Palette-1;double blend=o.Palette==0?Math.Max(0,(t%14-11.5)/2.5):0;blend=blend*blend*(3-2*blend);blend=Math.Round(blend*24)/24;paper=Mix(bg[idx],bg[(idx+1)%5],blend);light=Mix(fg[idx],fg[(idx+1)%5],blend);ink=Mix(paper,Color.Black,.73);shade=Mix(paper,Color.Black,.4);now=DateTime.Now;string k=paper.ToArgb()+now.ToString("yyyyMMddHHmmss");if(cached==null||key!=k){if(cached!=null)cached.Dispose();cached=new Bitmap(2200,1600,PixelFormat.Format32bppPArgb);using(inkBrush=new SolidBrush(ink))using(shadowBrush=new SolidBrush(Color.FromArgb(50,shade)))using(var a=Graphics.FromImage(cached))Desktop(a);key=k;}
 g.Clear(paper);g.SmoothingMode=SmoothingMode.None;g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.HighQuality;PointF[] anchors={new PointF(620,445),new PointF(1600,485),new PointF(1580,1110),new PointF(650,1120),new PointF(620,445)};double seg=t/24;int ai=(int)seg%4;double u=seg-Math.Floor(seg);u=u*u*(3-2*u);float cx=(float)(anchors[ai].X*(1-u)+anchors[ai+1].X*u),cy=(float)(anchors[ai].Y*(1-u)+anchors[ai+1].Y*u);float scale=(float)(Math.Max(size.Width/1120.0,size.Height/720.0)*(1.08+.07*Math.Sin(t/23)));var state=g.Save();g.TranslateTransform(size.Width/2f,size.Height/2f);g.RotateTransform((float)(17+6*Math.Sin(t/31)));g.ScaleTransform(scale,scale);g.TranslateTransform(-cx,-cy);g.DrawImage(cached,0,0,2200,1600);g.Restore(state);using(var v=new LinearGradientBrush(new Rectangle(0,0,size.Width,size.Height),Color.Transparent,Color.FromArgb(26,0,0,0),90))g.FillRectangle(v,0,0,size.Width,size.Height);if(o.Clock)Clock(g,size,o);if(hint){using(var f=new Font("Microsoft YaHei UI",9))using(var b=new SolidBrush(Color.FromArgb(200,light)))g.DrawString("F11 全屏    ·    空格 换色    ·    S 设置    ·    Esc 退出",f,b,new RectangleF(0,size.Height-38,size.Width,24),new StringFormat{Alignment=StringAlignment.Center});}
 }
 void Clock(Graphics g,Size s,Options o){g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;float unit=Math.Max(.24f,Math.Min(s.Width/1600f,s.Height/1000f));var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};string date=o.Chinese?now.ToString("M月d日 dddd",CultureInfo.GetCultureInfo("zh-CN")):now.ToString("dddd, d MMMM",CultureInfo.GetCultureInfo("en-US"));float y=s.Height*.092f;using(var b=new SolidBrush(light))using(var df=new Font("Segoe UI",25*unit,FontStyle.Regular,GraphicsUnit.Pixel))using(var tf=new Font("Segoe UI Semibold",112*unit,FontStyle.Regular,GraphicsUnit.Pixel)){g.DrawString(date,df,b,new RectangleF(0,y,s.Width,42*unit),sf);g.DrawString(now.ToString(o.Seconds?"HH:mm:ss":"HH:mm"),tf,b,new RectangleF(0,y+33*unit,s.Width,137*unit),sf);}}
 void Desktop(Graphics g){g.Clear(paper);g.SmoothingMode=SmoothingMode.None;Dither(g,0,0,2200,3040,8);Window(g,70,65,930,720,"Control Panel");Text(g,"Time",110,185,5);Analog(g,805,185,45);if(!textureOnly)Text(g,now.ToString("H:mm:ss"),205,370,10);Line(g,90,500,980,500);Radio(g,130,540,true);Text(g,"12hr.",195,540,5);Radio(g,550,540,false);Text(g,"24hr.",615,540,5);Line(g,90,625,980,625);Text(g,"Date",115,656,5);Sprite(g,new[]{"00111111111100","01100000000110","11011111111011","10000000000001","11111111111111","10000010000001","10011010011001","10001010101001","10001010010001","10011110111001","10000010000001","11111111111111"},360,650,5);if(!textureOnly)Text(g,now.ToString("M/d/yy"),550,656,5);
 Window(g,1065,65,1010,720,"Mouse");Text(g,"Rate of Insertion",1110,160,5);Text(g,"Point Blinking",1165,220,5);for(int i=0;i<3;i++){Radio(g,1230+i*245,355,i==1);Text(g,new[]{"Slow","Medium","Fast"}[i],1150+i*265,440,5);}Line(g,1085,530,2055,530);Text(g,"Tracking",1110,568,5);Text(g,"Speed",1110,625,5);Slider(g,1790,565,155,4);
 Window(g,70,855,930,660,"Sound");
 Sprite(g,new[]{"000001000100100","000011000010010","111111000010010","111111000010010","111111000010010","000011000010010","000001000100100"},180,1025,7);Text(g,"Speaker",180,1120,5);Text(g,"Volume",180,1180,5);
 Slider(g,SoundTrackX,SoundTrackY,SoundTrackHeight,7);for(int i=0;i<8;i++){int yy=SoundTrackY+8+i*47;Text(g,(7-i).ToString(),SoundTrackX-74,yy-12,5);Line(g,SoundTrackX-38,yy+4,SoundTrackX-14,yy+4);}
 Line(g,95,1400,975,1400);Box(g,145,1425,770,55,false);Text(g,"Simple Beep",185,1430,5);

 ExtraPanels(g);SignatureDocument(g);
 if(!textureOnly){Window(g,1065,855,1010,660,"System Message");Sprite(g,new[]{"00000000000000100","00000000000110001","00000000111000100","00000001100000000","00000001100000000","00000111111000000","00011111111110000","00111111111111000","01111111111111100","01111111111111100","11111111111111110","11111111111111110","11111111111111110","11111111111111110","01111111111111100","01111111111111100","00111111111111000","00011111111110000","00000111111000000"},1170,980,6);Text(g,"Sorry, a system error",1115,1190,5);Text(g,"occurred.",1115,1255,5);Text(g,"Illegal instruction",1115,1320,5);Box(g,1720,1400,265,72,false);Text(g,"Restart",1755,1416,5);}
 if(!textureOnly)Sprite(g,new[]{"100000000000","110000000000","111000000000","111100000000","111110000000","111111000000","111111100000","111111110000","111111111000","111111111100","111111000000","110110000000","100011000000","000011000000","000001100000"},1018,730,4);
 }
 void SignatureDocument(Graphics g){
 Window(g,300,2430,1575,500,"Untitled - SimpleText");
 Text(g,"File  Edit  Search  Font  Style",345,2520,4);
 Line(g,315,2570,1855,2570);
 // Signature content is drawn by the live typewriter layer in FluidView.
 // Pixel scroll bar, arrows and resize grip belong to the document frame.
 Box(g,1835,2585,20,305,false);
 Sprite(g,new[]{"00100","01110","11111"},1835,2590,4);
 Sprite(g,new[]{"11111","01110","00100"},1835,2870,4);
 Box(g,1835,2650,20,100,true);
 for(int i=0;i<4;i++)Line(g,1815+i*10,2905,1850,2905-i*10);
 }
 void ExtraPanels(Graphics g){
 Window(g,70,1645,930,660,"Control Panel");
 Line(g,465,1720,465,2285);
 // Keyboard / Map are the icon areas visible in the supplied video.
 Box(g,150,1790,250,105,false);
 for(int row=0;row<3;row++)for(int col=0;col<9;col++)Box(g,165+col*25,1805+row*25,15,15,false);
 Box(g,205,1880,140,10,false);Text(g,"Keyboard",125,1940,5);
 for(int yy=-14;yy<=14;yy++)for(int xx=-14;xx<=14;xx++){
 double d=Math.Sqrt(xx*xx+yy*yy);double ex=7*Math.Sqrt(Math.Max(0,1-yy*yy/196.0));
 if(d<14.7&&(Math.Abs(d-14)<.7||yy==0||yy==7||yy==-7||xx==0||Math.Abs(Math.Abs(xx)-ex)<.55))Cell(g,270+xx*5,2120+yy*5,5);
 }
 Text(g,"Map",220,2220,5);Text(g,"Key Repeat",520,1760,5);
 Text(g,"Slow",530,1830,5);Text(g,"Fast",800,1830,5);
 Box(g,535,1910,350,35,false);for(int x=565;x<875;x+=40)Line(g,x,1960,x,1980);
 Text(g,"Delay",520,2040,5);Text(g,"Long",530,2110,5);Text(g,"Short",760,2110,5);
 Box(g,535,2190,350,35,false);
 if(FluidApp.WithoutPattern)return;
 Window(g,1065,1645,1010,660,"Desktop Pattern");
 Text(g,"Edit Pattern",1120,1750,5);Text(g,"Preview",1625,1750,5);
 Box(g,1155,1800,330,330,false);
 for(int i=1;i<8;i++){Line(g,1155+i*40,1800,1155+i*40,2125);Line(g,1155,1800+i*40,1480,1800+i*40);}
 Box(g,1615,1800,350,330,false);
 Box(g,1160,2190,120,65,false);Text(g,"<",1200,2200,5);
 Box(g,1320,2190,120,65,false);Text(g,">",1360,2200,5);
 Text(g,"Desktop",1580,2200,5);
 }
 void Cell(Graphics g,int x,int y,int p){if(textureOnly){g.FillRectangle(shadowBrush,x+1.0f,y+1.5f,p-.3f,p-.3f);g.FillRectangle(inkBrush,x+.225f,y+.225f,p-.55f,p-.55f);g.FillRectangle(rimBrush,x+.225f,y+.225f,p-.55f,.35f);g.FillRectangle(rimBrush,x+.225f,y+.575f,.25f,p-.9f);return;}g.FillRectangle(shadowBrush,x+2,y+3,p-.5f,p-.5f);g.FillRectangle(inkBrush,x,y,p-.6f,p-.6f);}
 void Sprite(Graphics g,string[] rows,int x,int y,int p){for(int j=0;j<rows.Length;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]=='1')Cell(g,x+i*p,y+j*p,p);}
 void Text(Graphics g,string text,int x,int y,int p){foreach(char c in text){var rows=Glyph(c);Sprite(g,rows,x,y,p);x+=Advance(c)*p;}}
 void Line(Graphics g,int x,int y,int xx,int yy){int p=textureOnly?5:4;if(y==yy)for(int i=x;i<xx;i+=p)Cell(g,i,y,p);else for(int j=y;j<yy;j+=p)Cell(g,x,j,p);}
 void Box(Graphics g,int x,int y,int w,int h,bool fill){if(fill)using(var b=new SolidBrush(paper))g.FillRectangle(b,x,y,w,h);Line(g,x,y,x+w,y);Line(g,x,y+h,x+w,y+h);Line(g,x,y,x,y+h);Line(g,x+w,y,x+w,y+h);}
 void Dither(Graphics g,int x,int y,int w,int h,int p){if(textureOnly){for(int yy=y;yy<y+h;yy+=5)for(int xx=x;xx<x+w;xx+=5)if(((xx-x)/5+(yy-y)/5)%2==0)Cell(g,xx,yy,5);return;}for(int j=0;j<h;j+=p)for(int i=0;i<w;i+=p)if((i/p+j/p)%2==0)g.FillRectangle(inkBrush,x+i,y+j,p/2,p/2);}
 void Window(Graphics g,int x,int y,int w,int h,string title){if(!textureOnly)using(var b=new SolidBrush(shade))g.FillRectangle(b,x+12,y+14,w,h);Box(g,x,y,w,h,true);Box(g,x+10,y+10,w-20,h-20,false);Line(g,x+10,y+70,x+w-10,y+70);for(int k=0;k<5;k++)Line(g,x+25,y+20+k*10,x+w-25,y+20+k*10);int tp=textureOnly?5:4;int tw=0;foreach(char letter in title)tw+=Advance(letter)*tp;tw-=tp;using(var b=new SolidBrush(paper))g.FillRectangle(b,x+(w-tw)/2-16,y+13,tw+32,44);Text(g,title,x+(w-tw)/2,y+18,tp);Box(g,x+25,y+20,30,30,true);}
 void Radio(Graphics g,int x,int y,bool v){Sprite(g,new[]{"00111100","01000010","10000001","10000001","10000001","10000001","01000010","00111100"},x,y,6);if(v&&!textureOnly)Sprite(g,new[]{"1111","1111","1111","1111"},x+12,y+12,6);}
 void Slider(Graphics g,int x,int y,int h,int p){Box(g,x,y,12*p,h,false);Dither(g,x+2*p,y+2*p,8*p,h-4*p,p);if(!textureOnly){Box(g,x-p,y+h/3,14*p,6*p,true);Line(g,x,y+h/3+2*p,x+12*p,y+h/3+2*p);}}
 void Analog(Graphics g,int x,int y,int r){var used=new HashSet<string>();for(int i=0;i<100;i++){double a=i*Math.PI*2/100;int px=(int)Math.Round((r+Math.Cos(a)*r)/6)*6,py=(int)Math.Round((r+Math.Sin(a)*r)/6)*6;if(used.Add(px+":"+py))Cell(g,x+px,y+py,6);}if(!textureOnly){Ray(g,x+r,y+r,(now.Minute+now.Second/60.0)*Math.PI/30-Math.PI/2,r*.72);Ray(g,x+r,y+r,(now.Hour%12+now.Minute/60.0)*Math.PI/6-Math.PI/2,r*.48);}}
 void Ray(Graphics g,int x,int y,double a,double len){for(int i=0;i<len;i+=5)Cell(g,x+(int)Math.Round(Math.Cos(a)*i/6)*6,y+(int)Math.Round(Math.Sin(a)*i/6)*6,6);}
 static readonly Dictionary<char,string[]> Glyphs=BuildGlyphs();
 static Dictionary<char,string[]> BuildGlyphs(){var d=new Dictionary<char,string[]>();string data="A:01110/10001/10001/11111/10001/10001/10001;B:11110/10001/10001/11110/10001/10001/11110;C:01111/10000/10000/10000/10000/10000/01111;D:11110/10001/10001/10001/10001/10001/11110;E:11111/10000/10000/11110/10000/10000/11111;F:11111/10000/10000/11110/10000/10000/10000;G:01111/10000/10000/10111/10001/10001/01111;H:10001/10001/10001/11111/10001/10001/10001;I:111/010/010/010/010/010/111;J:00111/00010/00010/00010/10010/10010/01100;K:10001/10010/10100/11000/10100/10010/10001;L:10000/10000/10000/10000/10000/10000/11111;M:10001/11011/10101/10101/10001/10001/10001;N:10001/11001/10101/10011/10001/10001/10001;O:01110/10001/10001/10001/10001/10001/01110;P:11110/10001/10001/11110/10000/10000/10000;Q:01110/10001/10001/10001/10101/10010/01101;R:11110/10001/10001/11110/10100/10010/10001;S:01111/10000/10000/01110/00001/00001/11110;T:11111/00100/00100/00100/00100/00100/00100;U:10001/10001/10001/10001/10001/10001/01110;V:10001/10001/10001/10001/10001/01010/00100;W:10001/10001/10001/10101/10101/10101/01010;X:10001/10001/01010/00100/01010/10001/10001;Y:10001/10001/01010/00100/00100/00100/00100;Z:11111/00001/00010/00100/01000/10000/11111;0:01110/10001/10011/10101/11001/10001/01110;1:00100/01100/00100/00100/00100/00100/01110;2:01110/10001/00001/00010/00100/01000/11111;3:11110/00001/00001/01110/00001/00001/11110;4:00010/00110/01010/10010/11111/00010/00010;5:11111/10000/10000/11110/00001/00001/11110;6:01110/10000/10000/11110/10001/10001/01110;7:11111/00001/00010/00100/01000/01000/01000;8:01110/10001/10001/01110/10001/10001/01110;9:01110/10001/10001/01111/00001/00001/01110;.:0/0/0/0/0/1/1;/:00001/00001/00010/00100/01000/10000/10000; :000/000/000/000/000/000/000";foreach(var item in data.Split(';'))d[item[0]]=item.Substring(2).Split('/');d[':']=new[]{"0","1","1","0","1","1","0"};
 string lower="a:00000/00000/01110/00001/01111/10001/01111;b:10000/10000/11110/10001/10001/10001/11110;c:00000/00000/01111/10000/10000/10000/01111;d:00001/00001/01111/10001/10001/10001/01111;e:00000/00000/01110/10001/11111/10000/01111;f:00110/01001/01000/11100/01000/01000/01000;g:00000/01111/10001/10001/01111/00001/01110;h:10000/10000/11110/10001/10001/10001/10001;i:1/0/1/1/1/1/1;j:001/000/001/001/001/101/010;k:10000/10000/10010/10100/11000/10100/10010;l:10/10/10/10/10/10/01;m:00000/00000/11010/10101/10101/10101/10101;n:00000/00000/11110/10001/10001/10001/10001;o:00000/00000/01110/10001/10001/10001/01110;p:00000/11110/10001/10001/11110/10000/10000;q:00000/01111/10001/10001/01111/00001/00001;r:0000/0000/1011/1100/1000/1000/1000;s:00000/00000/01111/10000/01110/00001/11110;t:0100/0100/1110/0100/0100/0101/0010;u:00000/00000/10001/10001/10001/10001/01111;v:00000/00000/10001/10001/10001/01010/00100;w:00000/00000/10001/10001/10101/10101/01010;x:00000/00000/10001/01010/00100/01010/10001;y:00000/10001/10001/01111/00001/10001/01110;z:00000/00000/11111/00010/00100/01000/11111";foreach(var item in lower.Split(';'))d[item[0]]=item.Substring(2).Split('/');return d;}

 public static string[] Glyph(char c){return ReferenceFonts.Glyph(c);}public static int Advance(char c){return ReferenceFonts.Advance(c);}
 public static Bitmap Texture(bool dialog){return new Art().MakeTexture(dialog);} Bitmap MakeTexture(bool dialog){var a=this;a.textureOnly=true;a.paper=Color.White;a.ink=Color.Black;a.shade=Color.FromArgb(140,140,140);a.now=DateTime.Now;var bitmap=new Bitmap((dialog?1030:2200)*2,(dialog?684:3040)*2,PixelFormat.Format32bppPArgb);using(a.rimBrush=new SolidBrush(Color.FromArgb(65,255,255,255)))using(a.inkBrush=new SolidBrush(Color.Black))using(a.shadowBrush=new SolidBrush(Color.FromArgb(45,0,0,0)))using(var g=Graphics.FromImage(bitmap)){g.ScaleTransform(2,2);g.PixelOffsetMode=PixelOffsetMode.HighQuality;if(dialog){g.Clear(Color.White);g.TranslateTransform(-1065,-855);Window(g,1065,855,1010,660,"System Message");Sprite(g,new[]{"00000000000000100","00000000000110001","00000000111000100","00000001100000000","00000001100000000","00000111111000000","00011111111110000","00111111111111000","01111111111111100","01111111111111100","11111111111111110","11111111111111110","11111111111111110","11111111111111110","01111111111111100","01111111111111100","00111111111111000","00011111111110000","00000111111000000"},1170,980,6);Text(g,"Sorry, a system error",1115,1190,5);Text(g,"occurred.",1115,1255,5);Text(g,"Illegal instruction",1115,1320,5);Box(g,1720,1400,265,72,false);Text(g,"Restart",1755,1416,5);}else a.Desktop(g);}return bitmap;}
 public void Dispose(){if(cached!=null){cached.Dispose();cached=null;}}
}
}
