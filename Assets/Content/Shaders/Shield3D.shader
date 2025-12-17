Shader "Ships/Shield3D"
{
	Properties
	{
		_Color ("Tint", Color) = (0.2, 0.8, 1, 1)
		_Charge ("Charge", Range(0,1)) = 1

		// Эффект попадания
		_HitPos ("Hit Position (World)", Vector) = (0,0,0,0)
		_HitStrength ("Hit Strength", Range(0,1)) = 0
		_HitTime ("Hit Time", Float) = 0

		_NoiseTex ("Noise", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend One OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _NoiseTex;
			float4 _Color;
			float _Charge;

			float3 _HitPos;
			float _HitStrength;
			float _HitTime;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 worldPos : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Если щит разряжен — тускнеет/исчезает
				if (_Charge <= 0.001)
					discard;

				//-------------------------------------------------------
				// 1) RIM эффект — щит ярче по краям
				//-------------------------------------------------------
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
				float3 normal = normalize(i.worldPos - _HitPos); // временная нормаль
				float rim = pow(1.0 - saturate(dot(viewDir, normal)), 3.0);

				//-------------------------------------------------------
				// 2) Ripple от удара
				//-------------------------------------------------------
				float dist = distance(i.worldPos, _HitPos);

				float wave = sin(dist * 35.0 - _HitTime * 10.0);
				float falloff = exp(-dist * 8.0);

				float ripple = max(0, wave) * falloff * _HitStrength;

				//-------------------------------------------------------
				// 3) Смешиваем
				//-------------------------------------------------------
				float glow = rim * 0.6 + ripple * 1.5;
				glow *= _Charge;

				if (glow <= 0.001)
					discard;

				return float4(_Color.rgb * glow, glow);
			}
			ENDCG
		}
	}
}
