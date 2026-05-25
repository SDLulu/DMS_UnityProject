Shader "DMS/UI/TitleNeonOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0,1,1,1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.65
        _ScanStrength ("Scan Strength", Range(0, 1)) = 0.25
        _ScanDensity ("Scan Density", Range(8, 240)) = 72
        _ScanSpeed ("Scan Speed", Range(-8, 8)) = 0.7
        _SweepStrength ("Sweep Strength", Range(0, 1)) = 0.45
        _SweepSpeed ("Sweep Speed", Range(-4, 4)) = 0.55
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.08

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowStrength;
            float _ScanStrength;
            float _ScanDensity;
            float _ScanSpeed;
            float _SweepStrength;
            float _SweepSpeed;
            float _FlickerStrength;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                fixed4 col = IN.color;
                col.a *= tex.a;

                float t = _Time.y;
                float scan = 0.5 + 0.5 * sin((IN.texcoord.y + t * _ScanSpeed) * _ScanDensity);
                scan = pow(scan, 5.0);

                float sweepPhase = frac(t * _SweepSpeed);
                float sweep = 1.0 - saturate(abs(IN.texcoord.x - sweepPhase) * 8.0);
                sweep = sweep * sweep;

                float flicker = 1.0 + (sin(t * 17.0) * 0.5 + sin(t * 41.0) * 0.5) * _FlickerStrength;
                float glow = saturate(scan * _ScanStrength + sweep * _SweepStrength);

                col.rgb = col.rgb * flicker + _GlowColor.rgb * glow * _GlowStrength * col.a;
                col.a = saturate(col.a * (1.0 + glow * 0.45));

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
