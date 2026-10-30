using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
namespace RetroWindows {
sealed partial class SharedView {
 // Camera uses only the shared monotonic drift/zoom, without action-driven rocking.
 readonly Path storyInk=new Path{Fill=Brushes.Black,IsHitTestVisible=false,Visibility=Visibility.Hidden};
 readonly Path storyShadow=new Path{Fill=Brushes.Black,Opacity=.18,RenderTransform=new TranslateTransform(1.2,2.1),IsHitTestVisible=false,Visibility=Visibility.Hidden};
 readonly Rectangle storyCaret=new Rectangle{Fill=Brushes.Black,Width=3,Height=23,IsHitTestVisible=false,Visibility=Visibility.Hidden};
 readonly TranslateTransform storyCaretShift=new TranslateTransform();
 // Input is typed; program output arrives as complete lines after evaluation.
 static readonly string[] macThought={"(* 7,500,000 years later... *)","In[1]:= Pause[2]; 6 * 7","Out[1]= 42","(* What was the question? *)"};
 static readonly string[] winThought={@"C:\>QBASIC /RUN ANSWER.BAS","Computing...","7,500,000 years later:","42","Now, what was the question?",@"C:\>"};
 static readonly double[] macStarts={2,6,12.2,17};
 static readonly double[] winStarts={2,6,9,12.2,17,20};
 static readonly int[] macRows={245,343,441,565};
 static readonly int[] winRows={205,279,353,427,501,575};
 int storyKey=-1;
 void AddStoryLayers(){storyCaret.RenderTransform=storyCaretShift;plane.Children.Add(storyShadow);plane.Children.Add(storyInk);plane.Children.Add(storyCaret);}
 void UpdateStory(Shot shot,double age){
 bool show=shot.Scene==22&&referenceCard.Visibility==Visibility.Visible;
 storyInk.Visibility=storyShadow.Visibility=show?Visibility.Visible:Visibility.Hidden;
 if(!show){storyCaret.Visibility=Visibility.Hidden;storyKey=-1;return;}
 string[] lines=shot.Owner==0?macThought:winThought;double[] starts=shot.Owner==0?macStarts:winStarts;int[] rows=shot.Owner==0?macRows:winRows;
 double p=age/shot.Duration*24;int key=shot.Owner+1,line=0,count=0;const int pitch=3;
 for(int i=0;i<lines.Length;i++){int n=ThoughtCount(shot.Owner,i,p);key=unchecked(key*37+n);if(p>=starts[i]){line=i;count=n;}}
 if(key!=storyKey){storyKey=key;var geometry=new StreamGeometry();using(var pen=geometry.Open()){
 for(int i=0;i<lines.Length;i++)StoryLine(pen,shot.Owner,lines[i],ThoughtCount(shot.Owner,i,p),shot.Owner*2200+(shot.Owner==0?245:300),rows[i],pitch);
 }geometry.Freeze();storyInk.Data=storyShadow.Data=geometry;}
 bool input=shot.Owner==0?line!=2:line==0||line==5;
 bool caret=p>=2&&p<22&&input;storyCaret.Visibility=caret&&age%.95<.55?Visibility.Visible:Visibility.Hidden;
 double x=shot.Owner*2200+(shot.Owner==0?245:300);for(int i=0;i<count;i++)x+=StoryAdvance(shot.Owner,lines[line][i])*pitch;
 storyCaretShift.X=x+pitch;storyCaretShift.Y=rows[line];storyCaret.Height=(shot.Owner==0?Macintosh.ReferenceFonts.Glyph('4').Length:WindowsPixels.MonoGlyph('4').Length)*pitch;
 }
 static int ThoughtCount(int owner,int line,double p){string text=(owner==0?macThought:winThought)[line];double start=(owner==0?macStarts:winStarts)[line];if(p<start)return 0;bool typed=owner==0?line!=2:line==0;return typed?Math.Max(0,Math.Min(text.Length,(int)Math.Floor((p-start)/.075))):text.Length;}
 static int StoryAdvance(int owner,char c){return owner==0?Macintosh.ReferenceFonts.Advance(c):WindowsPixels.MonoAdvance(c);}
 static void StoryLine(StreamGeometryContext pen,int owner,string text,int count,double x,double y,int pitch){
 for(int k=0;k<count;k++){char ch=text[k];string[] glyph=owner==0?Macintosh.ReferenceFonts.Glyph(ch):WindowsPixels.MonoGlyph(ch);
 for(int row=0;row<glyph.Length;row++)for(int col=0;col<glyph[row].Length;col++)if(glyph[row][col]=='1'){
 double xx=x+col*pitch+.225,yy=y+row*pitch+.225,size=pitch-.55;
 pen.BeginFigure(new Point(xx,yy),true,true);pen.LineTo(new Point(xx+size,yy),true,false);pen.LineTo(new Point(xx+size,yy+size),true,false);pen.LineTo(new Point(xx,yy+size),true,false);
 }x+=StoryAdvance(owner,ch)*pitch;}
 }
}
}
