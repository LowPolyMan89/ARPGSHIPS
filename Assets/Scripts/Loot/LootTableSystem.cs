using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ships
{
	public static class LootTableSystem
	{
		public static (string itemId, string rarity) Roll(LootTable table)
		{
			// 1. Выбираем предмет
			LootEntry entry = RollItem(table);

			// 2. Выбираем его редкость
			string rarity = RollRarity(entry.Weights);

			return (entry.ItemId, rarity);
		}

		private static LootEntry RollItem(LootTable table)
		{
			int total = 0;
			foreach (var e in table.Drops)
				total += e.Chance;

			int roll = Random.Range(0, total);
			int accum = 0;

			foreach (var e in table.Drops)
			{
				accum += e.Chance;
				if (roll < accum)
					return e;
			}

			return table.Drops[0];
		}

		private static string RollRarity(int[] weights)
		{
			int total = 0;
			foreach (var w in weights)
				total += w;

			int roll = Random.Range(0, total);
			int accum = 0;

			for (int i = 0; i < weights.Length; i++)
			{
				accum += weights[i];
				if (roll < accum)
					return RarityIndexToName(i);
			}

			return "Common";
		}

		private static string RarityIndexToName(int idx)
		{
			return idx switch
			{
				0 => "Common",
				1 => "Uncommon",
				2 => "Rare",
				3 => "Epic",
				4 => "Legendary",
				_ => "Common"
			};
		}
	}
}
