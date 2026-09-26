Shader "SecretVirus/IllustratedCharacter"
{
    Properties { _MainTex("Albedo",2D)="white"{} _Color("Color",Color)=(1,1,1,1) _Glossiness("Smoothness",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Illustrated fullforwardshadows noforwardadd
        #pragma target 3.0
        sampler2D _MainTex;fixed4 _Color;
        struct Input {float2 uv_MainTex;};
        half4 LightingIllustrated(SurfaceOutput s,half3 lightDir,half attenuation)
        {
            half light=lerp(.38h,1.0h,smoothstep(-.25h,.65h,dot(s.Normal,lightDir)));
            return half4(s.Albedo*_LightColor0.rgb*light*attenuation,s.Alpha);
        }
        void surf(Input input,inout SurfaceOutput output)
        {
            fixed4 base=tex2D(_MainTex,input.uv_MainTex)*_Color;
            output.Albedo=base.rgb;output.Emission=base.rgb*.09;output.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
