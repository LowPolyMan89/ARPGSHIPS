using System.Collections.Generic;

namespace Ships
{
	public static class MetaSaveSystem
	{
		public static void Save(MetaState state)
		{
			if (state != null)
				SaveInventory(state.InventoryModel);
			ResourceLoader.SavePersistentJson(PathConstant.MetaFile, state, true);
		}

		public static MetaState Load()
		{
			if (!ResourceLoader.TryLoadPersistentJson(PathConstant.MetaFile, out MetaState meta))
				meta = new MetaState();

			meta.InventoryModel = LoadInventory(meta);
			InventoryUtils.RebuildEquippedCounts(meta);

			return meta;
		}

		private static PlayerInventoryModel LoadInventory(MetaState state)
		{
			if (ResourceLoader.TryLoadPersistentJson(PathConstant.InventoryConfig, out InventorySaveData save) &&
			    save?.Items != null)
			{
				var model = new PlayerInventoryModel();
				var removedDefault = false;
				foreach (var item in save.Items)
				{
					if (item == null)
						continue;

					var id = !string.IsNullOrEmpty(item.TemplateId) ? item.TemplateId : item.ItemId;
					if (string.IsNullOrEmpty(id))
						continue;
					id = NormalizeItemId(id);
					if (DefaultWeaponResolver.IsDefaultTemplateId(id))
					{
						removedDefault = true;
						continue;
					}

					var count = item.Count <= 0 ? 0 : item.Count;
					model.InventoryUniqueItems.Add(new InventoryItem
					{
						ItemId = id,
						TemplateId = id,
						Rarity = item.Rarity,
						Count = count
					});
				}
				if (removedDefault)
					SaveInventory(model);
				return model;
			}

			var fallback = new PlayerInventoryModel();
			var legacyMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
			if (state?.InventoryModel?.InventoryUniqueItems != null)
			{
				foreach (var item in state.InventoryModel.InventoryUniqueItems)
				{
					if (item == null)
						continue;

					var id = !string.IsNullOrEmpty(item.TemplateId) ? item.TemplateId : item.ItemId;
					if (string.IsNullOrEmpty(id))
						continue;
					id = NormalizeItemId(id);
					if (DefaultWeaponResolver.IsDefaultTemplateId(id))
						continue;

					if (!string.IsNullOrEmpty(item.ItemId) && !string.IsNullOrEmpty(item.TemplateId) &&
					    !string.Equals(item.ItemId, item.TemplateId, System.StringComparison.OrdinalIgnoreCase))
					{
						legacyMap[item.ItemId] = NormalizeItemId(item.TemplateId);
					}

					var rarity = string.IsNullOrEmpty(item.Rarity) ? DefaultWeaponResolver.DefaultRarity : item.Rarity;
					InventoryUtils.AddOrIncrease(fallback, id, rarity, 1);
				}
			}

			if (legacyMap.Count > 0)
				RemapPlacements(state, legacyMap);

			SaveInventory(fallback);
			return fallback;
		}

		private static void SaveInventory(PlayerInventoryModel model)
		{
			if (model?.InventoryUniqueItems == null)
				return;

			var save = new InventorySaveData();
			foreach (var item in model.InventoryUniqueItems)
			{
				if (item == null)
					continue;

				var id = !string.IsNullOrEmpty(item.TemplateId) ? item.TemplateId : item.ItemId;
				if (string.IsNullOrEmpty(id))
					continue;
				id = NormalizeItemId(id);
				if (DefaultWeaponResolver.IsDefaultTemplateId(id))
					continue;

				var count = item.Count <= 0 ? 0 : item.Count;
				save.Items.Add(new InventoryItem
				{
					ItemId = id,
					TemplateId = id,
					Rarity = item.Rarity,
					Count = count
				});
			}

			ResourceLoader.SavePersistentJson(PathConstant.InventoryConfig, save, true);
		}

		private static void RemapPlacements(MetaState state, Dictionary<string, string> legacyMap)
		{
			if (state?.Fit?.GridPlacements == null || legacyMap == null || legacyMap.Count == 0)
				return;

			foreach (var placement in state.Fit.GridPlacements)
			{
				if (placement == null || string.IsNullOrEmpty(placement.ItemId))
					continue;

				if (legacyMap.TryGetValue(placement.ItemId, out var templateId))
					placement.ItemId = templateId;
			}
		}

		private static string NormalizeItemId(string itemId)
		{
			if (string.IsNullOrEmpty(itemId))
				return itemId;

			return itemId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
				? itemId.Substring(0, itemId.Length - ".json".Length)
				: itemId;
		}

		[System.Serializable]
		private sealed class InventorySaveData
		{
			public List<InventoryItem> Items = new();
		}
	}
}
