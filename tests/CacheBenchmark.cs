using System;using System.Diagnostics;using System.IO;using System.Globalization;using System.Collections.Generic;using System.Windows;using System.Windows.Media;using System.Windows.Threading;
namespace RetroWindows {static class CacheBenchmark {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 static readonly double[] scenes={24,311,813,750,1205,1285};
 [STAThread]static void Main(string[] args){SetProcessDPIAware();var all=Stopwatch.StartNew();var app=new Application();var opts=new Options{FrameRate=60,Speed=100,Cinematic=true,Animate=true,GoveeLighting=false};var view=new SharedView(opts,false,false){RandomOrder=false,Hint=false};var window=new Window{Title="Retro Duet · 缓存性能实测（自动结束）",Width=1600,Height=1000,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterScreen,Content=view};
 var rows=new List<string>();rows.Add("second,scene,cpu_total_percent,working_mb,private_mb,ui_callbacks,elapsed_s");var process=Process.GetCurrentProcess();double previousCpu=0,previousTime=0;int ticks=0,callbacks=0;double construction=all.Elapsed.TotalMilliseconds;
 CompositionTarget.Rendering+=delegate{callbacks++;};
 var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
 window.Loaded+=delegate{view.SetTime(scenes[0]);process.Refresh();previousCpu=process.TotalProcessorTime.TotalSeconds;previousTime=all.Elapsed.TotalSeconds;timer.Start();};
 timer.Tick+=delegate{process.Refresh();double now=all.Elapsed.TotalSeconds,cpu=process.TotalProcessorTime.TotalSeconds,dt=now-previousTime;rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F5},{3:F3},{4:F3},{5},{6:F4}",ticks,ticks/8,(cpu-previousCpu)/dt/Environment.ProcessorCount*100,process.WorkingSet64/1048576.0,process.PrivateMemorySize64/1048576.0,callbacks,dt));callbacks=0;previousTime=now;previousCpu=cpu;ticks++;if(ticks>=48){timer.Stop();File.WriteAllLines(args[0],rows.ToArray());File.WriteAllText(args[0]+".meta",string.Format(CultureInfo.InvariantCulture,"construction_ms={0:F2}\ntotal_cpu_ms={1:F2}\nwall_s={2:F2}\ntier={3}\nlogical_cpus={4}\n",construction,cpu*1000,now,RenderCapability.Tier>>16,Environment.ProcessorCount));window.Close();return;}if(ticks%8==0)view.SetTime(scenes[ticks/8]);};app.Run(window);
 }
}}
