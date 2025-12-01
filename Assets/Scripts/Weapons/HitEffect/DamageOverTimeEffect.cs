using System.Collections;
using UnityEngine;

namespace Ships.HitEffect
{
    public class DamageOverTimeEffect : IStackableEffect
    {
        public string EffectId => "Burn";

        public bool CanStack { get; }
        public int MaxStacks { get; }

        private float chance;
        private float damagePerTick;
        private float duration;

        public DamageOverTimeEffect(
            float chance,
            float damagePerTick,
            float duration,
            bool canStack = true,
            int maxStacks = 5)
        {
            this.chance = chance;
            this.damagePerTick = damagePerTick;
            this.duration = duration;
            this.CanStack = canStack;
            this.MaxStacks = maxStacks;
        }

        public void Apply(ITargetable target, float damage, WeaponBase sourceWeapon)
        {
            if (Random.value > chance)
                return;

            if (target is not ShipBase ship)
                return;

            // 1) Стакаем/обновляем эффект на корабле
            ship.AddOrStackEffect(this, duration);

            // 2) Если DoT уже работает — НЕ запускать второй
            if (ship.RunningDotEffects.Contains(EffectId))
                return;

            // 3) Иначе запускаем DoT
            ship.RunningDotEffects.Add(EffectId);
            ship.StartCoroutine(DoDamageOverTime(ship));
        }

        private IEnumerator DoDamageOverTime(ShipBase ship)
        {
            while (true)
            {
                var eff = ship.GetEffect(EffectId);

                // Эффект исчез → снимаем
                if (eff == null)
                    break;

                // наносим фиксированный урон (НЕ зависит от стаков)
                if (ship.TryGetStat(StatType.HitPoint, out var hpStat))
                    hpStat.AddToCurrent(-damagePerTick);

                yield return new WaitForSeconds(1f);
            }

            // эффект закончился → символически убираем DoT из активных потоков
            ship.RunningDotEffects.Remove(EffectId);
        }
    }
}
