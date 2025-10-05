Shader "Westmark/SpecularSparkles_URP"
{
    Properties
    {
        _Color ("Color", Color) = (.5,.5,.5,1)
        _SpecColor ("Specular Color", Color) = (.5,.5,.5,1)
        _MainTex ("Texture", 2D) = "white" {}
        _SpecPow ("Specular Power", Range(1,50)) = 24
        _GlitterPow ("Glitter Power", Range(1,50)) = 5
        _SparkleDepth ("Sparkle Depth", Range(0,5)) = 1
        _NoiseScale ("Noise Scale", Range(0,5)) = 1
        _AnimSpeed ("Animation Speed", Range(0,5)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity_render-pipelines.core/ShaderLibrary/Lighting.hlsl"
            // если есть SparklesCG, Simplex3D, нужно портировать / включить

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _SpecColor;
            float _SpecPow;
            float _GlitterPow;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos;
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normal));
                return OUT;
            }

            fixed4 frag(Varyings IN) : SV_Target
            {
                float3 normal = IN.normalWS;
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                float3 refl = reflect(-viewDir, normal);

                float3 lightDir = _MainLightDirection; 
                float diff = max(0, dot(normal, lightDir)) * 0.5 + 0.5;
                float spec = saturate(dot(refl, lightDir));
                float glitter = pow(spec, _GlitterPow);
                spec = pow(spec, _SpecPow);

                // здесь нужно реализовать функцию Sparkles(...) вручную
                float sparkles = 0;

                fixed4 col = tex2D(_MainTex, IN.uv) * _Color * diff;
                col += _SpecColor * (saturate(sparkles * glitter * 5) + spec);
                return col;
            }
            ENDHLSL
        }
    }
}
