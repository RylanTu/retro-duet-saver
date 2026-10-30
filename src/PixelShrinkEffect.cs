using System;using System.Reflection;using System.Windows;using System.Windows.Media;using System.Windows.Media.Effects;
namespace RetroWindows {
sealed class PixelShrinkEffect:ShaderEffect {
 static readonly PixelShader shader=LoadShader();
 static PixelShader LoadShader(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PixelShrink.ps")){if(stream==null)return null;var value=new PixelShader();value.SetStreamSource(stream);value.Freeze();return value;}}
 public static bool Available{get{return shader!=null;}}
 public static readonly DependencyProperty InputProperty=RegisterPixelShaderSamplerProperty("Input",typeof(PixelShrinkEffect),0,SamplingMode.Bilinear);
 public System.Windows.Media.Brush Input{get{return (System.Windows.Media.Brush)GetValue(InputProperty);}set{SetValue(InputProperty,value);}}
 static DependencyProperty Constant(string name,int register){return DependencyProperty.Register(name,typeof(Point),typeof(PixelShrinkEffect),new UIPropertyMetadata(new Point(),PixelShaderConstantCallback(register)));}
 static readonly DependencyProperty PhaseProperty=Constant("Phase",0),InverseAProperty=Constant("InverseA",1),InverseBProperty=Constant("InverseB",2),InverseOffsetProperty=Constant("InverseOffset",3),ViewportProperty=Constant("Viewport",4),DirectionProperty=Constant("Direction",5),ForwardAProperty=Constant("ForwardA",6),ForwardBProperty=Constant("ForwardB",7),ForwardOffsetProperty=Constant("ForwardOffset",8);
 public PixelShrinkEffect(){PixelShader=shader;UpdateShaderValue(InputProperty);foreach(var p in new[]{PhaseProperty,InverseAProperty,InverseBProperty,InverseOffsetProperty,ViewportProperty,DirectionProperty,ForwardAProperty,ForwardBProperty,ForwardOffsetProperty})UpdateShaderValue(p);}
 public void Configure(Matrix camera,double width,double height,double progress,double pitch,Point direction){var inverse=camera;inverse.Invert();SetValue(PhaseProperty,new Point(progress,pitch));SetValue(InverseAProperty,new Point(inverse.M11,inverse.M12));SetValue(InverseBProperty,new Point(inverse.M21,inverse.M22));SetValue(InverseOffsetProperty,new Point(inverse.OffsetX,inverse.OffsetY));SetValue(ViewportProperty,new Point(width,height));SetValue(DirectionProperty,direction);SetValue(ForwardAProperty,new Point(camera.M11,camera.M12));SetValue(ForwardBProperty,new Point(camera.M21,camera.M22));SetValue(ForwardOffsetProperty,new Point(camera.OffsetX,camera.OffsetY));}
}
sealed partial class SharedView {
 readonly System.Windows.Controls.Grid sceneHost=new System.Windows.Controls.Grid();
 readonly System.Windows.Shapes.Rectangle pixelSurface=new System.Windows.Shapes.Rectangle{IsHitTestVisible=false,Visibility=Visibility.Hidden};
 VisualBrush pixelSource;PixelShrinkEffect pixelTransition;
 void InitializePixelSurface(){pixelSource=new VisualBrush(sceneLayer){AutoLayoutContent=false,ViewboxUnits=BrushMappingMode.Absolute,Stretch=Stretch.Fill};pixelSurface.Fill=pixelSource;sceneHost.Children.Add(sceneLayer);Children.Add(sceneHost);Children.Add(pixelSurface);}
 void UpdatePixelTransition(Matrix camera,Shot shot,double age,bool blank,double progress){
 if(!PixelShrinkEffect.Available)return;
 if(blank||progress>=1){pixelSurface.Visibility=Visibility.Hidden;pixelSurface.Effect=null;pixelSurface.Fill=null;sceneHost.Opacity=1;return;}
 if(pixelTransition==null)pixelTransition=new PixelShrinkEffect();
 // Match the broad spatial progression to the existing entry/exit direction.
 Point direction;switch(transitionKind){case 1:direction=new Point(-1,1);break;case 2:direction=new Point(1,0);break;case 3:case 8:direction=new Point(0,-1);break;case 7:direction=new Point(-1,0);break;case 4:direction=new Point(-1,-1);break;default:direction=new Point(1,1);break;}
 pixelTransition.Configure(camera,ActualWidth,ActualHeight,Math.Max(0,Math.Min(1,progress)),shot.Owner==0&&shot.Scene==19?3:5,direction);
 if(pixelSurface.Fill==null)pixelSurface.Fill=pixelSource;pixelSource.Viewbox=new Rect(0,0,ActualWidth,ActualHeight);pixelSurface.Visibility=Visibility.Visible;sceneHost.Opacity=0;if(pixelSurface.Effect!=pixelTransition)pixelSurface.Effect=pixelTransition;
 transitionVeil.Visibility=Visibility.Hidden;
 }
}
}
