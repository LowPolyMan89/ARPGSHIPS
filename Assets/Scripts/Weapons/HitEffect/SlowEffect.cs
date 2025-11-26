using UnityEngine;

namespace Ships.HitEffect
{
	public class SlowEffect : IOnHitEffect
	{
		private float chance;
		private float slowPercent;
		private float duration;

		public SlowEffect(float chance, float slowPercent, float duration)
		{
			this.chance = chance;
			this.slowPercent = slowPercent;
			this.duration = duration;
		}

		public void Apply(ITargetable target, float damage, WeaponBase sourceWeapon)
		{
			if (UnityEngine.Random.value > chance)
				return;

			if (target.TryGetStat(StatType.MoveSpeed, out var speedStat))
			{
				speedStat.AddModifier(
					new StatModifier(
						StatModifierType.PercentAdd,
						StatModifierTarget.Current,
						StatModifierPeriodicity.Timed,
						-slowPercent,
						remainingTicks: Mathf.CeilToInt(duration / Time.fixedDeltaTime),
						source: this
					)
				);
			}
		}
	}

}