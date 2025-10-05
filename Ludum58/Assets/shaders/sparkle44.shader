Shader "Custom/URP_Sparkle_NoFog"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _SparkleColor ("Sparkle Color", Color) = (1,1,1,1)
        _SparkleThreshold ("Threshold", Range(0,1)) = 0.9
        _NoiseScale ("Noise Scale", Float) = 5.0
        _AnimSpeed ("Anim Speed", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "SparklePass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // fog макросы убраны, нет multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float4 _SparkleColor;
            float _SparkleThreshold;
            float _NoiseScale;
            float _AnimSpeed;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                return OUT;
            }

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float ndotv = saturate(dot(IN.normalWS, viewDir));

                float2 uvn = IN.uv * _NoiseScale + _AnimSpeed * _Time.y;
                float noise = rand(uvn);

                float sparkleMask = smoothstep(_SparkleThreshold, 1.0, noise * ndotv);

                float h = frac(_Time.y * 0.5 + noise);
                float3 rgb = float3(h, 1.0 - h, sin(h * 6.28318) * 0.5 + 0.5);

                float4 sparkleCol = float4(rgb, 1.0) * sparkleMask;

                return baseCol + sparkleCol;
            }
            ENDHLSL
        }
    }
}
