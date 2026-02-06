Shader "TheBunkerGames/UI/CRT_Glitch_UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.5
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.1
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.05)) = 0.01
        _JitterIntensity ("Jitter Intensity", Range(0, 0.1)) = 0.02
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float _GlitchIntensity;
            float _ScanlineIntensity;
            float _NoiseIntensity;
            float _ChromaticAberration;
            float _JitterIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;

            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float time = _Time.y;

                // Jitter / Shake
                if (_GlitchIntensity > 0.1)
                {
                    float jitter = (rand(float2(time, 0)) - 0.5) * _JitterIntensity * _GlitchIntensity;
                    if (rand(float2(0, floor(uv.y * 20.0))) < _GlitchIntensity * 0.2)
                    {
                        uv.x += jitter;
                    }
                }

                // Chromatic Aberration
                float ab = _ChromaticAberration * _GlitchIntensity;
                fixed4 r = tex2D(_MainTex, uv + float2(ab, 0));
                fixed4 g = tex2D(_MainTex, uv);
                fixed4 b = tex2D(_MainTex, uv - float2(ab, 0));
                
                fixed4 color = fixed4(r.r, g.g, b.b, g.a);

                // Scanlines
                float scanline = sin(uv.y * 800.0 + time * 5.0) * 0.1 * _ScanlineIntensity;
                color.rgb -= scanline;

                // Noise
                float noise = (rand(uv + time) - 0.5) * _NoiseIntensity * (_GlitchIntensity + 0.5);
                color.rgb += noise;

                // Fade edges (CRT feel)
                float2 center = uv - 0.5;
                float dist = dot(center, center);
                color.rgb *= (1.0 - dist * 0.5);

                color *= IN.color;
                return color;
            }
            ENDCG
        }
    }
}
