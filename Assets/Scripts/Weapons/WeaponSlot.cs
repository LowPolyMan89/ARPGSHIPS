using System;

namespace Tanks
{
	using UnityEngine;

	public class WeaponSlot : MonoBehaviour, IAimOrigin
	{
		public Transform BaseTransform;
		[SerializeField] private float _allowedAngle = 45f;
		public float RotationSpeed = 180f;
		public bool IsTurret = false;
		public WeaponBase MountedWeapon;
		public WeaponTargeting WeaponTargeting;
		public SideType Side;
		[SerializeField] private TeamMask _hitMask;
		public WeaponSize SlotSize;

		public Vector3 Position => transform.position;
		public Vector3 Forward => transform.forward;
		public TeamMask HitMask => _hitMask;
		public float AllowedAngle => _allowedAngle;
		public float DetectionRange => MountedWeapon ? MountedWeapon.Model.Stats.GetStat(StatType.FireRange).Current : 9999f;

		public void Init(SideType sideType)
		{
			Side = sideType;

			_hitMask = sideType switch
			{
				SideType.Player => TeamMask.Enemy,
				SideType.Enemy => TeamMask.Player,
				SideType.Ally => TeamMask.Enemy | TeamMask.Player, 
				_ => TeamMask.All
			};

			if (transform.childCount > 0)
			{
				MountedWeapon = transform.GetComponentInChildren<WeaponBase>();
				WeaponTargeting = transform.GetComponentInChildren<WeaponTargeting>();
				if (WeaponTargeting && sideType != SideType.Player)
					WeaponTargeting.Mode = WeaponTargeting.AimMode.Auto;
			}

			if (MountedWeapon)
			{
				Stats stats = new Stats();
				stats.AddStat(new Stat(StatType.FireRange, 50));
				stats.AddStat(new Stat(StatType.FireRate, 1f));
				stats.AddStat(new Stat(StatType.ProjectileSpeed, 60));
				stats.AddStat(new Stat(StatType.MinDamage, 5));
				stats.AddStat(new Stat(StatType.MaxDamage, 10));
				stats.AddStat(new Stat(StatType.Accuracy, 0.05f));
				stats.AddStat(new Stat(StatType.CritChance, 0.1f));
				stats.AddStat(new Stat(StatType.CritMultiplier, 1.2f));
				stats.AddStat(new Stat(StatType.AmmoCount, 10));
				stats.AddStat(new Stat(StatType.ReloadTime, 5f));
				stats.AddStat(new Stat(StatType.ArmorPierce, 0f));
				MountedWeapon.Init(this, stats);
			}
				
		}
		private void Awake()
		{
			if (!BaseTransform)
				BaseTransform = transform.parent; // по умолчанию
		}

		public void RotateWeaponTowards(Vector3 worldDirection)
		{
			if (!MountedWeapon)
				return;

			WeaponRotator.Rotate(
				rotatingTransform: MountedWeapon.transform,
				baseTransform: BaseTransform,
				worldDirection: worldDirection,
				rotationSpeedDeg: RotationSpeed,
				maxAngleDeg: AllowedAngle
			);
		}

		public bool IsTargetWithinSector(Vector2 dir)
		{
			Vector2 forward = transform.forward;
			var angle = Vector3.Angle(forward, dir);
			return angle <= AllowedAngle;
		}
	}
}