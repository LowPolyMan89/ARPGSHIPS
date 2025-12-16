using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tanks
{
	public static class ItemGenerator
	{
		public static string WeaponConfigsPath =>
			Path.Combine(Application.streamingAssetsPath, "Configs/Weapons");

		public static string EffectsConfigsPath =>
			Path.Combine(Application.streamingAssetsPath, "Configs/Effects");

		public static string ModulesConfigsPath =>
			Path.Combine(Application.streamingAssetsPath, "Configs/Modules");

		public static string OutputPath =>
			Path.Combine(Application.persistentDataPath, "Inventory");

		private static Dictionary<string, EffectTemplate> _effectsCache;

		private static void EnsureEffectsLoaded()
		{
			if (_effectsCache != null)
				return;

			_effectsCache = new Dictionary<string, EffectTemplate>(StringComparer.OrdinalIgnoreCase);

			foreach (var file in LoadEffectsFiles())
			{
				var json = File.ReadAllText(Path.Combine(EffectsConfigsPath, file));
				var collection = JsonUtility.FromJson<EffectTemplateCollection>(json);

				foreach (var e in collection.Effects)
					_effectsCache[e.Name] = e;
			}
		}

		public static List<string> LoadWeaponFiles()
		{
			if (!Directory.Exists(WeaponConfigsPath))
				return new List<string>();

			return Directory.GetFiles(WeaponConfigsPath, "*.json")
				.Select(Path.GetFileName)
				.ToList();
		}

		public static List<string> LoadEffectsFiles()
		{
			if (!Directory.Exists(EffectsConfigsPath))
				return new List<string>();

			return Directory.GetFiles(EffectsConfigsPath, "*.json")
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
			if (total <= 0)
				return (entries[0].template, entries[0].rarity);

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

		public static GeneratedWeaponItem GenerateWeapon(string templateFile, string forcedRarity)
		{
			var fullPath = Path.Combine(WeaponConfigsPath, templateFile);
			var json = File.ReadAllText(fullPath);

			var template = JsonUtility.FromJson<WeaponTemplate>(json);

			string rarity = forcedRarity == "Random"
				? PickRandomRarity(template)
				: forcedRarity;

			var rarityData = FindRarity(template, rarity);
			if (rarityData == null)
			{
				Debug.LogWarning($"Rarity '{rarity}' not found for weapon '{template.Id}', fallback to first entry.");
				rarityData = template.Rarities.FirstOrDefault();
				rarity = rarityData?.Rarity ?? "Unknown";
			}

			var item = new GeneratedWeaponItem
			{
				ItemId = Services.UniqueIdGenerator.GenerateItemId(),
				TemplateId = template.Id,
				Name = template.Name,
				Rarity = rarity,
				Prefab = template.Prefab,
				Slot = template.Slot,
				DamageType = template.DamageType,
				Size = template.Size,
				Icon = template.Icon
			};

			List<StatValue> stats = new();
			if (rarityData?.Stats?.Entries != null)
			{
				foreach (var s in rarityData.Stats.Entries)
				{
					float v = Mathf.RoundToInt(Random.Range(s.Min, s.Max));
					stats.Add(new StatValue { Name = s.Name, Value = v });
				}
			}

			item.Stats = stats.ToArray();
			item.Effects = GenerateEffects(template, rarityData);

			SaveItem(item);
			return item;
		}

		private static WeaponTemplate.RarityEntry FindRarity(WeaponTemplate t, string rarity)
		{
			return t.Rarities.FirstOrDefault(
				r => r.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase));
		}

		private static string PickRandomRarity(WeaponTemplate tpl)
		{
			int total = tpl.Rarities.Sum(r => r.DropChance);
			if (total <= 0)
				return tpl.Rarities[0].Rarity;

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

		private static List<EffectValue> GenerateEffects(
			WeaponTemplate tpl,
			WeaponTemplate.RarityEntry rarity)
		{
			if (rarity == null || rarity.MaxEffectCount <= 0)
				return null;

			if (tpl.AvailableEffects == null || tpl.AvailableEffects.Length == 0)
				return null;

			EnsureEffectsLoaded();

			var effects = new List<EffectValue>();

			int count = Random.Range(1, rarity.MaxEffectCount + 1);
			var used = new HashSet<int>();

			while (effects.Count < count && used.Count < tpl.AvailableEffects.Length)
			{
				int i = Random.Range(0, tpl.AvailableEffects.Length);
				if (!used.Add(i))
					continue;

				var effectRef = tpl.AvailableEffects[i];

				if (!_effectsCache.TryGetValue(effectRef.Name, out var effectTpl))
				{
					Debug.LogWarning($"Effect template not found: {effectRef.Name}");
					continue;
				}

				if (effectRef.Stats?.Entries == null || effectRef.Stats.Entries.Length == 0)
					continue;

				var effect = new EffectValue
				{
					Name = effectTpl.Name,
					Stats = new List<StatValue>()
				};

				foreach (var s in effectRef.Stats.Entries)
				{
					float value = Random.Range(s.Min, s.Max);
					if (Mathf.Abs(value) > 10f)
						value = Mathf.Round(value);
					else
						value = (float)Math.Round(value, 2);

					effect.Stats.Add(new StatValue
					{
						Name = s.Name,
						Value = value
					});
				}

				effects.Add(effect);
			}

			return effects.Count > 0 ? effects : null;
		}

		private static void SaveItem(GeneratedWeaponItem item)
		{
			if (!Directory.Exists(OutputPath))
				Directory.CreateDirectory(OutputPath);

			var json = JsonUtility.ToJson(item, true);
			var path = Path.Combine(OutputPath, item.ItemId + ".json");

			File.WriteAllText(path, json);
			Debug.Log("Saved item at " + path);
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
		public string Prefab;
		public EffectTemplateRef[] AvailableEffects;

		public RarityEntry[] Rarities;

		[Serializable]
		public sealed class RarityEntry
		{
			public string Rarity;
			public int DropChance;
			public int MaxEffectCount;
			public StatRangeList Stats;
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
		public string Prefab;

		public StatValue[] Stats;
		public List<EffectValue> Effects;
		string IGeneratedItem.ItemId => ItemId;
		string IGeneratedItem.TemplateId => TemplateId;
		string IGeneratedItem.Name => Name;
		string IGeneratedItem.Rarity => Rarity;
	}

	[Serializable]
	public sealed class EffectValue
	{
		public string Name;
		public List<StatValue> Stats = new List<StatValue>();
	}

	[Serializable]
	public sealed class EffectTemplate
	{
		public string Name;
		public string Info;
		public string Icon;
		public string Script;
	}

	[Serializable]
	public sealed class EffectTemplateRef
	{
		public string Name;
		public StatRangeList Stats;
	}

	[Serializable]
	public sealed class EffectTemplateCollection
	{
		public EffectTemplate[] Effects;
	}

	[Serializable]
	public sealed class StatRangeList
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

	[Serializable]
	public sealed class StatValue
	{
		public string Name;
		public float Value;
	}
}
