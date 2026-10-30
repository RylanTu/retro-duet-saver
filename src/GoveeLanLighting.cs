using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Threading;

namespace RetroWindows {
 // One controller per saver session, never per monitor or rendered frame.
 sealed class GoveeLanLighting : IDisposable {
  const double LimitSeconds = 30 * 60;
  readonly Stopwatch lifetime = Stopwatch.StartNew();
  readonly AutoResetEvent wake = new AutoResetEvent(false);
  readonly Thread worker;
  readonly DispatcherTimer sampler;
  readonly Func<Color> readColor;
  readonly double saturation;
  readonly HashSet<string> allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  volatile bool stopping, finished;
  readonly Dispatcher ui = Dispatcher.CurrentDispatcher;
  int targetRgb, disposed, colorReady;
  static readonly IPEndPoint Discovery = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 4001);
  const string Scan = "{\"msg\":{\"cmd\":\"scan\",\"data\":{\"account_topic\":\"reserve\"}}}";
  const string On = "{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":1}}}";
  const string Off = "{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":0}}}";
  const string FullBrightness = "{\"msg\":{\"cmd\":\"brightness\",\"data\":{\"value\":100}}}";
  const string White = "{\"msg\":{\"cmd\":\"colorwc\",\"data\":{\"color\":{\"r\":255,\"g\":255,\"b\":255},\"colorTemInKelvin\":5400}}}";

  public GoveeLanLighting(Func<Color> colorSource, int saturationPercent) {
   readColor = colorSource;
   saturation = Math.Max(0, Math.Min(300, saturationPercent)) / 100.0;
   string filter = Environment.GetEnvironmentVariable("RETRO_DUET_GOVEE_IPS");
   if (!string.IsNullOrWhiteSpace(filter)) foreach (string ip in filter.Split(new[]{',',';',' '}, StringSplitOptions.RemoveEmptyEntries)) allowed.Add(ip.Trim());
   // Sample only after discovering a device.
   sampler = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(100) };
   sampler.Tick += Sample;
   worker = new Thread(Run) { IsBackground = true, Name = "Govee LAN ambient lighting" };
   if (Environment.GetEnvironmentVariable("RETRO_DUET_GOVEE") == "0") return;
   AppDomain.CurrentDomain.ProcessExit += ProcessExit;
   worker.Start();
  }

  void Sample(object sender, EventArgs args) {
   if (lifetime.Elapsed.TotalSeconds >= LimitSeconds) { if (sampler != null) sampler.Stop(); return; }
   Color c = DeepenColor(readColor());
   Interlocked.Exchange(ref targetRgb, (c.R << 16) | (c.G << 8) | c.B);
   Interlocked.Exchange(ref colorReady, 1);
  }
  // Stretch RGB chroma around its peak: preserve hue and peak brightness while
  // removing more of the gray/white component. Neutral colors remain neutral.
  Color DeepenColor(Color color) {
   double peak = Math.Max(color.R, Math.Max(color.G, color.B));
   double range = peak - Math.Min(color.R, Math.Min(color.G, color.B));
   if (range < 1) return color;
   double stretch = Math.Min(saturation, peak / range);
   return Color.FromRgb((byte)Math.Round(peak - (peak - color.R) * stretch),
    (byte)Math.Round(peak - (peak - color.G) * stretch),
    (byte)Math.Round(peak - (peak - color.B) * stretch));
  }
  void ProcessExit(object sender, EventArgs args) { StopWorker(); }
  void StopWorker() {
   stopping = true;
   try { wake.Set(); } catch (ObjectDisposedException) { }
   if (worker.IsAlive && Thread.CurrentThread != worker) worker.Join(1500);
  }
  public void Dispose() {
   if (Interlocked.Exchange(ref disposed, 1) != 0) return;
   sampler.Stop();
   sampler.Tick -= Sample;
   StopWorker();
   AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
   if (!worker.IsAlive) wake.Dispose();
   // The background worker owns its socket and wait handle through final restoration.
  }
  static string Rgb(int rgb) {
   return "{\"msg\":{\"cmd\":\"colorwc\",\"data\":{\"color\":{\"r\":" + ((rgb >> 16) & 255).ToString(CultureInfo.InvariantCulture) +
    ",\"g\":" + ((rgb >> 8) & 255).ToString(CultureInfo.InvariantCulture) + ",\"b\":" + (rgb & 255).ToString(CultureInfo.InvariantCulture) + "},\"colorTemInKelvin\":0}}}";
  }
  static void Send(UdpClient udp, IPEndPoint endpoint, string json) {
   try { byte[] bytes = Encoding.UTF8.GetBytes(json); udp.Send(bytes, bytes.Length, endpoint); }
   catch (SocketException) { } catch (ObjectDisposedException) { }
  }
  static void Discover(UdpClient udp) {
   // Explicitly use each live IPv4 interface; the default route may belong to a VPN.
   foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
    foreach (UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses) {
     if (address.Address.AddressFamily != AddressFamily.InterNetwork) continue;
     try {
      udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.Address.GetAddressBytes());
      Send(udp, Discovery, Scan);
     } catch (SocketException) { }
    }
   }
  }
  void Receive(UdpClient udp, Dictionary<string,IPEndPoint> devices, JavaScriptSerializer parser) {
   for (int n = 0; n < 32 && !stopping && lifetime.Elapsed.TotalSeconds < LimitSeconds && udp.Available > 0; n++) {
    IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
    byte[] bytes = udp.Receive(ref source);
    if (bytes.Length > 8192 || source.Address.AddressFamily != AddressFamily.InterNetwork) continue;
    try {
     var root = parser.DeserializeObject(Encoding.UTF8.GetString(bytes)) as Dictionary<string,object>;
     object value; if (root == null || !root.TryGetValue("msg", out value)) continue;
     var msg = value as Dictionary<string,object>;
     if (msg == null || !msg.TryGetValue("cmd", out value) || !object.Equals(value, "scan") || !msg.TryGetValue("data", out value)) continue;
     var data = value as Dictionary<string,object>;
     if (data == null || !data.ContainsKey("device") || !data.ContainsKey("sku")) continue;
     // Use the packet source, not an arbitrary IP embedded in received JSON.
     string ip = source.Address.ToString();
     if (devices.ContainsKey(ip) || (allowed.Count > 0 && !allowed.Contains(ip))) continue;
     var endpoint = new IPEndPoint(source.Address, 4003);
     devices.Add(ip, endpoint);
     if(devices.Count==1) ui.BeginInvoke(new Action(delegate { if(!stopping&&!finished){Sample(null,EventArgs.Empty);sampler.Start();} }));
     Log("Discovered " + ip + " (" + Convert.ToString(data["sku"], CultureInfo.InvariantCulture) + ")");
     if(Interlocked.CompareExchange(ref colorReady,0,0)!=0) Send(udp, endpoint, Rgb(Interlocked.CompareExchange(ref targetRgb, 0, 0)));
     Send(udp, endpoint, On);
    } catch (ArgumentException) { } catch (InvalidOperationException) { }
   }
  }
  static void Finish(UdpClient udp, IEnumerable<IPEndPoint> devices, bool turnOff) {
   // UDP has no delivery guarantee. Repeat final state without blocking the UI for long.
   for (int attempt = 0; attempt < 3; attempt++) {
    if (!turnOff && SystemShutdownRequested()) turnOff = true;
    foreach (IPEndPoint endpoint in devices) {
     if (turnOff) Send(udp, endpoint, Off);
     else { Send(udp, endpoint, White); Send(udp, endpoint, FullBrightness); Send(udp, endpoint, On); }
    }
    if (attempt < 2) Thread.Sleep(80);
   }
  }
  static bool SystemShutdownRequested() {
   try {
    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RetroDuetLighting", "shutdown.flag");
    if (!File.Exists(path)) return false;
    double age = (DateTime.UtcNow - File.GetLastWriteTimeUtc(path)).TotalSeconds;
    return age >= -5 && age < 120;
   } catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
  }
  void Run() {
   UdpClient udp = null; Mutex owner = null; bool owns = false, timedOut = false;
   var devices = new Dictionary<string,IPEndPoint>();
   try {
    owner = new Mutex(false, @"Local\RetroDuetSaver.GoveeLan");
    try { owns = owner.WaitOne(0); } catch (AbandonedMutexException) { owns = true; }
    if (!owns) return;
    // Exclusive port ownership avoids competing with another Govee controller.
    udp = new UdpClient(AddressFamily.InterNetwork);
    udp.ExclusiveAddressUse = true;
    udp.Client.ReceiveTimeout = 100;
    udp.Client.SendTimeout = 150;
    udp.Client.Bind(new IPEndPoint(IPAddress.Any, 4002));
    udp.Ttl = 1;
    udp.MulticastLoopback = false;
    var parser = new JavaScriptSerializer { MaxJsonLength = 8192, RecursionLimit = 12 };
    int initial = Interlocked.CompareExchange(ref targetRgb, 0, 0), sent = -1;
    double r = (initial >> 16) & 255, g = (initial >> 8) & 255, b = initial & 255;
    double nextScan = 0, previous = lifetime.Elapsed.TotalSeconds, lastSend = -1;
    while (!stopping) {
     double now = lifetime.Elapsed.TotalSeconds;
     if (now >= LimitSeconds) { timedOut = true; break; }
     try {
      if (now >= nextScan) { nextScan = now + 30; Discover(udp); }
      // Listen briefly after each scan; otherwise sleep until the next discovery.
      if(devices.Count==0 && now < nextScan-28) { wake.WaitOne((int)Math.Max(1,Math.Min(nextScan-now,LimitSeconds-now)*1000)); continue; }
      Receive(udp, devices, parser);
     } catch (SocketException) {
      // A sleeping/disconnected device may produce ICMP/WSAECONNRESET on Windows.
      // Keep the session alive so the remaining lights still obey the deadline.
     } catch (NetworkInformationException) { }
     if(devices.Count==0 || Interlocked.CompareExchange(ref colorReady,0,0)==0){wake.WaitOne(100);continue;}
     int target = Interlocked.CompareExchange(ref targetRgb, 0, 0);
     if(sent==-1){r=(target>>16)&255;g=(target>>8)&255;b=target&255;}
     double blend = 1 - Math.Exp(-Math.Min(1, now - previous) / .25); previous = now;
     r += (((target >> 16) & 255) - r) * blend;
     g += (((target >> 8) & 255) - g) * blend;
     b += ((target & 255) - b) * blend;
     int color = ((int)Math.Round(r) << 16) | ((int)Math.Round(g) << 8) | (int)Math.Round(b);
     if (color != sent || now - lastSend >= 2) {
      string json = Rgb(color);
      foreach (IPEndPoint endpoint in devices.Values) Send(udp, endpoint, json);
      sent = color; lastSend = now;
     }
     wake.WaitOne(100);
    }
   } catch (Exception error) { Log(error.GetType().Name + ": " + error.Message); }
   finally {
    finished=true;
    if(!ui.HasShutdownStarted) ui.BeginInvoke(new Action(delegate { sampler.Stop(); }));
    if (udp != null) {
     bool off = timedOut || lifetime.Elapsed.TotalSeconds >= LimitSeconds;
     if (devices.Count > 0) { Finish(udp, devices.Values, off); Log(off ? "30-minute limit: lights off." : "Saver exit: 5400 K, 100% brightness."); }
     udp.Close();
    }
    if (owns) owner.ReleaseMutex();
    if (owner != null) owner.Dispose();
    wake.Dispose();
   }
  }
  static void Log(string message) {
   try {
    string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroDuetSaver");
    Directory.CreateDirectory(folder);
    string file = Path.Combine(folder, "govee-lan.log");
    if (File.Exists(file) && new FileInfo(file).Length > 131072) File.WriteAllText(file, "");
    File.AppendAllText(file, DateTime.Now.ToString("s", CultureInfo.InvariantCulture) + " " + message + Environment.NewLine);
   } catch (IOException) { } catch (UnauthorizedAccessException) { }
  }
 }
}
