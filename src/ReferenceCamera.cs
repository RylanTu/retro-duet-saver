using System;
using System.Windows;
namespace RetroWindows {sealed partial class SharedView {
 // Steady reference-inspired passes: fixed tilt, one travel direction, no tracking spring.
 // Edit camera metadata only; scene duration, actor timing and transition identity stay intact.
 static Shot ReferenceCameraShot(Shot shot){
 switch(shot.Scene){
 case 19: if(shot.Owner==0){shot.Center=new Point(600,405);shot.Drift=new Point(330,-150);shot.Zoom=1.20;}else{shot.Drift=new Point(260,45);}break;
 case 1: shot.Drift=new Point(330,15);break;
 case 2: shot.Drift=new Point(45,260);shot.Zoom*=1.08;break;
 case 4: shot.Drift=new Point(280,-35);break;
 case 5: shot.Drift=new Point(230,150);break;
 case 7: shot.Drift=new Point(280,-30);break;
 case 10: shot.Drift=new Point(110,170);break;
 case 18: shot.Drift=new Point(300,-140);break;
 case 14: shot.Drift=new Point(190,120);break;
 case 15: shot.Drift=new Point(240,-90);break;
 case 17: shot.Center.Y-=45;shot.Drift=new Point(100,-160);break;
 case 20: shot.Drift=new Point(300,40);break;
 case 21: shot.Drift=new Point(260,10);break;
 case 6: shot.Drift=new Point(230,20);break;
 case 22: shot.Drift=new Point(65,100);break;
 }
 return shot;
 }
}}
