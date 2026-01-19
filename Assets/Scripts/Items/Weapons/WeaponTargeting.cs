using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class WeaponTargeting : MonoBehaviour
	{
		public WeaponBase Weapon;
		public float AimTolerance = 5f;
		public float RotationSpeedDeg = 720f;

		private readonly TargetFinder _finder = new();
		private ShipBase _owner;
		private ITargetable _currentTarget;
		private ITargetable _preferredTarget;
		private static readonly ShipClass[] SmallWeaponPriority =
		{
			ShipClass.Rocket,
			ShipClass.Frigate,
			ShipClass.Destroyer,
			ShipClass.Cruiser,
			ShipClass.Battleship,
			ShipClass.Flagship
		};
		private static readonly ShipClass[] MediumWeaponPriority =
		{
			ShipClass.Destroyer,
			ShipClass.Cruiser,
			ShipClass.Frigate,
			ShipClass.Battleship,
			ShipClass.Flagship
		};
		private static readonly ShipClass[] LargeWeaponPriority =
		{
			ShipClass.Battleship,
			ShipClass.Flagship,
			ShipClass.Cruiser,
			ShipClass.Destroyer,
			ShipClass.Frigate
		};

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

			AutoAimAndFire();
		}

		private void AutoAimAndFire()
		{
			var rotationSpeedDeg = GetRotationSpeedDeg();

			_finder.UpdateTargets(Battle.Instance.AllShips, _owner.HitMask);

			var platformTransform = Weapon.BaseTransform != null ? Weapon.BaseTransform : Weapon.transform;
			var yawTransform = Weapon.TurretTransform != null ? Weapon.TurretTransform : Weapon.transform;
			var barrelTransform = Weapon.BarrelTransform != null ? Weapon.BarrelTransform : yawTransform;

			var range = Weapon.Model.Stats.GetStat(StatType.FireRange).Current;
			var firePoint = Weapon.FirePoint != null ? Weapon.FirePoint.position : platformTransform.position;
			var origin = firePoint;
			var baseForward = platformTransform.forward;
			var maxAngle = Weapon.FireArcDeg <= 0 ? 360f : Weapon.FireArcDeg;

			var priority = ResolveTargetPriority(Weapon?.Model?.Size);
			var target = SelectTarget(origin, baseForward, maxAngle, range, priority);
			if (target == null)
				return;

			var projectileSpeed = Weapon.Model.Stats.GetStat(StatType.ProjectileSpeed).Current;
			var predicted = _finder.Predict(target, firePoint, projectileSpeed);
			var dir = predicted - origin;
			var flatDir = new Vector3(dir.x, 0f, dir.z);

			WeaponRotator.Rotate(
				rotatingTransform: yawTransform,
				baseTransform: platformTransform,
				worldDirection: flatDir,
				rotationSpeedDeg: rotationSpeedDeg,
				maxAngleDeg: maxAngle
			);

			if (barrelTransform != null && barrelTransform != yawTransform)
			{
				WeaponRotator.RotatePitch(
					barrelTransform: barrelTransform,
					yawTransform: yawTransform,
					worldDirection: dir,
					rotationSpeedDeg: rotationSpeedDeg,
					minPitchDeg: Weapon.BarrelMinPitchDeg,
					maxPitchDeg: Weapon.BarrelMaxPitchDeg,
					restLocalRotation: Weapon.BarrelRestLocalRotation
				);
			}

			var aimTransform = barrelTransform != null ? barrelTransform : yawTransform;
			var currentForward = aimTransform.forward;
			if (_finder.IsAimedAt(currentForward, dir, AimTolerance))
				Weapon.TryFire(target);
		}

		private float GetRotationSpeedDeg()
		{
			if (Weapon?.Model?.Stats != null &&
			    Weapon.Model.Stats.TryGetStat(StatType.RotationSpeed, out var stat) &&
			    stat != null && stat.Current > 0f)
				return stat.Current;

			return RotationSpeedDeg;
		}

		public void SetPreferredTarget(ITargetable target)
		{
			_preferredTarget = target;
		}

		private ITargetable SelectTarget(
			Vector3 origin,
			Vector3 forward,
			float maxAngleDeg,
			float range,
			IReadOnlyList<ShipClass> priority)
		{
			if (_preferredTarget != null &&
			    _finder.IsValidTarget(_preferredTarget, origin, forward, maxAngleDeg, range, priority))
			{
				_currentTarget = _preferredTarget;
				return _preferredTarget;
			}

			if (_currentTarget != null &&
			    _finder.IsValidTarget(_currentTarget, origin, forward, maxAngleDeg, range, priority))
				return _currentTarget;

			var target = _finder.FindBestTarget(origin, forward, maxAngleDeg, range, priority);
			_currentTarget = target;
			return target;
		}

		private static IReadOnlyList<ShipClass> ResolveTargetPriority(string weaponSize)
		{
			if (string.IsNullOrEmpty(weaponSize))
				return null;

			switch (weaponSize.Trim().ToLowerInvariant())
			{
				case "s":
				case "small":
					return SmallWeaponPriority;
				case "m":
				case "medium":
					return MediumWeaponPriority;
				case "l":
				case "large":
					return LargeWeaponPriority;
				default:
					return null;
			}
		}
	}
}
