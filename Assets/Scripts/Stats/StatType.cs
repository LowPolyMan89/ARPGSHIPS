namespace Ships
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
		RotationSpeed,

		// Resistances
		KineticResist,
		ThermalResist,
		ExplosionResist,
		
		//values
		Duration,
		Value,
		Chance // 0 - 1
	}
}
