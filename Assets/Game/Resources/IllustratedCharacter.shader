Shader "SecretVirus/IllustratedCharacter"
{
    Properties { _Skin("Skin light wrap",Range(0,1))=0  _MainTex("Albedo",2D)="white"{} _Color("Color",Color)=(1,1,1,1) _Glossiness("Smoothness",Range(0,1))=0 _Fabric("Fabric weave",Range(0,1))=0 _DetailAtlas("Painted material atlas",2D)="white"{} _AtlasRect("Atlas tile",Vector)=(0,0,1,1) _UseAtlas("Atlas strength",Range(0,1))=0 _DetailMean("Tile average",Float)=.3 _DetailTiling("Detail tiling",Float)=1 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Illustrated fullforwardshadows noforwardadd noambient
        #pragma target 3.0
        sampler2D _MainTex,_DetailAtlas;fixed4 _Color;half _Glossiness,_Fabric,_UseAtlas,_DetailMean,_DetailTiling,_Skin;float4 _AtlasRect;
        struct Input {float2 uv_MainTex;float4 color:COLOR;};
        half4 LightingIllustrated(SurfaceOutput s,half3 lightDir,half3 viewDir,half attenuation)
        {
            half light=smoothstep(-.18h,.78h,dot(s.Normal,lightDir));
            half3 shade=lerp(lerp(half3(.46,.43,.46),half3(.68,.59,.56),_Skin),half3(.98,.96,.92),light);
            half highlight=pow(max(0,dot(s.Normal,normalize(lightDir+viewDir))),lerp(12,70,_Glossiness))*_Glossiness*.25;
            return half4((s.Albedo*shade+highlight)*lerp(half3(1,1,1),_LightColor0.rgb,.35)*lerp(.72h,1.0h,attenuation),s.Alpha);
        }
        void surf(Input input,inout SurfaceOutput output)
        {
            fixed4 base=tex2D(_MainTex,input.uv_MainTex)*_Color;
            base.rgb*=lerp(fixed3(1,1,1),input.color.rgb,input.color.a);
            float2 detailUV=input.uv_MainTex*_DetailTiling;
            float2 atlasUV=_AtlasRect.xy+(.008+frac(detailUV)*.984)*_AtlasRect.zw;
            fixed3 detail=tex2Dgrad(_DetailAtlas,atlasUV,ddx(detailUV)*_AtlasRect.zw,ddy(detailUV)*_AtlasRect.zw).rgb;
            base.rgb*=lerp(1,clamp(dot(detail,fixed3(.333,.333,.334))/max(.03,_DetailMean),.65,1.4),_UseAtlas);
            half weave=1-_Fabric*.035*(.5+.5*sin(input.uv_MainTex.x*1100)*sin(input.uv_MainTex.y*1300));
            output.Albedo=base.rgb*weave;output.Emission=base.rgb*.04;output.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
