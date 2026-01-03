using System.Collections;
using TMPro;
using UnityEngine;

namespace Ships
{
	public class DamageUiElement : MonoBehaviour
	{
		public Canvas WorldSpaceCanvas;
		public TMP_Text DamageText;

		private Coroutine _routine;

		public void ShowDamage(CalculatedDamage calc, DamageUi_SO config)
		{
			if (config == null || calc == null || calc.FinalDamage <= 0f)
			{
				Destroy(gameObject);
				return;
			}

			if (WorldSpaceCanvas == null)
				WorldSpaceCanvas = GetComponentInChildren<Canvas>(true);
			if (DamageText == null)
				DamageText = GetComponentInChildren<TMP_Text>(true);

			var root = WorldSpaceCanvas != null ? WorldSpaceCanvas.transform : transform;

			root.position = calc.HitPoint;

			if (DamageText != null)
			{
				DamageText.text = Mathf.CeilToInt(calc.FinalDamage).ToString();
				DamageText.fontSize = calc.IsCrit ? config.CritFontSize : config.NormalFontSize;
				DamageText.color = ResolveDamageColor(calc, config);
			}

			if (_routine != null)
				StopCoroutine(_routine);
			_routine = StartCoroutine(AnimateRoutine(config, root));
		}

		private IEnumerator AnimateRoutine(DamageUi_SO config, Transform root)
		{
			var lifetime = Mathf.Max(0.01f, config.Lifetime);
			var start = root.position;
			var dir = GetFlyDirection(config);
			var end = start + (Vector3)(dir * config.MoveDistance);

			var baseColor = DamageText != null ? DamageText.color : Color.white;

			for (float t = 0f; t < lifetime; t += Time.deltaTime)
			{
				var normalized = t / lifetime;
				var moveT = EvaluateCurve(config.MoveCurve, normalized);
				root.position = Vector3.LerpUnclamped(start, end, moveT);

				if (DamageText != null)
				{
					var alpha = Mathf.Clamp01(EvaluateCurve(config.AlphaCurve, normalized));
					var c = baseColor;
					c.a = baseColor.a * alpha;
					DamageText.color = c;
				}

				yield return null;
			}

			Destroy(gameObject);
		}

		private static Vector2 GetFlyDirection(DamageUi_SO config)
		{
			var dir = config.Direction.sqrMagnitude > 0.001f ? config.Direction.normalized : Vector2.up;
			if (config.RandomAngle > 0f)
			{
				var angle = Random.Range(-config.RandomAngle, config.RandomAngle);
				dir = Quaternion.Euler(0f, 0f, angle) * dir;
			}

			return dir;
		}

		private static float EvaluateCurve(AnimationCurve curve, float t)
		{
			if (curve == null || curve.length == 0)
				return t;
			return curve.Evaluate(t);
		}

		private static Color ResolveDamageColor(CalculatedDamage calc, DamageUi_SO config)
		{
			if (calc.SourceWeapon?.Model != null && calc.SourceWeapon.Model.HasDamageType)
			{
				switch (calc.SourceWeapon.Model.DamageType)
				{
					case Tags.Kinetic:
						return config.KineticColor;
					case Tags.Energy:
						return config.EnergyColor;
					case Tags.Thermal:
						return config.ThermalColor;
				}
			}

			return config.KineticColor;
		}
	}
}
