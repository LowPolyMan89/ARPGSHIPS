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
		Energy,

		// Weapon
		FireRate,
		MinDamage,
		MaxDamage,
		CritChance,
		CritMultiplier,
		ProjectileSpeed,
		FireRange,
		Accuracy,
		AmmoCount,
		ReloadTime,
		RotationSpeed,
		KineticDamageBonus,
		ThermalDamageBonus,
		EnergyDamageBonus,

		// Resistances
		KineticResist,
		ThermalResist,
		EnergyResist,
		
		//values
		Duration,
		Value,
		Chance // 0 - 1
	}
}
