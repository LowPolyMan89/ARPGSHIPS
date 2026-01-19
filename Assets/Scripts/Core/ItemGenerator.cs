using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using TagType = Ships.Tags;

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

		public static ModuleLoadData GenerateModuleFromLoot(string lootTableId = null)
		{
			return GenerateModuleFromAllTemplates();
		}

		public static ModuleLoadData GenerateModule(string templateFile, string forcedRarity)
		{
			if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.ModulesConfigs, templateFile), out ModuleTemplate template))
			{
				Debug.LogError($"[ItemGenerator] Module template not found or invalid: {templateFile}");
				return null;
			}

			var rarity = string.Equals(forcedRarity, "Random", StringComparison.OrdinalIgnoreCase)
				? PickRandomRarity(template)
				: forcedRarity;

			var rarityData = FindRarity(template, rarity);
			if (rarityData == null)
			{
				rarityData = template.Rarities != null && template.Rarities.Length > 0
					? template.Rarities[0]
					: null;
				rarity = rarityData?.Rarity ?? "Common";
			}

			var item = new ModuleLoadData
			{
				ItemId = template.Id,
				TemplateId = template.Id,
				Name = template.Name,
				Rarity = string.IsNullOrEmpty(rarity) ? "Common" : rarity,
				Slot = string.IsNullOrEmpty(template.Slot) ? "Module" : template.Slot,
				Size = template.Size,
				GridWidth = template.GridWidth <= 0 ? 1 : template.GridWidth,
				GridHeight = template.GridHeight <= 0 ? 1 : template.GridHeight,
				AllowedGridTypes = EnumParsingHelpers.NormalizeStrings(template.AllowedGridTypes),
				AllowedGridTypeValues = EnumParsingHelpers.ParseGridTypes(template.AllowedGridTypes),
				EnergyCost = template.EnergyCost,
				ShipStatEffects = BuildModuleShipEffects(template, rarityData),
				WeaponStatEffects = BuildModuleWeaponEffects(template, rarityData),
				ActiveEffects = BuildModuleActiveEffects(template, rarityData)
			};

			return item;
		}

		private static ModuleLoadData GenerateModuleFromAllTemplates()
		{
			var files = LoadModuleFiles();
			if (files.Count == 0)
				return null;

			var pool = new List<(string template, string rarity, int weight)>();
			foreach (var file in files)
			{
				if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.ModulesConfigs, file), out ModuleTemplate template))
					continue;

				if (template.Rarities == null || template.Rarities.Length == 0)
				{
					pool.Add((template.Id, "Common", 1));
					continue;
				}

				foreach (var r in template.Rarities)
					pool.Add((template.Id, r.Rarity, r.DropChance));
			}

			var pick = PickGlobal(pool);
			return GenerateModule(pick.template + ".json", pick.rarity);
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
				ItemId = template.Id,
				TemplateId = template.Id,
				Name = template.Name,
				Rarity = rarity,
				Slot = template.Slot,
				DamageType = template.DamageType,
				Tags = EnumParsingHelpers.NormalizeStrings(template.Tags),
				TagValues = EnumParsingHelpers.ParseTags(template.Tags),
				Size = template.Size,
				GridWidth = template.GridWidth <= 0 ? 1 : template.GridWidth,
				GridHeight = template.GridHeight <= 0 ? 1 : template.GridHeight,
				AllowedGridTypes = EnumParsingHelpers.NormalizeStrings(template.AllowedGridTypes),
				AllowedGridTypeValues = EnumParsingHelpers.ParseGridTypes(template.AllowedGridTypes),
				FireArcDeg = template.FireArcDeg <= 0 ? 360f : template.FireArcDeg,
				EnergyCost = template.EnergyCost
			};

			return item;
		}

		private static WeaponTemplate.RarityEntry FindRarity(WeaponTemplate t, string rarity)
		{
			return t.Rarities.FirstOrDefault(
				r => r.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase));
		}

		private static ModuleRarityEntry FindRarity(ModuleTemplate t, string rarity)
		{
			if (t?.Rarities == null || t.Rarities.Length == 0 || string.IsNullOrEmpty(rarity))
				return null;

			for (var i = 0; i < t.Rarities.Length; i++)
			{
				var r = t.Rarities[i];
				if (r != null && r.Rarity != null &&
				    r.Rarity.Equals(rarity, StringComparison.OrdinalIgnoreCase))
					return r;
			}

			return null;
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

		private static string PickRandomRarity(ModuleTemplate tpl)
		{
			if (tpl?.Rarities == null || tpl.Rarities.Length == 0)
				return "Common";

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

		private static List<StatEffectModel> BuildModuleShipEffects(ModuleTemplate template, ModuleRarityEntry rarity)
		{
			if (rarity?.ShipStatEffects != null && rarity.ShipStatEffects.Length > 0)
				return CloneStatEffects(rarity.ShipStatEffects);

			return CloneStatEffects(template?.ShipStatEffects);
		}

		private static List<WeaponStatEffectModel> BuildModuleWeaponEffects(ModuleTemplate template, ModuleRarityEntry rarity)
		{
			if (rarity?.WeaponStatEffects != null && rarity.WeaponStatEffects.Length > 0)
				return CloneWeaponStatEffects(rarity.WeaponStatEffects);

			return CloneWeaponStatEffects(template?.WeaponStatEffects);
		}

		private static List<EffectModel> BuildModuleActiveEffects(ModuleTemplate template, ModuleRarityEntry rarity)
		{
			if (rarity?.ActiveEffects != null && rarity.ActiveEffects.Length > 0)
				return CloneActiveEffects(rarity.ActiveEffects);

			return CloneActiveEffects(template?.ActiveEffects);
		}

		private static StatValue[] CloneStatValues(StatValue[] source)
		{
			if (source == null || source.Length == 0)
				return Array.Empty<StatValue>();

			var result = new StatValue[source.Length];
			for (var i = 0; i < source.Length; i++)
			{
				var s = source[i];
				if (s == null)
					continue;

				result[i] = new StatValue
				{
					Name = s.Name,
					Value = s.Value
				};
			}

			return result;
		}

		private static List<EffectValue> CloneEffects(EffectValue[] source)
		{
			if (source == null || source.Length == 0)
				return null;

			var list = new List<EffectValue>(source.Length);
			for (var i = 0; i < source.Length; i++)
			{
				var effect = source[i];
				if (effect == null)
					continue;

				var copy = new EffectValue
				{
					Name = effect.Name,
					Stats = CloneStatValueList(effect.Stats)
				};
				list.Add(copy);
			}

			return list.Count > 0 ? list : null;
		}

		private static List<StatValue> CloneStatValueList(List<StatValue> source)
		{
			if (source == null || source.Count == 0)
				return new List<StatValue>();

			var list = new List<StatValue>(source.Count);
			for (var i = 0; i < source.Count; i++)
			{
				var s = source[i];
				if (s == null)
					continue;

				list.Add(new StatValue
				{
					Name = s.Name,
					Value = s.Value
				});
			}

			return list;
		}

		private static List<StatEffectModel> CloneStatEffects(IReadOnlyList<StatEffectModel> source)
		{
			if (source == null || source.Count == 0)
				return new List<StatEffectModel>();

			var list = new List<StatEffectModel>(source.Count);
			for (var i = 0; i < source.Count; i++)
			{
				var e = source[i];
				if (e == null)
					continue;

				list.Add(new StatEffectModel
				{
					Stat = e.Stat,
					Operation = e.Operation,
					Target = e.Target,
					Value = e.Value
				});
			}

			return list;
		}

		private static List<WeaponStatEffectModel> CloneWeaponStatEffects(IReadOnlyList<WeaponStatEffectModel> source)
		{
			if (source == null || source.Count == 0)
				return new List<WeaponStatEffectModel>();

			var list = new List<WeaponStatEffectModel>(source.Count);
			for (var i = 0; i < source.Count; i++)
			{
				var e = source[i];
				if (e == null || string.IsNullOrEmpty(e.Stat))
					continue;

				if (e.Filter != null && e.Filter.Tags != null && e.Filter.Tags.Length > 0 &&
				    (e.Filter.TagValues == null || e.Filter.TagValues.Length == 0))
					e.Filter.OnAfterDeserialize();

				list.Add(new WeaponStatEffectModel
				{
					Stat = e.Stat,
					Operation = e.Operation,
					Value = e.Value,
					Filter = e.Filter
				});
			}

			return list;
		}

		private static List<EffectModel> CloneActiveEffects(IReadOnlyList<EffectModel> source)
		{
			if (source == null || source.Count == 0)
				return new List<EffectModel>();

			var list = new List<EffectModel>(source.Count);
			for (var i = 0; i < source.Count; i++)
			{
				var e = source[i];
				if (e == null)
					continue;

				list.Add(new EffectModel
				{
					id = e.id,
					value = e.value
				});
			}

			return list;
		}

		private static void SaveItem(GeneratedWeaponItem item)
		{
			var relativePath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
			var saveData = new WeaponSaveData
			{
				ItemId = item.ItemId,
				TemplateId = item.TemplateId,
				Rarity = item.Rarity
			};
			ResourceLoader.SavePersistentJson(relativePath, saveData, true);
			Debug.Log("Saved item at " + ResourceLoader.GetPersistentPath(relativePath));
		}

		private static void SaveItem(ModuleLoadData item)
		{
			var relativePath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
			ResourceLoader.SavePersistentJson(relativePath, item, true);
			Debug.Log("Saved item at " + ResourceLoader.GetPersistentPath(relativePath));
		}

		[Serializable]
		private sealed class WeaponSaveData
		{
			public string ItemId;
			public string TemplateId;
			public string Rarity;
		}
	}

	[Serializable]
	public sealed class WeaponTemplate
	{
		public string Id;
		public string Name;
		public string Icon;
		public string IconInventory;
		public string IconOnDrag;
		public string IconOnFit;
		public string Slot;
		public string DamageType;
		public string[] Tags;
		public string Size;
		public int GridWidth = 1;
		public int GridHeight = 1;
		public string[] AllowedGridTypes;
		public float FireArcDeg = 360f;
		public float EnergyCost = 0f;
		public string Prefab;
		public string BattlePrefab;
		public string MetaPrefab;

		public RarityEntry[] Rarities;

		[Serializable]
		public sealed class RarityEntry
		{
			public string Rarity;
			public int DropChance;
			public StatValue[] Stats;
			public EffectValue[] Effects;
		}
	}

	[Serializable]
	public class GeneratedWeaponItem : IGeneratedItem, ISerializationCallbackReceiver
	{
		public string ItemId;
		public string TemplateId;
		public string Name;
		public string Rarity;

		public string Slot;
		public string DamageType;
		[SerializeField] public string[] Tags;
		[NonSerialized] public TagType[] TagValues;
		[SerializeField] public string[] AllowedGridTypes;
		[NonSerialized] public ShipGridType[] AllowedGridTypeValues;
		public string Size;

		public int GridWidth = 1;
		public int GridHeight = 1;
		public float FireArcDeg = 360f;
		public float EnergyCost = 0f;

		public StatValue[] Stats;
		public List<EffectValue> Effects;
		string IGeneratedItem.ItemId => ItemId;
		string IGeneratedItem.TemplateId => TemplateId;
		string IGeneratedItem.Name => Name;
		string IGeneratedItem.Rarity => Rarity;

		public void OnBeforeSerialize()
		{
			Tags = EnumParsingHelpers.NormalizeStrings(Tags);
			AllowedGridTypes = EnumParsingHelpers.NormalizeStrings(AllowedGridTypes);
		}

		public void OnAfterDeserialize()
		{
			if (Tags == null || Tags.Length == 0)
				TagValues = Array.Empty<TagType>();
			else
				TagValues = EnumParsingHelpers.ParseTags(Tags);

			if (AllowedGridTypes == null || AllowedGridTypes.Length == 0)
				AllowedGridTypeValues = Array.Empty<ShipGridType>();
			else
				AllowedGridTypeValues = EnumParsingHelpers.ParseGridTypes(AllowedGridTypes);
		}
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
	public sealed class EffectTemplateCollection
	{
		public EffectTemplate[] Effects;
	}

	[Serializable]
	public sealed class StatValue
	{
		public string Name;
		public float Value;
	}

	internal static class EnumParsingHelpers
	{
		public static string[] NormalizeStrings(string[] source)
		{
			return source == null
				? Array.Empty<string>()
				: source
					.Where(s => !string.IsNullOrEmpty(s))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToArray();
		}

		public static TagType[] ParseTags(string[] source)
		{
			if (source == null || source.Length == 0)
				return Array.Empty<TagType>();

			var parsed = new List<TagType>(source.Length);
			for (var i = 0; i < source.Length; i++)
			{
				var tagName = source[i];
				if (string.IsNullOrEmpty(tagName))
					continue;

				if (Enum.TryParse(tagName, true, out TagType tag))
				{
					parsed.Add(tag);
					continue;
				}

				if (int.TryParse(tagName, out var numeric) &&
				    Enum.IsDefined(typeof(TagType), numeric))
				{
					parsed.Add((TagType)numeric);
				}
			}

			return parsed.Distinct().ToArray();
		}

		public static ShipGridType[] ParseGridTypes(string[] source)
		{
			if (source == null || source.Length == 0)
				return Array.Empty<ShipGridType>();

			var parsed = new List<ShipGridType>(source.Length);
			for (var i = 0; i < source.Length; i++)
			{
				var typeName = source[i];
				if (string.IsNullOrEmpty(typeName))
					continue;

				if (Enum.TryParse(typeName, true, out ShipGridType gridType))
				{
					parsed.Add(gridType);
					continue;
				}

				if (int.TryParse(typeName, out var numeric) &&
				    Enum.IsDefined(typeof(ShipGridType), numeric))
				{
					parsed.Add((ShipGridType)numeric);
				}
			}

			return parsed.Distinct().ToArray();
		}
	}
}
