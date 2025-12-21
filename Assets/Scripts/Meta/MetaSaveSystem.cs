using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ships
{
	public static class MetaSaveSystem
	{
		public static void Save(MetaState state)
		{
			ResourceLoader.SavePersistentJson(PathConstant.MetaFile, state, true);
		}

		public static MetaState Load()
		{
			if (!ResourceLoader.TryLoadPersistentJson(PathConstant.MetaFile, out MetaState meta))
				meta = new MetaState();

			// подтягиваем сгенерённые предметы с диска, но без дублей
			LoadGeneratedItems(meta);
			CleanupMissingGeneratedItems(meta);

			return meta;
		}

		private static void LoadGeneratedItems(MetaState state)
		{
			var inventory = state.InventoryModel.InventoryUniqueItems;

			foreach (var file in ResourceLoader.GetPersistentFiles(PathConstant.Inventory, "*.json"))
			{
				var relativePath = Path.Combine(PathConstant.Inventory, file);
				if (!ResourceLoader.TryLoadPersistentJson(relativePath, out GeneratedWeaponItem gen))
					continue;

				// если такой ItemId уже есть в профиле - пропускаем
				var exists = inventory.Exists(i => i.ItemId == gen.ItemId);
				if (exists)
					continue;

				inventory.Add(new InventoryItem
				{
					ItemId = gen.ItemId,
					TemplateId = gen.TemplateId
				});
			}
		}

		private static void CleanupMissingGeneratedItems(MetaState state)
		{
			if (state == null)
				return;

			var inv = state.InventoryModel.InventoryUniqueItems;
			var removedIds = new HashSet<string>();

			for (var i = inv.Count - 1; i >= 0; i--)
			{
				var item = inv[i];
				if (item == null || string.IsNullOrEmpty(item.ItemId))
					continue;

				var relativePath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
				if (ResourceLoader.PersistentFileExists(relativePath))
					continue;

				removedIds.Add(item.ItemId);
				inv.RemoveAt(i);
			}

			if (removedIds.Count == 0)
				return;

			if (state.Fit?.GridPlacements != null)
				state.Fit.GridPlacements.RemoveAll(p => p != null && removedIds.Contains(p.ItemId));
		}
	}
}
