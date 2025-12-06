using System.IO;
using UnityEngine;

namespace Ships
{
	public static class LootLoader
	{
		private static readonly string LootPath =
			Path.Combine(Application.streamingAssetsPath, "Configs/Loot/LootTables");

		public static LootTable Load(string id)
		{
			var file = Path.Combine(LootPath, id + ".json");

			if (!File.Exists(file))
			{
				Debug.LogError("LootTable not found: " + file);
				return null;
			}

			var json = File.ReadAllText(file);
			return JsonUtility.FromJson<LootTable>(json);
		}
	}
}