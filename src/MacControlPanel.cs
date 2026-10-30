using System;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace RetroWindows {static partial class ReferenceContent {
 static BitmapSource controlThumb;
 public static BitmapSource ControlThumb(){if(controlThumb!=null)return controlThumb;using(var p=new Painter(true))using(var b=new Bitmap(144,60,System.Drawing.Imaging.PixelFormat.Format32bppPArgb)){using(var g=Graphics.FromImage(b)){g.Clear(System.Drawing.Color.White);g.ScaleTransform(2,2);p.ControlThumbArt(g);}var data=b.LockBits(new Rectangle(0,0,144,60),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);try{var raw=new byte[data.Stride*60];Marshal.Copy(data.Scan0,raw,0,raw.Length);for(int i=0;i<raw.Length;i+=4){byte a=(byte)(255-raw[i]);raw[i]=raw[i+1]=raw[i+2]=0;raw[i+3]=a;}controlThumb=BitmapSource.Create(144,60,192,192,PixelFormats.Pbgra32,null,raw,data.Stride);controlThumb.Freeze();}finally{b.UnlockBits(data);}}return controlThumb;}
 sealed partial class Painter {
 // One three-unit grid for borders, texture, icons and controls. Draw once into the card cache.
 void CPFill(Graphics g,int x,int y,int w,int h){for(int yy=y;yy<y+h;yy+=3)for(int xx=x;xx<x+w;xx+=3)Cell(g,xx,yy,3,black);}
 void CPFrame(Graphics g,int x,int y,int w,int h){Fill(g,paper,x,y,w,h);CPFill(g,x,y,w,3);CPFill(g,x,y+h-3,w,3);CPFill(g,x,y,3,h);CPFill(g,x+w-3,y,3,h);}
 void CPPattern(Graphics g,int x,int y,int w,int h,int pattern){for(int yy=0;yy<h;yy+=3)for(int xx=0;xx<w;xx+=3){int a=xx/3,b=yy/3;bool on=pattern==0?(a+b)%2==0:pattern==1?a%3==0:pattern==2?b%3==0:pattern==3?(a+b)%4<2:(a%4==0||b%4==0);if(on)Cell(g,x+xx,y+yy,3,black);}}
 void CPPanel(Graphics g,int x,int y,int w,int h){CPFrame(g,x,y,w,h);CPFrame(g,x+6,y+6,w-12,h-12);}
 void CPNumber(Graphics g,string text,int x,int y,int w,bool selected){
 // Stepped rounded buttons on the original panel, with optically centered numerals.
 Fill(g,paper,x,y,w,33);CPFill(g,x+6,y,w-12,3);CPFill(g,x+3,y+3,3,3);CPFill(g,x+w-6,y+3,3,3);CPFill(g,x,y+6,3,21);CPFill(g,x+w-3,y+6,3,21);CPFill(g,x+3,y+27,3,3);CPFill(g,x+w-6,y+27,3,3);CPFill(g,x+6,y+30,w-12,3);
 if(selected){CPFill(g,x+6,y+3,w-12,27);CPFill(g,x+3,y+6,w-6,21);}Text(g,text,x+(w-Width(text,3,false))/2,y+6,3,false,selected?paper:black);
 }
 public void ControlThumbArt(Graphics g){fineControlInk=true;CPFill(g,6,0,60,3);CPFill(g,3,3,66,3);CPFill(g,0,6,3,18);CPFill(g,69,6,3,18);CPFill(g,3,24,66,3);CPFill(g,6,27,60,3);CPFill(g,24,12,24,3);}
 void MacControlOverview(Graphics g){
 // Native reconstruction of Macintosh System 1.1 Control Panel. Coordinates follow
 // the original 512x342 screenshot at a three-unit logical pixel, not a bitmap overlay.
 fineControlInk=true;var saved=g.Save();g.TranslateTransform(114,81);
 CPFrame(g,18,60,936,516);CPFrame(g,24,66,924,504);
 for(int y=66;y<99;y+=6)CPFill(g,30,y,912,3);
 Fill(g,paper,336,60,300,39);Text(g,"Control Panel",354,66,4,false,black);CPFrame(g,42,63,33,30);
 CPFill(g,30,108,912,456);
 CPPanel(g,36,120,123,432);
 string[] speaker={"000010000000000","000110001001000","001110000100100","111110100100100","111110100100100","001110000100100","000110001001000","000010000000000"};
 Sprite(g,speaker,78,138,3,black);Sprite(g,speaker,78,507,3,black);
 CPFill(g,99,180,21,3);CPFill(g,96,183,27,3);CPFill(g,93,186,3,300);CPFill(g,123,186,3,300);CPFill(g,96,486,27,3);CPFill(g,99,489,21,3);CPPattern(g,99,189,21,294,0);
 for(int i=0;i<8;i++){Text(g,(7-i).ToString(),51,201+i*36,3,false,black);CPFill(g,72,210+i*36,12,3);}
 if(!LiveSliders){var thumb=g.Save();g.TranslateTransform(72,342);Fill(g,paper,0,0,72,30);ControlThumbArt(g);g.Restore(thumb);}
 CPPanel(g,171,120,444,96);
 // Menu-flash illustration and 0..3 selector at the top right.
 CPPanel(g,627,120,306,96);CPFrame(g,633,126,99,84);CPFill(g,633,132,99,12);Fill(g,paper,657,135,21,6);
 CPFrame(g,654,144,51,30);CPFill(g,663,150,9,3);CPFill(g,681,150,15,3);CPFill(g,663,162,30,3);CPFrame(g,654,183,51,21);CPFill(g,663,189,33,6);
 for(int i=0;i<3;i++){CPFill(g,639+i*3,165+i*6,3,3);CPFill(g,720-i*3,165+i*6,3,3);}
 CPPattern(g,738,126,189,84,0);CPFrame(g,750,141,162,51);
 for(int i=0;i<4;i++){int x=756+i*39;Fill(g,i==3?black:paper,x,147,36,39);Text(g,i.ToString(),x+9,153,4,false,i==3?paper:black);}
 // Keyboard repeat and key-touch: correctly staggered keys, turtle/hare and fingers.
 CPPanel(g,171,234,609,135);CPPattern(g,180,243,591,117,0);
 CPFrame(g,198,255,219,90);CPFrame(g,207,264,201,72);CPFrame(g,219,270,177,63);
 for(int r=0;r<4;r++){int y=273+r*12;int shift=r==1?6:r==2?12:0;for(int k=0;k<14;k++){int x=222+shift+k*12;if(x+9<393)Fill(g,black,x+9,y,3,9);}CPFill(g,219,y+9,177,3);}
 Fill(g,paper,249,321,117,9);CPFill(g,219,330,177,3);
 for(int row=0;row<2;row++){int y=255+row*51;Fill(g,paper,435,y,318,42);for(int i=0;i<5;i++){int x=501+i*39;bool selected=i==(row==0?(State>=1?3:1):2);Fill(g,selected?black:paper,x,y,36,42);Text(g,i.ToString(),x+9,y+6,4,false,selected?paper:black);}}
 Sprite(g,new[]{"0000011110000000","0011111111111100","1111111111111111","0011000000110000","0001000000010000"},444,267,3,black);
 Sprite(g,new[]{"0000010000100","0000110001100","0001111111000","0011111111100","1111111111110","0011000010011","0110000110000"},705,261,3,black);
 Sprite(g,new[]{"011011000","010101000","010101000","010001000","001110000","000100000","111111110","100000010"},453,309,3,black);
 Sprite(g,new[]{"001100000","001100000","001101100","001101100","001000100","001111100","000100000","111111110"},711,309,3,black);
 // Insertion-point blink: dotted radiating marks, three settings.
 CPPanel(g,792,234,144,135);for(int row=0;row<7;row++){int spread=Math.Abs(3-row)*3;CPFill(g,846-spread,252+row*6,3,3);CPFill(g,876+spread,252+row*6,3,3);}CPFill(g,861,252,3,45);
 CPFrame(g,804,312,117,45);for(int i=0;i<3;i++){int x=810+i*36;Fill(g,i==1?black:paper,x,318,33,33);Text(g,(i+1).ToString(),x+9,321,4,false,i==1?paper:black);}
 // Mouse tracking, desktop pattern editor/preview and double-click speed.
 CPPanel(g,171,387,129,165);CPFill(g,249,393,3,12);CPFill(g,246,408,9,3);CPFill(g,243,414,15,3);CPFill(g,237,420,27,3);
 CPFrame(g,219,426,60,75);CPFrame(g,225,432,48,63);CPFrame(g,234,432,30,15);
 for(int r=0;r<9;r++){int y=432+r*6;CPFill(g,201,y,3,3);if(r>1)CPFill(g,195,y,3,3);if(r>3)CPFill(g,189,y,3,3);CPFill(g,207,y,9,3);}
 CPNumber(g,"1",189,507,33,true);CPNumber(g,"0",246,507,33,false);
 CPPanel(g,312,387,345,165);CPFrame(g,336,405,126,126);CPFill(g,372,426,3,36);CPFill(g,357,441,33,3);
 for(int r=0;r<7;r++)for(int c=0;c<=r/2;c++)CPFill(g,444-c*30-(r%2)*15,423+r*15,12,12);
 CPFrame(g,489,405,144,126);CPFill(g,492,420,138,3);for(int y=426;y<525;y+=3)for(int x=495;x<627;x+=3)if((((x-495)/18+(y-426)/18)%2==0)&&((x+y)/3)%3!=0)Cell(g,x,y,3,black);
 CPPanel(g,669,387,267,165);CPFill(g,729,402,3,27);CPFill(g,726,435,9,3);CPFill(g,720,441,21,3);CPFill(g,714,447,33,3);
 CPFrame(g,690,456,81,87);CPFrame(g,702,462,57,81);CPFill(g,726,462,3,81);CPFrame(g,720,477,15,15);
 for(int col=0;col<3;col++){int x=783+col*51;for(int r=0;r<2;r++){int y=411+r*48+col*9;CPFill(g,x+9,y,9,12);CPFill(g,x,y+9,27,3);CPFill(g,x+3,y+12,21,3);CPFill(g,x+6,y+15,15,3);CPFill(g,x+9,y+18,9,3);}Sprite(g,col==2?new[]{"0011111100","0111111110","1111111111","1111111111","1111111111","1111111111","1111111111","1111111111","0111111110","0011111100"}:new[]{"0011111100","0100000010","1000000001","1000000001","1000000001","1000000001","1000000001","1000000001","0100000010","0011111100"},x,498,3,black);}
 g.Restore(saved);fineControlInk=false;
 }
 }
}}
