Shader "LaserCorridor/Emissive Beam"
{
    Properties
    {
        _BaseColor("Colour",Color)=(1,0.4,0.02,1)
        [HDR] _EmissionColor("Emission",Color)=(3,0.5,0.01,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Emissive"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input) { Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); return output; }
            half4 Frag(Varyings input) : SV_Target { return half4(_EmissionColor.rgb,1); }
            ENDHLSL
        }
    }
}
