using Tanks;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponTargeting : MonoBehaviour
{
	public TankTurret Turret;            // null → фиксированное оружие
	public WeaponBase Weapon;
	public bool IsAutoFire = true;
	public float AimTolerance = 3f;

	private TargetFinder finder = new();
	private TankBase owner;

	private void Awake()
	{
		owner = GetComponentInParent<TankBase>();
	}

	void Update()
	{
		if (!owner || !Weapon)
			return;

		finder.UpdateTargets(Battle.Instance.AllTanks, owner.HitMask);

		float range = Weapon.Model.Stats.GetStat(StatType.FireRange).Current;

		// определяем позицию и forward источника наведения

		Vector3 origin = Turret ? Turret.Pivot.position : Weapon.transform.position;
		Vector3 forward = Turret ? Turret.Pivot.forward : Weapon.transform.forward;
		float maxAngle = Turret ? Turret.MaxAngle : Weapon.Slot.AllowedAngle; // если нет турели → по слоту

		var target = finder.FindBestTarget(
			origin,
			forward,
			maxAngle,
			range
		);
		if (target == null)
			return;
		Debug.Log($"{gameObject.transform.root.name} find target: {target}");
		Vector3 predicted = finder.Predict(
			target,
			Weapon.FirePoint.position,
			Weapon.Model.Stats.GetStat(StatType.ProjectileSpeed).Current
		);

		Vector3 dir = predicted - origin;

		// --- вращение ---
		if (Turret)
		{
			Turret.RotateTowards(dir);
		}

		// --- стрельба, если наведено ---
		if (finder.IsAimedAt(Turret ? Turret.Pivot : Weapon.transform, dir, AimTolerance) && IsAutoFire)
		{
			Weapon.TryFire(target);
		}
			
	}
}