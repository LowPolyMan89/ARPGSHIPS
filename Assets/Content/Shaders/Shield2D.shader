Shader "Ships/Shield2D"
{
	Properties
	{
		_Color ("Tint", Color) = (0.2, 0.8, 1, 1)
		_MainTex ("Sprite", 2D) = "white" {}
		_NoiseTex ("Noise", 2D) = "white" {}

		_Charge ("Charge", Range(0,1)) = 1
		_HitPos ("Hit Position (World)", Vector) = (0,0,0,0)
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
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _NoiseTex;

			float4 _Color;
			float _Charge;

			float4 _HitPos;
			float _HitStrength;
			float _HitTime;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 worldXY : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldXY = world.xy;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 baseCol = tex2D(_MainTex, i.uv);

				if (baseCol.a <= 0.001 || _Charge <= 0.001)
					discard;

				float edge = pow(saturate(baseCol.a), 3.0);

				float dist = distance(i.worldXY, _HitPos.xy);
				float wave = sin(dist * 35.0 - _HitTime * 10.0);
				float falloff = exp(-dist * 8.0);

				float ripple = max(0, wave) * falloff * _HitStrength;

				float3 noise = tex2D(_NoiseTex, i.uv * 3.0 + float2(_HitTime * 0.05, 0)).rgb;

				float glow = (edge * 0.6 + ripple * 1.5) * _Charge;
				glow = max(glow, 0);

				if (glow <= 0.001)
					discard;

				float3 color = (_Color.rgb * (0.8 + noise * 0.2)) * glow;
				return float4(color, glow);
			}
			ENDCG
		}
	}
}
