using System.IO;
using UnityEngine;

namespace Tanks
{
	public static class MetaSaveSystem
	{
		private static readonly string _path =
			Path.Combine(Application.persistentDataPath, "meta.json");

		public static void Save(MetaState state)
		{
			var json = JsonUtility.ToJson(state, true);
			File.WriteAllText(_path, json);
		}

		public static MetaState Load()
		{
			if (!File.Exists(_path))
				return new MetaState();

			var json = File.ReadAllText(_path);
			var meta = JsonUtility.FromJson<MetaState>(json);

			// подтягиваем сгенерённые предметы с диска, но без дублей
			LoadGeneratedItems(meta);

			return meta;
		}

		private static void LoadGeneratedItems(MetaState state)
		{
			var folder = ItemGenerator.OutputPath;
			if (!Directory.Exists(folder))
				return;

			var inventory = state.InventoryModel.InventoryUniqueItems;

			foreach (var file in Directory.GetFiles(folder, "*.json"))
			{
				var json = File.ReadAllText(file);
				var gen = JsonUtility.FromJson<GeneratedWeaponItem>(json);

				// если такой ItemId уже есть в профиле — пропускаем
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
	}
}