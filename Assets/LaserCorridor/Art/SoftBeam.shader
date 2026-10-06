Shader "LaserCorridor/Soft Beam"
{
    Properties{_BaseColor("Color",Color)=(1,1,1,1) [HDR]_EmissionColor("Emission",Color)=(3,6,8,1) _Halo("Halo",Float)=0 _BeamClock("Gameplay clock",Float)=0 _GridFlow("Flowing lattice",Float)=0}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+20"}
        Pass
        {
            Tags{"LightMode"="UniversalForwardOnly"} Blend SrcAlpha One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)float4 _BaseColor,_EmissionColor;float _Halo,_BeamClock,_GridFlow;CBUFFER_END
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                float edge=abs(i.uv.y-.5)*2,glow=pow(saturate(1-edge),2);
                float shimmer=1+.035*sin(i.uv.x*59+_BeamClock*8);
                float phase=abs(frac(i.uv.x*3-_BeamClock*.7)-.5);
                float packet=1-smoothstep(.02,.16,phase);
                float flow=lerp(1,.55+packet*2.2,_GridFlow);
                float alpha=lerp(1,glow*_BaseColor.a,_Halo);
                return half4(_EmissionColor.rgb*shimmer*flow,alpha);
            }
            ENDHLSL
        }
    }
}
