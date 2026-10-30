using System;using System.Windows;using System.Windows.Media;using System.Windows.Shapes;
namespace RetroWindows {sealed partial class SharedView {
 readonly Path controlClock=new Path{Fill=Brushes.Black,IsHitTestVisible=false,Visibility=Visibility.Hidden};string controlClockValue;
 void UpdateControlClock(Shot shot){bool show=shot.Owner==0&&shot.Scene==19&&referenceCard.Visibility==Visibility.Visible;controlClock.Visibility=show?Visibility.Visible:Visibility.Hidden;if(!show)return;DateTime now=DateTime.Now;string date=now.ToString("M/d/yy"),time=now.ToString("H:mm:ss"),value=date+time;if(value==controlClockValue)return;controlClockValue=value;var shape=new StreamGeometry();using(var pen=shape.Open()){StoryLine(pen,0,date,date.Length,318,234,4);StoryLine(pen,0,time,time.Length,534,234,4);}shape.Freeze();controlClock.Data=shape;}
}}
