Shader "DMS/PlayerDashAfterimage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.15, 1, 0.92, 1)
        _GlowColor ("Glow Color", Color) = (0, 0.95, 1, 1)
        _ChannelOffset ("Channel Offset", Range(0, 0.02)) = 0.006
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "DashAfterimage"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GlowColor;
                float _ChannelOffset;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 redUv = input.uv + float2(_ChannelOffset, 0);
                float2 blueUv = input.uv - float2(_ChannelOffset, 0);

                half4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half red = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, redUv).r;
                half blue = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, blueUv).b;
                half alpha = center.a * input.color.a;

                half3 channelSplit = half3(red, center.g, blue);
                half3 cyanTint = lerp(channelSplit, _GlowColor.rgb, 0.62) * _Color.rgb;

                return half4(cyanTint * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
