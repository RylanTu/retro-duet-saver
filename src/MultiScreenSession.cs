using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms=System.Windows.Forms;
namespace RetroWindows {
// Exactly one live scene and one viewport cache. Reuse the target cache across every display.
sealed class MultiScreenSession:IDisposable {
 readonly Dispatcher dispatcher=Dispatcher.CurrentDispatcher;
 readonly SharedView source;readonly Grid sceneSurface=new Grid{ClipToBounds=true,IsHitTestVisible=false};
 readonly Options options;
 readonly List<DuetWindow> windows=new List<DuetWindow>();
 readonly BitmapCacheBrush sharedBrush;
 readonly BitmapCache sceneCache=new BitmapCache();
 readonly DispatcherTimer topologyTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};
 Viewbox sourceHost;
 bool rebuilding,closing,disposed;
 [DllImport("gdi32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateDC(string driver,string device,string output,IntPtr data);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern int GetDeviceCaps(IntPtr dc,int index);
 public MultiScreenSession(){
 options=Options.Load();source=new SharedView(options,true,false){Hint=false,ShowStats=false};
 sceneSurface.Children.Add(source);
 // No brush-owned cache: BitmapCacheBrush consumes the single cache on its target.
 sharedBrush=new BitmapCacheBrush(sceneSurface){AutoLayoutContent=false,BitmapCache=null};
 topologyTimer.Tick+=RebuildAfterChange;
 SystemEvents.DisplaySettingsChanged+=DisplayChanged;
 }
 public void Show(){Rebuild();}
 void DisplayChanged(object sender,EventArgs e){
 if(disposed||closing)return;
 dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(delegate{if(disposed||closing)return;topologyTimer.Stop();topologyTimer.Start();}));
 }
 void RebuildAfterChange(object sender,EventArgs e){topologyTimer.Stop();if(!disposed&&!closing)Rebuild();}
 int Refresh(Forms.Screen screen){var dc=CreateDC("DISPLAY",screen.DeviceName,null,IntPtr.Zero);if(dc==IntPtr.Zero)return 60;try{int hz=GetDeviceCaps(dc,116);return hz>1?hz:60;}finally{DeleteDC(dc);}}
 void Rebuild(){
 if(rebuilding||closing)return;
 var screens=Forms.Screen.AllScreens;if(screens.Length==0)return;
 rebuilding=true;var previous=new List<DuetWindow>(windows);
 try{
 // Keep a visible host alive while transferring the same source to a replacement host.
 if(sourceHost!=null)sourceHost.Child=null;
 windows.Clear();
 Forms.Screen primary=screens[0];foreach(var screen in screens)if(screen.Primary){primary=screen;break;}
 double width=1120,height=720;
 // The original camera uses this same logical viewport; center cropping reproduces each aspect ratio without distortion.
 source.Width=width;source.Height=height;sceneSurface.Width=width;sceneSurface.Height=height;sceneSurface.CacheMode=screens.Length>1?sceneCache:null;
 double dpiX=1,dpiY=1;using(var g=System.Drawing.Graphics.FromHwnd(IntPtr.Zero)){dpiX=g.DpiX/96.0;dpiY=g.DpiY/96.0;}
 double scale=.25;int fastest=1;
 foreach(var screen in screens){double fill=Math.Max(screen.Bounds.Width/(width*dpiX),screen.Bounds.Height/(height*dpiY));scale=Math.Max(scale,fill);fastest=Math.Max(fastest,Refresh(screen));}
 var maxTexture=RenderCapability.MaxHardwareTextureSize;
 double hardwareLimit=Math.Min(maxTexture.Width/Math.Max(1,width*dpiX),maxTexture.Height/Math.Max(1,height*dpiY));
 if(hardwareLimit>0)scale=Math.Min(scale,hardwareLimit);
 // Software rendering should not allocate a huge high-resolution cache.
 if((RenderCapability.Tier>>16)==0)scale=Math.Min(scale,1920.0/Math.Max(1,width*dpiX));
 sceneCache.RenderAtScale=Math.Max(.25,scale);
 source.RefreshLimit=Math.Min(options.FrameRate,fastest);
 sourceHost=new Viewbox{Stretch=Stretch.UniformToFill,StretchDirection=StretchDirection.Both,Child=sceneSurface,ClipToBounds=true};
 var main=new DuetWindow(primary.Bounds,sourceHost);windows.Add(main);main.Closed+=AnyClosed;
 foreach(var screen in screens){
 if(screen.DeviceName==primary.DeviceName)continue;
 // UniformToFill keeps circles and glyphs undistorted on portrait and ultrawide panels.
 var painted=new Rectangle{Width=width,Height=height,Fill=sharedBrush,IsHitTestVisible=false};
 var mirror=new Viewbox{Stretch=Stretch.UniformToFill,StretchDirection=StretchDirection.Both,Child=painted,ClipToBounds=true};
 var host=new DuetWindow(screen.Bounds,mirror);windows.Add(host);host.Closed+=AnyClosed;
 }
 foreach(var host in windows)host.Show();
 foreach(var old in previous){old.Closed-=AnyClosed;old.Close();}
 source.RefreshScheduling();
 }finally{rebuilding=false;}
 }
 void AnyClosed(object sender,EventArgs e){
 if(rebuilding||closing)return;closing=true;
 topologyTimer.Stop();foreach(var host in windows.ToArray()){host.Closed-=AnyClosed;host.Close();}
 windows.Clear();
 }
 public void Dispose(){
 if(disposed)return;disposed=true;closing=true;
 SystemEvents.DisplaySettingsChanged-=DisplayChanged;
 topologyTimer.Stop();topologyTimer.Tick-=RebuildAfterChange;
 foreach(var host in windows.ToArray()){host.Closed-=AnyClosed;host.Close();}
 windows.Clear();sharedBrush.Target=null;sceneSurface.CacheMode=null;sourceHost=null;
 }
}
}
