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

				var relativePath = Path.Combine(PathConstant.Inventory, p.ItemId + ".json");
				if (!ResourceLoader.TryLoadPersistentJson(relativePath, out WeaponLoadData data))
				{
					Debug.LogWarning($"[EnergyCalculator] Item data not found for '{p.ItemId}'");
					continue;
				}

				var energy = data.EnergyCost;
				if (energy >= 0f)
					report.Used += energy;
				else
					report.BonusMax += Mathf.Abs(energy);
			}

			return report;
		}
	}
}
