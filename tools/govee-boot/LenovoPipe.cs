using System;using System.Collections.Generic;using System.Diagnostics;using System.IO;using System.IO.Pipes;using System.Runtime.InteropServices;using System.Security.AccessControl;using System.Security.Principal;using System.Threading;using System.Web.Script.Serialization;
namespace RetroDuetLighting {
 static class LenovoPipe {
  internal const string PipeName="RetroDuetLighting.Lenovo.v1";
  static readonly string Journal=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"RetroDuetLighting","lenovo-session.json");
  static readonly object gate=new object();static readonly ManualResetEvent stop=new ManualResetEvent(false);static Thread thread;static NamedPipeServerStream current;static Action<string> log;
  [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetNamedPipeClientProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe,out uint pid);
  public static void Start(Action<string> logger){log=logger;thread=new Thread(Run){IsBackground=true,Name="Lenovo official screen sync"};thread.Start();}
  public static void Stop(){stop.Set();lock(gate){if(current!=null)current.Dispose();}if(thread!=null)thread.Join(4000);}
  static int Read(NamedPipeServerStream pipe,int timeout){byte[] b=new byte[1];var result=pipe.BeginRead(b,0,1,null,null);int ready=WaitHandle.WaitAny(new[]{stop,result.AsyncWaitHandle},timeout);if(ready!=1)return -1;try{return pipe.EndRead(result)==1?b[0]:-1;}finally{result.AsyncWaitHandle.Close();}}
  static PipeSecurity Security(){var acl=new PipeSecurity();acl.SetAccessRuleProtection(true,false);
   acl.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid,null),PipeAccessRights.FullControl,AccessControlType.Deny));
   foreach(var sid in new[]{WellKnownSidType.LocalSystemSid,WellKnownSidType.BuiltinAdministratorsSid})acl.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(sid,null),PipeAccessRights.FullControl,AccessControlType.Allow));
   acl.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.InteractiveSid,null),PipeAccessRights.ReadWrite,AccessControlType.Allow));return acl;
  }
  static bool ValidClient(NamedPipeServerStream pipe){uint pid;if(!GetNamedPipeClientProcessId(pipe.SafePipeHandle,out pid))return false;
   try{using(var process=Process.GetProcessById((int)pid)){if(process.SessionId==0)return false;string name=process.ProcessName;return name.Equals("RetroDuetSaver",StringComparison.OrdinalIgnoreCase);}}catch(ArgumentException){return false;}catch(System.ComponentModel.Win32Exception){return false;}
  }
  static Dictionary<string,object> Pending(){return (Dictionary<string,object>)new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Journal));}
  static bool Restore(OfficialEngine engine,Dictionary<string,object> before){for(int i=0;i<3;i++)try{engine.Restore(before);File.Delete(Journal);log("Lenovo original profile and effect restored.");return true;}catch(Exception e){log("Lenovo restore attempt: "+e.Message);Thread.Sleep(200);}return false;}
  static void Run(){
   try{
    if(File.Exists(Journal))using(var engine=new OfficialEngine())if(!Restore(engine,Pending()))return;
    while(!stop.WaitOne(0)){
     using(var pipe=new NamedPipeServerStream(PipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous,64,64,Security())){
      lock(gate){if(stop.WaitOne(0))return;current=pipe;}
      try{
       pipe.WaitForConnection();if(!ValidClient(pipe)||Read(pipe,5000)!=1)continue;
       using(var engine=new OfficialEngine()){
        var before=engine.Snapshot();if(Convert.ToInt32(before["index"])!=1){pipe.WriteByte(3);continue;}
        string text=new JavaScriptSerializer().Serialize(before);Directory.CreateDirectory(Path.GetDirectoryName(Journal));
        using(var f=new FileStream(Journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){byte[] data=System.Text.Encoding.UTF8.GetBytes(text);f.Write(data,0,data.Length);f.Flush(true);}
        try{
         if(stop.WaitOne(0))continue;
         engine.Begin(before);pipe.WriteByte(1);pipe.Flush();log("Lenovo official screen sync active; no custom color frames.");
         Read(pipe,Timeout.Infinite);
        }finally{if(!Restore(engine,before))stop.Set();}
       }
      }catch(Exception e){if(!stop.WaitOne(0))log("Lenovo session: "+e.Message);}
      finally{lock(gate){current=null;}}
     }
    }
   }catch(Exception e){log("Lenovo listener: "+e.Message);}
  }
 }
}
