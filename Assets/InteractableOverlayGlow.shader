Shader "Custom/InteractableOverlayGlow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (0, 0.9, 1, 1)
        _OutlineWidth ("Outline Thickness", Range(0.0005, 0.008)) = 0.002
        _FresnelPower ("Edge Rim Sharpness", Range(0.5, 6.0)) = 2.5
        _BlinkSpeed ("Blink / Pulse Speed", Range(0, 8)) = 3.0
        _MinGlow ("Min Glow (Idle)", Range(0.0, 1.0)) = 0.25
        _MaxGlow ("Max Glow (Peak)", Range(0.0, 3.0)) = 1.6
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+100" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend One One // Additive overlay

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float3 viewDirWS    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _OutlineWidth;
                float _FresnelPower;
                float _BlinkSpeed;
                float _MinGlow;
                float _MaxGlow;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Slight normal expansion to ensure outline sits just proud of mesh surface
                float3 extrudedOS = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                float3 positionWS = TransformObjectToWorld(extrudedOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewDirWS);
                float rim = pow(1.0 - saturate(dot(n, v)), _FresnelPower);

                float pulse = 0.5 + 0.5 * sin(_Time.y * _BlinkSpeed);
                float intensity = lerp(_MinGlow, _MaxGlow, pulse);

                // Additive glow composed of rim + subtle face highlight
                float glow = rim * 0.85 + 0.15;
                return half4(_GlowColor.rgb * glow * intensity, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
