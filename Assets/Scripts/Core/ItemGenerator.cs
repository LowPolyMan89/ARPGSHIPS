using System;
using System.IO;
using System.Collections.Generic;
using Tanks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tanks
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using Random = UnityEngine.Random;

	public static class ItemGenerator
	{
		public static string WeaponConfigsPath =>
			Path.Combine(Application.streamingAssetsPath, "Configs/Weapons");

		public static string ModulesConfigsPath =>
			Path.Combine(Application.streamingAssetsPath, "Configs/Modules");

		public static string OutputPath =>
			Path.Combine(Application.persistentDataPath, "Inventory");

		// =============================
		// Загрузка всех шаблонов
		// =============================
		public static List<string> LoadWeaponFiles()
		{
			if (!Directory.Exists(WeaponConfigsPath))
				return new List<string>();

			return Directory.GetFiles(WeaponConfigsPath, "*.json")
				.Select(Path.GetFileName)
				.ToList();
		}

		public static List<string> LoadModuleFiles()
		{
			if (!Directory.Exists(ModulesConfigsPath))
				return new List<string>();

			return Directory.GetFiles(ModulesConfigsPath, "*.json")
				.Select(Path.GetFileName)
				.ToList();
		}

		// =============================
		// Генерация через LootTable
		// =============================
		public static GeneratedWeaponItem GenerateWeaponFromLoot(string lootTableId)
		{
			if (!string.IsNullOrEmpty(lootTableId))
			{
				var table = LootLoader.Load(lootTableId);
				if (table != null)
				{
					var roll = LootTableSystem.Roll(table);
					return GenerateWeapon(roll.itemId + ".json", roll.rarity);
				}
			}

			return GenerateWeaponFromAllTemplates();
		}

		public static GeneratedWeaponItem GenerateModuleFromLoot(string lootTableId = null)
		{
			return GenerateModuleFromAllTemplates();
		}

		public static GeneratedWeaponItem GenerateModule(string templateFile, string forcedRarity)
		{
			return new GeneratedWeaponItem();
		}

		// =============================
		// Глобальный пул всех оружий
		// =============================
		private static GeneratedWeaponItem GenerateModuleFromAllTemplates()
		{
			return new GeneratedWeaponItem();
		}

		private static GeneratedWeaponItem GenerateWeaponFromAllTemplates()
		{
			var files = LoadWeaponFiles();
			if (files.Count == 0) return null;

			List<(string template, string rarity, int weight)> pool = new();

			foreach (var file in files)
			{
				var json = File.ReadAllText(Path.Combine(WeaponConfigsPath, file));
				var template = JsonUtility.FromJson<WeaponTemplate>(json);

				foreach (var r in template.Rarities)
					pool.Add((template.Id, r.Rarity, r.DropChance));
			}

			var pick = PickGlobal(pool);
			return GenerateWeapon(pick.template + ".json", pick.rarity);
		}

		private static (string template, string rarity) PickGlobal(
			List<(string template, string rarity, int weight)> entries)
		{
			int total = entries.Sum(e => e.weight);
			int roll = Random.Range(0, total);
			int accum = 0;

			foreach (var e in entries)
			{
				accum += e.weight;
				if (roll < accum)
					return (e.template, e.rarity);
			}

			return (entries[0].template, entries[0].rarity);
		}

		// =============================
		// Основной метод генерации
		// =============================
		public static GeneratedWeaponItem GenerateWeapon(string templateFile, string forcedRarity)
		{
			var fullPath = Path.Combine(WeaponConfigsPath, templateFile);
			var json = File.ReadAllText(fullPath);

			var template = JsonUtility.FromJson<WeaponTemplate>(json);

			string rarity = forcedRarity == "Random"
				? PickRandomRarity(template)
				: forcedRarity;

			var rarityData = FindRarity(template, rarity);

			var item = new GeneratedWeaponItem
			{
				ItemId = Services.UniqueIdGenerator.GenerateItemId(),
				TemplateId = template.Id,
				Name = template.Name,
				Rarity = rarity,

				Slot = template.Slot,
				DamageType = template.DamageType,
				Size = template.Size,
				Icon = template.Icon
			};

			List<StatValue> stats = new();
			foreach (var s in rarityData.Stats.Entries)
			{
				float v = Mathf.RoundToInt(Random.Range(s.Min, s.Max));
				stats.Add(new StatValue { Name = s.Name, Value = v });
			}

			item.Stats = stats.ToArray();

			item.Effects = GenerateEffects(template, rarityData);

			SaveItem(item);
			return item;
		}

		// =============================
		// Внутренние хелперы
		// =============================
		private static WeaponTemplate.RarityEntry FindRarity(WeaponTemplate t, string rarity)
		{
			return t.Rarities.FirstOrDefault(
				r => r.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase));
		}

		private static string PickRandomRarity(WeaponTemplate tpl)
		{
			int total = tpl.Rarities.Sum(r => r.DropChance);
			int roll = Random.Range(0, total);
			int accum = 0;

			foreach (var r in tpl.Rarities)
			{
				accum += r.DropChance;
				if (roll < accum)
					return r.Rarity;
			}

			return tpl.Rarities[0].Rarity;
		}

		private static List<string> GenerateEffects(WeaponTemplate tpl, WeaponTemplate.RarityEntry rarity)
		{
			var result = new List<string>();
			int max = rarity.MaxEffectCount;

			if (max <= 0 || tpl.AvailableEffects == null)
				return result;

			int count = Random.Range(0, max + 1);
			HashSet<int> used = new();

			while (result.Count < count)
			{
				int i = Random.Range(0, tpl.AvailableEffects.Length);
				if (used.Add(i))
					result.Add(tpl.AvailableEffects[i]);
			}

			return result;
		}

		private static void SaveItem(GeneratedWeaponItem item)
		{
			if (!Directory.Exists(OutputPath))
				Directory.CreateDirectory(OutputPath);

			var json = JsonUtility.ToJson(item, true);
			var path = Path.Combine(OutputPath, item.ItemId + ".json");

			File.WriteAllText(path, json);
			Debug.Log("Saved item → " + path);
		}
	}
}


[Serializable]
public sealed class WeaponTemplate
{
	public string Id;
	public string Name;
	public string Icon;
	public string Slot;
	public string DamageType;
	public string Size;

	public string[] AvailableEffects;

	public RarityEntry[] Rarities;

	[Serializable]
	public sealed class RarityEntry
	{
		public string Rarity;
		public int DropChance;
		public int MaxEffectCount;
		public StatList Stats;
	}

	[Serializable]
	public sealed class StatList
	{
		public StatRangeEntry[] Entries;
	}

	[Serializable]
	public sealed class StatRangeEntry
	{
		public string Name;
		public float Min;
		public float Max;
	}
}

[Serializable]
public class GeneratedWeaponItem : IGeneratedItem
{
	public string ItemId;
	public string TemplateId;
	public string Name;
	public string Rarity;

	public string Slot;
	public string DamageType;
	public string Size;
	public string Icon;

	public StatValue[] Stats;
	public List<string> Effects;
	string IGeneratedItem.ItemId => ItemId;
	string IGeneratedItem.TemplateId => TemplateId;
	string IGeneratedItem.Name => Name;
	string IGeneratedItem.Rarity => Rarity;
}


[Serializable]
public sealed class StatValue
{
	public string Name;
	public float Value;
}