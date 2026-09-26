Shader "SecretVirus/EnvironmentGlass"
{
 Properties { _Color("Glass tint",Color)=(.5,.7,.75,.22) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard alpha:fade
 #pragma target 3.0
 fixed4 _Color;
 struct Input {float3 viewDir;};
 void surf(Input IN,inout SurfaceOutputStandard o){o.Albedo=_Color.rgb;o.Smoothness=.8;o.Metallic=.15;o.Alpha=.13+.22*pow(1-saturate(abs(IN.viewDir.z)),3);}
 ENDCG
 } FallBack "Transparent/Diffuse"
}
