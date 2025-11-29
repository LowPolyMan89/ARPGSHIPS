using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public abstract class ShipBase : MonoBehaviour, ITargetable
	{
		public ShipVisual _visual;
		public SideType SideType;
		public Stats ShipStats;
		public List<StatVisual> StatVisuals = new List<StatVisual>();
		public List<ActiveEffectVisual> ActiveEffects = new();
		public WeaponController WeaponController;
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

		public void Init()
		{
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

			StartCoroutine(Tick());

			StatVisuals.Clear();
			foreach (var kvp in ShipStats.All)
			{
				StatVisuals.Add(new StatVisual { Name = kvp.Key });
			}

			_lastPos = transform.position;
		}

		private IEnumerator Tick()
		{
			while (true)
			{
				yield return new WaitForSeconds(1f);

				ShipStats.Tick();
				for (int i = ActiveEffects.Count - 1; i >= 0; i--)
				{
					var eff = ActiveEffects[i];
					eff.Remaining -= 1f;

					if (eff.Remaining <= 0)
						ActiveEffects.RemoveAt(i);
				}
			}
		}

		public void AddActiveEffect(string effectName, float duration)
		{
			ActiveEffects.Add(new ActiveEffectVisual(effectName, duration));
		}

		private void Update()
		{
			// обновляем Velocity
			var pos = transform.position;
			_velocity = (pos - _lastPos) / Time.deltaTime;
			_lastPos = pos;

			// обновляем StatVisuals
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
			
			WeaponController.OnUpdate();
		}

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