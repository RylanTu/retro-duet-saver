using System;using System.Collections.Generic;using System.IO;using System.Net;using System.Net.Sockets;using System.Text;
namespace RetroDuetLighting {static class BootLightingChecks {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Main(){var allow=new HashSet<string>();var empty=new string[0];
 var history=new[]{"192.168.1.181","192.168.1.182"};
 Check(BootService.SelectTargets(empty,history,allow).Length==2,"zero discovery retains historical targets");
 Check(BootService.SelectTargets(new[]{history[0]},history,allow).Length==2,"merge current and history without duplicates");
 allow.Add(history[1]);var filtered=BootService.SelectTargets(new[]{history[0]},history,allow);Check(filtered.Length==1&&filtered[0]==history[1],"allowlist applies to both sources");
 Check(BootService.SelectTargets(empty,empty,allow).Length==0,"allowlist alone never invents discovered targets");
 allow.Clear();Check(BootService.SelectTargets(empty,new[]{"bad","::1","0.0.0.0","255.255.255.255","239.255.255.250","127.0.0.1"},allow).Length==0,"reject invalid and non-unicast history");
 string folder=Path.Combine(Path.GetTempPath(),"RetroDuetLighting-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"last-discovered.txt");try{
 Check(BootService.ReadHistory(path,delegate(string s){throw new Exception(s);}).Length==0,"missing history is empty");File.WriteAllLines(path,history);Check(BootService.ReadHistory(path,delegate(string s){throw new Exception(s);}).Length==2,"old history file compatible");
 bool logged=false;using(var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){Check(BootService.ReadHistory(path,delegate(string s){logged=true;}).Length==0&&logged,"unreadable history logs and falls back");}
 }finally{File.Delete(path);Directory.Delete(folder);}
 using(var receiver=new UdpClient(new IPEndPoint(IPAddress.Loopback,0)))using(var sender=new UdpClient()){
 receiver.Client.ReceiveTimeout=1000;int port=((IPEndPoint)receiver.Client.LocalEndPoint).Port;string error;
 Check(BootService.TrySendOff(sender,IPAddress.Loopback,port,out error)&&error==null,"UDP send accepted");var from=new IPEndPoint(IPAddress.Any,0);string json=Encoding.UTF8.GetString(receiver.Receive(ref from));Check(json=="{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":0}}}","off only; no white or on");sender.Close();Check(!BootService.TrySendOff(sender,IPAddress.Loopback,port,out error)&&!string.IsNullOrEmpty(error),"send failure reason returned");
 }
 }
}}
