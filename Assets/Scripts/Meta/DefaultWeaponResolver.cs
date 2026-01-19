using System.IO;

namespace Ships
{
	public static class DefaultWeaponResolver
	{
		public const string DefaultRarity = "Common";
		private static readonly System.Collections.Generic.HashSet<string> DefaultTemplateIds =
			new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
			{
				"weapon_k_p_s_civilian_1",
				"weapon_k_p_m_civilian_1",
				"weapon_k_p_l_civilian_1"
			};

		public static bool TryGetDefaultTemplateId(ShipSocketSize size, out string templateId)
		{
			switch (size)
			{
				case ShipSocketSize.Small:
					templateId = "weapon_k_p_s_civilian_1";
					return true;
				case ShipSocketSize.Medium:
					templateId = "weapon_k_p_m_civilian_1";
					return true;
				case ShipSocketSize.Large:
					templateId = "weapon_k_p_l_civilian_1";
					return true;
				default:
					templateId = null;
					return false;
			}
		}

		public static bool TryBuildTemplateItem(string templateId, out InventoryItem item)
		{
			item = null;
			if (string.IsNullOrEmpty(templateId))
				return false;

			var templateFile = templateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
				? templateId
				: templateId + ".json";
			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateFile);
			if (!ResourceLoader.StreamingFileExists(templatePath))
				return false;

			item = new InventoryItem
			{
				ItemId = templateId,
				TemplateId = templateId,
				Rarity = DefaultRarity
			};
			return true;
		}

		public static bool IsDefaultTemplateId(string templateId)
		{
			if (string.IsNullOrEmpty(templateId))
				return false;

			if (templateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
				templateId = templateId.Substring(0, templateId.Length - ".json".Length);

			return DefaultTemplateIds.Contains(templateId);
		}
	}
}
