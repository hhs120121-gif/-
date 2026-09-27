Shader "SecretVirus/EnvironmentSurface"
{
 Properties { _Color("Albedo",Color)=(.5,.5,.5,1) _Glossiness("Smoothness",Range(0,1))=.25 _Metallic("Metallic",Range(0,1))=0 _Kind("Surface family",Float)=0 _EmissionColor("Emission",Color)=(0,0,0,0) _Visibility("Camera visibility",Range(0,1))=1 }
 SubShader {
 Tags { "RenderType"="Opaque" } LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 fixed4 _Color,_EmissionColor;half _Glossiness,_Metallic,_Kind,_Visibility;
 struct Input { float3 worldPos;float3 worldNormal;float4 screenPos; };
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);}
 void surf(Input IN,inout SurfaceOutputStandard o){
  float2 px=floor(IN.screenPos.xy/max(IN.screenPos.w,.0001)*_ScreenParams.xy);float threshold=frac(dot(px,float2(.75487766,.56984029)));
  clip(_Visibility>=.999?1:_Visibility-threshold-.001);
  float coarse=noise(IN.worldPos*2.1),fine=noise(IN.worldPos*38);float variation=.96+.075*coarse;
  if(_Kind>.5&&_Kind<1.5)variation=.96+.05*coarse+.018*fine;
  if(_Kind>1.5&&_Kind<2.5)variation=.91+.07*sin(IN.worldPos.x*52+noise(IN.worldPos*3)*5)+.09*coarse;
  if(_Kind>2.5&&_Kind<3.5)variation=.97+.04*sin(IN.worldPos.x*170)*sin(IN.worldPos.z*170);
  o.Albedo=_Color.rgb*variation;o.Metallic=_Metallic;o.Smoothness=_Glossiness*(.9+.1*fine);o.Occlusion=1;o.Emission=_EmissionColor.rgb;o.Alpha=1;
 }
 ENDCG
 } FallBack "Standard"
}
