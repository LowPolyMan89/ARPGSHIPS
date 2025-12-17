using UnityEngine;

namespace Ships
{
	public class WeaponTargeting : MonoBehaviour
	{
		public WeaponBase Weapon;
		public float AimTolerance = 3f;
		public float RotationSpeedDeg = 720f;

		private readonly TargetFinder _finder = new();
		private ShipBase _owner;

		private void Awake()
		{
			_owner = GetComponentInParent<ShipBase>();
		}

		private void Update()
		{
			if (_owner == null || Weapon == null || Weapon.Model == null)
				return;

			if (Battle.Instance == null)
				return;

			var plane = Battle.Instance.Plane;
			var aimPlane = plane == Battle.WorldPlane.XY ? WeaponRotator.AimPlane.XY : WeaponRotator.AimPlane.XZ;

			_finder.UpdateTargets(Battle.Instance.AllShips, _owner.HitMask);

			var baseTransform = Weapon.BaseTransform != null ? Weapon.BaseTransform : Weapon.transform;
			var rotatingTransform = Weapon.TurretTransform != null ? Weapon.TurretTransform : Weapon.transform;

			var range = Weapon.Model.Stats.GetStat(StatType.FireRange).Current;
			var firePoint = Weapon.FirePoint != null ? Weapon.FirePoint.position : baseTransform.position;
			var origin = firePoint;
			var baseForward = plane == Battle.WorldPlane.XY ? baseTransform.up : baseTransform.forward;
			var maxAngle = Weapon.FireArcDeg <= 0 ? 360f : Weapon.FireArcDeg;

			var target = _finder.FindBestTarget(origin, baseForward, maxAngle, range, plane);
			if (target == null)
				return;

			var projectileSpeed = Weapon.Model.Stats.GetStat(StatType.ProjectileSpeed).Current;
			var predicted = _finder.Predict(target, firePoint, projectileSpeed);
			var dir = predicted - origin;

			if (aimPlane == WeaponRotator.AimPlane.XY)
				dir.z = 0f;
			else
				dir.y = 0f;

			WeaponRotator.Rotate(
				rotatingTransform: rotatingTransform,
				baseTransform: baseTransform,
				worldDirection: dir,
				rotationSpeedDeg: RotationSpeedDeg,
				maxAngleDeg: maxAngle,
				aimPlane: aimPlane
			);

			var currentForward = plane == Battle.WorldPlane.XY ? rotatingTransform.up : rotatingTransform.forward;
			if (_finder.IsAimedAt(currentForward, dir, AimTolerance))
			{
				Weapon.TryFire(target);
			}
		}
	}
}
