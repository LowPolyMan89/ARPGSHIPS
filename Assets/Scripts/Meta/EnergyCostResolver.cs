using System.IO;

namespace Ships
{
	public static class EnergyCostResolver
	{
		public static float ResolveEnergyCost(InventoryItem item)
		{
			return TryResolveEnergyCost(item, out var cost) ? cost : 0f;
		}

		public static bool TryResolveEnergyCost(InventoryItem item, out float cost)
		{
			cost = 0f;
			if (item == null)
				return false;

			var templateId = InventoryUtils.ResolveItemId(item);

			if (string.IsNullOrEmpty(templateId))
				return false;

			templateId = templateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
				? templateId
				: templateId + ".json";

			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateId);
			if (!ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template) || template == null)
				return false;

			cost = template.EnergyCost;
			return true;
		}
	}
}
