using System;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using System.IO;using System.Runtime.InteropServices;using System.Windows;using System.Windows.Controls;using System.Windows.Input;using System.Windows.Interop;using System.Windows.Media;using System.Windows.Media.Imaging;using System.Windows.Shapes;
using Drawing=System.Drawing;using Forms=System.Windows.Forms;using WCanvas=System.Windows.Controls.Canvas;
namespace RetroWindows{
static class DuetApp{
 [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 [STAThread]static void Main(string[] args){SetProcessDPIAware();string mode=args.Length==0?"":args[0].ToLowerInvariant();var launch=SaverLaunch.Parse(args,string.Equals(System.IO.Path.GetExtension(Process.GetCurrentProcess().MainModule.FileName),".scr",StringComparison.OrdinalIgnoreCase));if(launch.Kind==SaverLaunchKind.Invalid)return;
 if(mode=="--route"){File.WriteAllText(args[1],SharedView.RouteInfo());return;}
 if(launch.Kind==SaverLaunchKind.Settings||mode=="--settings"){Forms.Application.EnableVisualStyles();using(var f=new Settings())f.ShowDialog();return;}
 if(mode=="--inspect"){var v=new SharedView(Options.Load(),false,false);File.WriteAllText(args[1],v.Inspect());return;}
 if(mode=="--render"||mode=="--frames"){
 var view=new SharedView(Options.Load(),false,false);view.RandomOrder=false;view.Hint=false;int width=mode=="--frames"?960:1600,height=mode=="--frames"?600:1000;view.Measure(new Size(width,height));view.Arrange(new Rect(0,0,width,height));
 double from=args.Length>2?double.Parse(args[2],CultureInfo.InvariantCulture):0,to=mode=="--frames"?double.Parse(args[3],CultureInfo.InvariantCulture):from;
 int fps=mode=="--frames"?(args.Length>4?int.Parse(args[4]):60):1;int count=mode=="--frames"?(int)Math.Round((to-from)*fps)+1:1;if(mode=="--frames")Directory.CreateDirectory(args[1]);
 if(mode=="--render"&&args.Length>3&&args[3]=="--warm"){for(double warm=Math.Max(0,from-25);warm<from;warm+=1.0/30)view.SetTime(warm);}
 for(int i=0;i<count;i++){view.SetTime(from+i/(double)fps);view.UpdateLayout();var image=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);image.Render(view);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(mode=="--frames"?System.IO.Path.Combine(args[1],i.ToString("D4")+".png"):args[1]))png.Save(stream);}return;}
 var app=new Application{ShutdownMode=ShutdownMode.OnLastWindowClose};
 if(launch.Kind==SaverLaunchKind.Preview){if(!ScreenSaverPreview.IsValidParent(launch.Parent))return;app.ShutdownMode=ShutdownMode.OnExplicitShutdown;try{using(var embeddedPreview=new ScreenSaverPreview(launch.Parent,delegate{app.Shutdown();}))app.Run();}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}return;}
 if(launch.Kind==SaverLaunchKind.ScreenSaver){using(var session=new MultiScreenSession()){session.Show();app.Run();}return;}
 var preview=new DuetWindow();if(mode=="--mac-control")preview.ControlPreview();if(mode=="--camera")preview.Seek(16);if(mode=="--story")preview.Seek(1642);if(mode=="--story-windows")preview.Seek(1684);if(mode=="--clock")preview.Seek(29);if(mode=="--macintosh")preview.Seek(7);if(mode=="--paint")preview.Seek(694);if(mode=="--profile"){preview.Seek(args.Length>2?double.Parse(args[2],CultureInfo.InvariantCulture):0);preview.Profile(args[1]);}if(mode=="--review"||mode=="--review-normal")preview.Review(mode=="--review");if(mode=="--bridge")preview.Seek(SharedView.TransitionPreviewTime);if(mode=="--windows")preview.Seek(SharedView.WindowsClockTime+7);if(mode=="--assistant")preview.Seek(SharedView.AssistantTime+7);if(mode=="--new-scenes")preview.Seek(980+6);if(mode=="--windows-review")preview.Seek(1276+6);if(mode=="--scifi")preview.Seek(224+6);app.Run(preview);
 }
}
sealed class DuetWindow:Window {
 public void Seek(double t){view.RandomOrder=false;view.SetTime(t); }
 public void ControlPreview(){view.ControlPanelReview=true;view.RandomOrder=false;view.SetTime(0);}
 public void Profile(string path){view.ProfilePath=path;}
 public void Review(bool fast){Title="Retro Duet · 交互检查模式";view.ReviewMode=true;view.ReviewFast=fast;view.RandomOrder=false;view.ShowStats=true;view.SetTime(0);}

 readonly Stopwatch watch=Stopwatch.StartNew();Options opts=Options.Load();SharedView view;bool saver,full;Point initial;Rect oldBounds;WindowState oldState;
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
 public DuetWindow():this(true){}
 DuetWindow(bool buildView){Title="Macintosh × Windows · Retro Scenes v3.6.8";Width=1120;Height=730;MinWidth=640;MinHeight=400;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Brushes.Black;if(buildView)Build();KeyDown+=KeysDown;MouseMove+=delegate{if(saver&&watch.Elapsed.TotalSeconds>1.5){var p=Forms.Cursor.Position;if(Math.Abs(p.X-initial.X)>8||Math.Abs(p.Y-initial.Y)>8)Close();}};MouseDown+=delegate{if(saver&&watch.Elapsed.TotalSeconds>1.5)Close();};}
 void HideSystemCursor(){Cursor=Cursors.None;ForceCursor=true;Loaded+=delegate{Mouse.UpdateCursor();};}
 void Build(){view=new SharedView(opts,saver,false);Content=view;}
 public DuetWindow(Drawing.Rectangle bounds):this(false){saver=full=true;HideSystemCursor();WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.Manual;Topmost=true;ShowInTaskbar=false;initial=new Point(Forms.Cursor.Position.X,Forms.Cursor.Position.Y);Build();SourceInitialized+=delegate{SetWindowPos(new WindowInteropHelper(this).Handle,new IntPtr(-1),bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0040);};}
 public DuetWindow(Drawing.Rectangle bounds,FrameworkElement sharedContent):this(false){saver=full=true;HideSystemCursor();WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.Manual;Topmost=true;ShowInTaskbar=false;initial=new Point(Forms.Cursor.Position.X,Forms.Cursor.Position.Y);Content=sharedContent;SourceInitialized+=delegate{SetWindowPos(new WindowInteropHelper(this).Handle,new IntPtr(-1),bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0040);};}
 void KeysDown(object sender,KeyEventArgs e){if(saver||e.Key==Key.Escape){Close();return;}if(e.Key==Key.F11||e.Key==Key.Return){if(!full){oldBounds=new Rect(Left,Top,Width,Height);oldState=WindowState;WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;var bounds=Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).Bounds;SetWindowPos(new WindowInteropHelper(this).Handle,IntPtr.Zero,bounds.X,bounds.Y,bounds.Width,bounds.Height,0x0044);full=true;}else{WindowStyle=WindowStyle.SingleBorderWindow;ResizeMode=ResizeMode.CanResize;Left=oldBounds.Left;Top=oldBounds.Top;Width=oldBounds.Width;Height=oldBounds.Height;WindowState=oldState;full=false;}view.Hint=!full;}
 if(view.ReviewMode&&(e.Key==Key.PageDown||e.Key==Key.PageUp||e.Key==Key.Home)){view.ReviewStep(e.Key==Key.PageDown?1:e.Key==Key.PageUp?-1:0);e.Handled=true;return;}
 if(e.Key==Key.Space)opts.Palette=(opts.Palette+1)%17;if(e.Key==Key.P){opts.Animate=!opts.Animate;view.RefreshScheduling();}if(e.Key==Key.A)opts.Cinematic=!opts.Cinematic;if(e.Key==Key.F2)view.ShowStats=!view.ShowStats;
 if(e.Key==Key.S){view.Suspend=true;view.RefreshScheduling();try{using(var f=new Settings()){new WindowInteropHelper(this);f.ShowDialog(new DialogOwner(new WindowInteropHelper(this).Handle));}opts=Options.Load();view.Options=opts;}finally{view.Suspend=false;view.RefreshScheduling();}}
 }
 sealed class DialogOwner:Forms.IWin32Window {readonly IntPtr h;public DialogOwner(IntPtr h){this.h=h;}public IntPtr Handle{get{return h;}}}
}

sealed partial class SharedView:Grid{
 public Color AmbientColor { get { return ((SolidColorBrush)windows.Paper).Color; } }
 public string ProfilePath;readonly List<double> profileIntervals=new List<double>(),profileCosts=new List<double>();double profilePrevious=-1,profileStartCpu;
 public bool ControlPanelReview;public int RefreshLimit;public Options Options;public bool Hint,ShowStats,Suspend;
 readonly Macintosh.FluidView mac;readonly FluidView windows;readonly Macintosh.Options macOptions=new Macintosh.Options();
 readonly WCanvas plane=new WCanvas{Width=4400,Height=3040,IsHitTestVisible=false};
 readonly Grid sceneLayer=new Grid{ClipToBounds=true,IsHitTestVisible=false};readonly Rectangle transitionVeil=new Rectangle{IsHitTestVisible=false};readonly LinearGradientBrush reveal=new LinearGradientBrush(Colors.Transparent,Colors.White,new Point(0,0),new Point(1,1));WCanvas macWorld,windowsWorld;readonly Image referenceCard=new Image{Width=1200,Height=800,Stretch=Stretch.Fill};readonly Rectangle bootDesktop=new Rectangle{Width=10000,Height=10000,Visibility=Visibility.Hidden,IsHitTestVisible=false};readonly Rectangle bootPaper=new Rectangle{Visibility=Visibility.Hidden,IsHitTestVisible=false};int activeCard=-1;readonly MatrixTransform camera=new MatrixTransform();readonly RadialGradientBrush cornerReveal=new RadialGradientBrush();int transitionKind=-1;
 readonly FrameworkElement mouse;readonly TranslateTransform mouseShift=new TranslateTransform();
 readonly TextBlock hint=new TextBlock(),stats=new TextBlock();
 readonly Stopwatch watch=Stopwatch.StartNew();double motion,lastReal,nextFrame,lastStats,mouseX,mouseY;int frames;bool initializedMouse,precise,mouseHandoff;int mouseOwner=-1;
 readonly System.Windows.Threading.DispatcherTimer idleTimer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};Window hostWindow;bool renderHooked;
 int refresh=60;readonly bool embedded;TimeSpan previous=TimeSpan.MinValue;double cadenceClock=-1,cadenceCredit=1;
 [DllImport("winmm.dll")]static extern uint timeBeginPeriod(uint value);
 [DllImport("winmm.dll")]static extern uint timeEndPeriod(uint value);
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateDC(string driver,string device,string output,IntPtr data);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern int GetDeviceCaps(IntPtr dc,int index);
 struct Shot{public int Owner,Scene;public Point Center,Drift;public double Duration,Angle,Zoom;public Shot(int owner,int scene,double x,double y,double duration,double angle,double zoom,double dx,double dy){Owner=owner;Scene=scene;Center=new Point(x,y);Duration=duration;Angle=angle;Zoom=zoom;Drift=new Point(dx,dy);}}
 static readonly Shot[] shots={
 new Shot(0,7,600,400,22,-16,1,180,-60),
 new Shot(0,0,530,390,76,17,1.08,120,70),
 new Shot(0,1,1540,465,42,17,1.04,260,-80),
 new Shot(0,2,630,1180,44,-15,1.06,220,100),
 new Shot(0,5,1550,1940,40,16,1,250,80),
 new Shot(0,8,600,400,42,-16,1,170,80),
 new Shot(0,10,600,400,28,16,1.12,180,40),
 new Shot(0,6,1100,2660,58,16,1.03,180,70),
 new Shot(0,3,1560,1160,42,-16,1.03,230,80),
 new Shot(0,9,600,400,42,16,1,230,-80),
 new Shot(0,4,650,1940,40,-15,1.02,200,60),
 new Shot(1,7,2800,400,22,-16,1,180,-60),
 new Shot(1,0,2850,465,76,17,1.06,260,120),
 new Shot(1,1,3740,465,44,-15,1.03,230,70),
 new Shot(1,2,2830,1180,44,16,1.05,230,90),
 new Shot(1,5,3750,1940,44,-16,1,220,80),
 new Shot(1,8,2800,400,42,16,1,180,70),
 new Shot(1,12,2800,400,48,-16,1,140,50),
 new Shot(1,6,3300,2660,58,16,1.03,180,70),
 new Shot(1,3,3760,1160,42,-16,1.02,230,80),
 new Shot(1,9,2800,400,42,16,1,230,-80),
 new Shot(1,4,2850,1940,42,-15,1.02,220,70),
 new Shot(0,14,600,400,40,16,1,180,60),
 new Shot(0,15,600,400,40,-16,1,180,60),
 new Shot(0,16,600,400,50,16,1,180,60),
 new Shot(0,17,600,400,46,-16,1,180,60),
 new Shot(0,18,600,400,40,16,1,180,60),
 new Shot(0,19,600,400,40,-16,1,180,60),
 new Shot(0,20,600,400,40,16,1,180,60),
 new Shot(1,14,3750,1940,40,-16,1,180,60),
 new Shot(1,15,2800,400,40,-16,1,180,60),
 new Shot(1,16,2800,400,50,10,.84,100,40),
 new Shot(1,17,2800,400,46,-16,1,180,60),
 new Shot(1,18,2800,400,40,16,1,180,60),
 new Shot(1,19,2800,400,40,-16,1,180,60),
 new Shot(1,20,2800,400,40,16,1,180,60),
 new Shot(1,10,2800,400,28,16,1.05,160,40),
 new Shot(1,21,2800,400,42,-16,1,170,50),
 new Shot(0,22,600,410,42,-12,.96,140,45),
 new Shot(1,22,2800,410,42,12,.96,140,45)
 };

 // One scheduler belongs to the shared renderer, so every monitor sees the same random scene.
 int reviewSerial;public bool ReviewMode,ReviewFast;public bool RandomOrder=true;
 const double ReviewSeconds=9.6;static readonly int[] ReviewOrder={27,37,28,35,16,33,31,30,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,17,18,19,20,21,22,23,24,25,26,29,32,34,36,38,39};
 Shot ReviewAt(double t,out double age){if(ReviewFast){reviewSerial=(int)(Math.Max(0,t)/ReviewSeconds);age=(t%ReviewSeconds)/ReviewSeconds*shots[ReviewOrder[reviewSerial%ReviewOrder.Length]].Duration;return shots[ReviewOrder[reviewSerial%ReviewOrder.Length]];}double cycle=t%Duration;int i=0;while(i<ReviewOrder.Length-1&&cycle>=shots[ReviewOrder[i]].Duration){cycle-=shots[ReviewOrder[i]].Duration;i++;}reviewSerial=(int)(t/Duration)*ReviewOrder.Length+i;age=cycle;return shots[ReviewOrder[i]];}
 public void ReviewStep(int direction){double age;Shot shot=ReviewAt(motion,out age);if(ReviewFast)motion=Math.Max(0,(reviewSerial+(direction==0?0:direction))*ReviewSeconds);else{motion-=age;if(direction>0)motion+=shot.Duration;else if(direction<0){int previous=(reviewSerial+ReviewOrder.Length-1)%ReviewOrder.Length;motion=Math.Max(0,motion-shots[ReviewOrder[previous]].Duration);}}SetTime(motion);}
readonly Random random=new Random(Guid.NewGuid().GetHashCode());
 Shot randomShot;double randomStart,randomEnd;bool randomReady;int lastRandomScene=-1;
 static int ClockIndex(int owner){for(int i=0;i<shots.Length;i++)if(shots[i].Owner==owner&&shots[i].Scene==0)return i;return 0;}
 internal static int RandomSceneIndex(Random rng,bool first,int previous=-1){if(first)return ClockIndex(rng.Next(2));if(previous<0||previous>=shots.Length)return rng.Next(shots.Length);int choice=rng.Next(shots.Length-1);return choice>=previous?choice+1:choice;}
 Shot PickRandom(bool first){lastRandomScene=RandomSceneIndex(random,first,lastRandomScene);return shots[lastRandomScene];}
 Shot RandomShotAt(double t,out double age){if(!randomReady||t<randomStart){randomStart=0;randomShot=PickRandom(true);randomEnd=randomShot.Duration;randomReady=true;}
 while(t>=randomEnd){randomStart=randomEnd;randomShot=PickRandom(false);randomEnd+=randomShot.Duration;}age=t-randomStart;return randomShot;}
 static double ShotTime(int i){double t=0;for(int n=0;n<i;n++)t+=shots[n].Duration;return t;}
 static double Duration{get{return ShotTime(shots.Length);}}
 public static double WindowsStartTime{get{for(int i=0;i<shots.Length;i++)if(shots[i].Owner==1)return ShotTime(i);return 0;}}
 public static double WindowsClockTime{get{for(int i=0;i<shots.Length;i++)if(shots[i].Owner==1&&shots[i].Scene==0)return ShotTime(i);return 0;}}
 public static double AssistantTime{get{for(int i=0;i<shots.Length;i++)if(shots[i].Scene==12)return ShotTime(i);return 0;}}
 public static double TransitionPreviewTime{get{return WindowsStartTime-5;}}
 public static double StopTime(int oldIndex){int[] scenes={0,1,3,5,6,4,2,2,4,6,5,3,1,0};int owner=oldIndex<7?0:1;for(int i=0;i<shots.Length;i++)if(shots[i].Owner==owner&&shots[i].Scene==scenes[oldIndex])return ShotTime(i)+shots[i].Duration*.5;return 0;}
 public static string RouteInfo(){var text=new System.Text.StringBuilder();for(int i=0;i<shots.Length;i++)text.AppendLine(i+" "+(shots[i].Owner==0?"Mac":"Windows")+" scene="+shots[i].Scene+" t="+ShotTime(i).ToString("F3",CultureInfo.InvariantCulture)+" duration="+shots[i].Duration);text.AppendLine("Loop="+Duration.ToString("F3",CultureInfo.InvariantCulture));return text.ToString();}

 static readonly string[] CalculatorValues={"0","1","19","198","1984"};ReferenceMotion.Sample referenceMotion;readonly Rectangle pressFace=new Rectangle{Fill=Brushes.Black,Opacity=.20,IsHitTestVisible=false};readonly Rectangle pressEdge=new Rectangle{Stroke=Brushes.Black,StrokeThickness=3,Fill=Brushes.Transparent,IsHitTestVisible=false};readonly System.Windows.Shapes.Path referenceSelection=new System.Windows.Shapes.Path{Stroke=Brushes.Black,StrokeThickness=3,IsHitTestVisible=false};readonly Rectangle movingKnob=new Rectangle{Width=35,Height=47,Stroke=Brushes.Black,StrokeThickness=3,IsHitTestVisible=false};
 readonly System.Windows.Shapes.Path terminalInk=new System.Windows.Shapes.Path{Fill=Brushes.Black,IsHitTestVisible=false};readonly Rectangle terminalCaret=new Rectangle{Fill=Brushes.Black,Width=16,Height=3,IsHitTestVisible=false};int terminalState=-1;Rect selectedCharacter=Rect.Empty;
 readonly Image macVolumeThumb=new Image{Width=72,Height=30,Stretch=Stretch.Fill,Visibility=Visibility.Hidden,IsHitTestVisible=false,RenderTransform=new TranslateTransform()};
 readonly Image pressedControlImage=new Image{Stretch=Stretch.Fill,IsHitTestVisible=false};readonly System.Windows.Shapes.Path paintDrawing=new System.Windows.Shapes.Path{Fill=Brushes.Black,IsHitTestVisible=false};readonly Rectangle paintSelection=new Rectangle{Stroke=Brushes.Black,StrokeThickness=4,IsHitTestVisible=false};int paintShapeStep=-1;
 readonly Rectangle stateCover=new Rectangle{IsHitTestVisible=false};readonly Image stateImage=new Image{Stretch=Stretch.Fill,IsHitTestVisible=false};readonly Rectangle secondKnob=new Rectangle{Stroke=Brushes.Black,StrokeThickness=3,IsHitTestVisible=false};
 readonly System.Windows.Shapes.Path referenceDynamic=new System.Windows.Shapes.Path{Fill=Brushes.Black,IsHitTestVisible=false};string referenceValue="";
 void UpdateReference(Shot shot,double age){bool calculator=shot.Scene==17;referenceDynamic.Visibility=calculator&&referenceCard.Visibility==Visibility.Visible?Visibility.Visible:Visibility.Hidden;if(!calculator){referenceValue="";return;}
 string value=CalculatorValues[referenceMotion.Digits],key=shot.Owner+":"+value;if(key==referenceValue)return;referenceValue=key;int pitch=shot.Owner==0?6:3;int width=0;foreach(char c in value)width+=(shot.Owner==0?Macintosh.ReferenceFonts.Advance(c):WindowsPixels.Advance(c))*pitch;
 var geometry=new StreamGeometry();using(var pen=geometry.Open()){int x=shot.Owner*2200+(shot.Owner==0?1086:922)-width;foreach(char c in value){string[] rows=shot.Owner==0?Macintosh.ReferenceFonts.Glyph(c):WindowsPixels.Glyph(c);for(int j=0;j<rows.Length;j++)for(int i=0;i<rows[j].Length;i++)if(rows[j][i]=='1'){double xx=x+i*pitch+.225,yy=214+j*pitch+.225;pen.BeginFigure(new Point(xx,yy),true,true);pen.LineTo(new Point(xx+pitch-.55,yy),true,false);pen.LineTo(new Point(xx+pitch-.55,yy+pitch-.55),true,false);pen.LineTo(new Point(xx,yy+pitch-.55),true,false);}x+=(shot.Owner==0?Macintosh.ReferenceFonts.Advance(c):WindowsPixels.Advance(c))*pitch;}}geometry.Freeze();referenceDynamic.Data=geometry;
 }
 // Moving overlays retain their layout slot; only their render transform changes.
 static void Place(Rectangle visual,double x,double y,double w,double h){if(visual.Width!=w)visual.Width=w;if(visual.Height!=h)visual.Height=h;var shift=visual.RenderTransform as TranslateTransform;if(shift==null){shift=new TranslateTransform();visual.RenderTransform=shift;}if(shift.X!=x)shift.X=x;if(shift.Y!=y)shift.Y=y;}
 void UpdateReferenceFeedback(Shot shot){
 bool extra=shot.Scene>=7&&Options.Cinematic&&referenceCard.Visibility==Visibility.Visible;double x=shot.Owner*2200;Rect r=referenceMotion.Button;bool down=extra&&referenceMotion.Down&&!r.IsEmpty;
 bool charMap=extra&&shot.Owner==1&&shot.Scene==18&&!referenceMotion.Selection.IsEmpty;referenceSelection.Visibility=charMap?Visibility.Visible:Visibility.Hidden;if(charMap){referenceSelection.StrokeThickness=1.25;referenceSelection.Opacity=.65;if(selectedCharacter!=referenceMotion.Selection){selectedCharacter=referenceMotion.Selection;var selected=new RectangleGeometry(new Rect(x+selectedCharacter.X+1,selectedCharacter.Y+1,selectedCharacter.Width-2,selectedCharacter.Height-2));selected.Freeze();referenceSelection.Data=selected;}}else selectedCharacter=Rect.Empty;
 bool macPaint=extra&&shot.Owner==0&&shot.Scene==14;paintSelection.Visibility=macPaint&&referenceMotion.ViewState>=1?Visibility.Visible:Visibility.Hidden;paintDrawing.Visibility=macPaint?Visibility.Visible:Visibility.Hidden;if(macPaint){Place(paintSelection,128,301,61,51);int step=(int)Math.Round(referenceMotion.DrawProgress*100);if(step!=paintShapeStep){paintShapeStep=step;var shape=new StreamGeometry();using(var pen=shape.Open()){var used=new System.Collections.Generic.HashSet<int>();double w=340*step/100.0,h=250*step/100.0;for(int i=0;i<180&&step>0;i++){double angle=i*Math.PI/90;int xx=(int)Math.Round((480+w*.5+Math.Cos(angle)*w*.5)/4)*4,yy=(int)Math.Round((260+h*.5+Math.Sin(angle)*h*.5)/4)*4;if(!used.Add(yy*2000+xx))continue;pen.BeginFigure(new Point(xx+.225,yy+.225),true,true);pen.LineTo(new Point(xx+3.45,yy+.225),true,false);pen.LineTo(new Point(xx+3.45,yy+3.45),true,false);pen.LineTo(new Point(xx+.225,yy+3.45),true,false);}}shape.Freeze();paintDrawing.Data=shape;}}else paintShapeStep=-1;
 bool calculatorDown=down&&shot.Scene==17;pressFace.Visibility=down?Visibility.Visible:Visibility.Hidden;pressEdge.Visibility=down&&!calculatorDown?Visibility.Visible:Visibility.Hidden;pressedControlImage.Visibility=calculatorDown?Visibility.Visible:Visibility.Hidden;if(down){pressFace.Fill=calculatorDown?windows.Paper:Brushes.Black;pressFace.Opacity=calculatorDown?1:.2;Place(pressFace,x+r.X,r.Y,r.Width,r.Height);Place(pressEdge,x+r.X,r.Y,r.Width,r.Height);}if(calculatorDown){pressedControlImage.Source=ReferenceContent.PressedCalculator(shot.Owner,referenceMotion.PressedKey);pressedControlImage.Width=r.Width;pressedControlImage.Height=r.Height;WCanvas.SetLeft(pressedControlImage,x+r.X);WCanvas.SetTop(pressedControlImage,r.Y);}
 if(!synchronousCards)ReferenceContent.PrepareInteraction(shot.Owner,shot.Scene);
 var patch=extra&&ReferenceContent.InteractionStates(shot.Owner,shot.Scene)>0?ReferenceContent.Interaction(shot.Owner,shot.Scene,referenceMotion.ViewState,synchronousCards):null;
 stateCover.Visibility=stateImage.Visibility=patch!=null?Visibility.Visible:Visibility.Hidden;if(patch!=null){stateCover.Fill=windows.Paper;Place(stateCover,x+patch.Bounds.X,patch.Bounds.Y,patch.Bounds.Width,patch.Bounds.Height);stateImage.Source=patch.Image;stateImage.Width=patch.Bounds.Width;stateImage.Height=patch.Bounds.Height;WCanvas.SetLeft(stateImage,x+patch.Bounds.X);WCanvas.SetTop(stateImage,patch.Bounds.Y);}else stateImage.Source=null;
 bool slider=extra&&((shot.Owner==1&&shot.Scene==21)||(shot.Owner==0&&shot.Scene==19));bool two=slider&&shot.Owner==0;
 movingKnob.Visibility=slider?Visibility.Visible:Visibility.Hidden;secondKnob.Visibility=Visibility.Hidden;macVolumeThumb.Visibility=two?Visibility.Visible:Visibility.Hidden;
 // Slider tracks are drawn without thumbs in the base texture. Only these live thumbs exist.

 if(slider){movingKnob.Fill=secondKnob.Fill=windows.Paper;if(two){movingKnob.StrokeThickness=0;Place(movingKnob,186,referenceMotion.SliderY,72,30);var shift=(TranslateTransform)macVolumeThumb.RenderTransform;shift.X=186;shift.Y=referenceMotion.SliderY;}else{movingKnob.StrokeThickness=3;Place(movingKnob,x+referenceMotion.SliderX,505,35,47);}}

 }
 static void TerminalLine(StreamGeometryContext pen,string text,double x,double y){foreach(char ch in text){var rows=WindowsPixels.MonoGlyph(ch);for(int r=0;r<rows.Length;r++)for(int c=0;c<rows[r].Length;c++)if(rows[r][c]=='1'){double xx=x+c*2+.225,yy=y+r*2+.225;pen.BeginFigure(new Point(xx,yy),true,true);pen.LineTo(new Point(xx+1.45,yy),true,false);pen.LineTo(new Point(xx+1.45,yy+1.45),true,false);pen.LineTo(new Point(xx,yy+1.45),true,false);}x+=WindowsPixels.MonoAdvance(ch)*2;}}
 void UpdateTerminal(Shot shot,double age){bool show=shot.Owner==1&&shot.Scene==16&&referenceCard.Visibility==Visibility.Visible;terminalInk.Visibility=show?Visibility.Visible:Visibility.Hidden;terminalCaret.Visibility=show&&age%.9<.5?Visibility.Visible:Visibility.Hidden;if(!show){terminalState=-1;return;}int state=referenceMotion.ViewState;string command=@"C:\>"+"rm -rf /".Substring(0,Math.Min(8,state));if(state!=terminalState){terminalState=state;var geometry=new StreamGeometry();using(var pen=geometry.Open()){TerminalLine(pen,command,2200+76,580);if(state>=9){TerminalLine(pen,"Bad command or file name",2200+76,624);TerminalLine(pen,@"C:\>",2200+76,668);}}geometry.Freeze();terminalInk.Data=geometry;}string caretText=state>=9?@"C:\>":command;double left=2200+76;foreach(char ch in caretText)left+=WindowsPixels.MonoAdvance(ch)*2;WCanvas.SetLeft(terminalCaret,left+2);WCanvas.SetTop(terminalCaret,state>=9?693:605);}
 bool synchronousCards;
 void SelectCard(Shot shot,double age){int cardScene=shot.Scene==7&&age<5?11:shot.Scene;if(shot.Scene==12&&age%6>5.72)cardScene=13;int key=shot.Scene>=7&&!(shot.Owner==1&&shot.Scene==14)?shot.Owner*100+cardScene:-1;if(key==activeCard)return;bool extra=key>=0;BitmapSource card=null;if(extra){if(shot.Scene==12){if(synchronousCards){ReferenceContent.Image(shot.Owner,12);ReferenceContent.Image(shot.Owner,13);}else{ReferenceContent.TryImage(shot.Owner,12);ReferenceContent.TryImage(shot.Owner,13);}}card=synchronousCards?ReferenceContent.Image(shot.Owner,cardScene):ReferenceContent.TryImage(shot.Owner,cardScene);if(card==null){macWorld.Visibility=windowsWorld.Visibility=Visibility.Hidden;referenceCard.Visibility=Visibility.Hidden;return;}}activeCard=key;bool boot=shot.Owner==0&&(cardScene==7||cardScene==11);bootDesktop.Visibility=bootPaper.Visibility=boot?Visibility.Visible:Visibility.Hidden;if(boot){bool happy=cardScene==7;bootPaper.Width=happy?125:160;bootPaper.Height=160;bootPaper.Clip=ReferenceContent.BootOutline(happy);WCanvas.SetLeft(bootPaper,happy?537:520);WCanvas.SetTop(bootPaper,285);}macWorld.Visibility=windowsWorld.Visibility=extra?Visibility.Hidden:Visibility.Visible;referenceCard.Visibility=extra?Visibility.Visible:Visibility.Hidden;
 if(extra){referenceCard.Source=card;WCanvas.SetLeft(referenceCard,shot.Owner*2200);WCanvas.SetTop(referenceCard,0);}else referenceCard.Source=null;
 }
 public SharedView(Options options,bool saver,bool embedded){Options=options;this.embedded=embedded;Hint=!saver&&!embedded;ClipToBounds=true;Cursor=Cursors.None;
 cornerReveal.GradientStops.Add(new GradientStop(Colors.Transparent,0));cornerReveal.GradientStops.Add(new GradientStop(Colors.Transparent,.68));cornerReveal.GradientStops.Add(new GradientStop(Colors.White,1));
 SyncOptions();var commonPaper=new SolidColorBrush();mac=new Macintosh.FluidView(macOptions,false,false,commonPaper);windows=new FluidView(options,false,false,commonPaper);
 var mw=mac.DetachWorld();var ww=windows.DetachWorld();macWorld=mw;windowsWorld=ww;WCanvas.SetLeft(ww,2200);plane.Children.Add(mw);plane.Children.Add(ww);plane.RenderTransform=camera;plane.HorizontalAlignment=HorizontalAlignment.Left;plane.VerticalAlignment=VerticalAlignment.Top;sceneLayer.Children.Add(plane);InitializePixelSurface();transitionVeil.Fill=reveal;Children.Add(transitionVeil);referenceCard.Visibility=Visibility.Hidden;var tileBrush=new ImageBrush(ReferenceContent.CheckerTile()){TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,10,10),Stretch=Stretch.Fill};tileBrush.Freeze();bootDesktop.Fill=tileBrush;bootPaper.Fill=windows.Paper;WCanvas.SetLeft(bootDesktop,-5000);WCanvas.SetTop(bootDesktop,-5000);plane.Children.Add(bootDesktop);plane.Children.Add(bootPaper);plane.Children.Add(referenceCard);plane.Children.Add(stateCover);plane.Children.Add(stateImage);plane.Children.Add(referenceDynamic);plane.Children.Add(terminalInk);plane.Children.Add(terminalCaret);plane.Children.Add(paintDrawing);plane.Children.Add(paintSelection);plane.Children.Add(movingKnob);plane.Children.Add(secondKnob);macVolumeThumb.Source=ReferenceContent.ControlThumb();plane.Children.Add(macVolumeThumb);plane.Children.Add(referenceSelection);plane.Children.Add(pressFace);plane.Children.Add(pressEdge);plane.Children.Add(pressedControlImage);AddStoryLayers();plane.Children.Add(controlClock);
 // A single cursor stays above every window on the common drawing plane.
 mouse=windows.CursorVisual;mouse.RenderTransform=mouseShift;WCanvas.SetZIndex(mouse,1000);plane.Children.Add(mouse);Background=windows.Paper;
 var glow=new RadialGradientBrush{Center=new Point(.42,.3),GradientOrigin=new Point(.42,.3),RadiusX=.78,RadiusY=.95};glow.GradientStops.Add(new GradientStop(Color.FromArgb(29,255,248,221),0));glow.GradientStops.Add(new GradientStop(Colors.Transparent,1));glow.Freeze();Children.Add(new Rectangle{Fill=glow,IsHitTestVisible=false});
 hint.Text="F11 全屏 · 空格 换色 · A 自动鼠标 · P 暂停 · S 设置 · F2 帧率";hint.Foreground=Brushes.White;hint.FontSize=12;hint.HorizontalAlignment=HorizontalAlignment.Center;hint.VerticalAlignment=VerticalAlignment.Bottom;hint.Margin=new Thickness(12,0,12,16);Children.Add(hint);
 stats.Foreground=Brushes.White;stats.Background=new SolidColorBrush(Color.FromArgb(140,0,0,0));stats.HorizontalAlignment=HorizontalAlignment.Left;stats.VerticalAlignment=VerticalAlignment.Bottom;stats.Margin=new Thickness(16,0,0,44);stats.Padding=new Thickness(8);Children.Add(stats);
 Loaded+=delegate{var window=Window.GetWindow(this);if(window!=null){var screen=Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);var dc=CreateDC("DISPLAY",screen.DeviceName,null,IntPtr.Zero);if(dc!=IntPtr.Zero){int hz=GetDeviceCaps(dc,116);if(hz>1)refresh=hz;DeleteDC(dc);}}if(RefreshLimit>0)refresh=RefreshLimit;hostWindow=window;if(hostWindow!=null)hostWindow.StateChanged+=HostStateChanged;idleTimer.Tick+=IdleTick;RefreshScheduling();};
 Unloaded+=delegate{CompositionTarget.Rendering-=Rendering;renderHooked=false;idleTimer.Stop();idleTimer.Tick-=IdleTick;if(hostWindow!=null)hostWindow.StateChanged-=HostStateChanged;if(precise){timeEndPeriod(1);precise=false;}};
 }
 void HostStateChanged(object sender,EventArgs e){RefreshScheduling();}
 void IdleTick(object sender,EventArgs e){if(!Suspend)Update(motion,0,false);}
 public void RefreshScheduling(){if(!IsLoaded)return;bool minimized=hostWindow!=null&&hostWindow.WindowState==WindowState.Minimized;bool running=Options.Animate&&!Suspend&&!minimized;
 if(running&&!renderHooked){CompositionTarget.Rendering+=Rendering;renderHooked=true;lastReal=watch.Elapsed.TotalSeconds;nextFrame=lastReal;cadenceClock=-1;cadenceCredit=1;}
 if(!running&&renderHooked){CompositionTarget.Rendering-=Rendering;renderHooked=false;}
 if(running&&!precise)precise=timeBeginPeriod(1)==0;else if(!running&&precise){timeEndPeriod(1);precise=false;}
 if(!running&&!Suspend&&!minimized)idleTimer.Start();else idleTimer.Stop();
 }
 void SyncOptions(){macOptions.FrameRate=Options.FrameRate;macOptions.Palette=Options.Palette;macOptions.Speed=Options.Speed;macOptions.Cinematic=Options.Cinematic;macOptions.Animate=Options.Animate;}
 public string Inspect(){
 Options.Cinematic=true;SyncOptions();var log=new System.Text.StringBuilder();var full=new Rect(0,0,2200,3040);
 foreach(int owner in new[]{0,1}){
 log.AppendLine(owner==0?"Macintosh":"Windows");
 foreach(double phase in new[]{3.0,6.0,10.0,13.6,14.0,20.0}){
 double time=100+phase; if(owner==0){mac.CompositeFrame(time,3,phase,full);mac.CompositeFrame(time+2,3,phase,full);log.AppendLine("dialog p="+phase+": "+mac.FrameState);}
 else{windows.CompositeFrame(time,3,phase,full);windows.CompositeFrame(time+2,3,phase,full);log.AppendLine("dialog p="+phase+": "+windows.FrameState);}
 }
 if(owner==0){mac.CompositeFrame(140,-1,0,full);log.AppendLine("after leaving: "+mac.FrameState);mac.CompositeFrame(150,2,6,full);mac.CompositeFrame(152,2,6,full);log.AppendLine("volume drag: "+mac.FrameState);}
 else{windows.CompositeFrame(140,-1,0,full);log.AppendLine("after leaving: "+windows.FrameState);windows.CompositeFrame(150,2,6,full);windows.CompositeFrame(152,2,6,full);log.AppendLine("volume drag: "+windows.FrameState);foreach(double p in new[]{5.0,8.0,12.0,15.0}){windows.CompositeFrame(160+p,1,p,full);windows.CompositeFrame(162+p,1,p,full);log.AppendLine("keyboard drag p="+p+": "+windows.FrameState);}}
 }
 return log.ToString();
 }
 public Point AuditPointer{get{return new Point(mouseX,mouseY);}}
 public string AuditState{get{return referenceValue;}}
 public void SetTime(double t){double dt=Math.Max(0,Math.Min(.25,t-motion));motion=t;synchronousCards=true;try{Update(t,dt,!initializedMouse);}finally{synchronousCards=false;} }
 void Rendering(object sender,EventArgs args){var e=(RenderingEventArgs)args;if(e.RenderingTime==previous)return;previous=e.RenderingTime;double now=watch.Elapsed.TotalSeconds,dt=now-lastReal;
 if(Suspend||(hostWindow!=null&&hostWindow.WindowState==WindowState.Minimized)){lastReal=now;return;}
 int target=Options.Animate?Math.Min(refresh,embedded?15:Options.FrameRate):2;double renderClock=now;if(cadenceClock<0||renderClock<cadenceClock){cadenceClock=renderClock;cadenceCredit=1;}cadenceCredit+=Math.Max(0,renderClock-cadenceClock)*Math.Max(1,target);cadenceClock=renderClock;if(cadenceCredit<.999)return;cadenceCredit=Math.Min(1,Math.Max(0,cadenceCredit-1));lastReal=now;
 if(Options.Animate)motion+=Math.Min(dt,.25)*Options.Speed/100.0*1.2;long updateStart=Stopwatch.GetTimestamp();Update(motion,Math.Min(dt,.25),false);double cost=(Stopwatch.GetTimestamp()-updateStart)*1000.0/Stopwatch.Frequency;frames++;
 if(ProfilePath!=null&&now>=2){var proc=Process.GetCurrentProcess();if(profilePrevious<0)profileStartCpu=proc.TotalProcessorTime.TotalMilliseconds;else{profileIntervals.Add((now-profilePrevious)*1000);profileCosts.Add(cost);}profilePrevious=now;if(now>=12&&profileCosts.Count>0){profileIntervals.Sort();double cpu=proc.TotalProcessorTime.TotalMilliseconds-profileStartCpu,total=0,sum=0;foreach(double n in profileIntervals)total+=n;foreach(double n in profileCosts)sum+=n;proc.Refresh();File.WriteAllText(ProfilePath,string.Format(CultureInfo.InvariantCulture,"UI_fps={0:F2}\nUpdate_ms={1:F4}\nP95_interval_ms={2:F3}\nProcess_cpu_ms_per_s={3:F2}\nWorking_MB={4:F1}\nPrivate_MB={5:F1}\nTier={6}\nDisplay_Hz={7}\nSamples={8}\nUI callbacks, not GPU presentation timestamps.\n",profileIntervals.Count*1000/total,sum/profileCosts.Count,profileIntervals[(int)(profileIntervals.Count*.95)],cpu/10,proc.WorkingSet64/1048576.0,proc.PrivateMemorySize64/1048576.0,RenderCapability.Tier>>16,refresh,profileCosts.Count));ProfilePath=null;Window.GetWindow(this).Close();return;}}
 if(now-lastStats>=1){if(ShowStats)stats.Text=string.Format(CultureInfo.InvariantCulture,"{0:F0} fps | Mac × Windows | one camera",frames/(now-lastStats));lastStats=now;frames=0;}
 }
 // Quintic easing has zero endpoint velocity and acceleration. Extra slide
 // displacement therefore joins the underlying camera without a speed jump.
 static double CameraEase(double value){double u=Math.Max(0,Math.Min(1,value));return u*u*u*(u*(u*6-15)+10);}
 // Keep a slow drift during transitions, then gently accelerate through the shot.
 static double CameraTravel(double value){double u=Math.Max(0,Math.Min(1,value));return .4*u+.6*CameraEase(u);}
 static double CameraScale(Shot shot,double progress){
 // Distinct shot sizes: slow approach, retreat and close detail. No zoom reset at a wipe.
 double u=Math.Max(0,Math.Min(1,progress)),curve=CameraEase(u),a,b;
 switch(shot.Scene){case 7:a=.92;b=1.12;break;case 0:a=1.04;b=1.48;break;case 1:a=1.28;b=1.24;break;case 2:a=1.18;b=1.23;break;case 8:a=1.08;b=1.40;break;case 19:a=1.20;b=1.26;break;case 14:a=1.35;b=1.03;break;default:int kind=(shot.Scene+shot.Owner*3)%3;a=kind==0?1.30:1.02;b=kind==0?1.05:kind==1?1.28:1.12;break;}
 return a+(b-a)*curve;
 }
 void Update(double t,double dt,bool direct){
 if(ActualWidth<=0||ActualHeight<=0)return;
 SyncOptions();windows.Options=Options;
 double age;Shot stop;if(ControlPanelReview){stop=shots[27];age=t%stop.Duration;}else if(ReviewMode){stop=ReviewAt(t,out age);}else if(RandomOrder)stop=RandomShotAt(t,out age);else{double cycle=((t%Duration)+Duration)%Duration;int shotIndex=0;age=cycle;while(shotIndex<shots.Length-1&&age>=shots[shotIndex].Duration){age-=shots[shotIndex].Duration;shotIndex++;}stop=shots[shotIndex];}stop=ReferenceCameraShot(stop);double u=age/stop.Duration;
 referenceMotion=ReferenceMotion.At(stop.Owner,stop.Scene,age,stop.Duration);bool blank=age<.6||age>stop.Duration-.6;SelectCard(stop,age);UpdateReference(stop,age);UpdateReferenceFeedback(stop);UpdateTerminal(stop,age);UpdateStory(stop,age);UpdateControlClock(stop);
 double revealProgress=age<4.8?(age-.6)/4.2:age>stop.Duration-4.8?(stop.Duration-.6-age)/4.2:1;
 if(blank){sceneLayer.Visibility=Visibility.Hidden;transitionVeil.Visibility=Visibility.Hidden;}
 else{sceneLayer.Visibility=Visibility.Visible;if(revealProgress>=1)transitionVeil.Visibility=Visibility.Hidden;else{transitionVeil.Visibility=Visibility.Visible;double r=Math.Max(0,Math.Min(1,revealProgress));r=r*r*(3-2*r);bool entering=age<4.8;int kind=(stop.Scene*3+stop.Owner*5+(entering?0:2))%7;if(!entering){double angle=stop.Angle*Math.PI/180,dx=-stop.Drift.X*Math.Cos(angle)+stop.Drift.Y*.84*Math.Sin(angle),dy=-stop.Drift.X*Math.Sin(angle)-stop.Drift.Y*.84*Math.Cos(angle);kind=Math.Abs(dx)>=Math.Abs(dy)?(dx<0?2:7):(dy<0?3:8);}transitionVeil.Opacity=1;
 if(kind==5){transitionVeil.Fill=windows.Paper;transitionVeil.Opacity=1-r;}
 else if(kind==6){transitionVeil.Fill=cornerReveal;cornerReveal.Center=cornerReveal.GradientOrigin=new Point(entering?.12:.88,entering?.85:.15);double radius=.001+2.2*r;cornerReveal.RadiusX=cornerReveal.RadiusY=radius;}
 else{transitionVeil.Fill=reveal;double edge=-.2+1.4*r;Point a,b;if(kind==0){a=new Point(edge-.18,edge-.18);b=new Point(edge+.18,edge+.18);}else if(kind==1){a=new Point(1-edge+.18,edge-.18);b=new Point(1-edge-.18,edge+.18);}else if(kind==2){a=new Point(edge-.18,.5);b=new Point(edge+.18,.5);}else if(kind==3){a=new Point(.5,1-edge+.18);b=new Point(.5,1-edge-.18);}else if(kind==7){a=new Point(1-edge+.18,.5);b=new Point(1-edge-.18,.5);}else if(kind==8){a=new Point(.5,1-edge+.18);b=new Point(.5,1-edge-.18);}else{a=new Point(1-edge+.18,1-edge+.18);b=new Point(1-edge-.18,1-edge-.18);}reveal.StartPoint=a;reveal.EndPoint=b;}transitionKind=kind;}}
 double travel=CameraTravel(u);var center=new Point(stop.Center.X+stop.Drift.X*1.12*(travel-.5),stop.Center.Y+stop.Drift.Y*1.12*(travel-.5));
 double framing=CameraScale(stop,u),tilt=stop.Angle,shear=-7;
 double zoom=Math.Max(ActualWidth/1120,ActualHeight/720)*stop.Zoom*framing;var m=Matrix.Identity;m.Translate(-center.X,-center.Y);m.Scale(zoom,zoom*.84);m.Skew(shear,0);m.Rotate(tilt);m.Translate(ActualWidth/2,ActualHeight/2);if(!blank&&revealProgress<1&&transitionKind==5){double fade=1-CameraEase(revealProgress);var direction=m.Transform(new Vector(-stop.Drift.X,-stop.Drift.Y));if(direction.Length>.001){direction.Normalize();double distance=ActualWidth*.16*fade*(age<4.8?-1:1);m.Translate(direction.X*distance,direction.Y*distance);}}camera.Matrix=m;UpdatePixelTransition(m,stop,age,blank,revealProgress);
 var inverse=m;inverse.Invert();Rect visible=Rect.Transform(new Rect(0,0,ActualWidth,ActualHeight),inverse);visible.Inflate(16,16);
 bool paintAgain=stop.Owner==1&&stop.Scene==14;bool native=(stop.Scene<7||paintAgain)&&!blank;double local=u*24;
 double actorTime=synchronousCards?t:ReviewMode?reviewSerial*80+age:double.NaN;
 macWorld.Visibility=native&&stop.Owner==0?Visibility.Visible:Visibility.Hidden;windowsWorld.Visibility=native&&stop.Owner==1?Visibility.Visible:Visibility.Hidden;
 mac.CompositeFrame(t,native&&stop.Owner==0?stop.Scene:-1,local,native&&stop.Owner==0?visible:Rect.Empty,actorTime);
 Rect winVisible=native&&stop.Owner==1?visible:Rect.Empty;if(!winVisible.IsEmpty)winVisible.Offset(-2200,0);windows.CompositeFrame(t,native&&stop.Owner==1?(paintAgain?5:stop.Scene):-1,local,winVisible,actorTime);
 // A colored gradient produces the same veil directly, without an opacity-mask surface.
 if(transitionVeil.Visibility==Visibility.Visible){Color paperColor=((SolidColorBrush)windows.Paper).Color;Color clear=paperColor;clear.A=0;if(reveal.GradientStops[1].Color!=paperColor){reveal.GradientStops[0].Color=clear;reveal.GradientStops[1].Color=paperColor;cornerReveal.GradientStops[0].Color=clear;cornerReveal.GradientStops[1].Color=clear;cornerReveal.GradientStops[2].Color=paperColor;}}
 Point target;if(stop.Scene>=7&&!paintAgain){target=referenceMotion.Pointer;target.X+=stop.Owner*2200;}else{target=stop.Owner==0?mac.CursorPosition:windows.CursorPosition;if(stop.Owner==1)target.X+=2200;}
 if(mouseOwner!=stop.Owner){mouseHandoff=false;mouseOwner=stop.Owner;mouseX=target.X;mouseY=target.Y;}
 if(!initializedMouse||direct){mouseX=target.X;mouseY=target.Y;initializedMouse=true;}else if(mouseHandoff){double dx=target.X-mouseX,dy=target.Y-mouseY,length=Math.Sqrt(dx*dx+dy*dy),step=260*dt;if(length<=step){mouseX=target.X;mouseY=target.Y;mouseHandoff=false;}else{mouseX+=dx*step/length;mouseY+=dy*step/length;}}else{mouseX=target.X;mouseY=target.Y;}
 mouseShift.X=mouseX;mouseShift.Y=mouseY;mouse.Visibility=Options.Cinematic&&referenceMotion.Show&&stop.Scene!=7&&stop.Scene!=10&&(stop.Scene<7||paintAgain||referenceCard.Visibility==Visibility.Visible)?Visibility.Visible:Visibility.Collapsed;
 if(ReviewMode)hint.Text="检查版 · "+(ReviewFast?"8秒/场景":"常规节奏")+" · "+(stop.Owner==0?"Mac":"Windows")+" scene "+stop.Scene+" · PgDn 下一场 / PgUp 上一场 / Home 重播 / P 暂停";
 hint.Visibility=Hint?Visibility.Visible:Visibility.Collapsed;stats.Visibility=ShowStats?Visibility.Visible:Visibility.Collapsed;
 }
}
}
