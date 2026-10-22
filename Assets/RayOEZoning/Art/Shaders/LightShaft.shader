// Additive, soft-edged light cone for the CCTV searchlights. The mesh is rebuilt every frame by
// ScannerVisual with each radial ray clipped against obstructions, so the shaft stops at cover.
Shader "RayOEZoning/Light Shaft"
{
    Properties
    {
        _Color ("Color", Color) = (0.82, 0.9, 1, 1)
        _Intensity ("Intensity", Float) = 0.085
        _Falloff ("Length falloff", Float) = 1.35
        _EdgePower ("Edge softness", Float) = 1.6
        _DepthFade ("Surface fade distance", Float) = 0.55
        _NearFade ("Viewer fade distance", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "LightShaft"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Falloff;
                half _EdgePower;
                half _DepthFade;
                half _NearFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = abs(dot(normalize(input.normalWS), view));
                half edge = pow(saturate(facing), _EdgePower);
                // Fade in over the first few percent so the converging rays do not stack into a hot streak at the lens.
                half along = pow(saturate(1.0 - input.uv.y), _Falloff) * smoothstep(0.0, 0.08, input.uv.y);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragmentDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                half surface = saturate((sceneDepth - fragmentDepth) / max(0.001, _DepthFade));
                half viewer = saturate((fragmentDepth - 0.25) / max(0.001, _NearFade));

                half3 color = _Color.rgb * (_Intensity * edge * along * surface * viewer);
                color = MixFogColor(color, half3(0, 0, 0), input.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
