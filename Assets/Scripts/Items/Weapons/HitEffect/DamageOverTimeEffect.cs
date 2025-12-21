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

            if (target is not ShipBase shipBase)
                return;

            // 1) Стакаем/обновляем эффект на корабле
            shipBase.AddOrStackEffect(this, duration);

            // 2) Если DoT уже работает - НЕ запускать второй
            if (shipBase.RunningDotEffects.Contains(EffectId))
                return;

            // 3) Иначе запускаем DoT
            shipBase.RunningDotEffects.Add(EffectId);
            shipBase.StartCoroutine(DoDamageOverTime(shipBase));
        }

        private IEnumerator DoDamageOverTime(ShipBase ship)
        {
            while (true)
            {
                var eff = ship.GetEffect(EffectId);

                if (eff == null)
                {
                    Debug.Log($"[DOT END] {EffectId} on {ship.name}");
                    break;
                }

                if (ship.TryGetStat(StatType.HitPoint, out var hpStat))
                {
                    hpStat.AddToCurrent(-damagePerTick);

                    Debug.Log(
                        $"[DOT TICK] {EffectId} on {ship.name} " +
                        $"| Damage={damagePerTick} " +
                        $"| Stacks={eff.Stacks} " +
                        $"| HP={hpStat.Current}"
                    );
                }

                yield return new WaitForSeconds(1f);
            }

            ship.RunningDotEffects.Remove(EffectId);
        }

    }
}
