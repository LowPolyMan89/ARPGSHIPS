namespace Ships
{
	public enum StatType
	{
		// Ship
		HP,
		MoveSpeed,
		TurnSpeed,
		Shield,
		ShieldRegen,

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