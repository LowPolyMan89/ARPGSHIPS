using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ships
{
	public static class ModuleBuilder
	{
		public static GameObject BuildBattle(string moduleItemId, Transform mountPoint, ShipBase owner)
		{
			return BuildInternal(moduleItemId, mountPoint, useMetaPrefab: false);
		}

		public static GameObject BuildMeta(string moduleItemId, Transform mountPoint, ShipBase owner)
		{
			return BuildInternal(moduleItemId, mountPoint, useMetaPrefab: true);
		}

		public static bool TryLoadModuleData(string itemId, out ModuleLoadData data)
		{
			data = null;
			if (string.IsNullOrEmpty(itemId))
				return false;

			if (TryBuildModuleData(itemId, DefaultWeaponResolver.DefaultRarity, out data))
				return true;

			var relativePath = Path.Combine(PathConstant.Inventory, itemId + ".json");
			if (!ResourceLoader.TryLoadPersistentJson(relativePath, out ModuleLoadData legacy))
				return false;

			if (!IsModuleData(legacy))
				return false;

			data = legacy;
			return true;
		}

		public static bool TryLoadModuleData(InventoryItem item, out ModuleLoadData data)
		{
			data = null;
			if (item == null)
				return false;

			var templateId = InventoryUtils.ResolveItemId(item);
			if (string.IsNullOrEmpty(templateId))
				return false;

			var rarity = string.IsNullOrEmpty(item.Rarity) ? DefaultWeaponResolver.DefaultRarity : item.Rarity;
			return TryBuildModuleData(templateId, rarity, out data);
		}

		public static bool TryLoadModuleTemplate(string templateId, out ModuleTemplate template)
		{
			template = null;
			if (string.IsNullOrEmpty(templateId))
				return false;

			var file = templateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? templateId
				: templateId + ".json";
			var path = Path.Combine(PathConstant.ModulesConfigs, file);
			return ResourceLoader.TryLoadStreamingJson(path, out template);
		}

		private static bool IsModuleData(ModuleLoadData data)
		{
			if (data == null)
				return false;

			if (string.Equals(data.Slot, "Module", StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		private static GameObject BuildInternal(string moduleItemId, Transform mountPoint, bool useMetaPrefab)
		{
			ModuleLoadData data = null;
			if (!TryLoadModuleTemplate(moduleItemId, out var template))
			{
				if (TryLoadModuleData(moduleItemId, out data) && !string.IsNullOrEmpty(data.TemplateId))
					TryLoadModuleTemplate(data.TemplateId, out template);

				if (template == null)
				{
					Debug.LogWarning($"[ModuleBuilder] Module template not found for '{moduleItemId}'");
					return null;
				}
			}

			var prefabId = useMetaPrefab
				? (!string.IsNullOrEmpty(template.MetaPrefab) ? template.MetaPrefab : template.Prefab)
				: (!string.IsNullOrEmpty(template.BattlePrefab) ? template.BattlePrefab : template.Prefab);
			if (string.IsNullOrEmpty(prefabId))
				return null;

			var slot = !string.IsNullOrEmpty(template.Slot) ? template.Slot : data?.Slot;
			var go = ResourceLoader.InstantiatePrefab(slot, prefabId, mountPoint, false);
			if (go == null)
			{
				Debug.LogWarning($"[ModuleBuilder] Failed to instantiate module prefab '{prefabId}'");
				return null;
			}

			return go;
		}

		public static bool TryBuildModuleData(string templateId, string rarityId, out ModuleLoadData data)
		{
			data = null;
			if (string.IsNullOrEmpty(templateId))
				return false;

			if (!TryLoadModuleTemplate(templateId, out var template))
				return false;

			var rarity = FindRarity(template, rarityId);
			var resolvedRarity = rarity?.Rarity ?? rarityId ?? DefaultWeaponResolver.DefaultRarity;

			data = new ModuleLoadData
			{
				TemplateId = !string.IsNullOrEmpty(template.Id) ? template.Id : templateId,
				Name = template.Name,
				Rarity = resolvedRarity,
				Slot = string.IsNullOrEmpty(template.Slot) ? "Module" : template.Slot,
				Size = template.Size,
				GridWidth = template.GridWidth <= 0 ? 1 : template.GridWidth,
				GridHeight = template.GridHeight <= 0 ? 1 : template.GridHeight,
				AllowedGridTypes = EnumParsingHelpers.NormalizeStrings(template.AllowedGridTypes),
				EnergyCost = template.EnergyCost,
				ShipStatEffects = BuildModuleShipEffects(template, rarity),
				WeaponStatEffects = BuildModuleWeaponEffects(template, rarity),
				ActiveEffects = BuildModuleActiveEffects(template, rarity)
			};
			data.OnAfterDeserialize();
			return true;
		}

		private static ModuleRarityEntry FindRarity(ModuleTemplate template, string rarityId)
		{
			if (template?.Rarities == null || template.Rarities.Length == 0)
				return null;

			if (!string.IsNullOrEmpty(rarityId))
			{
				for (var i = 0; i < template.Rarities.Length; i++)
				{
					var entry = template.Rarities[i];
					if (entry != null && entry.Rarity != null &&
					    entry.Rarity.Equals(rarityId, StringComparison.OrdinalIgnoreCase))
						return entry;
				}
			}

			return template.Rarities[0];
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
	}
}
