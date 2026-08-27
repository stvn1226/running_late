Shader "UI/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.2, 1)
        _OutlineWidth ("Outline Width (Pixels)", Range(0, 16)) = 3.0
        _OutlineGlow ("Outline Glow Multiplier", Range(1, 3)) = 1.2
        _PulseSpeed ("Pulse Speed (0 = off)", Range(0, 10)) = 0.0
        _PulseMin ("Pulse Min Alpha", Range(0, 1)) = 0.4

        [Header(UI Stencil and Masking)]
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineGlow;
            float _PulseSpeed;
            float _PulseMin;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // 16-direction circular sampling offsets for smooth round outlines
            static const float2 directions[16] = {
                float2(1.0, 0.0),
                float2(-1.0, 0.0),
                float2(0.0, 1.0),
                float2(0.0, -1.0),
                float2(0.7071, 0.7071),
                float2(-0.7071, 0.7071),
                float2(0.7071, -0.7071),
                float2(-0.7071, -0.7071),
                float2(0.9239, 0.3827),
                float2(-0.9239, 0.3827),
                float2(0.9239, -0.3827),
                float2(-0.9239, -0.3827),
                float2(0.3827, 0.9239),
                float2(-0.3827, 0.9239),
                float2(0.3827, -0.9239),
                float2(-0.3827, -0.9239)
            };

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 originalColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                originalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (originalColor.a - 0.001);
                #endif

                if (_OutlineWidth <= 0.001)
                {
                    return originalColor;
                }

                // Sample in a 16-point circle around the pixel to find maximum surrounding alpha
                float2 pixelOffset = _MainTex_TexelSize.xy * _OutlineWidth;
                float maxSampledAlpha = 0.0;

                [unroll]
                for (int i = 0; i < 16; i++)
                {
                    float2 sampleUV = IN.texcoord + directions[i] * pixelOffset;
                    fixed4 sampleColor = (tex2D(_MainTex, sampleUV) + _TextureSampleAdd);
                    maxSampledAlpha = max(maxSampledAlpha, sampleColor.a);
                }

                // Calculate outline factor (where outer alpha exists but inner alpha is transparent)
                float outlineFactor = saturate(maxSampledAlpha - originalColor.a);

                // Optional pulsation
                float pulseFactor = 1.0;
                if (_PulseSpeed > 0.001)
                {
                    pulseFactor = lerp(_PulseMin, 1.0, 0.5 + 0.5 * sin(_Time.y * _PulseSpeed * 3.14159));
                }

                fixed4 outCol = _OutlineColor * _OutlineGlow;
                outCol.a = _OutlineColor.a * pulseFactor;

                // Composite: inner sprite rendered crisply on top of the outer glow outline
                fixed4 finalColor;
                finalColor.rgb = lerp(outCol.rgb, originalColor.rgb, originalColor.a);
                finalColor.a = max(originalColor.a, outlineFactor * outCol.a);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}
