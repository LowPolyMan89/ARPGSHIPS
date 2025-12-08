using System.Collections.Generic;

namespace Tanks
{
    public sealed class Stat : IStat
    {
        public StatType Name { get; }
        public float BaseMaximum { get; private set; }
        public float BaseCurrent { get; private set; }
        public float Maximum { get; private set; }
        public float Current { get; private set; }

        public float Amount
        {
            get
            {
                if (Maximum <= 0f) 
                    return 0f;
                var n = Current / Maximum;
                if (float.IsNaN(n) || float.IsInfinity(n))
                    return 0f;
                return n;
            }
        }
        
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        public Stat(StatType name, float baseMaximum, float? baseCurrent = null)
        {
            Name = name;
            BaseMaximum = baseMaximum;
            BaseCurrent = baseCurrent ?? baseMaximum;
            Maximum = BaseMaximum;
            Current = BaseCurrent;
        }

        public void SetBaseValues(float baseMaximum, float? baseCurrent = null, bool resetCurrentToFull = false)
        {
            BaseMaximum = baseMaximum;
            BaseCurrent = baseCurrent ?? BaseMaximum;
            Recalculate(resetCurrentToFull);
        }

        public void SetBaseMaximum(float baseMaximum, bool resetCurrentToFull = false)
        {
            BaseMaximum = baseMaximum;
            Recalculate(resetCurrentToFull);
        }

        public void SetBaseCurrent(float baseCurrent)
        {
            BaseCurrent = baseCurrent;
            Recalculate(false);
        }

        public void AddToCurrent(float delta)
        {
            Current += delta;
            if (Current > Maximum)
                Current = Maximum;
            if (Current < 0)
                Current = 0;
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            Recalculate(false);
        }

        public void RemoveModifier(StatModifier modifier)
        {
            if (_modifiers.Remove(modifier))
            {
                Recalculate(false);
            }
        }

        public void RemoveModifiersFromSource(object source)
        {
            if (source == null)
                return;

            var changed = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].Source == source)
                {
                    _modifiers.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                Recalculate(false);
            }
        }

        public void Tick()
        {
            var changed = false;

            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var mod = _modifiers[i];
                if (mod.Periodicity == StatModifierPeriodicity.Timed)
                {
                    mod.RemainingTicks--;
                    if (mod.RemainingTicks <= 0)
                    {
                        _modifiers.RemoveAt(i);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                Recalculate(false);
            }
        }

        private void Recalculate(bool resetCurrentToFull)
        {
            // сохраняем прошлую пропорцию
            var previousMax = Maximum > 0 ? Maximum : BaseMaximum;
            var previousRatio = previousMax > 0 ? Current / previousMax : 1f;

            // новая максималка
            var newMaximum = ApplyModifiers(BaseMaximum, StatModifierTarget.Maximum);

            // базовое текущее (до модификаторов Current)
            var newCurrentBase = resetCurrentToFull ? newMaximum : BaseCurrent;

            // применяем модификаторы к Current (их у MoveSpeed нет)
            var hasCurrentMods = HasModifiersForTarget(StatModifierTarget.Current);
            var newCurrent = ApplyModifiers(newCurrentBase, StatModifierTarget.Current);

            // ---- ВАЖНО: выбираем поведение в зависимости от типа стата ----

            bool isResource = Name == StatType.HitPoint || 
                              Name == StatType.Shield;

            if (!hasCurrentMods)
            {
                if (resetCurrentToFull)
                {
                    newCurrent = newMaximum;
                }
                else if (isResource)
                {
                    // HP/Shield сохраняют процент
                    newCurrent = newMaximum * previousRatio;
                }
                else
                {
                    // ВСЕ ПАРАМЕТРЫ (MoveSpeed, Accel, FireRate, TurnSpeed и т.д.)
                    // ВСЕГДА возвращаются на максимум после снятия модификаторов
                    newCurrent = newMaximum;
                }
            }

            // clamp
            if (newCurrent > newMaximum) newCurrent = newMaximum;
            if (newCurrent < 0) newCurrent = 0;

            Maximum = newMaximum;
            Current = newCurrent;
        }

        private bool HasModifiersForTarget(StatModifierTarget target)
        {
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Target == target)
                    return true;
            }

            return false;
        }

        private float ApplyModifiers(float baseValue, StatModifierTarget target)
        {
            var flatAdd = 0f;
            var percentAdd = 0f;
            var percentMult = 1f;
            var hasSet = false;
            var setValue = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                if (mod.Target != target)
                    continue;

                switch (mod.Type)
                {
                    case StatModifierType.Flat:
                        flatAdd += mod.Value;
                        break;

                    case StatModifierType.PercentAdd:
                        percentAdd += mod.Value;
                        break;

                    case StatModifierType.PercentMult:
                        percentMult *= (1f + mod.Value);
                        break;

                    case StatModifierType.Set:
                        hasSet = true;
                        setValue = mod.Value;
                        break;
                }
            }

            var value = baseValue + flatAdd;
            value *= (1f + percentAdd);
            value *= percentMult;

            if (hasSet)
                value = setValue;

            return value;
        }
    }
}
