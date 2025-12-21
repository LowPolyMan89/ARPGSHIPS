using System.IO;
using UnityEngine;

namespace Ships
{
	public static class LootLoader
	{
		public static LootTable Load(string id)
		{
			var relativePath = Path.Combine(PathConstant.LootTables, id + ".json");

			if (!ResourceLoader.TryLoadStreamingJson(relativePath, out LootTable table))
			{
				Debug.LogError("LootTable not found: " + ResourceLoader.GetStreamingPath(relativePath));
				return null;
			}

			return table;
		}
	}
}
