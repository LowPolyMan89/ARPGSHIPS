using UnityEngine;

namespace Ships.HitEffect
{
	public class SlowEffect : IOnHitEffect
	{
		private float chance;
		private float slowPercent; // 10 = 10%
		private float duration;    // секунды

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

			if (target.TryGetStat(StatType.MoveSpeed, out var stat))
			{
				float fraction = slowPercent / 100f;

				stat.AddModifier(
					new StatModifier(
						StatModifierType.PercentAdd,
						StatModifierTarget.Maximum,
						StatModifierPeriodicity.Timed,
						-fraction,
						remainingTicks: Mathf.CeilToInt(duration), // <== 1 секунда = 1 тик
						source: this
					)
				);
			}
		}
	}


}