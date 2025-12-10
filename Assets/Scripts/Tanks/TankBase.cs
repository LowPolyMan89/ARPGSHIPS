using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Tanks
{
	public abstract class TankBase : MonoBehaviour, ITargetable
	{
		public TankVisual _visual;
		public SideType SideType;
		[SerializeField] private TeamMask _team;
		public TeamMask HitMask;
		public Stats TankStats;
		[SerializeField] private TargetSize size = TargetSize.Medium;
		public List<StatVisual> StatVisuals = new();
		public TankTurret Turret;
		public TurretAimSystem AimSystem;
		public List<ShieldSector> ShieldSectors = new();
		public HashSet<string> RunningDotEffects = new();
		public Transform Transform => transform;
		public TargetSize Size => size;

		private Vector3 _lastPos;
		private Vector3 _velocity;
		public Vector3 Velocity => _velocity;

		public TeamMask Team => _team;

		public bool IsAlive
		{
			get
			{
				if (TankStats.TryGetStat(StatType.HitPoint, out var hp))
					return hp.Current > 0;
				return true;
			}
		}

		public virtual void LoadShipFromConfig(string fileName)
		{
			TankStats = new Stats();
			var data = HullLoader.Load(fileName);
			var fields = typeof(StatContainer).GetFields(
				BindingFlags.Public | BindingFlags.Instance);

			foreach (var f in fields)
			{
				var fieldName = f.Name;
				if (!Enum.TryParse(fieldName, out StatType statType))
					continue;
				var value = (float)f.GetValue(data.stats);
				TankStats.AddStat(new Stat(statType, value));
			}
			
			if (TryGetComponent<ShieldController>(out var controller))
			{
				foreach (var sector in ShieldSectors)
				{
					var modelFromSide = data.shields.Find(x =>
					{
						return Enum.TryParse<ShieldSide>(x.id, true, out var parsed) 
						       && parsed == sector.Side;
					});

					sector.InitFromConfig(
						hp: modelFromSide.value,
						regen: modelFromSide.regeneration,
						restoreDelay: modelFromSide.restoreTime
					);

					controller.AddSector(sector);
				}
				
			}
		}
		
		public void LoadTankFromPrefab()
		{
			if (TryGetComponent<ShieldController>(out var controller))
			{
				foreach (var sector in ShieldSectors)
				{
					sector.InitFromPrefab();
					controller.AddSector(sector);
				}
			}
		}
		

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
			_team = SideType switch
			{
				SideType.Player => TeamMask.Player,
				SideType.Enemy => TeamMask.Enemy,
				SideType.Ally => TeamMask.Ally,
				_ => TeamMask.Neutral
			};

			if (_visual != null)
			{
				_visual.Unload();
				_visual.Load();
			}
			else
			{
				_visual = new TankVisual();
				_visual.Load();
			}
			Battle.Instance.AllTanks.Add(this);
			StartCoroutine(TickEffects());
			// отправляем визуализаторам данные статов
			StatVisuals.Clear();
			foreach (var kvp in TankStats.All)
			{
				StatVisuals.Add(new StatVisual { Name = kvp.Key });
			}

			_lastPos = transform.position;
		}
		


		// ============================================================
		// DAMAGE → EFFECTS
		// ============================================================

		public void TakeDamage(CalculatedDamage calc)
		{
			if (calc.FinalDamage > 0)
			{
				if (TryGetStat(StatType.HitPoint, out var hp))
					hp.AddToCurrent(-calc.FinalDamage);
			}

			if (calc.SourceWeapon?.Model?.Effects != null)
			{
				foreach (var effect in calc.SourceWeapon.Model.Effects)
					effect.Apply(this, calc.FinalDamage, calc.SourceWeapon);
			}
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
				TankStats.Tick();

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
					Battle.Instance.AllTanks.Remove(this);

				Destroy(gameObject);
				return;
			}

			// обновить Velocity
			var pos = transform.position;
			_velocity = (pos - _lastPos) / Time.deltaTime;
			_lastPos = pos;

			// обновляем визуальное состояние статов
			foreach (var kvp in TankStats.All)
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

			if (AimSystem != null && AimSystem.Turret != null)
			{
				AimSystem.Update();
			}
		}


		// ============================================================
		// INTERFACE
		// ============================================================

		public bool TryGetStat(StatType name, out IStat stat)
		{
			bool result = TankStats.TryGetStat(name, out var s);
			stat = s;
			return result;
		}

		public IStat GetStat(StatType name) => TankStats.GetStat(name);
		public IEnumerable<IStat> GetAllStats() => TankStats.All.Values;
	}
}