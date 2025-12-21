using UnityEngine;

namespace Ships.HitEffect
{
	public class SlowEffect : IStackableEffect
	{
		public string EffectId => "Slow";
		public bool CanStack { get; }
		public int MaxStacks { get; }

		private float chance;
		private float slowPercent;
		private float duration;

		public SlowEffect(float chance, float slowPercent, float duration, bool canStack = false, int maxStacks = 1)
		{
			this.chance = chance;
			this.slowPercent = slowPercent;
			this.duration = duration;
			this.CanStack = canStack;
			this.MaxStacks = maxStacks;
		}

		public void Apply(ITargetable target, float damage, WeaponBase sourceWeapon)
		{
			if (Random.value > chance) return;
			if (!(target is ShipBase ship)) return;

			// 1) Обновить стаки
			ship.AddOrStackEffect(this, duration);

			// 2) Удалить старые модификаторы
			var stat = ship.GetStat(StatType.MoveSpeed) as Stat;
			stat.RemoveModifiersFromSource(this);

			// 3) Наложить новый модификатор
			var inst = ship.GetEffect(EffectId);

			float totalSlow = slowPercent / 100f * inst.Stacks;

			stat.AddModifier(new StatModifier(
				StatModifierType.PercentAdd,
				StatModifierTarget.Maximum,
				StatModifierPeriodicity.Timed,
				-totalSlow,
				remainingTicks: Mathf.CeilToInt(duration),
				source: this
			));
		}
	}



}
