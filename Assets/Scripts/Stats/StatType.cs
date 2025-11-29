namespace Ships
{
	public enum StatType
	{
		// Ship
		HitPoint,
		MoveSpeed,
		TurnSpeed,
		Shield,
		ShieldRegen,
		Acceleration,
		BrakePower,

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

		// Resistances
		KineticResist,
		ThermalResist,
		ExplosionResist,
	}
}