using System;using System.Windows;
namespace RetroWindows {
// Timed waypoints: arrive, settle, press, release. Coordinates use the untransformed scene canvas.
static class ReferenceMotion {
 public struct Sample {public Point Pointer;public Rect Button,Selection;public bool Down,Show;public int Digits,ViewState,PressedKey;public double SliderX,SliderY,DrawProgress;}
 static readonly System.Collections.Generic.Dictionary<int,Point[]> tracks=new System.Collections.Generic.Dictionary<int,Point[]>();static readonly double[] arrival={0,6.5,12.7,19};
 static readonly double[] charMapClicks={4.6,7.3,10,12.4,15,17.4,20};
 static readonly Point[] charMapTargets={new Point(117,298),new Point(1010,602),new Point(597,252),new Point(1010,602),new Point(549,252),new Point(1010,602),new Point(1010,667)};
 static readonly int[] macKeys={0,12,6,5,8};static readonly int[] winKeys={0,13,3,2,7};
 public static double Ease(double x){x=Math.Max(0,Math.Min(1,x));return x*x*x*(x*(x*6-15)+10);}
 static Point Mix(Point a,Point b,double u){u=Ease(u);return new Point(a.X+(b.X-a.X)*u,a.Y+(b.Y-a.Y)*u);}
 public static Rect CalculatorButton(int owner,int i){int k=owner==0?macKeys[i]:winKeys[i];if(owner==1&&i==0)return new Rect(840,294,110,62);return owner==0?new Rect(86+(k%4)*260,298+(k/4)*83,246,72):new Rect(250+(k%6)*118,375+(k/6)*80,106,68);}
 static Point Center(Rect r){return new Point(r.X+r.Width*.5,r.Y+r.Height*.5);}
 public static Sample At(int owner,int scene,double age,double duration){var s=new Sample{Show=scene!=22&&scene!=7&&scene!=10&&scene!=12&&scene!=16&&!(owner==0&&scene==18),Pointer=new Point(1000,680),Button=Rect.Empty,Selection=Rect.Empty,SliderX=-1,SliderY=-1};double p=age/duration*24;
 if(scene==17){Point previous=new Point(owner==0?1040:990,650);for(int i=0;i<5;i++){double click=5.7+i*2.05;Rect button=CalculatorButton(owner,i);Point next=Center(button);if(p<click-.25){s.Pointer=Mix(previous,next,(p-(click-1.65))/1.4);return s;}s.Pointer=next;s.PressedKey=i;s.Down=p>=click&&p<click+.18;s.Button=button;if(p<click+.18)return s;s.Digits=i;previous=next;}s.Digits=4;s.Pointer=Mix(previous,new Point(1015,700),(p-16)/2);return s;}
 Point[] points;int trackKey=owner*100+scene;if(!tracks.TryGetValue(trackKey,out points)){
 if(scene==8)points=owner==0?new[]{new Point(690,210),new Point(840,417),new Point(840,515),new Point(1070,610)}:new[]{new Point(1010,650),new Point(853,220),new Point(430,465),new Point(730,550)};
 else if(scene==9)points=new[]{new Point(1050,650),new Point(198,255),new Point(466,421),new Point(734,587)};
 else if(scene==12)points=new[]{new Point(1080,690),new Point(916,565),new Point(866,390),new Point(650,610)};
 else if(scene==14)points=owner==0?new[]{new Point(1010,680),new Point(95,325),new Point(157,480),new Point(710,430)}:new[]{new Point(1010,680),new Point(120,365),new Point(150,475),new Point(700,440)};
 else if(scene==15)points=owner==0?new[]{new Point(1010,680),new Point(300,665),new Point(760,420),new Point(670,410)}:new[]{new Point(1080,670),new Point(250,303),new Point(130,631),new Point(1040,225)};
 else if(scene==16)points=new[]{new Point(1070,650),new Point(390,300),new Point(680,440),new Point(990,640)};
 else if(scene==18)points=owner==0?new[]{new Point(1070,650),new Point(375,235),new Point(675,392),new Point(975,549)}:new[]{new Point(1090,700),new Point(117,298),new Point(1010,602),new Point(1010,667)};
 else if(scene==19)points=owner==0?new[]{new Point(1080,700),new Point(222,438),new Point(750,357),new Point(900,650)}:new[]{new Point(1080,700),new Point(220,290),new Point(144,327),new Point(970,530)};
 else if(scene==20)points=owner==0?new[]{new Point(1090,700),new Point(190,375),new Point(278,462),new Point(700,635)}:new[]{new Point(1090,700),new Point(235,563),new Point(710,598),new Point(1010,698)};
 else if(scene==21)points=new[]{new Point(1090,700),new Point(477,528),new Point(690,528),new Point(970,530)};
 else points=new[]{new Point(1010,680),new Point(730,560),new Point(900,650),new Point(1040,650)};
 tracks[trackKey]=points;}s.Pointer=points[0];for(int i=1;i<points.Length;i++){double start=arrival[i]-2.6;if(p<start)break;s.Pointer=Mix(points[i-1],points[i],(p-start)/2.6);}
 if(owner==0&&scene==14){Point tool=new Point(158.5,326.5),start=new Point(480,260),end=new Point(820,510);s.Pointer=Mix(new Point(1010,680),tool,(p-3.9)/2.6);s.ViewState=p>=6.88?1:0;s.Button=new Rect(128,301,61,51);s.Down=p>=6.7&&p<6.88;if(p>=7.3)s.Pointer=Mix(tool,start,(p-7.3)/2.3);if(p>=10){s.DrawProgress=Ease((p-10)/6);s.Pointer=new Point(start.X+(end.X-start.X)*s.DrawProgress,start.Y+(end.Y-start.Y)*s.DrawProgress);s.Down=p<16;s.Button=Rect.Empty;}if(p>=17)s.Pointer=Mix(end,new Point(1050,675),(p-17)/2.6);return s;}
 // Each state is committed on release; the view consumes this exact same sample.
 if(scene==9){s.Down=(p>=6.7&&p<6.88)||(p>=12.9&&p<13.08);s.Button=p<10?new Rect(150,210,96,94):new Rect(418,376,96,94);s.ViewState=p>=13.08?2:p>=6.88?1:0;}
 if(owner==0&&scene==8){s.Down=(p>=6.7&&p<6.88)||(p>=12.9&&p<13.08);s.Button=p<10?new Rect(745,399,280,49):new Rect(745,497,280,49);s.ViewState=p>=13.08?2:p>=6.88?1:0;}
 if(owner==1&&scene==8){s.Down=p>=6.7&&p<6.88;s.Button=new Rect(826,194,54,52);s.ViewState=p>=6.88?1:0;}
 if(owner==1&&scene==15){s.Down=(p>=6.7&&p<6.88)||(p>=12.9&&p<13.08);s.Button=p<10?new Rect(102,286,400,35):new Rect(118,619,24,24);s.ViewState=p>=13.08?2:p>=6.88?1:0;}
 if(owner==1&&scene==18){double[] clicks=charMapClicks;Point[] targets=charMapTargets;Point prior=new Point(1090,700);s.Pointer=prior;for(int i=0;i<clicks.Length;i++){double click=clicks[i];if(p<click-.2){s.Pointer=Mix(prior,targets[i],(p-(click-2))/1.8);break;}s.Pointer=targets[i];if(p<click+.18){s.Down=p>=click;s.Button=i==6?new Rect(930,642,160,51):i%2==1?new Rect(930,577,160,51):new Rect(targets[i].X-12,targets[i].Y-23,24,46);break;}s.ViewState=i+1;prior=targets[i];}if(s.ViewState>=1){int code=s.ViewState>=5?50:s.ViewState>=3?52:64;int index=code-32;s.Selection=new Rect(105+(index%32)*24,229+(index/32)*46,24,46);}}

 if(owner==1&&scene==20){s.Down=(p>=6.7&&p<6.88)||(p>=12.9&&p<13.08)||(p>=19.3&&p<19.48);s.Button=p>=19.3?new Rect(948,674,144,47):p<10?new Rect(121,546,231,35):new Rect(576,581,290,35);s.ViewState=p>=19.48?3:p>=13.08?2:p>=6.88?1:0;}
 if(owner==0&&scene==20){s.Down=(p>=6.7&&p<6.88)||(p>=12.9&&p<13.08)||(p>=19.3&&p<19.48);s.Button=p>=19.3?new Rect(477,590,114,85):p<10?new Rect(177,361,28,28):new Rect(264,448,28,28);s.ViewState=p>=19.48?3:p>=13.08?2:p>=6.88?1:0;if(p>=16.4)s.Pointer=Mix(new Point(278,462),new Point(534,632.5),(p-16.4)/2.6);}
 if(owner==1&&scene==16){s.ViewState=p>=11.5?9:Math.Max(0,Math.Min(8,(int)((p-5.5)/.6)));}
 if(owner==0&&scene==15){s.Down=(p>=6.7&&p<6.88)||(p>=12.9&&p<13.08);s.Button=p<10?new Rect(271,650,58,32):new Rect(744,404,32,32);s.ViewState=p>=13.08?2:p>=6.88?1:0;}
 if(owner==1&&scene==19){s.Down=(p>=6.7&&p<6.88)||(p>=7.02&&p<7.20)||(p>=12.9&&p<13.08);s.Button=p<10?new Rect(172,240,96,94):new Rect(132,315,24,24);s.ViewState=p>=13.08?2:p>=7.20?1:0;}
 if(owner==0&&scene==19){s.SliderY=423+108*Ease((p-7.2)/4);s.Down=(p>=7.2&&p<11.2)||(p>=13.3&&p<13.5);s.ViewState=p>=13.5?1:0;if(p>=7.2&&p<11.2)s.Pointer=new Point(222,s.SliderY+15);else if(p>=11.2&&p<13.3)s.Pointer=Mix(new Point(222,546),new Point(750,357),(p-11.2)/2.1);else if(p>=13.3&&p<17.7)s.Pointer=new Point(750,357);else if(p>=17.7)s.Pointer=Mix(new Point(750,357),new Point(1080,650),(p-17.7)/2);}

 if(owner==1&&scene==21){s.SliderX=460+200*Ease((p-7.2)/4.8);s.Down=p>=7.2&&p<12;if(p>=7.2&&p<12)s.Pointer=new Point(s.SliderX+17,528);if(p>=12)s.Pointer=Mix(new Point(677,528),new Point(970,530),(p-16.4)/2.6);}

 return s;
 }
}
}
