namespace Ships
{
	public enum StatType
	{
		// Ship (base)
		HitPoint,
		TurnSpeed,
		Shield,
		ShieldRegen,
		MoveSpeed,
		MoveSpeedRear,
		Acceleration,
		BrakePower,
		ShieldRestoreDelay,
		BackSpeed,
		Energy,
		PowerCell,
		AfterburnerSpeed,
		AfterburnerTime,
		Evasion,

		// Weapon (base)
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
		Penetration,
		RocketSpeed,
		ExplosionRadius,
		Spread,

		// Resistances
		KineticResist,
		ThermalResist,
		EnergyResist,

		// Meta bonuses (percent unless noted)
		HitPointBonus,
		ShieldBonus,
		ShieldRegenBonus,
		DamageBonus,
		MoveSpeedBonus,
		AccelerationBonus,
		TurnSpeedBonus,
		EvasionBonus,
		AfterburnerSpeedBonus,
		AfterburnerTimeBonus,
		KineticDamageBonus,
		ThermalDamageBonus,
		EnergyDamageBonus,
		SmallWeaponDamageBonus,
		MediumWeaponDamageBonus,
		LargeWeaponDamageBonus,
		PenetrationBonus,
		CritChanceBonus,
		CritMultiplierBonus,
		ProjectileSpeedBonus,
		FireRangeBonus,
		AccuracyBonus,
		ReloadTimeBonus,
		RotationSpeedBonus,
		FireRateBonus,
		KineticResistBonus,
		ThermalResistBonus,
		EnergyResistBonus,
		ExplosionRadiusBonus,
		RocketSpeedBonus,
		AbilityCooldownBonus,
		ShieldDamageBonus,

		// Meta bonuses (flat)
		PowerCellBonus,
		ProjectileAmmoBonus,
		EnergyBonus,
		Mechanic,
		Pilot,
		Warrior,
		
		//values
		Duration,
		Value,
		Chance // 0 - 1
	}
}
