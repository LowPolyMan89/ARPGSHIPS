using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ships
{
    public abstract class ShipBase : MonoBehaviour, ITargetable
    {
        public ShipVisual _visual;
        public SideType SideType;
        public TeamMask Team;
        public Stats ShipStats;

        public List<StatVisual> StatVisuals = new();
        public WeaponController WeaponController;

        public List<ShieldSector> ShieldSectors = new();
        public HashSet<string> RunningDotEffects = new();
        public Transform Transform => transform;

        public bool IsAlive
        {
            get
            {
                if (ShipStats.TryGetStat(StatType.HitPoint, out var hp))
                    return hp.Current > 0;
                return true;
            }
        }

        [SerializeField] private TargetSize size = TargetSize.Medium;
        public TargetSize Size => size;

        private Vector3 _lastPos;
        private Vector2 _velocity;
        public Vector2 Velocity => _velocity;

        // -------------------------------
        //  STACKABLE EFFECT SYSTEM
        // -------------------------------
        public class ActiveEffectInstance
        {
            public string EffectId;
            public int Stacks;
            public float Remaining;
            public float Duration;

            public ActiveEffectInstance(string id, float duration)
            {
                EffectId = id;
                Duration = duration;
                Remaining = duration;
                Stacks = 1;
            }
        }

        public List<ActiveEffectInstance> ActiveEffects = new();


        // ============================================================
        // INIT
        // ============================================================

        public void Init()
        {

            Team = SideType switch
            {
                SideType.Player => TeamMask.Player,
                SideType.Enemy  => TeamMask.Enemy,
                SideType.Ally   => TeamMask.Ally,
                _ => TeamMask.Neutral
            };
            
            if (_visual != null)
            {
                _visual.Unload();
                _visual.Load();
            }
            else
            {
                _visual = new ShipVisual();
                _visual.Load();
            }

            WeaponController.Init(SideType);
            Battle.Instance.AllShips.Add(this);

            InitShields();
            StartCoroutine(TickEffects());

            // отправляем визуализаторам данные статов
            StatVisuals.Clear();
            foreach (var kvp in ShipStats.All)
            {
                StatVisuals.Add(new StatVisual { Name = kvp.Key });
            }

            _lastPos = transform.position;
        }


        private void InitShields()
        {
            if (TryGetComponent<ShieldController>(out var controller))
            {
                foreach (var sector in ShieldSectors)
                {
                    sector.Init();
                    controller.AddSector(sector);
                }
            }
        }


        // ============================================================
        // DAMAGE → EFFECTS
        // ============================================================

        public void TakeDamage(float dmg, Vector2 hitPoint, WeaponBase source)
        {
            if (ShipStats.TryGetStat(StatType.HitPoint, out var hp))
                hp.AddToCurrent(-dmg);

            foreach (var effect in source.Model.Effects)
                effect.Apply(this, dmg, source);
        }


        // ============================================================
        // STACKABLE EFFECT API
        // ============================================================

        public ActiveEffectInstance GetEffect(string effectId)
        {
            return ActiveEffects.FirstOrDefault(e => e.EffectId == effectId);
        }

        public void AddOrStackEffect(IStackableEffect effect, float duration)
        {
            var inst = GetEffect(effect.EffectId);

            if (inst == null)
            {
                inst = new ActiveEffectInstance(effect.EffectId, duration);
                ActiveEffects.Add(inst);
            }
            else
            {
                inst.Remaining = duration;

                if (effect.CanStack)
                    inst.Stacks = Mathf.Min(inst.Stacks + 1, effect.MaxStacks);
                else
                    inst.Stacks = 1;
            }
        }


        // ============================================================
        // EFFECT TICK
        // ============================================================

        private IEnumerator TickEffects()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                // 1) Тик модификаторов статов
                ShipStats.Tick();

                // 2) Тик эффектов
                for (int i = ActiveEffects.Count - 1; i >= 0; i--)
                {
                    var eff = ActiveEffects[i];
                    eff.Remaining -= 1f;

                    if (eff.Remaining <= 0)
                        ActiveEffects.RemoveAt(i);
                }
            }
        }


        // ============================================================
        // UPDATE (velocity, stat visuals, weapons)
        // ============================================================

        private void Update()
        {
            if (!IsAlive)
            {
                if (SideType == SideType.Enemy)
                    Battle.Instance.AllShips.Remove(this);

                Destroy(gameObject);
                return;
            }

            // обновить Velocity
            var pos = transform.position;
            _velocity = (pos - _lastPos) / Time.deltaTime;
            _lastPos = pos;

            // обновляем визуальное состояние статов
            foreach (var kvp in ShipStats.All)
            {
                var statType = kvp.Key;
                var stat = kvp.Value;

                foreach (var visual in StatVisuals)
                {
                    if (visual.Name == statType)
                    {
                        visual.BaseCurrent = stat.BaseCurrent;
                        visual.BaseMaximum = stat.BaseMaximum;
                        visual.Current = stat.Current;
                        visual.Maximum = stat.Maximum;

                        visual.ModifierVisuals.Clear();
                        foreach (var mod in stat.Modifiers)
                        {
                            visual.ModifierVisuals.Add(new StatModifierVisual
                            {
                                Type = mod.Type,
                                Target = mod.Target,
                                Periodicity = mod.Periodicity,
                                Value = mod.Value,
                                RemainingTicks = mod.RemainingTicks,
                                SourceName = mod.Source?.ToString()
                            });
                        }
                    }
                }
            }

            // обновление оружия
            WeaponController.OnUpdate();
        }


        // ============================================================
        // INTERFACE
        // ============================================================

        public bool TryGetStat(StatType name, out IStat stat)
        {
            bool result = ShipStats.TryGetStat(name, out var s);
            stat = s;
            return result;
        }

        public IStat GetStat(StatType name) => ShipStats.GetStat(name);
        public IEnumerable<IStat> GetAllStats() => ShipStats.All.Values;
    }
}
