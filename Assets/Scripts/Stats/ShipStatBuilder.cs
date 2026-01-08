using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Ships
{
	public sealed class ShipStatBuildResult
	{
		public Stats ShipStats;
		public List<WeaponStatEffectModel> WeaponEffects = new();
	}

	public static class ShipStatBuilder
	{
		public static ShipStatBuildResult Build(
			MetaState state,
			bool includeHull,
			bool includeModules,
			bool includeMeta)
		{
			var result = new ShipStatBuildResult
			{
				ShipStats = new Stats()
			};

			if (state == null)
				return result;

			HullModel hull = null;
			if (includeHull && !string.IsNullOrEmpty(state.SelectedShipId))
				hull = HullLoader.Load(state.SelectedShipId);

			if (includeHull && hull != null)
			{
				ApplyHullStats(result.ShipStats, hull);
				StatEffectApplier.ApplyAll(result.ShipStats, hull.StatEffects, StatModifierSource.Hull, hull);
			}

			if (includeMeta)
				StatEffectApplier.ApplyAll(result.ShipStats, state.MainStatEffects, StatModifierSource.Main, state);

			if (includeModules)
				ApplyModuleEffects(state, result.ShipStats, result.WeaponEffects);

			ShipStatBonusApplier.Apply(result.ShipStats);

			return result;
		}

		private static void ApplyHullStats(Stats stats, HullModel hull)
		{
			if (stats == null || hull?.stats == null)
				return;

			var fields = typeof(StatContainer).GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var f in fields)
			{
				if (!Enum.TryParse(f.Name, out StatType statType))
					continue;

				var value = (float)f.GetValue(hull.stats);
				if (Mathf.Approximately(value, 0f))
					continue;

				var stat = stats.GetOrCreateStat(statType, 0f);
				stat.AddModifier(new StatModifier(
					StatModifierType.Flat,
					StatModifierTarget.Maximum,
					StatModifierPeriodicity.Permanent,
					value,
					source: hull,
					sourceType: StatModifierSource.Hull));
			}
		}

		private static void ApplyModuleEffects(
			MetaState state,
			Stats stats,
			List<WeaponStatEffectModel> weaponEffects)
		{
			if (state?.Fit?.GridPlacements == null)
				return;

			for (var i = 0; i < state.Fit.GridPlacements.Count; i++)
			{
				var placement = state.Fit.GridPlacements[i];
				if (placement == null ||
				    placement.GridType != ShipGridType.ModuleGrid ||
				    string.IsNullOrEmpty(placement.ItemId))
					continue;

				if (!ModuleBuilder.TryLoadModuleData(placement.ItemId, out var module))
					continue;

				StatEffectApplier.ApplyAll(stats, module.ShipStatEffects, StatModifierSource.Module, module);

				if (module.WeaponStatEffects != null && module.WeaponStatEffects.Count > 0)
					weaponEffects.AddRange(module.WeaponStatEffects);
			}
		}
	}
}
