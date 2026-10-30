sampler2D input : register(s0);
float2 phasePitch : register(c0);
float2 inverseA : register(c1);
float2 inverseB : register(c2);
float2 inverseOffset : register(c3);
float2 viewport : register(c4);
float2 direction : register(c5);
float2 forwardA : register(c6);
float2 forwardB : register(c7);
float2 forwardOffset : register(c8);
float4 main(float2 uv:TEXCOORD):COLOR {
 float2 screen=uv*viewport;
 float2 world=screen.x*inverseA+screen.y*inverseB+inverseOffset;
 float pitch=phasePitch.y;
 float2 cell=floor(world/pitch);
 float2 center=(cell+.5)*pitch;
 float2 local=(world-center)/pitch;
 float position=dot(uv-.5,direction)/(abs(direction.x)+abs(direction.y))+.5;
 float variation=(frac(cell.x*.754877+cell.y*.569841)-.5)*.025;
 float size=saturate((phasePitch.x*1.85-position+variation)/.85);
 size=size*size*(3-2*size);
 float edge=max(abs(local.x),abs(local.y));
 float coverage=saturate((size*.5-edge)*pitch/.65+.5)*step(.003,size);
 coverage=lerp(coverage,1,step(.999,size));
 float2 sourceWorld=center+local*pitch/max(size,.003);
 float2 sourceUV=(sourceWorld.x*forwardA+sourceWorld.y*forwardB+forwardOffset)/viewport;
 return tex2D(input,sourceUV)*coverage;
}
