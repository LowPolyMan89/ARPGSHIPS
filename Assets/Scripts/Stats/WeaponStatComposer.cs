using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public static class WeaponStatComposer
	{
		public static Stats Compose(
			Stats baseStats,
			WeaponModel weaponModel,
			Stats shipStats,
			IReadOnlyList<WeaponStatEffectModel> moduleEffects)
		{
			var result = baseStats != null ? baseStats.Clone() : new Stats();
			var context = BuildContext(weaponModel);

			ApplyMetaBonuses(result, shipStats, context);
			ApplyModuleEffects(result, moduleEffects, context);

			return result;
		}

		public static Stats BuildBaseStats(IEnumerable<StatValue> values)
		{
			var stats = new Stats();
			if (values == null)
				return stats;

			foreach (var entry in values)
			{
				if (entry == null || string.IsNullOrEmpty(entry.Name))
					continue;
				if (!Enum.TryParse(entry.Name, true, out StatType statType))
					continue;

				stats.AddStat(new Stat(statType, entry.Value));
			}

			return stats;
		}

		private static WeaponContext BuildContext(WeaponModel model)
		{
			return new WeaponContext
			{
				HasDamageType = model != null && model.HasDamageType,
				DamageType = model != null ? model.DamageType : default,
				Size = model != null ? model.Size : null,
				Tags = model?.Tags ?? Array.Empty<Tags>()
			};
		}

		private static void ApplyMetaBonuses(Stats stats, Stats shipStats, WeaponContext context)
		{
			if (stats == null)
				return;

			var damageBonus = GetStatValue(shipStats, StatType.DamageBonus);
			var typeBonus = GetDamageTypeBonus(shipStats, context);
			var sizeBonus = GetSizeBonus(shipStats, context);

			var minDamage = GetValue(stats, StatType.MinDamage);
			var maxDamage = GetValue(stats, StatType.MaxDamage);

			if (minDamage > 0f || maxDamage > 0f)
			{
				var damageMult = (1f + damageBonus) * (1f + typeBonus) * (1f + sizeBonus);
				minDamage *= damageMult;
				maxDamage *= damageMult;
				SetValue(stats, StatType.MinDamage, minDamage);
				SetValue(stats, StatType.MaxDamage, maxDamage);
			}

			ApplyPercentBonus(stats, StatType.FireRate, GetStatValue(shipStats, StatType.FireRateBonus));
			ApplyPercentBonus(stats, StatType.ReloadTime, GetStatValue(shipStats, StatType.ReloadTimeBonus));
			ApplyPercentBonus(stats, StatType.ProjectileSpeed, GetStatValue(shipStats, StatType.ProjectileSpeedBonus));
			ApplyPercentBonus(stats, StatType.FireRange, GetStatValue(shipStats, StatType.FireRangeBonus));
			ApplyPercentBonus(stats, StatType.Accuracy, GetStatValue(shipStats, StatType.AccuracyBonus));
			ApplyPercentBonus(stats, StatType.CritChance, GetStatValue(shipStats, StatType.CritChanceBonus));
			ApplyPercentBonus(stats, StatType.CritMultiplier, GetStatValue(shipStats, StatType.CritMultiplierBonus));
			ApplyPercentBonus(stats, StatType.Penetration, GetStatValue(shipStats, StatType.PenetrationBonus));
			ApplyPercentBonus(stats, StatType.RotationSpeed, GetStatValue(shipStats, StatType.RotationSpeedBonus));
			ApplyPercentBonus(stats, StatType.RocketSpeed, GetStatValue(shipStats, StatType.RocketSpeedBonus));
			ApplyPercentBonus(stats, StatType.ExplosionRadius, GetStatValue(shipStats, StatType.ExplosionRadiusBonus));

			var ammoBonus = GetStatValue(shipStats, StatType.ProjectileAmmoBonus);
			if (!Mathf.Approximately(ammoBonus, 0f))
				ApplyFlatBonus(stats, StatType.AmmoCount, ammoBonus);
		}

		private static void ApplyModuleEffects(
			Stats stats,
			IReadOnlyList<WeaponStatEffectModel> moduleEffects,
			WeaponContext context)
		{
			if (stats == null || moduleEffects == null || moduleEffects.Count == 0)
				return;

			for (var i = 0; i < moduleEffects.Count; i++)
			{
				var effect = moduleEffects[i];
				if (effect == null || string.IsNullOrEmpty(effect.Stat))
					continue;

				if (!MatchesFilter(effect.Filter, context))
					continue;

				if (!Enum.TryParse(effect.Stat, true, out StatType statType))
					continue;

				if (TryApplyDamageBonusEffect(stats, context, statType, effect))
					continue;

				if (TryApplyBonusToWeaponStat(stats, statType, effect))
					continue;

				ApplyStatEffect(stats, statType, effect);
			}
		}

		private static bool TryApplyDamageBonusEffect(
			Stats stats,
			WeaponContext context,
			StatType statType,
			WeaponStatEffectModel effect)
		{
			if (statType == StatType.DamageBonus)
				return ApplyDamageEffect(stats, effect);

			if (statType == StatType.KineticDamageBonus && context.HasDamageType && context.DamageType == Tags.Kinetic)
				return ApplyDamageEffect(stats, effect);

			if (statType == StatType.ThermalDamageBonus && context.HasDamageType && context.DamageType == Tags.Thermal)
				return ApplyDamageEffect(stats, effect);

			if (statType == StatType.EnergyDamageBonus && context.HasDamageType && context.DamageType == Tags.Energy)
				return ApplyDamageEffect(stats, effect);

			if (statType == StatType.SmallWeaponDamageBonus && IsSize(context, "Small"))
				return ApplyDamageEffect(stats, effect);

			if (statType == StatType.MediumWeaponDamageBonus && IsSize(context, "Medium"))
				return ApplyDamageEffect(stats, effect);

			if (statType == StatType.LargeWeaponDamageBonus && IsSize(context, "Large"))
				return ApplyDamageEffect(stats, effect);

			return false;
		}

		private static bool ApplyDamageEffect(Stats stats, WeaponStatEffectModel effect)
		{
			var minDamage = GetValue(stats, StatType.MinDamage);
			var maxDamage = GetValue(stats, StatType.MaxDamage);
			if (minDamage <= 0f && maxDamage <= 0f)
				return false;

			ApplyOperation(ref minDamage, effect.Operation, effect.Value);
			ApplyOperation(ref maxDamage, effect.Operation, effect.Value);
			SetValue(stats, StatType.MinDamage, minDamage);
			SetValue(stats, StatType.MaxDamage, maxDamage);
			return true;
		}

		private static bool TryApplyBonusToWeaponStat(Stats stats, StatType statType, WeaponStatEffectModel effect)
		{
			if (TryMapBonusStat(statType, out var target))
			{
				ApplyStatEffect(stats, target, effect);
				return true;
			}

			return false;
		}

		private static void ApplyStatEffect(Stats stats, StatType statType, WeaponStatEffectModel effect)
		{
			var value = GetValue(stats, statType);
			ApplyOperation(ref value, effect.Operation, effect.Value);
			SetValue(stats, statType, value);
		}

		private static void ApplyOperation(ref float value, StatEffectOperation operation, float operand)
		{
			switch (operation)
			{
				case StatEffectOperation.Add:
					value += operand;
					break;
				case StatEffectOperation.PercentAdd:
				case StatEffectOperation.PercentMult:
					value *= (1f + operand);
					break;
				case StatEffectOperation.Set:
					value = operand;
					break;
			}
		}

		private static bool TryMapBonusStat(StatType bonus, out StatType target)
		{
			switch (bonus)
			{
				case StatType.FireRateBonus:
					target = StatType.FireRate;
					return true;
				case StatType.ReloadTimeBonus:
					target = StatType.ReloadTime;
					return true;
				case StatType.ProjectileSpeedBonus:
					target = StatType.ProjectileSpeed;
					return true;
				case StatType.FireRangeBonus:
					target = StatType.FireRange;
					return true;
				case StatType.AccuracyBonus:
					target = StatType.Accuracy;
					return true;
				case StatType.CritChanceBonus:
					target = StatType.CritChance;
					return true;
				case StatType.CritMultiplierBonus:
					target = StatType.CritMultiplier;
					return true;
				case StatType.PenetrationBonus:
					target = StatType.Penetration;
					return true;
				case StatType.RotationSpeedBonus:
					target = StatType.RotationSpeed;
					return true;
				case StatType.RocketSpeedBonus:
					target = StatType.RocketSpeed;
					return true;
				case StatType.ExplosionRadiusBonus:
					target = StatType.ExplosionRadius;
					return true;
				case StatType.ProjectileAmmoBonus:
					target = StatType.AmmoCount;
					return true;
				default:
					target = default;
					return false;
			}
		}

		private static float GetValue(Stats stats, StatType type)
		{
			return stats != null && stats.TryGetStat(type, out var stat) ? stat.Maximum : 0f;
		}

		private static void SetValue(Stats stats, StatType type, float value)
		{
			if (stats == null)
				return;

			var stat = stats.GetOrCreateStat(type, value);
			stat.SetBaseValues(value, value, resetCurrentToFull: true);
		}

		private static float GetStatValue(Stats stats, StatType type)
		{
			return stats != null && stats.TryGetStat(type, out var stat) ? stat.Current : 0f;
		}

		private static void ApplyPercentBonus(Stats stats, StatType target, float bonus)
		{
			if (Mathf.Approximately(bonus, 0f))
				return;

			var value = GetValue(stats, target);
			value *= (1f + bonus);
			SetValue(stats, target, value);
		}

		private static void ApplyFlatBonus(Stats stats, StatType target, float bonus)
		{
			if (Mathf.Approximately(bonus, 0f))
				return;

			var value = GetValue(stats, target);
			value += bonus;
			SetValue(stats, target, value);
		}

		private static float GetDamageTypeBonus(Stats shipStats, WeaponContext context)
		{
			if (!context.HasDamageType)
				return 0f;

			return context.DamageType switch
			{
				Tags.Kinetic => GetStatValue(shipStats, StatType.KineticDamageBonus),
				Tags.Thermal => GetStatValue(shipStats, StatType.ThermalDamageBonus),
				Tags.Energy => GetStatValue(shipStats, StatType.EnergyDamageBonus),
				_ => 0f
			};
		}

		private static float GetSizeBonus(Stats shipStats, WeaponContext context)
		{
			if (IsSize(context, "Small"))
				return GetStatValue(shipStats, StatType.SmallWeaponDamageBonus);
			if (IsSize(context, "Medium"))
				return GetStatValue(shipStats, StatType.MediumWeaponDamageBonus);
			if (IsSize(context, "Large"))
				return GetStatValue(shipStats, StatType.LargeWeaponDamageBonus);
			return 0f;
		}

		private static bool IsSize(WeaponContext context, string size)
		{
			return !string.IsNullOrEmpty(context.Size) &&
			       context.Size.Equals(size, StringComparison.OrdinalIgnoreCase);
		}

		private static bool MatchesFilter(WeaponEffectFilter filter, WeaponContext context)
		{
			if (filter == null)
				return true;

			if (!string.IsNullOrEmpty(filter.DamageType))
			{
				if (!context.HasDamageType ||
				    !filter.DamageType.Equals(context.DamageType.ToString(), StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (!string.IsNullOrEmpty(filter.Size))
			{
				if (string.IsNullOrEmpty(context.Size) ||
				    !filter.Size.Equals(context.Size, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (filter.TagValues != null && filter.TagValues.Length > 0)
			{
				var hasTag = false;
				for (var i = 0; i < filter.TagValues.Length; i++)
				{
					if (Array.IndexOf(context.Tags, filter.TagValues[i]) >= 0)
					{
						hasTag = true;
						break;
					}
				}

				if (!hasTag)
					return false;
			}

			return true;
		}

		private struct WeaponContext
		{
			public bool HasDamageType;
			public Tags DamageType;
			public string Size;
			public Tags[] Tags;
		}
	}
}
