using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
namespace RetroWindows {
static class LightingChecks {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Main(){
 Check(GoveeLanLighting.DiscoveryIdleMilliseconds(0,30,0)==0,"listen immediately after scan");
 Check(GoveeLanLighting.DiscoveryIdleMilliseconds(1.9,30,0)==0,"keep response window open");
 Check(GoveeLanLighting.DiscoveryIdleMilliseconds(2,30,0)==28000,"sleep after response window");
 Check(GoveeLanLighting.DiscoveryIdleMilliseconds(30,30,0)==0,"wake for next scan");
 Check(GoveeLanLighting.DiscoveryIdleMilliseconds(10,30,1)==0,"active devices never enter discovery idle");
 Check(GoveeLanLighting.DiscoveryIdleMilliseconds(1799,1820,0)==1000,"idle respects half-hour deadline");
 using(var receiver=new UdpClient(new IPEndPoint(IPAddress.Loopback,0)))using(var sender=new UdpClient()){
 receiver.Client.ReceiveTimeout=1000;var target=(IPEndPoint)receiver.Client.LocalEndPoint;
 var finish=typeof(GoveeLanLighting).GetMethod("Finish",BindingFlags.Static|BindingFlags.NonPublic);
 finish.Invoke(null,new object[]{sender,new[]{target},true});
 for(int i=0;i<3;i++){var from=new IPEndPoint(IPAddress.Any,0);string json=Encoding.UTF8.GetString(receiver.Receive(ref from));Check(json.Contains("\"turn\"")&&json.Contains("\"value\":0"),"off retry "+i);}
 Check(receiver.Available==0,"shutdown sends no white or on commands");
 }
 }
}}
