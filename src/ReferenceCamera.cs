using System;
using System.Windows;
namespace RetroWindows {sealed partial class SharedView {
 // Steady reference-inspired passes: fixed tilt, one travel direction, no tracking spring.
 // Edit camera metadata only; scene duration, actor timing and transition identity stay intact.
 static class StoryFraming {
 internal static readonly double[] CenterX={Center(0,macThought),Center(1,winThought)};
 static double Center(int owner,string[] lines){
 int longest=0;
 foreach(string line in lines){int width=0;foreach(char ch in line)width+=3*StoryAdvance(owner,ch);longest=Math.Max(longest,width);}
 return owner*2200+(owner==0?245:300)+longest*.5;
 }}
 // Endpoints use the drawing coordinates of the subject on its own desktop.
 static Shot FrameSubject(Shot shot,double fromX,double fromY,double toX,double toY){
 shot.Center=new Point(shot.Owner*2200+(fromX+toX)*.5,(fromY+toY)*.5);
 shot.Drift=new Point(toX-fromX,toY-fromY);
 return shot;
 }
 static Shot ReferenceCameraShot(Shot shot){
 bool mac=shot.Owner==0;
 switch(shot.Scene){
 case 0: // Calendar or title first; finish on the actual clock digits.
 return mac?FrameSubject(shot,475,320,535,373):FrameSubject(shot,540,395,740,506);
 case 1: if(!mac)shot.Zoom=.90;
 return mac?FrameSubject(shot,1300,345,1730,392):FrameSubject(shot,1525,390,1780,609);
 case 2: return mac?FrameSubject(shot,510,1110,710,1280):FrameSubject(shot,560,1080,750,1275);
 case 3: return FrameSubject(shot,1460,1050,1620,1290);
 case 4: return mac?FrameSubject(shot,445,1840,800,1920):FrameSubject(shot,645,1850,676,2100);
 case 5: case 14:
 if(!mac)return FrameSubject(shot,1380,1820,1650,1970);
 return shot.Scene==5?FrameSubject(shot,1480,1850,1375,1978):FrameSubject(shot,365,345,640,385);
 case 6: return mac?FrameSubject(shot,780,2615,1010,2750):FrameSubject(shot,1010,2630,1140,2760);
 case 7: return FrameSubject(shot,600,390,600,mac?365:440);
 case 8: // Move back from the formatting control to the text it changes.
 return mac?FrameSubject(shot,680,340,440,365):FrameSubject(shot,650,270,325,378);
 case 9: return FrameSubject(shot,300,275,465,430);
 case 10: return mac?FrameSubject(shot,600,355,600,520):FrameSubject(shot,550,285,495,499);
 case 12: return FrameSubject(shot,600,400,905,447);
 case 15: return mac?FrameSubject(shot,430,535,665,440):FrameSubject(shot,390,318,797,594);
 case 16: return mac?FrameSubject(shot,435,270,390,480):FrameSubject(shot,375,290,282,600);
 case 17: // End at the readout after the final key press.
 return mac?FrameSubject(shot,520,472,875,244):FrameSubject(shot,573,475,810,237);
 case 18: return mac?FrameSubject(shot,380,248,674,390):FrameSubject(shot,500,365,496,650);
 case 19: if(mac)shot.Zoom=1.08;
 return mac?FrameSubject(shot,410,405,600,310):FrameSubject(shot,330,325,585,500);
 case 20: if(!mac)shot.Zoom=.87;
 return mac?FrameSubject(shot,360,390,530,610):FrameSubject(shot,540,365,688,560);
 case 21: shot.Zoom=.90;return FrameSubject(shot,485,345,635,528);
 case 22:
 var rows=mac?macRows:winRows;
 double textX=StoryFraming.CenterX[shot.Owner]-shot.Owner*2200;
 return FrameSubject(shot,textX,rows[0]+45,textX,rows[rows.Length-1]-35);
 }
 return shot;
 }
}}
