Shader "UI/UniversalBar"
{
    Properties
    {
        [Header(Base Textures)]
        _MainTex ("Base Texture", 2D) = "white" {}
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _GradientTex ("Gradient Texture", 2D) = "white" {}

        [Header(Fill Settings)]
        _Fill ("Fill Amount", Range(0,1)) = 1
        _InvertFill ("Invert Fill", Range(0,1)) = 0
        _FillMode ("Fill Mode (0=Both,1=FromMin,2=FromMax)", Range(0,2)) = 0
        _CutAxis ("Cut Axis (0=X,1=Y)", Range(0,1)) = 0

        [Header(UV Area)]
        _UVMinX ("UV Min X", Range(0,1)) = 0
        _UVMaxX ("UV Max X", Range(0,1)) = 1
        _UVMinY ("UV Min Y", Range(0,1)) = 0
        _UVMaxY ("UV Max Y", Range(0,1)) = 1

        _CenterX ("Center X", Range(0,1)) = 0.5
        _CenterY ("Center Y", Range(0,1)) = 0.5

        [Header(Gradient)]
        _GradientDir ("Gradient Dir", Range(0,1)) = 0

        [Header(Pulse)]
        _PulseEnabled ("Pulse Enabled", Range(0,1)) = 0
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 3
        _PulseAmplitude ("Pulse Amplitude", Range(0,0.5)) = 0.05
        _PulseThreshold ("Pulse Threshold", Range(0,1)) = 0.3

        [Header(Color Tint)]
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    CustomEditor "UniversalBarEditor"
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OverlayTex;
            sampler2D _GradientTex;

            float _Fill;
            float _InvertFill;
            float _FillMode;
            float _CutAxis;

            float _UVMinX, _UVMaxX;
            float _UVMinY, _UVMaxY;

            float _CenterX, _CenterY;

            float _GradientDir;

            float _PulseEnabled;
            float _PulseSpeed;
            float _PulseAmplitude;
            float _PulseThreshold;

            float4 _Tint;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float rawAxis = lerp(i.uv.x, i.uv.y, _CutAxis);

                float uvMin = lerp(_UVMinX, _UVMinY, _CutAxis);
                float uvMax = lerp(_UVMaxX, _UVMaxY, _CutAxis);

                float range = max(uvMax - uvMin, 1e-5);
                float t = (rawAxis - uvMin) / range;

                float centerLocal = lerp(_CenterX, _CenterY, _CutAxis);
                t = t - centerLocal + 0.5;

                float fill = lerp(_Fill, 1.0 - _Fill, _InvertFill);

                if (_PulseEnabled > 0.5 && fill < _PulseThreshold)
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmplitude;
                    fill = saturate(fill + pulse);
                }

                fill = saturate(fill);

                float minB = 0;
                float maxB = 1;

                if (_FillMode < 0.5)
                {
                    float halfFill = fill * 0.5;
                    minB = 0.5 - halfFill;
                    maxB = 0.5 + halfFill;
                }
                else if (_FillMode < 1.5)
                {
                    minB = 0.0;
                    maxB = fill;
                }
                else
                {
                    minB = 1.0 - fill;
                    maxB = 1.0;
                }

                if (t < minB || t > maxB)
                    discard;

                float gradPos = fill;
                gradPos = lerp(gradPos, 1.0 - gradPos, _GradientDir);
                gradPos = saturate(gradPos);

                float4 gradCol = tex2D(_GradientTex, float2(gradPos, 0.5));

                float4 baseCol = tex2D(_MainTex, i.uv);
                float4 overlayCol = tex2D(_OverlayTex, i.uv);

                float4 finalCol = baseCol * overlayCol * gradCol * _Tint;

                if (finalCol.a <= 0.001)
                    discard;

                return finalCol;
            }
            ENDCG
        }
    }
}
