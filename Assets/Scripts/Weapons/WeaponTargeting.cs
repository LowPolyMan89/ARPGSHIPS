using System;
using Ships;
using UnityEngine;

public class WeaponTargeting : MonoBehaviour
{
	public ShipTurret Turret;        // null → фиксированное оружие
	public WeaponBase Weapon;
	public float AimTolerance = 3f;

	private readonly TargetFinder _finder = new();
	private ShipBase _owner;
	private PlayerInputSystem _input;

	private void Awake()
	{
		_owner = GetComponentInParent<ShipBase>();
		_input = GetComponentInParent<PlayerInputSystem>();
		if (_input == null)
			_input = FindObjectOfType<PlayerInputSystem>();
	}

	private void Update()
	{
		if (!_owner || !Weapon || Weapon.Slot == null || Weapon.Model == null)
			return;

		var worldPlane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XZ;
		var aimPlane = worldPlane == Battle.WorldPlane.XY ? WeaponRotator.AimPlane.XY : WeaponRotator.AimPlane.XZ;

		var slot = Weapon.Slot;
		var activateType = slot.ActivateSlotWeaponType;

		_finder.UpdateTargets(Battle.Instance.AllShips, _owner.HitMask);

		var range = Weapon.Model.Stats.GetStat(StatType.FireRange).Current;

		var origin = Turret ? Turret.Pivot.position : Weapon.transform.position;
		var forward = Turret
			? (worldPlane == Battle.WorldPlane.XY ? Turret.Pivot.up : Turret.Pivot.forward)
			: (worldPlane == Battle.WorldPlane.XY ? Weapon.transform.up : Weapon.transform.forward);
		var maxAngle = Turret ? Turret.MaxAngle : slot.AllowedAngle;

		var target = _finder.FindBestTarget(origin, forward, maxAngle, range, worldPlane);

		// направление стрельбы
		var dir = forward;
		if (target != null)
		{
			var projectileSpeed = Weapon.Model.Stats.GetStat(StatType.ProjectileSpeed).Current;
			var predicted = _finder.Predict(
				target,
				Weapon.FirePoint.position,
				projectileSpeed
			);
			dir = predicted - origin;
		}

		if (aimPlane == WeaponRotator.AimPlane.XY)
			dir.z = 0f;
		else
			dir.y = 0f;

		// вращаем башню
		if (Turret)
			Turret.RotateTowards(dir);

		switch (activateType)
		{
			case ActivateSlotWeaponType.Auto:
				// авто-режим: только по цели и только когда наведено
				var currentForward = Turret
					? (worldPlane == Battle.WorldPlane.XY ? Turret.Pivot.up : Turret.Pivot.forward)
					: (worldPlane == Battle.WorldPlane.XY ? Weapon.transform.up : Weapon.transform.forward);

				if (target != null &&
				    _finder.IsAimedAt(currentForward, dir, AimTolerance))
				{
					Weapon.TryFire(target);
				}
				break;

			case ActivateSlotWeaponType.LMB:
				// кнопочный режим: можно стрелять даже без цели
				if (_input != null && _input.FireLMB)
				{
					Weapon.TryFire(target); // target может быть null → стрельба вперёд
				}
				break;

			case ActivateSlotWeaponType.RMB:
				if (_input != null && _input.FireRMB)
				{
					Weapon.TryFire(target);
				}
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
