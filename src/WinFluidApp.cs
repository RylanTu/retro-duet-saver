using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Path = System.Windows.Shapes.Path;
using WCanvas = System.Windows.Controls.Canvas;
namespace RetroWindows {
static class FluidApp {
 public static bool WithoutPaint;public static readonly Stopwatch StartupWatch=Stopwatch.StartNew();
 [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 [STAThread]static void Main(string[] args){SetProcessDPIAware();WithoutPaint=Array.Exists(args,delegate(string a){return a=="--no-paint";});string mode=args.Length==0?"":args[0].ToLowerInvariant();
 if(mode.StartsWith("/c")||mode=="--settings"){Forms.Application.EnableVisualStyles();using(var f=new Settings())f.ShowDialog();return;}
 if(mode=="--demo"||mode=="--signature-demo"){bool signatureDemo=mode=="--signature-demo";Directory.CreateDirectory(args[1]);var view=new FluidView(Options.Load(),false,false);view.Hint=false;view.Width=800;view.Height=500;view.Measure(new Size(800,500));view.Arrange(new Rect(0,0,800,500));for(int i=0;i<(signatureDemo?150:100);i++){view.SetTime(signatureDemo?240+i/15.0:51+i/20.0);view.UpdateLayout();var image=new RenderTargetBitmap(800,500,96,96,PixelFormats.Pbgra32);image.Render(view);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(System.IO.Path.Combine(args[1],i.ToString("D3")+".png")))png.Save(stream);}return;}
 if(mode=="--font-info"){File.WriteAllText(args[1],WindowsPixels.FontInfo);return;}
 if(mode=="--atlas"){using(var b=Art.Texture(false))b.Save(args[1]);return;}
 if(mode=="--render"){double t=6;if(args.Length>2)double.TryParse(args[2],NumberStyles.Float,CultureInfo.InvariantCulture,out t);var view=new FluidView(Options.Load(),false,false);view.Hint=false;view.Width=1600;view.Height=1000;view.Measure(new Size(1600,1000));view.Arrange(new Rect(0,0,1600,1000));view.UpdateLayout();view.SetTime(t);view.UpdateLayout();var b=new RenderTargetBitmap(1600,1000,96,96,PixelFormats.Pbgra32);b.Render(view);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(b));using(var stream=File.Create(args[1]))encoder.Save(stream);return;}
 var app=new Application();app.ShutdownMode=ShutdownMode.OnLastWindowClose;
 if(mode.StartsWith("/p")){long h;string raw=mode.Contains(":")?mode.Substring(mode.IndexOf(':')+1):args.Length>1?args[1]:"0";if(long.TryParse(raw,out h)&&h!=0)app.Run(new SaverWindow(new IntPtr(h)));return;}
 if(mode.StartsWith("/s")){var windows=new List<SaverWindow>();bool closing=false;foreach(var s in Forms.Screen.AllScreens){var w=new SaverWindow(s.Bounds);windows.Add(w);w.Closed+=delegate{if(closing)return;closing=true;foreach(var f in windows)f.Close();};}foreach(var w in windows)w.Show();app.Run();return;}
 var preview=new SaverWindow();if((mode=="--audit"||mode=="--audit-sweep")&&args.Length>1)preview.Audit(args[1]);if(mode=="--audit-sweep")preview.AuditSweep();if(mode=="--signature")preview.SignaturePreview();if(mode=="--profile"&&args.Length>1)preview.Profile(args[1]);app.Run(preview);
 }
}
sealed class SaverWindow:Window {
 public void SignaturePreview(){view.SetTime(FluidApp.WithoutPaint?180:240);}
 public void AuditSweep(){view.AuditRate=10;}
 public void Audit(string path){view.AuditPath=path;view.Options.Speed=100;view.Options.Animate=true;view.Options.Cinematic=true;view.Options.Palette=0;view.Options.FrameRate=144;}
 public void Profile(string path){view.ProfilePath=path;view.ShowStats=true;}
 readonly Stopwatch watch=Stopwatch.StartNew();Options opts=Options.Load();FluidView view;bool saver,embedded,full;Point initial;Rect oldBounds;WindowState oldState;
 [DllImport("user32.dll")]static extern IntPtr SetParent(IntPtr a,IntPtr b);
 [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int n,int v);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out RECT r);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
 [StructLayout(LayoutKind.Sequential)]struct RECT{public int left,top,right,bottom;}
 public SaverWindow():this(true){}
 SaverWindow(bool buildView){Title="Retro Windows · Windows 95/98 v2";Width=1120;Height=730;MinWidth=640;MinHeight=400;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Brushes.Black;if(buildView)Build();KeyDown+=KeysDown;MouseMove+=delegate{if(saver&&watch.Elapsed.TotalSeconds>1.5){var p=Forms.Cursor.Position;if(Math.Abs(p.X-initial.X)>8||Math.Abs(p.Y-initial.Y)>8)Close();}};MouseDown+=delegate{if(saver&&watch.Elapsed.TotalSeconds>1.5)Close();};}
 void Build(){view=new FluidView(opts,saver,embedded);Content=view;}
 public SaverWindow(Drawing.Rectangle bounds):this(false){saver=full=true;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.Manual;Topmost=true;ShowInTaskbar=false;initial=new Point(Forms.Cursor.Position.X,Forms.Cursor.Position.Y);Build();SourceInitialized+=delegate{SetWindowPos(new WindowInteropHelper(this).Handle,new IntPtr(-1),bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0040);};}
 public SaverWindow(IntPtr parent):this(false){embedded=true;MinWidth=MinHeight=0;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.Manual;Build();SourceInitialized+=delegate{IntPtr h=new WindowInteropHelper(this).Handle;SetParent(h,parent);SetWindowLong(h,-16,0x50000000);RECT r;if(GetClientRect(parent,out r))SetWindowPos(h,IntPtr.Zero,0,0,r.right-r.left,r.bottom-r.top,0x0044);};}
 void KeysDown(object sender,KeyEventArgs e){if(embedded)return;if(saver||e.Key==Key.Escape){Close();return;}if(e.Key==Key.F11||e.Key==Key.Return){if(!full){oldBounds=new Rect(Left,Top,Width,Height);oldState=WindowState;WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;var bounds=Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).Bounds;SetWindowPos(new WindowInteropHelper(this).Handle,IntPtr.Zero,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0044);full=true;}else{WindowStyle=WindowStyle.SingleBorderWindow;ResizeMode=ResizeMode.CanResize;Left=oldBounds.Left;Top=oldBounds.Top;Width=oldBounds.Width;Height=oldBounds.Height;WindowState=oldState;full=false;}view.Hint=!full;}
 if(e.Key==Key.Space)opts.Palette=(opts.Palette+1)%17;if(e.Key==Key.P)opts.Animate=!opts.Animate;if(e.Key==Key.A)opts.Cinematic=!opts.Cinematic;if(e.Key==Key.F2)view.ShowStats=!view.ShowStats;
 if(e.Key==Key.S){view.Suspend=true;try{using(var f=new Settings()){new WindowInteropHelper(this);f.ShowDialog(new DialogOwner(new WindowInteropHelper(this).Handle));}opts=Options.Load();view.Options=opts;}finally{view.Suspend=false;}}
 }
 sealed class DialogOwner:Forms.IWin32Window {readonly IntPtr h;public DialogOwner(IntPtr h){this.h=h;}public IntPtr Handle{get{return h;}}}
}
sealed class FluidView:Grid {
 public string AuditPath;public double AuditRate=1;
 struct AuditFrame {public double Time,Interval,Update;public int Scene,Tiles;}
 struct AuditMemory {public double Time,Private,Working,Cpu;}
 readonly List<AuditFrame> auditFrames=new List<AuditFrame>(65536);
 readonly List<AuditMemory> auditMemory=new List<AuditMemory>(512);
 readonly Process auditProcess=Process.GetCurrentProcess();
 bool auditStarted;double auditPrevious=-1,auditNextMemory,auditPreviousCpu,auditPreviousMemoryAt,auditStartup;
 bool RecordAudit(double now,double cpu){
 if(AuditPath==null)return false;
 if(!auditStarted){if(now<3)return false;auditStarted=true;motion=0;colorClock=0;auditStartup=FluidApp.StartupWatch.Elapsed.TotalMilliseconds;auditPrevious=now;}
 int visible=0;foreach(var tile in surfaceTiles)if(tile.Visible)visible++;
 auditFrames.Add(new AuditFrame{Time=motion,Interval=(now-auditPrevious)*1000,Update=cpu,Scene=sceneIndex,Tiles=visible});auditPrevious=now;
 if(now>=auditNextMemory){auditNextMemory=now+1;auditProcess.Refresh();double processCpu=auditProcess.TotalProcessorTime.TotalMilliseconds;
 auditMemory.Add(new AuditMemory{Time=motion,Private=auditProcess.PrivateMemorySize64/1048576.0,Working=auditProcess.WorkingSet64/1048576.0,Cpu=auditPreviousMemoryAt>0?(processCpu-auditPreviousCpu)/(now-auditPreviousMemoryAt):0});auditPreviousCpu=processCpu;auditPreviousMemoryAt=now;
 File.WriteAllText(AuditPath+".progress",motion.ToString("F1",CultureInfo.InvariantCulture));}
 if(motion<420)return false;
 using(var writer=new StreamWriter(AuditPath+".frames.csv")){writer.WriteLine("time_s,scene,interval_ms,update_ms,visible_tiles");foreach(var sample in auditFrames)writer.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0:F4},{1},{2:F4},{3:F4},{4}",sample.Time,sample.Scene,sample.Interval,sample.Update,sample.Tiles));}
 using(var writer=new StreamWriter(AuditPath+".memory.csv")){writer.WriteLine("time_s,private_mb,working_mb,cpu_ms_per_s");foreach(var sample in auditMemory)writer.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0:F3},{1:F3},{2:F3},{3:F3}",sample.Time,sample.Private,sample.Working,sample.Cpu));}
 File.WriteAllText(AuditPath+".meta.txt",string.Format(CultureInfo.InvariantCulture,"WPF tier: {0}\nDisplay Hz: {1}\nViewport DIP: {2} x {3}\nInstrumentation starts after 3s warmup; motion cycle 420s. AuditRate=1 is real time; audit-sweep uses 10x.\nUI intervals are not GPU presentation timestamps.\n",tier,nativeRefresh,ActualWidth,ActualHeight));
 AuditPath=null;Window.GetWindow(this).Close();return true;
 }
 public string ProfilePath;readonly List<double> intervals=new List<double>();double profilePrevious=-1,profileCpu;public Options Options;public bool Suspend;public bool Hint;public bool ShowStats;
 readonly bool saver,embedded;readonly Stopwatch watch=Stopwatch.StartNew();TimeSpan previousRendering=TimeSpan.MinValue;
 double lastClock=-1,motion,lastReal,submitted=-1,nextFrame,statsAt,frameCost;double colorClock,wallElapsed,paletteChangedAt=-10;int observedPalette=-1;Lab paletteFrom;int frames;double fps;
 readonly SolidColorBrush paper=new SolidColorBrush(),ink=new SolidColorBrush(Colors.Black),light=new SolidColorBrush();readonly SolidColorBrush control=new SolidColorBrush(Colors.Transparent);
 readonly WCanvas world=new WCanvas{Width=2200,Height=3040};readonly WCanvas dialog=new WCanvas{Width=1030,Height=684};
 readonly MatrixTransform camera=new MatrixTransform();readonly TranslateTransform dialogShift=new TranslateTransform();readonly TranslateTransform cursorShift=new TranslateTransform();
 readonly ScaleTransform dialogScale=new ScaleTransform(1,1,505,330);readonly TranslateTransform dialogLift=new TranslateTransform();double dialogPopAt=double.NaN;bool dialogWasOpen=true;
 readonly TranslateTransform repeatShift=new TranslateTransform();readonly TranslateTransform volumeShift=new TranslateTransform();readonly TranslateTransform speedShift=new TranslateTransform();
 const string SignatureAuthor="By RylanTu",SignatureMotto="Passion for a better world";
 const double SignatureLeft=700,SignatureAuthorTop=2620,SignatureMottoTop=2740;
 static readonly Drawing.Rectangle SignatureAuthorInk=WindowsPixels.MonoBounds(SignatureAuthor),SignatureMottoInk=WindowsPixels.MonoBounds(SignatureMotto);
 static readonly double SignatureAuthorPitch=MacInkHeight(SignatureAuthor)*7.0/Math.Max(1,SignatureAuthorInk.Height),SignatureMottoPitch=MacInkHeight(SignatureMotto)*5.0/Math.Max(1,SignatureMottoInk.Height);
 static int MacInkHeight(string text){int top=99,bottom=-1;foreach(char ch in text){var rows=Macintosh.ReferenceFonts.Glyph(ch);for(int y=0;y<rows.Length;y++)if(rows[y].IndexOf('1')>=0){top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}return Math.Max(1,bottom-top+1);}
 readonly Path signatureAuthor=new Path(),signatureMotto=new Path();readonly TranslateTransform signatureCaretShift=new TranslateTransform();
 double signatureTypingAt=double.NaN,signatureLastStroke=-100;int signatureAuthorCount=-1,signatureMottoCount=-1,signatureCaretLine=-1;
 readonly Path signatureCaret=new Path();readonly Image paintStroke=new Image{Width=240,Height=240,Stretch=Stretch.Fill,IsHitTestVisible=false};
 const int PaintOriginX=1530,PaintOriginY=1855,PaintPixels=480;readonly byte[] paintBuffer=new byte[PaintPixels*PaintPixels*4];readonly HashSet<long> paintedCells=new HashSet<long>();WriteableBitmap paintBitmap;int paintLastIndex=-1;
 double volume=1.0/3,volumeEntry=1.0/3;int mineState,paintSample=-1;bool mineClick1,mineClick2,mineNewGame,paintNewDone;readonly WCanvas paintMenu=new WCanvas{Width=260,Height=150};
 static readonly BitmapSource[] mineImages=new BitmapSource[4];readonly Image mineBoard=new Image{ClipToBounds=true,Width=Art.MineSize,Height=Art.MineSize,Stretch=Stretch.Fill};
 readonly Path digital=new Path(),hands=new Path(),date=new Path(),selectedClock=new Path(),selectedRate=new Path(),caret=new Path();
 readonly TextBlock hintLabel=new TextBlock(),statsLabel=new TextBlock();
 readonly FrameworkElement pointer;readonly FrameworkElement volumeKnob;readonly FrameworkElement speedKnob;readonly Rectangle restartPress=new Rectangle();int priorScene=-1;double actorChangedAt,actorFromX,actorFromY;
 [DllImport("winmm.dll")]static extern uint timeBeginPeriod(uint p);
 [DllImport("winmm.dll")]static extern uint timeEndPeriod(uint p);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateDC(string driver,string device,string output,IntPtr data);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern int GetDeviceCaps(IntPtr dc,int index);
 bool precise;int nativeRefresh=60;
 void Precision(bool enabled){if(enabled==precise)return;if(enabled){precise=timeBeginPeriod(1)==0;}else{timeEndPeriod(1);precise=false;}}
 void DetectDisplay(){var w=Window.GetWindow(this);if(w==null)return;var screen=Forms.Screen.FromHandle(new WindowInteropHelper(w).Handle);var dc=CreateDC("DISPLAY",screen.DeviceName,null,IntPtr.Zero);if(dc!=IntPtr.Zero){try{int hz=GetDeviceCaps(dc,116);nativeRefresh=hz>1?hz:60;}finally{DeleteDC(dc);}}}
 readonly int tier=RenderCapability.Tier>>16;bool twelve;double blinkPhase,blinkAt=-1;double blinkPosition=.5,blinkEntry=.5,dialogX,dialogY,dialogEntryX,dialogEntryY;bool dialogOpen=true;int rate=1;bool lastTwelve;int sceneIndex;double sceneLocal;
 static readonly Color[] Bg={Color.FromRgb(139,107,14),Color.FromRgb(137,44,58),Color.FromRgb(101,50,130),Color.FromRgb(27,96,114),Color.FromRgb(96,118,37),Color.FromRgb(157,159,149),Color.FromRgb(142,84,22),Color.FromRgb(139,57,30),Color.FromRgb(125,43,89),Color.FromRgb(110,47,121),Color.FromRgb(79,59,135),Color.FromRgb(33,70,138),Color.FromRgb(22,85,128),Color.FromRgb(36,114,61),Color.FromRgb(119,115,25),Color.FromRgb(140,114,16)};
 static readonly Color[] ReferenceSpectrum={Color.FromRgb(139,107,14),Color.FromRgb(139,104,14),Color.FromRgb(139,100,12),Color.FromRgb(139,96,11),Color.FromRgb(139,93,12),Color.FromRgb(141,91,14),Color.FromRgb(142,89,16),Color.FromRgb(142,87,19),Color.FromRgb(142,84,22),Color.FromRgb(141,80,23),Color.FromRgb(141,76,25),Color.FromRgb(140,71,26),Color.FromRgb(140,67,28),Color.FromRgb(140,63,30),Color.FromRgb(139,57,30),Color.FromRgb(139,50,31),Color.FromRgb(139,45,36),Color.FromRgb(139,44,46),Color.FromRgb(137,44,58),Color.FromRgb(133,44,69),Color.FromRgb(129,43,79),Color.FromRgb(125,43,89),Color.FromRgb(122,44,98),Color.FromRgb(118,45,107),Color.FromRgb(114,47,114),Color.FromRgb(110,47,121),Color.FromRgb(105,48,126),Color.FromRgb(101,50,130),Color.FromRgb(96,52,132),Color.FromRgb(91,54,133),Color.FromRgb(85,56,134),Color.FromRgb(79,59,135),Color.FromRgb(73,61,136),Color.FromRgb(65,63,137),Color.FromRgb(56,66,138),Color.FromRgb(44,67,138),Color.FromRgb(33,70,138),Color.FromRgb(23,73,136),Color.FromRgb(21,80,133),Color.FromRgb(22,85,128),Color.FromRgb(25,91,122),Color.FromRgb(27,96,114),Color.FromRgb(28,100,105),Color.FromRgb(28,103,94),Color.FromRgb(29,107,84),Color.FromRgb(30,110,72),Color.FromRgb(36,114,61),Color.FromRgb(45,117,51),Color.FromRgb(60,118,46),Color.FromRgb(74,119,43),Color.FromRgb(86,119,40),Color.FromRgb(96,118,37),Color.FromRgb(104,117,33),Color.FromRgb(112,116,30),Color.FromRgb(119,115,25),Color.FromRgb(126,115,23),Color.FromRgb(133,116,20),Color.FromRgb(138,116,18),Color.FromRgb(140,114,16),Color.FromRgb(140,111,15)};

 static readonly string[] CursorPixels={"100000000000","110000000000","111000000000","111100000000","111110000000","111111000000","111111100000","111111110000","111111111000","111111111100","111111000000","110110000000","100011000000","000011000000","000001100000"};
 sealed class SurfaceMaterial {public BitmapSource Image;public int X,Y,Width,Height;}
 static List<SurfaceMaterial> desktopMaterials,dialogMaterials;
 sealed class SurfaceTile {public FrameworkElement Visual;public Rect Bounds;public bool Visible=true;}
 readonly List<SurfaceTile> surfaceTiles=new List<SurfaceTile>();
 Rect visibleWorld=new Rect(0,0,2200,3040);
 double nextColorUpdate=-1,nextClockPoll=-1;DateTime lastDate=DateTime.MinValue;
 static readonly Rect TimeArea=new Rect(70,65,930,720),MouseArea=new Rect(1065,65,1010,720),SignatureArea=new Rect(620,2430,1242,500);
 static readonly Point[] CameraStops={new Point(650,465),new Point(1540,465),new Point(1560,1160),new Point(1550,1940),new Point(1100,2660),new Point(650,1940),new Point(630,1180)};
 static readonly int[] CameraRegions={0,1,3,5,6,4,2};
 static readonly Point[] LightCameraStops={new Point(650,465),new Point(1540,465),new Point(1560,1160),new Point(1100,2660),new Point(650,1940),new Point(630,1180)};
 static readonly int[] LightCameraRegions={0,1,3,6,4,2};

 public FluidView(Options options,bool saver,bool embedded,SolidColorBrush commonPaper=null){if(commonPaper!=null)paper=commonPaper;Options=options;ink.Freeze();control.Freeze();this.saver=saver;this.embedded=embedded;Hint=!saver&&!embedded;ClipToBounds=true;Background=paper;Cursor=Cursors.None;world.RenderTransform=camera;world.HorizontalAlignment=HorizontalAlignment.Left;world.VerticalAlignment=VerticalAlignment.Top;
 bool createdMaterials=desktopMaterials==null;if(createdMaterials)CreateMaterials();AddSurface(world,desktopMaterials,0,0);
 AddPath(signatureAuthor);AddPath(signatureMotto);AddPath(signatureCaret);signatureCaret.RenderTransform=signatureCaretShift;
 UpdateSignature(0);
 AddMineLayer();BuildPaintMenu();paintBitmap=new WriteableBitmap(PaintPixels,PaintPixels,192,192,PixelFormats.Pbgra32,null);paintStroke.Source=paintBitmap;paintStroke.Clip=new RectangleGeometry(new Rect(0,0,240,237));WCanvas.SetLeft(paintStroke,PaintOriginX);WCanvas.SetTop(paintStroke,PaintOriginY);world.Children.Add(paintStroke);
 AddPath(digital);AddPath(hands);AddPath(caret);
 selectedClock.Data=Geometry.Empty;selectedClock.RenderTransform=new TranslateTransform(562,552);
 selectedRate.Data=Geometry.Empty;selectedRate.RenderTransform=new TranslateTransform(1487,367);
 volumeKnob=Knob(39,21);volumeKnob.RenderTransform=volumeShift;volumeShift.X=Art.SoundTrackX-19;world.Children.Add(volumeKnob);speedKnob=Knob(33,54);speedKnob.RenderTransform=speedShift;speedShift.X=1528;speedShift.Y=595;world.Children.Add(speedKnob);
 dialog.Background=null;dialog.Children.Add(new Rectangle{Width=1010,Height=660,Fill=paper});WCanvas.SetZIndex(dialog,100);var dialogTransforms=new TransformGroup();dialogTransforms.Children.Add(dialogScale);dialogTransforms.Children.Add(dialogLift);dialogTransforms.Children.Add(dialogShift);dialog.RenderTransform=dialogTransforms;AddSurface(dialog,dialogMaterials,1065,855);world.Children.Add(dialog);
 restartPress.Width=225;restartPress.Height=69;restartPress.Fill=ink;restartPress.Opacity=.24;WCanvas.SetLeft(restartPress,498);WCanvas.SetTop(restartPress,555);dialog.Children.Add(restartPress);
 var cursorGroup=new WCanvas{Width=64,Height=76};var cursorShadow=new Path{Data=Pixels(CursorPixels,2,3,4),Fill=ink,Opacity=.22};cursorGroup.Children.Add(cursorShadow);cursorGroup.Children.Add(new Path{Data=Pixels(CursorPixels,0,0,4),Fill=ink});cursorGroup.Children.Add(new Path{Data=Pixels(CursorInterior(),0,0,4),Fill=paper});pointer=cursorGroup;pointer.RenderTransform=cursorShift;WCanvas.SetZIndex(pointer,20);world.Children.Add(pointer);Children.Add(world);var glow=new RadialGradientBrush{Center=new Point(.42,.3),GradientOrigin=new Point(.42,.3),RadiusX=.78,RadiusY=.95};glow.GradientStops.Add(new GradientStop(Color.FromArgb(29,255,248,221),0));glow.GradientStops.Add(new GradientStop(Color.FromArgb(6,255,248,221),.55));glow.GradientStops.Add(new GradientStop(Colors.Transparent,1));glow.Freeze();Children.Add(new Rectangle{Fill=glow,IsHitTestVisible=false});var shade=new LinearGradientBrush(Color.FromArgb(0,0,0,0),Color.FromArgb(15,0,0,0),90);shade.Freeze();Children.Add(new Rectangle{Fill=shade,IsHitTestVisible=false});
 hintLabel.Text="F11 全屏   ·   空格 换色   ·   A 自动鼠标   ·   S 设置   ·   F2 帧率";hintLabel.Foreground=light;hintLabel.FontSize=12;hintLabel.HorizontalAlignment=HorizontalAlignment.Center;hintLabel.VerticalAlignment=VerticalAlignment.Bottom;hintLabel.Margin=new Thickness(12,0,12,16);Children.Add(hintLabel);
 statsLabel.FontFamily=new FontFamily("Consolas");statsLabel.FontSize=13;statsLabel.Foreground=light;statsLabel.Background=new SolidColorBrush(Color.FromArgb(130,0,0,0));statsLabel.HorizontalAlignment=HorizontalAlignment.Left;statsLabel.VerticalAlignment=VerticalAlignment.Bottom;statsLabel.Margin=new Thickness(16,0,0,48);statsLabel.Padding=new Thickness(8);Children.Add(statsLabel);
 // Release temporary startup buffers once, before animation begins.
 if(createdMaterials){GC.Collect(2,GCCollectionMode.Forced);GC.WaitForPendingFinalizers();GC.Collect(2,GCCollectionMode.Forced);}
 RenderOptions.SetBitmapScalingMode(world,BitmapScalingMode.Linear);Loaded+=delegate{DetectDisplay();Precision(!embedded);lastReal=watch.Elapsed.TotalSeconds;CompositionTarget.Rendering+=Rendering;};Unloaded+=delegate{CompositionTarget.Rendering-=Rendering;Precision(false);};
 }
 void AddPath(Path p){p.Fill=ink;var shadow=new Path{Fill=Brushes.Black,Opacity=.27};shadow.SetBinding(Path.DataProperty,new System.Windows.Data.Binding("Data"){Source=p});shadow.SetBinding(Path.RenderTransformProperty,new System.Windows.Data.Binding("RenderTransform"){Source=p});shadow.SetBinding(Path.VisibilityProperty,new System.Windows.Data.Binding("Visibility"){Source=p});WCanvas.SetLeft(shadow,1.1);WCanvas.SetTop(shadow,1.9);world.Children.Add(shadow);world.Children.Add(p);}
 // Shadow is a frozen, prefiltered mask at one sample per world unit.
 // Never blur or resample the foreground glyphs; their exact source mask stays intact.
 // Small frozen masks stay reusable as color changes. No full-world color bitmap cache.
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static void CreateMaterials(){var main=Mask(false);desktopMaterials=MakeMaterials(main,2200,3040);var message=Mask(true);dialogMaterials=MakeMaterials(message,1030,684);}
 // Crops share one immutable source rather than duplicating all full-resolution pixels.
 static BitmapSource SharedCrop(BitmapSource source,Int32Rect region){
 var image=new CroppedBitmap(source,region);image.Freeze();return image;
 }
 static List<SurfaceMaterial> MakeMaterials(BitmapSource raster,int w,int h){
 var materials=new List<SurfaceMaterial>();const int size=512;
 for(int y=0;y<h;y+=size)for(int x=0;x<w;x+=size){int tw=Math.Min(size,w-x),th=Math.Min(size,h-y);
 materials.Add(new SurfaceMaterial{X=x,Y=y,Width=tw,Height=th,Image=SharedCrop(raster,new Int32Rect(x*2,y*2,tw*2,th*2))});}
 return materials;
 }
 void AddSurface(WCanvas parent,List<SurfaceMaterial> materials,int originX,int originY){
 foreach(var m in materials){var tile=new Image{Width=m.Width,Height=m.Height,Source=m.Image,Stretch=Stretch.Fill,IsHitTestVisible=false};WCanvas.SetLeft(tile,m.X);WCanvas.SetTop(tile,m.Y);parent.Children.Add(tile);
 var bounds=new Rect(originX+m.X-12,originY+m.Y-12,m.Width+24,m.Height+24);if(originX!=0)bounds.Inflate(160,90);surfaceTiles.Add(new SurfaceTile{Visual=tile,Bounds=bounds});}
 }
 void CullSurfaces(Matrix cameraMatrix){
 if(ActualWidth<=0||ActualHeight<=0||!cameraMatrix.HasInverse)return;
 cameraMatrix.Invert();visibleWorld=System.Windows.Rect.Transform(new Rect(0,0,ActualWidth,ActualHeight),cameraMatrix);visibleWorld.Inflate(12,12);
 foreach(var tile in surfaceTiles){bool show=visibleWorld.IntersectsWith(tile.Bounds);if(show!=tile.Visible){tile.Visual.Opacity=show?1:0;tile.Visible=show;}}
 }

 FrameworkElement Knob(double w,double h){var group=new WCanvas{Width=w,Height=h,Background=paper};using(var b=Art.KnobTexture((int)w,(int)h))group.Children.Add(new Image{Width=w,Height=h,Source=FromBitmap(b)});return group;}

 // Preserve the source cells exactly. Do not quantize an already rasterized glyph.
 // Textures are drawn at 2x resolution to retain narrow edges while tilted.
 static BitmapSource Mask(bool dialog){using(var b=Art.Texture(dialog)){var rect=new Drawing.Rectangle(0,0,b.Width,b.Height);var data=b.LockBits(rect,Drawing.Imaging.ImageLockMode.ReadOnly,Drawing.Imaging.PixelFormat.Format32bppPArgb);try{var raw=new byte[data.Stride*b.Height];Marshal.Copy(data.Scan0,raw,0,raw.Length);for(int i=0;i<raw.Length;i+=4){byte alpha=(byte)(255-raw[i]);raw[i]=raw[i+1]=raw[i+2]=0;raw[i+3]=alpha;}var image=BitmapSource.Create(b.Width,b.Height,192,192,PixelFormats.Pbgra32,null,raw,data.Stride);image.Freeze();return Macintosh.FluidView.BakeInk(image);}finally{b.UnlockBits(data);}}}

 bool compositeMode;double nextCull=-1,nextActor=-1;int lastCompositeScene=-2;Rect lastCullRect;
 public WCanvas DetachWorld(){compositeMode=true;Children.Remove(world);world.RenderTransform=Transform.Identity;world.Children.Remove(pointer);return world;}
 public FrameworkElement CursorVisual{get{return pointer;}}
 public string FrameState{get{return string.Format(CultureInfo.InvariantCulture,"scene={0}, cursor=({1:F2},{2:F2}), dialog=({3:F2},{4:F2}), open={5}, volume_grip=({6:F2},{7:F2})",priorScene,cursorShift.X,cursorShift.Y,dialogX,dialogY,dialogOpen,volumeShift.X+19,volumeShift.Y+10)+string.Format(CultureInfo.InvariantCulture,", keyboard_grip=({0:F2},622), blink_period_s={1:F3}",speedShift.X+16,1.6-blinkPosition*.65);}}
 public Point CursorPosition{get{return new Point(cursorShift.X,cursorShift.Y);}}
 public Brush Paper{get{return paper;}}
 public void CompositeFrame(double t,int scene,double local,Rect visible){motion=t;colorClock=t;wallElapsed=watch.Elapsed.TotalSeconds;sceneIndex=scene;sceneLocal=local;visibleWorld=visible;
 bool changed=scene!=lastCompositeScene;
 if(changed||t>=nextCull||!lastCullRect.Contains(visible)){nextCull=t+.1;lastCullRect=visible;if(!lastCullRect.IsEmpty)lastCullRect.Inflate(32,32);foreach(var tile in surfaceTiles){bool show=lastCullRect.IntersectsWith(tile.Bounds);if(show!=tile.Visible){tile.Visual.Visibility=show?Visibility.Visible:Visibility.Hidden;tile.Visible=show;}}}
 lastCompositeScene=scene;
 Update(t,false);
 }
 public void SetTime(double t){motion=t;colorClock=t;wallElapsed=t;Update(t,true);}
 void Rendering(object sender,EventArgs args){var e=(RenderingEventArgs)args;if(e.RenderingTime==previousRendering)return;previousRendering=e.RenderingTime;double now=watch.Elapsed.TotalSeconds;wallElapsed=now;double dt=now-lastReal;lastReal=now;if(Suspend){Precision(false);return;}var window=Window.GetWindow(this);if(window!=null&&window.WindowState==WindowState.Minimized){Precision(false);return;}Precision(!embedded);
 if(Options.Animate){motion+=Math.Min(dt,.25)*Options.Speed/100.0*(AuditPath==null?1:AuditRate);colorClock+=Math.Min(dt,.25)*(AuditPath==null?1:AuditRate);}int target=embedded?15:Options.FrameRate>=144?144:Options.FrameRate>=120?120:60;target=Math.Min(target,nativeRefresh);if(now<nextFrame-.0005)return;nextFrame+=1.0/target;while(nextFrame<=now)nextFrame+=1.0/target;submitted=now;
 long begin=Stopwatch.GetTimestamp();Update(motion,false);double cpu=(Stopwatch.GetTimestamp()-begin)*1000.0/Stopwatch.Frequency;frameCost+=cpu;frames++;if(RecordAudit(now,cpu))return;if(ProfilePath!=null&&now>=2){if(profilePrevious>=0){intervals.Add((now-profilePrevious)*1000);profileCpu+=cpu;}profilePrevious=now;if(now>=12&&intervals.Count>0){var sorted=new List<double>(intervals);sorted.Sort();double total=0;foreach(double i in intervals)total+=i;File.WriteAllText(ProfilePath,string.Format(CultureInfo.InvariantCulture,"WPF tier: {0}\nNative display refresh: {9}\nViewport DIP: {1} x {2}\nTarget: {3}\nUI submitted fps: {4:F2}\nMean CPU update ms: {5:F3}\nMedian frame interval ms: {6:F3}\nP95 frame interval ms: {7:F3}\nSamples: {8}\nThis measures UI submission cadence, not GPU presentation.\n",tier,ActualWidth,ActualHeight,target,intervals.Count*1000/total,profileCpu/intervals.Count,sorted[sorted.Count/2],sorted[(int)(sorted.Count*.95)],intervals.Count,nativeRefresh));ProfilePath=null;Window.GetWindow(this).Close();return;}}if(now-statsAt>=1){fps=frames/(now-statsAt);if(ShowStats)statsLabel.Text=string.Format(CultureInfo.InvariantCulture,"{0:F0} fps | target {1} | CPU {2:F2} ms | WPF tier {3}",fps,target,frameCost/frames,tier);frames=0;frameCost=0;statsAt=now;}
 }
 static double Ease(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
 static double Lerp(double a,double b,double p){return a+(b-a)*p;}
 static Color Mix(Color a,Color b,double t){return Color.FromRgb((byte)Lerp(a.R,b.R,t),(byte)Lerp(a.G,b.G,t),(byte)Lerp(a.B,b.B,t));}

 // Oklab interpolation keeps perceived lightness consistent through long color fades.
 struct Lab {public double L,A,B;public Lab(double l,double a,double b){L=l;A=a;B=b;}}
 static readonly Lab[] BgLabs=MakeLabs(Bg);static readonly Lab[] SpectrumLabs=MakeLabs(ReferenceSpectrum);
 static Lab[] MakeLabs(Color[] colors){var labs=new Lab[colors.Length];for(int i=0;i<labs.Length;i++)labs[i]=ToLab(colors[i]);return labs;}
 static double SoftEase(double t){t=Math.Max(0,Math.Min(1,t));return t*t*t*(t*(t*6-15)+10);}
 static double Linear(byte channel){double c=channel/255.0;return c<=.04045?c/12.92:Math.Pow((c+.055)/1.055,2.4);}
 static Lab ToLab(Color color){double r=Linear(color.R),g=Linear(color.G),b=Linear(color.B);double l=Math.Pow(.4122214708*r+.5363325363*g+.0514459929*b,1.0/3);double m=Math.Pow(.2119034982*r+.6806995451*g+.1073969566*b,1.0/3);double s=Math.Pow(.0883024619*r+.2817188376*g+.6299787005*b,1.0/3);return new Lab(.2104542553*l+.7936177850*m-.0040720468*s,1.9779984951*l-2.4285922050*m+.4505937099*s,.0259040371*l+.7827717662*m-.8086757660*s);}
 static Lab Blend(Lab a,Lab b,double t){return new Lab(Lerp(a.L,b.L,t),Lerp(a.A,b.A,t),Lerp(a.B,b.B,t));}

 static double Cubic(double p0,double p1,double p2,double p3,double t){double t2=t*t,t3=t2*t;return .5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t2+(-p0+3*p1-3*p2+p3)*t3);}
 static Lab SpectrumAt(int i,double t){int n=SpectrumLabs.Length;var a=SpectrumLabs[(i-1+n)%n];var b=SpectrumLabs[i%n];var c=SpectrumLabs[(i+1)%n];var d=SpectrumLabs[(i+2)%n];return new Lab(Cubic(a.L,b.L,c.L,d.L,t),Cubic(a.A,b.A,c.A,d.A,t),Cubic(a.B,b.B,c.B,d.B,t));}
 static float Clamp(double v){return (float)Math.Max(0,Math.Min(1,v));}
 static Color FromLab(Lab c){double l=c.L+.3963377774*c.A+.2158037573*c.B;double m=c.L-.1055613458*c.A-.0638541728*c.B;double s=c.L-.0894841775*c.A-1.2914855480*c.B;l=l*l*l;m=m*m*m;s=s*s*s;return Color.FromScRgb(1,Clamp(4.0767416621*l-3.3077115913*m+.2309699292*s),Clamp(-1.2684380046*l+2.6097574011*m-.3413193965*s),Clamp(-.0041960863*l-.7034186147*m+1.7076147010*s));}

 void Update(double t,bool force){if(force||wallElapsed>=nextColorUpdate||observedPalette!=Options.Palette){nextColorUpdate=wallElapsed+1.0/30;int idx=Math.Max(0,Math.Min(Bg.Length-1,Options.Palette-1));double phase=(colorClock/180.0%1)*SpectrumLabs.Length;int segment=(int)Math.Floor(phase);Lab target=Options.Palette==0?SpectrumAt(segment,phase-segment):BgLabs[idx];if(observedPalette!=Options.Palette){if(observedPalette>=0){paletteFrom=ToLab(paper.Color);paletteChangedAt=wallElapsed;}observedPalette=Options.Palette;}double manual=SoftEase((wallElapsed-paletteChangedAt)/3);paper.Color=FromLab(manual<1?Blend(paletteFrom,target,manual):target);light.Color=Mix(paper.Color,Color.FromRgb(250,244,224),.8);}
 if(!compositeMode||sceneIndex!=priorScene||t>=nextActor){bool moving=Options.Cinematic&&sceneIndex>=0&&sceneLocal<18;nextActor=t+(moving?1.0/60:.1);if(sceneIndex>=0||priorScene>=0||visibleWorld.IntersectsWith(MouseArea)||visibleWorld.IntersectsWith(SignatureArea))Perform(sceneIndex,sceneLocal,t);}

 UpdateDialogPop(t);
 // Only the clock panel reads wall time; offscreen content catches up on entry.
 if(force||(visibleWorld.IntersectsWith(TimeArea)&&wallElapsed>=nextClockPoll)){var now=DateTime.Now;nextClockPoll=wallElapsed+(1000-now.Millisecond)/1000.0+.002;double second=Math.Floor(now.TimeOfDay.TotalSeconds);
 if(force||lastClock!=second||lastTwelve!=twelve){lastClock=second;lastTwelve=twelve;digital.Data=PixelText(now.ToString(twelve?"h:mm:ss":"H:mm:ss"),613,544,3,true);hands.Data=ClockHands(now);}
 if(force||lastDate!=now.Date){lastDate=now.Date;date.Data=Geometry.Empty;}
 }
 if(!compositeMode){hintLabel.Visibility=Hint?Visibility.Visible:Visibility.Collapsed;statsLabel.Visibility=ShowStats?Visibility.Visible:Visibility.Collapsed;}
 }
 void UpdateDialogPop(double t){
 if(double.IsNaN(dialogPopAt))return;
 double progress=Math.Max(0,Math.Min(1,(t-dialogPopAt)/.22)),ease=1-Math.Pow(1-progress,3);
 dialogScale.ScaleX=dialogScale.ScaleY=.96+.04*ease;dialogLift.Y=8*(1-ease);dialog.Opacity=.3+.7*ease;
 if(progress>=1){dialogPopAt=double.NaN;dialog.CacheMode=null;}
 }
 void Perform(int scene,double p,double t){
 bool active=Options.Cinematic,pressed=false;double cx=1020,cy=730;
 if(priorScene!=scene){if(scene==6){signatureTypingAt=t+Math.Max(.8,(8-p)*2.5);signatureAuthorCount=signatureMottoCount=-1;}if(scene==1)blinkEntry=blinkPosition;if(scene==3){dialogEntryX=dialogX;dialogEntryY=dialogY;dialogOpen=true;}if(scene==2)volumeEntry=volume;if(scene==4){mineClick1=mineClick2=mineNewGame=false;}if(scene==5){paintNewDone=false;}}
 if(visibleWorld.IntersectsWith(SignatureArea))UpdateSignature(t);restartPress.Visibility=Visibility.Collapsed;paintMenu.Visibility=active&&scene==5&&p>=1.5&&p<2.8?Visibility.Visible:Visibility.Collapsed;
 if(active){
 if(scene==0){cx=Lerp(900,740,Ease(p/7));cy=Lerp(195,550,Ease(p/7));}
 if(scene==1){if(p<4)blinkPosition=blinkEntry;else if(p<8)blinkPosition=Lerp(blinkEntry,.15,Ease((p-4)/4));else if(p<12)blinkPosition=Lerp(.15,.5,Ease((p-8)/4));else if(p<16)blinkPosition=Lerp(.5,.9,Ease((p-12)/4));else blinkPosition=.9;
 cx=1320+blinkPosition*417+16;cy=622;if(p<4){cx=Lerp(1190,cx,Ease(p/4));cy=Lerp(260,cy,Ease(p/4));}if(p>17){cx=Lerp(cx,2000,Ease((p-17)/3));cy=Lerp(cy,720,Ease((p-17)/3));}pressed=p>=4&&p<16;}
 if(scene==2){if(p<3)volume=volumeEntry;else if(p<8)volume=Lerp(volumeEntry,.04,Ease((p-3)/5));else if(p<10)volume=.04;else if(p<15)volume=Lerp(.04,.86,Ease((p-10)/5));else volume=.86;double ky=Art.SoundTrackY+volume*Art.SoundTravel;cx=Art.SoundTrackX;cy=ky+10;if(p<3){cx=Lerp(380,cx,Ease(p/3));cy=Lerp(1220,cy,Ease(p/3));}if(p>=16){cx=Lerp(cx,875,Ease((p-16)/2));cy=Lerp(cy,1435,Ease((p-16)/2));}pressed=(p>=3&&p<8)||(p>=10&&p<15);}
 if(scene==3){double amount=Ease((p-5)/5),targetX=Math.Min(140,dialogEntryX+70),targetY=Math.Min(76,dialogEntryY+38);dialogX=Lerp(dialogEntryX,targetX,amount);dialogY=Lerp(dialogEntryY,targetY,amount);cx=1450+dialogX;cy=889+dialogY;if(p<4){cx=Lerp(1870,cx,Ease(p/4));cy=Lerp(1160,cy,Ease(p/4));}if(p>=10){cx=Lerp(1450+dialogX,1675+dialogX,Ease((p-10)/3));cy=Lerp(889+dialogY,1444+dialogY,Ease((p-10)/3));}pressed=(p>=5&&p<10)||(p>=13.5&&p<13.8);if(p>=13.5&&p<13.8)restartPress.Visibility=Visibility.Visible;if(p>=13.8)dialogOpen=p>=14.0;if(p>=14.0){dialogX=0;dialogY=0;}}
 if(scene==4){cx=Lerp(385,644,Ease(p/2.5));cy=Lerp(1760,1828,Ease(p/2.5));if(p>=3){cx=Lerp(644,620,Ease((p-3)/2));cy=Lerp(1828,2062,Ease((p-3)/2));}if(p>=2.95&&!mineNewGame){mineNewGame=true;SetMineState(0);}if(p>=9){cx=Lerp(620,716,Ease((p-9)/3));cy=Lerp(2062,2158,Ease((p-9)/3));}pressed=(p>=2.8&&p<3.05)||(p>=6&&p<6.3)||(p>=13&&p<13.3);if(p>=6.15&&!mineClick1){mineClick1=true;SetMineState(mineState|1);}if(p>=13.15&&!mineClick2){mineClick2=true;SetMineState(mineState|2);}}
 if(scene==5){
 double progress=Math.Max(0,Math.Min(1,(p-5)/10));Point pt=PaintPoint(progress);
 if(p<1.8){cx=1130;cy=1750;}else if(p<2.8){cx=Lerp(1130,1150,Ease((p-1.8)/.7));cy=Lerp(1750,1800,Ease((p-1.8)/.7));}else if(p<5){cx=Lerp(1150,pt.X,Ease((p-2.8)/2.2));cy=Lerp(1800,pt.Y,Ease((p-2.8)/2.2));}else{cx=pt.X;cy=pt.Y;}
 if(p>=2.65&&!paintNewDone){paintNewDone=true;ResetPaint();}
 if(p>=5)UpdatePaint(progress);
 pressed=(p>=1.45&&p<1.7)||(p>=2.55&&p<2.75)||(p>=5&&p<15);
 }
 if(scene==6){cx=Lerp(1630,1190,Ease(p/7));cy=Lerp(2560,2795,Ease(p/7));}
 }
 
 volumeShift.Y=Art.SoundTrackY+volume*Art.SoundTravel;double knobAlpha=pressed&&scene==2?.88:1;if(volumeKnob.Opacity!=knobAlpha)volumeKnob.Opacity=knobAlpha;dialogShift.X=1065+dialogX;dialogShift.Y=855+dialogY;if(dialogOpen&&!dialogWasOpen){dialogPopAt=t;dialog.CacheMode=new BitmapCache(2);}dialogWasOpen=dialogOpen;dialog.Visibility=dialogOpen?Visibility.Visible:Visibility.Hidden;
 speedShift.X=1320+blinkPosition*417;double period=1.6-blinkPosition*.65;if(blinkAt<0)blinkAt=t;blinkPhase=(blinkPhase+Math.Max(0,Math.Min(.25,t-blinkAt))/period)%1;blinkAt=t;if(visibleWorld.IntersectsWith(MouseArea)){var state=blinkPhase<.5?Visibility.Visible:Visibility.Hidden;if(caret.Visibility!=state)caret.Visibility=state;}
 if(caret.Data==null)caret.Data=Pixels(new[]{"111","010","010","010","010","010","010","111"},1925,583,4);
 if(priorScene!=scene){actorChangedAt=t;actorFromX=cursorShift.X;actorFromY=cursorShift.Y;priorScene=scene;}
 double approach=Ease((t-actorChangedAt)/1.2);cursorShift.X=Lerp(actorFromX,cx,approach);cursorShift.Y=Lerp(actorFromY,cy,approach);pointer.Visibility=active?Visibility.Visible:Visibility.Collapsed;
 }

 // Fixed per-character timings give a natural rhythm without frame-dependent randomness.
 static double StrokeDelay(string text,int i){return .09+((i*7+3)%5)*.022+(text[i]==' '?.085:0);}
 static int TypedCount(string text,double elapsed,out double duration){duration=0;int count=0;for(int i=0;i<text.Length;i++){duration+=StrokeDelay(text,i);if(elapsed>=duration)count++;}return count;}
 void UpdateSignature(double t){
 double elapsed=double.IsNaN(signatureTypingAt)?-1:t-signatureTypingAt,firstDuration,secondDuration;
 int first=TypedCount(SignatureAuthor,elapsed,out firstDuration);
 double secondElapsed=elapsed-firstDuration-.8;
 int second=TypedCount(SignatureMotto,secondElapsed,out secondDuration);
 bool newline=secondElapsed>=0;
 bool changed=first!=signatureAuthorCount||second!=signatureMottoCount;
 if(first!=signatureAuthorCount){signatureAuthor.Data=PixelText(SignatureAuthor.Substring(0,first),SignatureLeft-SignatureAuthorInk.X*SignatureAuthorPitch,SignatureAuthorTop-SignatureAuthorInk.Y*SignatureAuthorPitch,SignatureAuthorPitch,false,true);signatureAuthorCount=first;signatureLastStroke=t;}
 if(second!=signatureMottoCount){signatureMotto.Data=PixelText(SignatureMotto.Substring(0,second),SignatureLeft-SignatureMottoInk.X*SignatureMottoPitch,SignatureMottoTop-SignatureMottoInk.Y*SignatureMottoPitch,SignatureMottoPitch,false,true);signatureMottoCount=second;signatureLastStroke=t;}
 int line=newline?1:0;changed=changed||signatureCaretLine!=line;if(changed){double pitch=newline?SignatureMottoPitch:SignatureAuthorPitch;string current=newline?SignatureMotto.Substring(0,second):SignatureAuthor.Substring(0,first);
 if(signatureCaretLine!=line){signatureCaret.Data=Pixels(Stem(newline?SignatureMottoInk.Height:SignatureAuthorInk.Height),0,0,pitch);signatureCaretLine=line;}
 double x=SignatureLeft-(newline?SignatureMottoInk.X:SignatureAuthorInk.X)*pitch;foreach(char ch in current)x+=WindowsPixels.MonoAdvance(ch)*pitch;
 signatureCaretShift.X=x+pitch;signatureCaretShift.Y=newline?SignatureMottoTop:SignatureAuthorTop;}
 bool show=t-signatureLastStroke<.3||t%1.15<.575;
 signatureCaret.Visibility=show?Visibility.Visible:Visibility.Hidden;
 }


 static string[] Stem(int height){var rows=new string[height];for(int i=0;i<height;i++)rows[i]="1";return rows;}
 static string[] CursorInterior(){var rows=new string[CursorPixels.Length];for(int y=0;y<rows.Length;y++){var line=new char[CursorPixels[y].Length];for(int x=0;x<line.Length;x++)line[x]=y>0&&y<rows.Length-1&&x>0&&x<line.Length-1&&CursorPixels[y][x]=='1'&&CursorPixels[y-1][x]=='1'&&CursorPixels[y+1][x]=='1'&&CursorPixels[y][x-1]=='1'&&CursorPixels[y][x+1]=='1'?'1':'0';rows[y]=new string(line);}return rows;}
 static BitmapSource FromBitmap(Drawing.Bitmap b){var rect=new Drawing.Rectangle(0,0,b.Width,b.Height);var data=b.LockBits(rect,Drawing.Imaging.ImageLockMode.ReadOnly,Drawing.Imaging.PixelFormat.Format32bppPArgb);try{var bytes=new byte[data.Stride*b.Height];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);for(int i=0;i<bytes.Length;i+=4){byte alpha=(byte)(255-bytes[i]);bytes[i]=bytes[i+1]=bytes[i+2]=0;bytes[i+3]=alpha;}var image=BitmapSource.Create(b.Width,b.Height,192,192,PixelFormats.Pbgra32,null,bytes,data.Stride);image.Freeze();return image;}finally{b.UnlockBits(data);}}
 void BuildPaintMenu(){paintMenu.Width=465;paintMenu.Height=414;paintMenu.Background=paper;paintMenu.Visibility=Visibility.Collapsed;using(var b=Art.PaintMenuTexture())paintMenu.Children.Add(new Image{Source=FromBitmap(b),Width=465,Height=414});WCanvas.SetLeft(paintMenu,1083);WCanvas.SetTop(paintMenu,1771);world.Children.Add(paintMenu);WCanvas.SetZIndex(paintMenu,60);}
 void AddMineLayer(){if(mineImages[0]==null)for(int i=0;i<4;i++)using(var b=Art.MineTexture(i))mineImages[i]=FromBitmap(b);WCanvas.SetLeft(mineBoard,Art.MineX);WCanvas.SetTop(mineBoard,Art.MineY);world.Children.Add(mineBoard);surfaceTiles.Add(new SurfaceTile{Visual=mineBoard,Bounds=new Rect(Art.MineX,Art.MineY,Art.MineSize,Art.MineSize)});SetMineState(0);}
 void SetMineState(int state){mineState=state;mineBoard.Source=mineImages[state];}
 struct BrushPoint {public Point P;public bool Down;public BrushPoint(double x,double y,bool down){P=new Point(x,y);Down=down;}}
 static readonly BrushPoint[] PaintRoute=MakePaintRoute();
 static BrushPoint[] MakePaintRoute(){var list=new List<BrushPoint>();for(int i=0;i<=96;i++){double a=-Math.PI/2+i*Math.PI*2/96;list.Add(new BrushPoint(1650+105*Math.Cos(a),1975+105*Math.Sin(a),i>0));}list.Add(new BrushPoint(1610,1940,false));list.Add(new BrushPoint(1610,1960,true));list.Add(new BrushPoint(1690,1940,false));list.Add(new BrushPoint(1690,1960,true));for(int i=0;i<=30;i++){double a=i*Math.PI/30;list.Add(new BrushPoint(1650+58*Math.Cos(a),1990+58*Math.Sin(a),i>0));}return list.ToArray();}
 static Point PaintPoint(double progress){double n=progress*(PaintRoute.Length-1);int i=Math.Min(PaintRoute.Length-2,(int)n);double u=n-i;return new Point(Lerp(PaintRoute[i].P.X,PaintRoute[i+1].P.X,u),Lerp(PaintRoute[i].P.Y,PaintRoute[i+1].P.Y,u));}
 static void InkStamp(StreamGeometryContext c,HashSet<long> used,Point p){int x=(int)Math.Round(p.X/3)*3,y=(int)Math.Round(p.Y/3)*3;long key=((long)x<<32)|(uint)y;if(used.Add(key))Rect(c,x+.15,y+.15,2.7,2.7);}
 static void InkSegment(StreamGeometryContext c,HashSet<long> used,Point a,Point b){int steps=Math.Max(1,(int)Math.Ceiling((b-a).Length/3));for(int j=0;j<=steps;j++)InkStamp(c,used,new Point(Lerp(a.X,b.X,j/(double)steps),Lerp(a.Y,b.Y,j/(double)steps)));}
 void ResetPaint(){paintSample=-1;paintLastIndex=-1;paintedCells.Clear();Array.Clear(paintBuffer,0,paintBuffer.Length);paintBitmap.WritePixels(new Int32Rect(0,0,PaintPixels,PaintPixels),paintBuffer,PaintPixels*4,0);}
 void PaintStamp(Point p,ref Int32Rect dirty){
 int gx=(int)Math.Round(p.X/3)*3,gy=(int)Math.Round(p.Y/3)*3;long key=((long)gx<<32)|(uint)gy;if(!paintedCells.Add(key))return;
 int x=(gx-PaintOriginX)*2,y=(gy-PaintOriginY)*2;
 for(int j=0;j<10;j++)for(int i=0;i<9;i++){int xx=x+i,yy=y+j;if(xx<0||yy<0||xx>=PaintPixels||yy>=PaintPixels)continue;double coverage=0;
 // Reproduce the existing six-pixel ink cell and its offset shadow in the tiny bitmap.
 if(i<6&&j<6)coverage=(i==0||i==5?.7:1)*(j==0||j==5?.7:1);
 double sx=i-2.2,sy=j-3.8,shadow=Math.Max(0,Math.Min(sx+1,6-sx))*Math.Max(0,Math.Min(sy+1,6-sy));shadow=Math.Min(1,shadow)*.27;
 byte alpha=(byte)(255*(coverage+shadow*(1-coverage)));int index=(yy*PaintPixels+xx)*4+3;if(alpha>paintBuffer[index])paintBuffer[index]=alpha;
 }
 var r=new Int32Rect(Math.Max(0,x),Math.Max(0,y),Math.Max(0,Math.Min(PaintPixels,x+9)-Math.Max(0,x)),Math.Max(0,Math.Min(PaintPixels,y+10)-Math.Max(0,y)));
 if(r.Width>0&&r.Height>0){if(dirty.IsEmpty||dirty.Width==0)dirty=r;else{int left=Math.Min(dirty.X,r.X),top=Math.Min(dirty.Y,r.Y),right=Math.Max(dirty.X+dirty.Width,r.X+r.Width),bottom=Math.Max(dirty.Y+dirty.Height,r.Y+r.Height);dirty=new Int32Rect(left,top,right-left,bottom-top);}}
 }
 void PaintSegment(Point a,Point b,ref Int32Rect dirty){int steps=Math.Max(1,(int)Math.Ceiling((b-a).Length/3));for(int j=0;j<=steps;j++)PaintStamp(new Point(Lerp(a.X,b.X,j/(double)steps),Lerp(a.Y,b.Y,j/(double)steps)),ref dirty);}
 void UpdatePaint(double progress){int sample=(int)Math.Floor(progress*(PaintRoute.Length-1)*4);if(sample==paintSample)return;if(sample<paintSample)ResetPaint();paintSample=sample;
 double n=sample/4.0;int last=(int)n;var dirty=Int32Rect.Empty;
 for(int i=Math.Max(0,paintLastIndex+1);i<=last;i++){var p=PaintRoute[i];if(i==0||!p.Down)PaintStamp(p.P,ref dirty);else PaintSegment(PaintRoute[i-1].P,p.P,ref dirty);}paintLastIndex=last;
 if(last<PaintRoute.Length-1&&PaintRoute[last+1].Down){var a=PaintRoute[last].P;var b=PaintRoute[last+1].P;PaintSegment(a,new Point(Lerp(a.X,b.X,n-last),Lerp(a.Y,b.Y,n-last)),ref dirty);}
 if(!dirty.IsEmpty&&dirty.Width>0)paintBitmap.WritePixels(dirty,paintBuffer,PaintPixels*4,dirty.Y*PaintPixels*4+dirty.X*4);
 }

 static StreamGeometry Pixels(string[] rows,double x,double y,double p){var g=new StreamGeometry{FillRule=FillRule.Nonzero};using(var c=g.Open())AddPixels(c,rows,x,y,p);g.Freeze();return g;}
 static void Rect(StreamGeometryContext c,double x,double y,double w,double h){c.BeginFigure(new Point(x,y),true,true);c.LineTo(new Point(x+w,y),true,false);c.LineTo(new Point(x+w,y+h),true,false);c.LineTo(new Point(x,y+h),true,false);}


 static void AddPixels(StreamGeometryContext c,string[] rows,double x,double y,double p){double gap=Math.Min(.55,p*.11);for(int j=0;j<rows.Length;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]=='1'){double px=x+i*p,py=y+j*p;Rect(c,px+gap*.5,py+gap*.5,p-gap,p-gap);}}


 static StreamGeometry PixelText(string text,double x,double y,double p,bool large=false,bool mono=false){var g=new StreamGeometry{FillRule=FillRule.Nonzero};using(var c=g.Open()){foreach(char ch in text){var rows=mono?WindowsPixels.MonoGlyph(ch):large?WindowsPixels.Digit(ch):Art.Glyph(ch);AddPixels(c,rows,x,y,p);x+=(mono?WindowsPixels.MonoAdvance(ch):Art.Advance(ch))*p;}}g.Freeze();return g;}

 static StreamGeometry ClockHands(DateTime now){var g=new StreamGeometry{FillRule=FillRule.Nonzero};using(var c=g.Open()){Ray(c,742,382,(now.Minute+now.Second/60.0)*Math.PI/30-Math.PI/2,69);Ray(c,742,382,(now.Hour%12+now.Minute/60.0)*Math.PI/6-Math.PI/2,45);}g.Freeze();return g;}
 static void Ray(StreamGeometryContext c,double x,double y,double a,double len){for(int i=0;i<len;i+=5)Rect(c,x+Math.Round(Math.Cos(a)*i/6)*6,y+Math.Round(Math.Sin(a)*i/6)*6,5.4,5.4);}
}
}
