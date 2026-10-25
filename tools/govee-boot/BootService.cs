using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace RetroDuetLighting {
 static class BootService {
  const string Name = "RetroDuetLighting";
  static readonly string DataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RetroDuetLighting");
  static readonly string Marker = Path.Combine(DataRoot, "shutdown.flag");
  static readonly ManualResetEvent cancel = new ManualResetEvent(false), done = new ManualResetEvent(false);
  static readonly object gate = new object(), logGate = new object();
  static readonly HashSet<string> devices = new HashSet<string>();
  static readonly HashSet<string> allowed = new HashSet<string>();
  static readonly MainProc mainCallback = ServiceMain;
  static readonly HandlerProc handlerCallback = Control;
  static IntPtr statusHandle;
  static Thread bootWorker;
  static int stopping, discoverySeconds = 120;
  const string Scan = "{\"msg\":{\"cmd\":\"scan\",\"data\":{\"account_topic\":\"reserve\"}}}";
  const string White = "{\"msg\":{\"cmd\":\"colorwc\",\"data\":{\"color\":{\"r\":255,\"g\":255,\"b\":255},\"colorTemInKelvin\":5400}}}";
  const string Bright = "{\"msg\":{\"cmd\":\"brightness\",\"data\":{\"value\":100}}}";
  const string On = "{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":1}}}";
  const string Off = "{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":0}}}";
  delegate void MainProc(int argc, IntPtr argv);
  delegate int HandlerProc(int control, int eventType, IntPtr eventData, IntPtr context);
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct Entry { [MarshalAs(UnmanagedType.LPWStr)] public string Name; public MainProc Proc; }
  [StructLayout(LayoutKind.Sequential)] struct Status { public int Type, State, Accepted, ExitCode, SpecificExit, Checkpoint, WaitHint; }
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool StartServiceCtrlDispatcher([In] Entry[] entries);
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr RegisterServiceCtrlHandlerEx(string name, HandlerProc handler, IntPtr context);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool SetServiceStatus(IntPtr handle, ref Status status);
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr OpenSCManager(string machine,string database,int access);
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr OpenService(IntPtr manager,string name,int access);
  [DllImport("advapi32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool ChangeServiceConfig2(IntPtr service,int level,ref int timeout);
  [DllImport("advapi32.dll")] static extern bool CloseServiceHandle(IntPtr handle);
  static int ConfigureTimeout() {
   IntPtr manager=OpenSCManager(null,null,1);if(manager==IntPtr.Zero)return Marshal.GetLastWin32Error();
   IntPtr service=IntPtr.Zero;
   try{service=OpenService(manager,Name,2);if(service==IntPtr.Zero)return Marshal.GetLastWin32Error();int timeout=10000;return ChangeServiceConfig2(service,7,ref timeout)?0:Marshal.GetLastWin32Error();}
   finally{if(service!=IntPtr.Zero)CloseServiceHandle(service);CloseServiceHandle(manager);}
  }
  static int Main(string[] args) { if(args.Length==1&&args[0]=="--configure")return ConfigureTimeout(); return StartServiceCtrlDispatcher(new[]{new Entry{Name=Name, Proc=mainCallback},new Entry()}) ? 0 : Marshal.GetLastWin32Error(); }
  static void Report(int state, int exitCode) {
   var status = new Status {Type=0x10,State=state,Accepted=state==4 ? 0x105 : 0,ExitCode=exitCode,Checkpoint=state==2||state==3 ? 1 : 0,WaitHint=state==2||state==3 ? 5000 : 0};
   if(statusHandle!=IntPtr.Zero) SetServiceStatus(statusHandle, ref status);
  }
  static void ServiceMain(int argc, IntPtr argv) {
   statusHandle=RegisterServiceCtrlHandlerEx(Name,handlerCallback,IntPtr.Zero); if(statusHandle==IntPtr.Zero)return;
   Report(2,0);
   try {
    Directory.CreateDirectory(DataRoot);
    if(File.Exists(Marker))File.Delete(Marker);
    LoadConfig();
    bootWorker=new Thread(Boot){IsBackground=true,Name="Govee boot discovery"};
    // Start the worker before accepting stop controls, eliminating a start/stop race.
    bootWorker.Start(); Report(4,0); done.WaitOne();
   } catch(Exception error) {Log(error.ToString());cancel.Set();Report(1,1064);}
  }
  static int Control(int control,int eventType,IntPtr data,IntPtr context) {
   if(control==4){Report(Interlocked.CompareExchange(ref stopping,0,0)==0?4:3,0);return 0;}
   if(control!=1&&control!=5&&control!=15)return 120;
   if(Interlocked.Exchange(ref stopping,1)==0){
    Report(3,0); cancel.Set();
    new Thread(delegate(){Finish(control!=1);}){IsBackground=true,Name="Govee shutdown off"}.Start();
   }
   return 0;
  }
  static void LoadConfig() {
   string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.json");
   if(!File.Exists(path))return;
   var root=new JavaScriptSerializer().DeserializeObject(File.ReadAllText(path)) as Dictionary<string,object>;
   object value;
   if(root!=null&&root.TryGetValue("DeviceIps",out value)){
    var list=value as object[];if(list==null)throw new FormatException("DeviceIps must be an array.");
    foreach(object item in list){IPAddress ip;if(!IPAddress.TryParse(Convert.ToString(item,CultureInfo.InvariantCulture),out ip)||ip.AddressFamily!=AddressFamily.InterNetwork)throw new FormatException("DeviceIps must contain IPv4 addresses.");allowed.Add(ip.ToString());}
   }
   if(root!=null&&root.TryGetValue("DiscoverySeconds",out value))discoverySeconds=Math.Max(10,Math.Min(600,Convert.ToInt32(value,CultureInfo.InvariantCulture)));
  }
  static void Send(UdpClient udp,IPAddress ip,int port,string message) {
   try {byte[] bytes=Encoding.UTF8.GetBytes(message);udp.Send(bytes,bytes.Length,new IPEndPoint(ip,port));}
   catch(SocketException){}catch(ObjectDisposedException){}
  }
  static void Multicast(UdpClient udp) {
   foreach(NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()){
    if(nic.OperationalStatus!=OperationalStatus.Up||nic.NetworkInterfaceType==NetworkInterfaceType.Loopback||nic.NetworkInterfaceType==NetworkInterfaceType.Tunnel)continue;
    foreach(UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses){
     if(cancel.WaitOne(0))return;
     if(address.Address.AddressFamily!=AddressFamily.InterNetwork)continue;
     try{udp.Client.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.MulticastInterface,address.Address.GetAddressBytes());Send(udp,IPAddress.Parse("239.255.255.250"),4001,Scan);}catch(SocketException){}
    }
   }
  }
  static void Boot() {
   var elapsed=Stopwatch.StartNew();
   Log("Service started: waiting up to "+discoverySeconds+" seconds for Govee LAN devices.");
   while(!cancel.WaitOne(0)&&elapsed.Elapsed.TotalSeconds<discoverySeconds){
    try{ScanOnce();}catch(SocketException error){Log("Discovery retry: "+error.SocketErrorCode);}catch(NetworkInformationException){}catch(Exception error){Log(error.Message);}
    lock(gate){if(devices.Count>0)break;}
    if(cancel.WaitOne(3000))return;
   }
   lock(gate)Log("Boot discovery complete: "+devices.Count+" device(s). Discovery port released; service idle.");
  }
  static void ScanOnce() {
   using(var udp=new UdpClient(AddressFamily.InterNetwork)){
    udp.ExclusiveAddressUse=true;udp.Client.SendTimeout=100;udp.Client.ReceiveTimeout=100;
    udp.Client.Bind(new IPEndPoint(IPAddress.Any,4002));udp.MulticastLoopback=false;
    var timer=Stopwatch.StartNew();var parser=new JavaScriptSerializer{MaxJsonLength=8192,RecursionLimit=12};
    Multicast(udp);
    while(timer.Elapsed.TotalSeconds<2&&!cancel.WaitOne(50)){
     // Bound work even if another LAN host sends malformed packets continuously.
     for(int n=0;n<32&&timer.Elapsed.TotalSeconds<2&&!cancel.WaitOne(0)&&udp.Available>0;n++){
      var sender=new IPEndPoint(IPAddress.Any,0);byte[] bytes=udp.Receive(ref sender);if(bytes.Length>8192)continue;
      try{
       var root=parser.DeserializeObject(Encoding.UTF8.GetString(bytes)) as Dictionary<string,object>;object value;
       if(root==null||!root.TryGetValue("msg",out value))continue;
       var msg=value as Dictionary<string,object>;
       if(msg==null||!msg.TryGetValue("cmd",out value)||!object.Equals(value,"scan")||!msg.TryGetValue("data",out value))continue;
       var data=value as Dictionary<string,object>;if(data==null||!data.ContainsKey("device")||!data.ContainsKey("sku"))continue;
       string ip=sender.Address.ToString();if(allowed.Count>0&&!allowed.Contains(ip))continue;
       lock(gate){
        if(cancel.WaitOne(0)||devices.Contains(ip))continue;
        devices.Add(ip);
        for(int attempt=0;attempt<3&&!cancel.WaitOne(0);attempt++){
         Send(udp,sender.Address,4003,White);Send(udp,sender.Address,4003,Bright);Send(udp,sender.Address,4003,On);
         if(attempt<2)cancel.WaitOne(60);
        }
        File.WriteAllLines(Path.Combine(DataRoot,"last-discovered.txt"),new List<string>(devices).ToArray());
        Log("Boot white 5400 K / 100% sent to "+ip);
       }
      }catch(ArgumentException){}catch(InvalidOperationException){}
     }
    }
   }
  }
  static void Finish(bool shutdown) {
   try {
    if(shutdown)try{File.WriteAllText(Marker,DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture));}catch(IOException error){Log(error.Message);}catch(UnauthorizedAccessException error){Log(error.Message);}
    if(bootWorker!=null)bootWorker.Join(1500);
    string[] targets;lock(gate){targets=new List<string>(devices).ToArray();}
    using(var udp=new UdpClient(AddressFamily.InterNetwork)){
     udp.Client.SendTimeout=100;
     // No bind to discovery port 4002 during shutdown. Repeat over ~2 seconds.
     for(int attempt=0;attempt<8&&targets.Length>0;attempt++){
      foreach(string ip in targets)Send(udp,IPAddress.Parse(ip),4003,Off);
      if(attempt<7)Thread.Sleep(250);
     }
    }
    Log((shutdown?"System shutdown":"Service stop")+": off sent to "+targets.Length+" device(s).");
   }catch(Exception error){Log(error.ToString());}
   finally{Report(1,0);done.Set();}
  }
  static void Log(string message) {
   lock(logGate)try{
    Directory.CreateDirectory(DataRoot);string path=Path.Combine(DataRoot,"service.log");
    if(File.Exists(path)&&new FileInfo(path).Length>131072)File.WriteAllText(path,"");
    File.AppendAllText(path,DateTime.Now.ToString("s",CultureInfo.InvariantCulture)+" "+message+Environment.NewLine);
   }catch(IOException){}catch(UnauthorizedAccessException){}
  }
 }
}
