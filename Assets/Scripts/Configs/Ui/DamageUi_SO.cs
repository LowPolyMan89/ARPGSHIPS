using UnityEngine;

namespace Ships
{
	[CreateAssetMenu(fileName = "DamageUi_SO", menuName = "Scriptable Objects/DamageUi_SO")]
	public class DamageUi_SO : ScriptableObject
	{
		public Color KineticColor;
		public Color EnergyColor;
		public Color ThermalColor;
		public int NormalFontSize = 1;
		public int CritFontSize = 2;
		public float Lifetime = 0.8f;
		public float MoveDistance = 1f;
		public Vector2 Direction = Vector2.up;
		public float RandomAngle = 20f;
		public AnimationCurve MoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		public AnimationCurve AlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
		public DamageUiElement DamageUiElementPrefab;
	}
}
