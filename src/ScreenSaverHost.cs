using System;using System.Globalization;using System.Runtime.InteropServices;using System.Windows;using System.Windows.Interop;using System.Windows.Threading;
namespace RetroWindows {
 enum SaverLaunchKind {Windowed,Settings,Preview,ScreenSaver,Invalid}
 struct SaverLaunch {public SaverLaunchKind Kind;public IntPtr Parent;
 public static SaverLaunch Parse(string[] args,bool scr){var result=new SaverLaunch{Kind=args.Length==0&&scr?SaverLaunchKind.Settings:SaverLaunchKind.Windowed};if(args.Length==0)return result;string arg=args[0].Trim();if(arg.Length<2||arg.StartsWith("--"))return result;if(arg[0]!='/'&&arg[0]!='-')return result;string rest="";int end=arg.IndexOfAny(new[]{':',' ','\t'});string flag=(end<0?arg:arg.Substring(0,end)).ToLowerInvariant();if(end>=0)rest=arg.Substring(end+1).Trim();if(rest.Length==0&&args.Length>1)rest=args[1].Trim();
 if(flag=="/s"||flag=="-s"){result.Kind=rest.Length==0?SaverLaunchKind.ScreenSaver:SaverLaunchKind.Invalid;return result;}
 if(flag=="/c"||flag=="-c"){result.Kind=SaverLaunchKind.Settings;return result;}
 if(flag=="/p"||flag=="-p"){long value;bool ok=rest.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?long.TryParse(rest.Substring(2),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out value):long.TryParse(rest,NumberStyles.Integer,CultureInfo.InvariantCulture,out value);result.Kind=ok&&value>0?SaverLaunchKind.Preview:SaverLaunchKind.Invalid;if(result.Kind==SaverLaunchKind.Preview)result.Parent=new IntPtr(value);return result;}
 result.Kind=SaverLaunchKind.Invalid;return result;}
 }
 // A preview is born as a native child. It can never become a detached WPF top-level window.
 sealed class ScreenSaverPreview:IDisposable {
 readonly IntPtr parent;readonly Action finished;readonly HwndSource source;readonly SharedView view;readonly DispatcherTimer timer;bool closing,disposed;int width=-1,height=-1;
 [StructLayout(LayoutKind.Sequential)]struct RECT{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out RECT r);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
 public static bool IsValidParent(IntPtr h){RECT r;return h!=IntPtr.Zero&&IsWindow(h)&&GetClientRect(h,out r)&&r.Right>r.Left&&r.Bottom>r.Top;}
 public IntPtr Handle{get{return source.Handle;}}
 public ScreenSaverPreview(IntPtr host,Action onFinished){if(!IsValidParent(host))throw new ArgumentException("Invalid screensaver preview parent.");parent=host;finished=onFinished;RECT r;GetClientRect(host,out r);var parameters=new HwndSourceParameters("Retro Duet embedded preview"){ParentWindow=host,WindowStyle=unchecked((int)0x56000000),PositionX=0,PositionY=0,Width=r.Right-r.Left,Height=r.Bottom-r.Top};source=new HwndSource(parameters);source.AddHook(Messages);view=new SharedView(Options.Load(),false,true){Hint=false,RefreshLimit=15};source.RootVisual=view;Resize(r);timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=Tick;timer.Start();}
 IntPtr Messages(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled){if(message==0x0002||message==0x0082)Finish();return IntPtr.Zero;}
 void Finish(){if(closing||disposed)return;closing=true;if(timer!=null)timer.Stop();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate{if(finished!=null)finished();}));}
 void Tick(object sender,EventArgs e){CheckParent();}
 public void CheckParent(){if(closing||disposed)return;if(source.IsDisposed){Finish();return;}RECT r;if(!IsWindow(parent)||!GetClientRect(parent,out r)){Finish();return;}if(r.Right<=r.Left||r.Bottom<=r.Top){Finish();return;}Resize(r);}
 void Resize(RECT r){int w=r.Right-r.Left,h=r.Bottom-r.Top;if(w==width&&h==height)return;width=w;height=h;SetWindowPos(source.Handle,IntPtr.Zero,0,0,w,h,0x0014);var dip=source.CompositionTarget.TransformFromDevice.Transform(new Point(w,h));view.Width=dip.X;view.Height=dip.Y;}
 public void Dispose(){if(disposed)return;disposed=true;if(timer!=null){timer.Stop();timer.Tick-=Tick;}if(!source.IsDisposed){source.RemoveHook(Messages);source.RootVisual=null;source.Dispose();}}
 }
 sealed class SaverInputState {bool armed;Point origin;uint input;
 public bool Update(double elapsed,Point point,uint lastInput){if(elapsed<.8)return false;if(!armed){armed=true;origin=point;input=lastInput;return false;}return input!=lastInput||Math.Abs(point.X-origin.X)>8||Math.Abs(point.Y-origin.Y)>8;}
 }
}
