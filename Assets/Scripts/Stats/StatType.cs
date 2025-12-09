namespace Tanks
{
	public enum StatType
	{
		// Ship
		HitPoint,
		MoveSpeed,
		MoveSpeedRear,
		TurnSpeed,
		Shield,
		ShieldRegen,
		Acceleration,
		BrakePower,
		ShieldRestoreDelay,
		BackSpeed,
		Armor,

		// Weapon
		FireRate,
		MinDamage,
		MaxDamage,
		CritChance,
		CritMultiplier,
		ProjectileSpeed,
		ArmorPierce,
		FireRange,
		Accuracy,
		AmmoCount,
		ReloadTime,

		// Resistances
		KineticResist,
		ThermalResist,
		ExplosionResist,
	}
}