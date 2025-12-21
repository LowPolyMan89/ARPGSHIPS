using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ships
{
	public static class ItemGenerator
	{
		public static string WeaponConfigsPath =>
			ResourceLoader.GetStreamingPath(PathConstant.WeaponsConfigs);

		public static string EffectsConfigsPath =>
			ResourceLoader.GetStreamingPath(PathConstant.EffectsConfigs);

		public static string ModulesConfigsPath =>
			ResourceLoader.GetStreamingPath(PathConstant.ModulesConfigs);

		public static string OutputPath =>
			ResourceLoader.GetPersistentPath(PathConstant.Inventory);

		private static Dictionary<string, EffectTemplate> _effectsCache;

		private static void EnsureEffectsLoaded()
		{
			if (_effectsCache != null)
				return;

			_effectsCache = new Dictionary<string, EffectTemplate>(StringComparer.OrdinalIgnoreCase);

			foreach (var file in LoadEffectsFiles())
			{
				if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.EffectsConfigs, file), out EffectTemplateCollection collection))
					continue;

				foreach (var e in collection.Effects)
					_effectsCache[e.Name] = e;
			}
		}

		public static List<string> LoadWeaponFiles()
		{
			return ResourceLoader.GetStreamingFiles(PathConstant.WeaponsConfigs, "*.json")
				.ToList();
		}

		public static List<string> LoadEffectsFiles()
		{
			return ResourceLoader.GetStreamingFiles(PathConstant.EffectsConfigs, "*.json")
				.ToList();
		}

		public static List<string> LoadModuleFiles()
		{
			return ResourceLoader.GetStreamingFiles(PathConstant.ModulesConfigs, "*.json")
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

			var pool = new List<(string template, string rarity, int weight)>();

			foreach (var file in files)
			{
				if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.WeaponsConfigs, file), out WeaponTemplate template))
					continue;

				foreach (var r in template.Rarities)
					pool.Add((template.Id, r.Rarity, r.DropChance));
			}

			var pick = PickGlobal(pool);
			return GenerateWeapon(pick.template + ".json", pick.rarity);
		}

		private static (string template, string rarity) PickGlobal(
			List<(string template, string rarity, int weight)> entries)
		{
			var total = entries.Sum(e => e.weight);
			if (total <= 0)
				return (entries[0].template, entries[0].rarity);

			var roll = Random.Range(0, total);
			var accum = 0;

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
			if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.WeaponsConfigs, templateFile), out WeaponTemplate template))
			{
				Debug.LogError($"[ItemGenerator] Weapon template not found or invalid: {templateFile}");
				return null;
			}

			var rarity = forcedRarity == "Random"
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
				Icon = template.Icon,
				GridWidth = template.GridWidth <= 0 ? 1 : template.GridWidth,
				GridHeight = template.GridHeight <= 0 ? 1 : template.GridHeight,
				AllowedGridTypes = template.AllowedGridTypes,
				FireArcDeg = template.FireArcDeg <= 0 ? 360f : template.FireArcDeg
			};

			var stats = new List<StatValue>();
			if (rarityData?.Stats?.Entries != null)
			{
				foreach (var s in rarityData.Stats.Entries)
				{
					var v = Mathf.RoundToInt(Random.Range(s.Min, s.Max));
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
			var total = tpl.Rarities.Sum(r => r.DropChance);
			if (total <= 0)
				return tpl.Rarities[0].Rarity;

			var roll = Random.Range(0, total);
			var accum = 0;

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

			var count = Random.Range(1, rarity.MaxEffectCount + 1);
			var used = new HashSet<int>();

			while (effects.Count < count && used.Count < tpl.AvailableEffects.Length)
			{
				var i = Random.Range(0, tpl.AvailableEffects.Length);
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
					var value = Random.Range(s.Min, s.Max);
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
			var relativePath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
			ResourceLoader.SavePersistentJson(relativePath, item, true);
			Debug.Log("Saved item at " + ResourceLoader.GetPersistentPath(relativePath));
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
		public int GridWidth = 1;
		public int GridHeight = 1;
		public ShipGridType[] AllowedGridTypes;
		public float FireArcDeg = 360f;
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

		public int GridWidth = 1;
		public int GridHeight = 1;
		public ShipGridType[] AllowedGridTypes;
		public float FireArcDeg = 360f;

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
