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

			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var generatedPath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
				if (ResourceLoader.TryLoadPersistentJson(generatedPath, out GeneratedWeaponItem weapon) && weapon != null)
				{
					cost = weapon.EnergyCost;
					return true;
				}
			}

			if (string.IsNullOrEmpty(item.TemplateId))
				return false;

			var templateId = item.TemplateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
				? item.TemplateId
				: item.TemplateId + ".json";

			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateId);
			if (!ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template) || template == null)
				return false;

			cost = template.EnergyCost;
			return true;
		}
	}
}
