using System.Collections;
using UnityEngine;

namespace Tanks.HitEffect
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

            if (target is not TankBase tankBase)
                return;

            // 1) Стакаем/обновляем эффект на корабле
            tankBase.AddOrStackEffect(this, duration);

            // 2) Если DoT уже работает — НЕ запускать второй
            if (tankBase.RunningDotEffects.Contains(EffectId))
                return;

            // 3) Иначе запускаем DoT
            tankBase.RunningDotEffects.Add(EffectId);
            tankBase.StartCoroutine(DoDamageOverTime(tankBase));
        }

        private IEnumerator DoDamageOverTime(TankBase tank)
        {
            while (true)
            {
                var eff = tank.GetEffect(EffectId);

                if (eff == null)
                {
                    Debug.Log($"[DOT END] {EffectId} on {tank.name}");
                    break;
                }

                if (tank.TryGetStat(StatType.HitPoint, out var hpStat))
                {
                    hpStat.AddToCurrent(-damagePerTick);

                    Debug.Log(
                        $"[DOT TICK] {EffectId} on {tank.name} " +
                        $"| Damage={damagePerTick} " +
                        $"| Stacks={eff.Stacks} " +
                        $"| HP={hpStat.Current}"
                    );
                }

                yield return new WaitForSeconds(1f);
            }

            tank.RunningDotEffects.Remove(EffectId);
        }

    }
}