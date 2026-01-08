using UnityEngine;

namespace Ships
{
	public static class ShipStatBonusApplier
	{
		private static readonly object BonusSource = new object();

		public static void Apply(Stats stats)
		{
			if (stats == null)
				return;

			ApplyPercentBonus(stats, StatType.HitPoint, StatType.HitPointBonus);
			ApplyPercentBonus(stats, StatType.Shield, StatType.ShieldBonus);
			ApplyPercentBonus(stats, StatType.ShieldRegen, StatType.ShieldRegenBonus);
			ApplyPercentBonus(stats, StatType.MoveSpeed, StatType.MoveSpeedBonus);
			ApplyPercentBonus(stats, StatType.Acceleration, StatType.AccelerationBonus);
			ApplyPercentBonus(stats, StatType.TurnSpeed, StatType.TurnSpeedBonus);
			ApplyPercentBonus(stats, StatType.Evasion, StatType.EvasionBonus);
			ApplyPercentBonus(stats, StatType.AfterburnerSpeed, StatType.AfterburnerSpeedBonus);
			ApplyPercentBonus(stats, StatType.AfterburnerTime, StatType.AfterburnerTimeBonus);
			ApplyPercentBonus(stats, StatType.KineticResist, StatType.KineticResistBonus);
			ApplyPercentBonus(stats, StatType.ThermalResist, StatType.ThermalResistBonus);
			ApplyPercentBonus(stats, StatType.EnergyResist, StatType.EnergyResistBonus);

			ApplyFlatBonus(stats, StatType.Energy, StatType.EnergyBonus);
			ApplyFlatBonus(stats, StatType.PowerCell, StatType.PowerCellBonus);
		}

		private static void ApplyPercentBonus(Stats stats, StatType target, StatType bonus)
		{
			var stat = stats.GetOrCreateStat(target, 0f);
			stat.RemoveModifiersFromSource(BonusSource);

			var bonusValue = GetStatValue(stats, bonus);
			if (Mathf.Approximately(bonusValue, 0f))
				return;
			stat.AddModifier(new StatModifier(
				StatModifierType.PercentAdd,
				StatModifierTarget.Maximum,
				StatModifierPeriodicity.Permanent,
				bonusValue,
				source: BonusSource,
				sourceType: StatModifierSource.Buff));
		}

		private static void ApplyFlatBonus(Stats stats, StatType target, StatType bonus)
		{
			var stat = stats.GetOrCreateStat(target, 0f);
			stat.RemoveModifiersFromSource(BonusSource);

			var bonusValue = GetStatValue(stats, bonus);
			if (Mathf.Approximately(bonusValue, 0f))
				return;
			stat.AddModifier(new StatModifier(
				StatModifierType.Flat,
				StatModifierTarget.Maximum,
				StatModifierPeriodicity.Permanent,
				bonusValue,
				source: BonusSource,
				sourceType: StatModifierSource.Buff));
		}

		private static float GetStatValue(Stats stats, StatType type)
		{
			return stats != null && stats.TryGetStat(type, out var stat) ? stat.Current : 0f;
		}
	}
}
