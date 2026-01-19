Shader "Ships/Shield3D"
{
	Properties
	{
		[HDR] _Color ("Emission Color", Color) = (0.2, 0.8, 1, 1)
		[HDR] _HitColor ("Hit Color (A=Strength)", Color) = (1, 1, 1, 1)
		[HDR] _BreakColor ("Break Color (A=Strength)", Color) = (1, 0.6, 0.2, 1)
		_EmissionStrength ("Emission Strength", Float) = 1

		_Charge ("Charge", Range(0,1)) = 1

		_HitFalloff ("Hit Falloff", Float) = 8
		_HitBoost ("Hit Boost", Float) = 1
		_HitWaveFrequency ("Hit Wave Frequency", Float) = 35
		_HitWaveSpeed ("Hit Wave Speed", Float) = 10
		_HitWaveStrength ("Hit Wave Strength", Float) = 0.6
		_HitMaskMin ("Hit Mask Min", Range(0,1)) = 0.35

		_IdleGlintStrength ("Idle Glint Strength", Range(0,2)) = 0.6
		_IdleGlintScale ("Idle Glint Scale", Float) = 1
		_IdleGlintSpeed ("Idle Glint Speed", Float) = 0.5
		_IdleGlintThreshold ("Idle Glint Threshold", Range(0,1)) = 0.7

		_RimStrength ("Rim Strength", Range(0,2)) = 0.6
		_RimPower ("Rim Power", Range(0.5,8)) = 3

		_Appear ("Appear", Range(0,1)) = 0
		_Break ("Break", Range(0,1)) = 0

		_MaskTex ("Mask (A)", 2D) = "white" {}
		_MaskRotation ("Mask Rotation", Range(0,360)) = 0
		_MaskRotationSpeed ("Mask Rotation Speed", Float) = 0
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

			sampler2D _MaskTex;
			float4 _MaskTex_ST;
			sampler2D _NoiseTex;

			float4 _Color;
			float4 _HitColor;
			float4 _BreakColor;
			float _EmissionStrength;
			float _Charge;

			float4 _HitPos[6];
			float _HitStrength[6];
			float _HitTime[6];
			float _HitFalloff;
			float _HitBoost;
			float _HitWaveFrequency;
			float _HitWaveSpeed;
			float _HitWaveStrength;
			float _HitMaskMin;

			float _IdleGlintStrength;
			float _IdleGlintScale;
			float _IdleGlintSpeed;
			float _IdleGlintThreshold;

			float _RimStrength;
			float _RimPower;

			float _Appear;
			float _Break;

			float _MaskRotation;
			float _MaskRotationSpeed;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float2 uv : TEXCOORD2;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.uv = TRANSFORM_TEX(v.uv, _MaskTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float charge = saturate(_Charge);
				float active = step(0.001, charge);

				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
				float3 normal = normalize(i.worldNormal);
				float rim = pow(1.0 - saturate(dot(viewDir, normal)), _RimPower) * _RimStrength;

				float2 noiseUv = i.uv * _IdleGlintScale + _Time.y * _IdleGlintSpeed;
				float noise = tex2D(_NoiseTex, noiseUv).r;
				float glint = saturate((noise - _IdleGlintThreshold) / max(0.001, 1.0 - _IdleGlintThreshold));
				glint = pow(glint, 2.0) * _IdleGlintStrength;

				float hitSum = 0.0;
				for (int h = 0; h < 6; h++)
				{
					float strength = _HitStrength[h];
					if (strength <= 0.001)
						continue;

					float dist = distance(i.worldPos, _HitPos[h].xyz);
					float hitCore = exp(-dist * _HitFalloff);
					float wave = sin(dist * _HitWaveFrequency - _HitTime[h] * _HitWaveSpeed);
					float hitWave = max(0, wave) * hitCore * _HitWaveStrength;
					hitSum += (hitCore + hitWave) * strength;
				}
				float hit = hitSum * _HitBoost;

				float idle = active * (glint + rim * glint);
				idle *= lerp(0.2, 1.0, charge);
				float appear = active * _Appear;

				float angle = radians(_MaskRotation + _Time.y * _MaskRotationSpeed);
				float s = sin(angle);
				float c = cos(angle);
				float2 maskUv = i.uv - 0.5;
				maskUv = float2(maskUv.x * c - maskUv.y * s, maskUv.x * s + maskUv.y * c) + 0.5;

				float mask = tex2D(_MaskTex, maskUv).a;
				float hitMask = max(mask, _HitMaskMin);
				float idleIntensity = mask * (idle + appear);
				float hitIntensity = hitMask * hit;
				float breakIntensity = mask * _Break;
				float intensity = saturate(idleIntensity + hitIntensity + breakIntensity);

				if (intensity <= 0.001)
					discard;

				float3 color = _Color.rgb * _EmissionStrength * (idleIntensity * _Color.a);
				color += _HitColor.rgb * _EmissionStrength * (hitIntensity * _HitColor.a);
				color += _BreakColor.rgb * _EmissionStrength * (breakIntensity * _BreakColor.a);
				return float4(color, intensity);
			}
			ENDCG
		}
	}
}
