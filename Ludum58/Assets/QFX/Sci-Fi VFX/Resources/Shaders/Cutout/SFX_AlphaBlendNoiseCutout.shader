Shader "QFX/SFX_URP/AlphaBlendNoiseCutout"
{
    Properties
    {
        // те же поля, что у исходного
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _Noise("Noise", 2D) = "white" {}
        _Cutoff("Mask Clip Value", Range(0,1)) = 0.5
        _Speed("Speed (XY)", Vector) = (0,0,0,0)
        _AlphaCutout("Alpha Cutout", Range(0,1)) = 0
        _MainTex("Main Tex", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "FORWARD_UNLIT"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 3.0
            // SRP Batcher friendly
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;

                float4 _MainTex_ST;
                float4 _Noise_ST;

                float  _Cutoff;
                float  _AlphaCutout;
                float2 _Speed;        // XY используются для скролла шума
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_Noise);   SAMPLER(sampler_Noise);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain     : TEXCOORD0;
                float2 uvNoise    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS);

                o.uvMain  = TRANSFORM_TEX(v.uv0, _MainTex);

                // скроллинг шума: uv * ST + t * speed
                float2 uvN = TRANSFORM_TEX(v.uv0, _Noise);
                float t = _Time.y;
                uvN += _Speed * t;
                o.uvNoise = uvN;

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvMain);
                float  noiseR  = SAMPLE_TEXTURE2D(_Noise,   sampler_Noise,   i.uvNoise).r;

                // отсечение по шуму
                clip(noiseR - _Cutoff);

                // финальная альфа: альфа текстуры * цвет * (1 - AlphaCutout)
                float a = baseCol.a * _Color.a * saturate(1.0 - _AlphaCutout);

                // цвет: модульный цвет * текстура
                float3 rgb = baseCol.rgb * _Color.rgb;

                return float4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
