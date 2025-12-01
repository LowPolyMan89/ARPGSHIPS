Shader "Ships/ShieldSectorShield"
{
    Properties
    {
        _MainTex ("Shield Sprite", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "white" {}

        _Color ("Tint", Color) = (0.2, 0.8, 1, 1)

        _Charge ("Charge", Range(0,1)) = 1

        _SectorStart ("Sector Start", Range(-180,180)) = -45
        _SectorEnd ("Sector End", Range(-180,180)) = 45

        _HitPoint ("Hit Point UV", Vector) = (0.5, 0.5, 0, 0)
        _HitStrength ("Hit Strength", Range(0,1)) = 0
        _HitTime ("Hit Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NoiseTex;

            fixed4 _Color;
            float _Charge;

            float _SectorStart;
            float _SectorEnd;

            float4 _HitPoint; // xy = UV
            float _HitStrength;
            float _HitTime;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // helper: angle in degrees (-180..180)
            float GetAngleDeg(float2 uv)
            {
                float2 p = (uv - 0.5) * 2.0; // центр в (0,0)

                // считаем угол как SignedAngle(Vector2.up, dir):
                float a = atan2(p.x, p.y); // сначала X, потом Y!
                float deg = -degrees(a); // минус, чтобы право было -90

                return deg; // -180..180
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 main = tex2D(_MainTex, i.uv);
                if (main.a <= 0.001)
                    discard;

                // Новое: если нет заряда – вообще не рисуем щит
                if (_Charge <= 0.001)
                    discard;

                // --- СЕКТОРНАЯ МАСКА ---
                float angleDeg = GetAngleDeg(i.uv);

                float inSector = 0.0;

                if (_SectorStart <= _SectorEnd)
                {
                    float a1 = step(_SectorStart, angleDeg);
                    float a2 = step(angleDeg, _SectorEnd);
                    inSector = a1 * a2;
                }
                else
                {
                    float left = step(_SectorStart, angleDeg);
                    float right = step(angleDeg, _SectorEnd);
                    inSector = saturate(left + right);
                }

                // если пиксель вне сектора – не рендерим
                if (inSector <= 0.001)
                    discard;

                // --- RIM ---
                float2 p = (i.uv - 0.5) * 2.0;
                float r = length(p);
                float rim = saturate(1.0 - r);
                rim = pow(rim, 3.0);

                // --- ШУМ ---
                float2 nUV = i.uv * 2.0 + float2(_Time.y * 0.2, _Time.y * 0.3);
                float noise = tex2D(_NoiseTex, nUV).r;

                // --- RIPPLE ---
                float2 hitUV = _HitPoint.xy;
                float d = distance(i.uv, hitUV);
                float wave = sin(d * 40.0 - _HitTime * 8.0);
                float falloff = exp(-d * 10.0);
                float ripple = max(0.0, wave) * falloff * _HitStrength;

                // --- GLOW ---
                float glow = (rim * 0.7 + noise * 0.3 + ripple) * _Charge;

                fixed3 col = _Color.rgb * (0.4 + 0.6 * _Charge) * glow;

                float alpha = main.a * glow;

                // Ещё одна защита: если альфа 0 – discard
                if (alpha <= 0.001)
                    discard;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}