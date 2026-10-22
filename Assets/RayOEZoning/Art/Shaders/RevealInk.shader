// Hidden writing that exists only where the held flashlight's cone falls. RevealInk.Push sets the
// light's position, direction, range and cone every frame; outside the cone the ink is fully clear.
Shader "RayOEZoning/Reveal Ink"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Reveal"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            // xyz position, w = 1 while the flashlight is on.
            float4 _RevealLightPosition;
            // xyz direction, w = range.
            float4 _RevealLightDirection;
            // x = cosine of the cone's half angle, y = cosine where the ink reaches full strength.
            float4 _RevealLightCone;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 toInk = input.positionWS - _RevealLightPosition.xyz;
                float distance = length(toInk);
                float facing = dot(toInk / max(distance, 1e-4), _RevealLightDirection.xyz);
                half cone = smoothstep(_RevealLightCone.x, _RevealLightCone.y, facing);
                half reach = 1.0 - smoothstep(_RevealLightDirection.w * 0.85, _RevealLightDirection.w, distance);
                half alpha = _BaseColor.a * cone * reach * _RevealLightPosition.w;
                clip(alpha - 0.004);
                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
