using System.Collections;
using UnityEngine;

namespace Ships.HitEffect
{
	public class DamageOverTimeEffect : IOnHitEffect
	{
		private float chance;
		private float damagePerTick;
		private float duration;

		public DamageOverTimeEffect(float chance, float damagePerTick, float duration)
		{
			this.chance = chance;
			this.damagePerTick = damagePerTick;
			this.duration = duration;
		}

		public void Apply(ITargetable target, float damage, WeaponBase sourceWeapon)
		{
			if (UnityEngine.Random.value > chance)
				return;

			// показать эффект в UI / Debug Panel
			if (target is ShipBase ship)
				ship.AddActiveEffect("Burn", duration);

			// наносим урон
			if (target is MonoBehaviour mb)
				mb.StartCoroutine(DoDamageOverTime(target));
		}

		private IEnumerator DoDamageOverTime(ITargetable target)
		{
			int ticks = Mathf.CeilToInt(duration);

			for (int i = 0; i < ticks; i++)
			{
				if (!target.IsAlive)
					yield break;

				if (target.TryGetStat(StatType.HP, out var hpStat))
					hpStat.AddToCurrent(-damagePerTick);

				yield return new WaitForSeconds(1f);
			}
		}
	}

}