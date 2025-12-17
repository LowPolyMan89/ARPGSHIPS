using UnityEngine;

namespace Ships
{
	public static class DamageCalculator
	{
		public static CalculatedDamage CalculateHit(
			float projectileDamage,
			float armorPierce,
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
			var damage = critDamage;

			// ============================================================
			// 2) Если попали в щит — броню не учитываем
			// ============================================================

			if (wasShieldHit)
			{
				result.AfterShield = damage;
				result.FinalDamage = damage; // щит сам поглотит сколько может
				LogDamage(result);
				return result;
			}

			// ============================================================
			// 3) Броня (по корпусу)
			// ============================================================
			var armor = 0f;
			if (target.TryGetStat(StatType.Armor, out var armorStat))
				armor = armorStat.Current;

			var effectiveArmor = Mathf.Max(armor - armorPierce, 0);
			var dmgAfterArmor = damage * (100f / (100f + effectiveArmor));

			result.AfterArmor = dmgAfterArmor;
			result.FinalDamage = dmgAfterArmor;

			LogDamage(result);
			return result;
		}


		private static void LogDamage(CalculatedDamage cd)
		{
			Debug.Log(
				$"[DamageCalc] Raw={cd.RawDamage} | " +
				$"ShieldHit={cd.WasShieldHit} | " +
				$"AfterShield={cd.AfterShield} | " +
				$"AfterArmor={cd.AfterArmor} | " +
				$"FinalDamage={cd.FinalDamage} |" +
				$"CritChance={cd.CritChance} is Crit: {cd.IsCrit}"
			);
		}
	}

	public class CalculatedDamage
	{
		public float RawDamage;
		public float AfterShield;
		public float AfterArmor;
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
