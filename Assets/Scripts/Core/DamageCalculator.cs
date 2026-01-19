using UnityEngine;

namespace Ships
{
	public static class DamageCalculator
	{
		public static CalculatedDamage CalculateHit(
			float projectileDamage,
			Vector3 hitPoint,
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

			var damage = projectileDamage;

			// ============================================================
			// 1) Если попали в щит - броню не учитываем
			// ============================================================

			if (wasShieldHit)
			{
				damage = ApplyShieldDamageBonus(damage, sourceWeapon);
			}
			else
			{
				// ============================================================
				// 2) Сопротивления по типу урона
				// ============================================================
				damage = ApplyDamageResist(damage, sourceWeapon, target);
			}

			// ============================================================
			// 3) Проверка попадания (Accuracy vs Evasion)
			// ============================================================
			if (!RollHit(sourceWeapon, target))
			{
				result.FinalDamage = 0f;
				return result;
			}

			// ============================================================
			// 4) Крит
			// ============================================================
			var critChance = sourceWeapon.Model.Stats.GetStat(StatType.CritChance)?.Current ?? 0f;
			var critMult = sourceWeapon.Model.Stats.GetStat(StatType.CritMultiplier)?.Current ?? 1f;
			var isCrit = Random.value < critChance;
			if (isCrit)
				damage *= critMult;

			result.IsCrit = isCrit;
			result.CritChance = critChance;
			result.CritBonus = isCrit ? critMult : 1f;
			result.AfterShield = damage;
			result.FinalDamage = damage;
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

		private static float ApplyShieldDamageBonus(float damage, WeaponBase sourceWeapon)
		{
			if (sourceWeapon?.Owner != null &&
			    sourceWeapon.Owner.TryGetStat(StatType.ShieldDamageBonus, out var shieldBonus))
				return damage * (1f + Mathf.Max(0f, shieldBonus.Current));

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
					return ApplyResist(damage, kineticResist.Current, sourceWeapon);

				if (tag == Tags.Thermal &&
				    target.TryGetStat(StatType.ThermalResist, out var thermalResist))
					return ApplyResist(damage, thermalResist.Current, sourceWeapon);

				if (tag == Tags.Energy &&
				    target.TryGetStat(StatType.EnergyResist, out var energyResist))
					return ApplyResist(damage, energyResist.Current, sourceWeapon);
			}

			return damage;
		}

		private static float ApplyResist(float damage, float resistValue, WeaponBase sourceWeapon)
		{
			var resist = Mathf.Clamp(resistValue, 0f, 0.75f);
			var penetration = sourceWeapon?.Model?.Stats?.GetStat(StatType.Penetration)?.Current ?? 0f;
			var effectiveResist = Mathf.Max(0f, resist - penetration);
			return damage * (1f - effectiveResist);
		}

		private static bool RollHit(WeaponBase sourceWeapon, ITargetable target)
		{
			var accuracyStat = sourceWeapon?.Model?.Stats?.GetStat(StatType.Accuracy);
			var weaponAccuracy = accuracyStat != null ? accuracyStat.Current : 1f;
			var evasion = target != null && target.TryGetStat(StatType.Evasion, out var evas)
				? evas.Current
				: 0f;

			var hitChance = Mathf.Clamp01(weaponAccuracy - evasion);
			return Random.value <= hitChance;
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
		public Vector3 HitPoint;
		public WeaponBase SourceWeapon;

		public bool WasShieldHit;
		public bool WasDirectHull;
	}
}
