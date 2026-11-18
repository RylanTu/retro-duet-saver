using System;using System.Diagnostics;using System.IO;using System.IO.Pipes;using System.Runtime.InteropServices;using System.Text;using System.Threading;
namespace RetroWindows {
 sealed class LenovoOfficialSync:IDisposable {
  readonly ManualResetEvent stop=new ManualResetEvent(false);readonly object gate=new object();NamedPipeClientStream pipe;Thread worker;
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetNamedPipeServerProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe,out uint pid);
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,uint id);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder path,ref int size);
  [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
  static void Log(string message){try{string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RetroDuetSaver");Directory.CreateDirectory(root);File.AppendAllText(Path.Combine(root,"lenovo-sync.log"),DateTime.Now.ToString("s")+" "+message+Environment.NewLine);}catch(IOException){}catch(UnauthorizedAccessException){}}
  [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr OpenSCManager(string machine,string database,uint access);
  [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr OpenService(IntPtr manager,string name,uint access);
  [DllImport("advapi32.dll",SetLastError=true)]static extern bool QueryServiceStatusEx(IntPtr service,int level,byte[] buffer,int size,out int needed);
  [DllImport("advapi32.dll")]static extern bool CloseServiceHandle(IntPtr handle);
  static bool TrustedServer(NamedPipeClientStream p){
   uint pid;if(!GetNamedPipeServerProcessId(p.SafePipeHandle,out pid))return false;
   IntPtr manager=OpenSCManager(null,null,1);if(manager==IntPtr.Zero)return false;IntPtr service=IntPtr.Zero;
   try{
    service=OpenService(manager,"RetroDuetLighting",4);if(service==IntPtr.Zero)return false;
    byte[] status=new byte[36];int needed;if(!QueryServiceStatusEx(service,0,status,status.Length,out needed)||BitConverter.ToUInt32(status,28)!=pid||BitConverter.ToInt32(status,4)!=4)return false;
    using(var key=Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\RetroDuetLighting")){
     string configured=key==null?null:key.GetValue("ImagePath") as string;
     string root=Environment.GetEnvironmentVariable("ProgramW6432")??Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
     string expected=Path.Combine(root,@"RetroDuetLighting\RetroDuetLighting.exe");
     return configured!=null&&string.Equals(Environment.ExpandEnvironmentVariables(configured).Trim().Trim('"'),expected,StringComparison.OrdinalIgnoreCase);
    }
   }finally{if(service!=IntPtr.Zero)CloseServiceHandle(service);CloseServiceHandle(manager);}
  }
  public LenovoOfficialSync(){worker=new Thread(Run){IsBackground=true,Name="Official Lenovo sync connection"};worker.Start();}
  void Run(){try{using(var p=new NamedPipeClientStream(".","RetroDuetLighting.Lenovo.v1",PipeDirection.InOut,PipeOptions.Asynchronous)){
   lock(gate){if(stop.WaitOne(0))return;pipe=p;}p.Connect(1000);if(!TrustedServer(p))throw new IOException("Unexpected lighting service");if(stop.WaitOne(0))return;
   p.WriteByte(1);p.Flush();byte[] b=new byte[1];var result=p.BeginRead(b,0,1,null,null);
   int ready=WaitHandle.WaitAny(new[]{stop,result.AsyncWaitHandle},8000);if(ready!=1)return;int count=p.EndRead(result);result.AsyncWaitHandle.Close();
   if(count!=1||b[0]!=1){Log("Official sync unavailable; original lighting retained.");return;}
   Log("Official screen sync active.");stop.WaitOne();
  }}catch(TimeoutException){Log("Lighting service is not installed or not running.");}catch(Exception e){if(!stop.WaitOne(0))Log(e.Message);}finally{lock(gate){pipe=null;}}}
  public void Dispose(){stop.Set();lock(gate){if(pipe!=null)pipe.Dispose();}}
 }
}
