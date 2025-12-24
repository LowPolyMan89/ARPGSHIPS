using UnityEngine;

namespace Ships
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class WeaponSectorVisual : WeaponArcRenderer
	{
		[Header("Source")]
		[SerializeField] private WeaponBase _weapon;
		[SerializeField] private bool _useWeaponStats = true;

		[Header("Manual Settings (fallback)")]
		public float Range = 20f;        // дальность сектора
		public float Angle = 60f;        // угол сектора (в градусах)
		public float InnerRadius = 0f;   // внутренняя дуга (обычно 0)

		[Header("Quality")]
		public int segments = 48;

		private float _lastRange = -1f;
		private float _lastArc = -1f;
		private ArcSpace _lastSpace = ArcSpace.WorldXY;

		protected override void Awake()
		{
			base.Awake();
			_disableObjectOnHide = false; // keep object active to refresh when stats appear

			if (_weapon == null)
				_weapon = GetComponentInParent<WeaponBase>();

			var seg = Mathf.Max(4, segments);
			_minSegments = seg;
			_maxSegments = seg;
		}

		private void LateUpdate()
		{
			var arcDeg = ResolveArc();
			var radius = ResolveRange();
			var space = ResolveSpace();
			var scale = ResolveScale(space);

			if (arcDeg <= 0f || radius <= 0f || scale <= 0f)
			{
				ClearMesh();
				return;
			}

			var radiusLocal = radius / scale;
			var innerLocal = InnerRadius > 0f ? InnerRadius / scale : 0f;

			if (Mathf.Approximately(arcDeg, _lastArc) &&
			    Mathf.Approximately(radiusLocal, _lastRange) &&
			    space == _lastSpace)
			{
				return;
			}

			RenderArc(arcDeg, radiusLocal, Vector3.zero, space, innerLocal);

			_lastArc = arcDeg;
			_lastRange = radiusLocal;
			_lastSpace = space;
		}

		private float ResolveArc()
		{
			if (_useWeaponStats && _weapon != null)
				return _weapon.FireArcDeg <= 0f ? 360f : _weapon.FireArcDeg;

			return Angle;
		}

		private float ResolveRange()
		{
			if (_useWeaponStats && _weapon?.Model?.Stats != null)
			{
				if (_weapon.Model.Stats.TryGetStat(StatType.FireRange, out var stat))
					return stat.Current;

				return _weapon.Model.Stats.GetStat(StatType.FireRange).Current;
			}

			return Range;
		}

		private ArcSpace ResolveSpace()
		{
			if (Battle.Instance != null && Battle.Instance.Plane == Battle.WorldPlane.XZ)
				return ArcSpace.WorldXZ;

			return ArcSpace.WorldXY;
		}

		private float ResolveScale(ArcSpace space)
		{
			var ls = transform.lossyScale;
			if (space == ArcSpace.WorldXZ)
				return Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z));

			return Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
		}
	}
}
