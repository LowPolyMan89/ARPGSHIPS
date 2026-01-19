using System.IO;
using UnityEngine;

namespace Ships
{
	public struct EnergyReport
	{
		public float BaseMax;
		public float BonusMax;
		public float Used;

		public float Max => BaseMax + BonusMax;
		public bool CanStart => Used <= Max;
	}

	public static class EnergyCalculator
	{
		public static EnergyReport Calculate(MetaState state)
		{
			var report = new EnergyReport();
			if (state == null)
				return report;

			var hull = HullLoader.Load(state.SelectedShipId);
			report.BaseMax = hull != null ? hull.Energy : 0f;
			report.BonusMax = 0f;
			report.Used = 0f;

			var placements = state.Fit?.GridPlacements;
			if (placements == null || placements.Count == 0)
				return report;

			for (var i = 0; i < placements.Count; i++)
			{
				var p = placements[i];
				if (p == null || string.IsNullOrEmpty(p.ItemId))
					continue;

				float energy = 0f;
				if (ModuleBuilder.TryLoadModuleData(p.ItemId, out var module))
				{
					energy = module.EnergyCost;
				}
				else
				{
					if (TryResolveWeaponEnergy(p.ItemId, out var cost))
					{
						energy = cost;
					}
					else
					{
						Debug.LogWarning($"[EnergyCalculator] Item data not found for '{p.ItemId}'");
						continue;
					}
				}

				if (energy >= 0f)
					report.Used += energy;
				else
					report.BonusMax += Mathf.Abs(energy);
			}

			return report;
		}

		private static bool TryResolveWeaponEnergy(string templateId, out float cost)
		{
			cost = 0f;
			if (string.IsNullOrEmpty(templateId))
				return false;

			var file = templateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
				? templateId
				: templateId + ".json";
			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, file);
			if (!ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template) || template == null)
				return false;

			cost = template.EnergyCost;
			return true;
		}
	}
}
