using UnityEngine;

namespace Ships
{
	public static class DamageCalculator
	{
		public static CalculatedDamage CalculateHit(
			float projectileDamage,
			Vector2 hitPoint,
			WeaponBase sourceWeapon,
			ITargetable target,
			bool wasShieldHit
		)
		{
			var result = new CalculatedDamage
			{
				RawDamage = projectileDamage,
				WasShieldHit = wasShieldHit,
				SourceWeapon = sourceWeapon,
				HitPoint = hitPoint
			};

			// ============================================================
			// 1) КРИТ ШАНС
			// ============================================================
			var critChance = sourceWeapon.Model.Stats.GetStat(StatType.CritChance)?.Current ?? 0f;
			var critMult = sourceWeapon.Model.Stats.GetStat(StatType.CritMultiplier)?.Current ?? 1f;

			var isCrit = Random.value < critChance;
			var critDamage = isCrit ? (sourceWeapon.Model.Stats.GetMaximum(StatType.MaxDamage ) * 0.9f) * critMult : projectileDamage;

			result.IsCrit = isCrit;
			result.CritChance = critChance;
			result.CritBonus = isCrit ? critMult : 1f;
			var damage = ApplyDamageBonus(critDamage, sourceWeapon);

			// ============================================================
			// 2) Если попали в щит — броню не учитываем
			// ============================================================

			if (wasShieldHit)
			{
				result.AfterShield = damage;
				result.FinalDamage = damage; // щит сам поглотит сколько может
				//LogDamage(result);
				return result;
			}

			// ============================================================
			// 3) Сопротивления по типу урона
			// ============================================================
			var dmgAfterResist = ApplyDamageResist(damage, sourceWeapon, target);

			result.FinalDamage = dmgAfterResist;

			//LogDamage(result);
			return result;
		}


		private static void LogDamage(CalculatedDamage cd)
		{
			Debug.Log(
				$"[DamageCalc] Raw={cd.RawDamage} | " +
				$"ShieldHit={cd.WasShieldHit} | " +
				$"AfterShield={cd.AfterShield} | " +
				$"FinalDamage={cd.FinalDamage} |" +
				$"CritChance={cd.CritChance} is Crit: {cd.IsCrit}"
			);
		}

		private static float ApplyDamageBonus(float damage, WeaponBase sourceWeapon)
		{
			if (!TryGetDamageTag(sourceWeapon, out var tag))
				return damage;

			if (sourceWeapon?.Owner != null)
			{
				if (tag == Tags.Kinetic &&
				    sourceWeapon.Owner.TryGetStat(StatType.KineticDamageBonus, out var kineticBonus))
					return damage * (1f + Mathf.Max(0f, kineticBonus.Current));

				if (tag == Tags.Thermal &&
				    sourceWeapon.Owner.TryGetStat(StatType.ThermalDamageBonus, out var thermalBonus))
					return damage * (1f + Mathf.Max(0f, thermalBonus.Current));

				if (tag == Tags.Energy &&
				    sourceWeapon.Owner.TryGetStat(StatType.EnergyDamageBonus, out var energyBonus))
					return damage * (1f + Mathf.Max(0f, energyBonus.Current));
			}

			return damage;
		}

		private static float ApplyDamageResist(float damage, WeaponBase sourceWeapon, ITargetable target)
		{
			if (!TryGetDamageTag(sourceWeapon, out var tag))
				return damage;

			if (target != null)
			{
				if (tag == Tags.Kinetic &&
				    target.TryGetStat(StatType.KineticResist, out var kineticResist))
					return damage * (1f - Mathf.Clamp01(kineticResist.Current));

				if (tag == Tags.Thermal &&
				    target.TryGetStat(StatType.ThermalResist, out var thermalResist))
					return damage * (1f - Mathf.Clamp01(thermalResist.Current));

				if (tag == Tags.Energy &&
				    target.TryGetStat(StatType.EnergyResist, out var energyResist))
					return damage * (1f - Mathf.Clamp01(energyResist.Current));
			}

			return damage;
		}

		private static bool TryGetDamageTag(WeaponBase sourceWeapon, out Tags tag)
		{
			if (sourceWeapon?.Model != null && sourceWeapon.Model.HasDamageType)
			{
				tag = sourceWeapon.Model.DamageType;
				return true;
			}

			tag = default;
			return false;
		}
	}

	public class CalculatedDamage
	{
		public float RawDamage;
		public float AfterShield;
		public float FinalDamage;
		public bool IsCrit;
		public float CritChance;
		public float CritBonus;
		public Vector2 HitPoint;
		public WeaponBase SourceWeapon;

		public bool WasShieldHit;
		public bool WasDirectHull;
	}
}
