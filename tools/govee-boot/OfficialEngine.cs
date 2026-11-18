using System;using System.Collections;using System.Collections.Generic;using System.IO;using System.Runtime.InteropServices;using System.Text;using System.Web.Script.Serialization;
namespace RetroDuetLighting {
 sealed class OfficialEngine:IDisposable {
  readonly System.Diagnostics.Process process;readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=1048576};bool disposed;
  public OfficialEngine(){
   string root=AppDomain.CurrentDomain.BaseDirectory;
   var info=new System.Diagnostics.ProcessStartInfo(Path.Combine(root,@"runtime\python.exe"),"-I -S \""+Path.Combine(root,"engine_worker.py")+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=root};
   process=System.Diagnostics.Process.Start(info);
   try{var hello=(Dictionary<string,object>)json.DeserializeObject(Read());if(!hello.ContainsKey("ready"))throw new InvalidOperationException("Official engine worker unavailable");}catch{Dispose();throw;}
  }
  string Read(){var pending=process.StandardOutput.ReadLineAsync();if(!pending.Wait(8000))throw new TimeoutException("Official lighting engine timeout");if(pending.Result==null)throw new InvalidOperationException("Official engine worker exited");return pending.Result;}
  public Dictionary<string,object> Call(string action,object payload){
   switch(action){case "Get-LightingProfileIndex":case "Get-LightingProfileDetails":case "Get-BrightnessLevel":case "Get-YLogoStatus":case "Get-GetDynamicLighting":case "Set-LightingProfileDetails":case "Set-LightingProfileIndex":break;default:throw new ArgumentException("Unsupported lighting operation");}
   process.StandardInput.WriteLine(json.Serialize(new{action=action,payload=payload??new object()}));process.StandardInput.Flush();
   var reply=(Dictionary<string,object>)json.DeserializeObject(Read());if(!object.Equals(reply["ok"],true))throw new InvalidOperationException(Convert.ToString(reply["error"]));return (Dictionary<string,object>)reply["items"];
  }
  public int Index(){return Convert.ToInt32(Call("Get-LightingProfileIndex",null)["profileId"]);}
  static object Arg(object body){return new Dictionary<string,object>{{"F2015",body}};}
  public Dictionary<string,object> Snapshot(){
   int index=Index();var d=Call("Get-LightingProfileDetails",Arg(new{profileId=index}));
   return new Dictionary<string,object>{{"index",index},{"profile",new Dictionary<string,object>{{"profileId",d["profileId"]},{"layers",d["layers"]}}},{"brightness",Call("Get-BrightnessLevel",null)},{"logo",Call("Get-YLogoStatus",null)},{"dynamic",Call("Get-GetDynamicLighting",null)}};
  }
  public void Begin(Dictionary<string,object> before){
   var profile=(Dictionary<string,object>)before["profile"];var keys=new SortedSet<int>();
   foreach(Dictionary<string,object> layer in (IEnumerable)profile["layers"])foreach(object key in (IEnumerable)layer["keys"])keys.Add(Convert.ToInt32(key));
   Call("Set-LightingProfileDetails",Arg(new{profileId=before["index"],layers=new[]{new{layerId=1,keys=new List<int>(keys).ToArray(),animationConfig=new{animationId=1005,speed=0,clockwise=0,direction=0,colorType=0,colorSize=0,colorList=new object[0]}}}}));
  }
  public void Restore(Dictionary<string,object> before){
   Call("Set-LightingProfileDetails",Arg(before["profile"]));Call("Set-LightingProfileIndex",Arg(new{profileId=before["index"]}));
   for(int i=0;i<4;i++){System.Threading.Thread.Sleep(150);if(Index()!=Convert.ToInt32(before["index"]))throw new InvalidOperationException("Original profile not restored");}
   if(json.Serialize(Snapshot())!=json.Serialize(before))throw new InvalidOperationException("Original effect configuration differs");
  }
  public void Dispose(){if(!disposed){disposed=true;try{process.StandardInput.Close();if(!process.WaitForExit(1000)){process.Kill();process.WaitForExit();}}finally{process.Dispose();}}}
 }
}
