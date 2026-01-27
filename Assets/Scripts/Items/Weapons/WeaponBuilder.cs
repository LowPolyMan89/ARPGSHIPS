using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TagType = Ships.Tags;

namespace Ships
{
	public static class WeaponBuilder
	{
		public static WeaponBase BuildBattle(string weaponId, Transform mountPoint, ShipBase owner, InventoryItem item = null)
		{
			return BuildInternal(weaponId, mountPoint, owner, useMetaPrefab: false, item: item);
		}

		public static WeaponBase BuildMeta(string weaponId, Transform mountPoint, ShipBase owner, InventoryItem item = null)
		{
			return BuildInternal(weaponId, mountPoint, owner, useMetaPrefab: true, item: item);
		}

		private static WeaponBase BuildInternal(string weaponId, Transform mountPoint, ShipBase owner, bool useMetaPrefab, InventoryItem item)
		{
			WeaponLoadData data = null;
			if (item == null)
			{
				var relativePath = Path.Combine(PathConstant.Inventory, weaponId + ".json");
				ResourceLoader.TryLoadPersistentJson(relativePath, out data);
			}

			var templateId = !string.IsNullOrEmpty(item?.ResolvedId)
				? item.ResolvedId
				: (!string.IsNullOrEmpty(data?.TemplateId) ? data.TemplateId : weaponId);
			if (string.IsNullOrEmpty(templateId))
			{
				Debug.LogError($"[WeaponBuilder] TemplateId missing for weapon item '{weaponId}'");
				return null;
			}

			var templateFile = templateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? templateId
				: templateId + ".json";
			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateFile);
			if (!ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
			{
				Debug.LogError($"[WeaponBuilder] Weapon template not found: {templatePath}");
				return null;
			}

			var prefabId = useMetaPrefab
				? (!string.IsNullOrEmpty(template.MetaPrefab) ? template.MetaPrefab : template.Prefab)
				: (!string.IsNullOrEmpty(template.BattlePrefab) ? template.BattlePrefab : template.Prefab);
			var slot = !string.IsNullOrEmpty(template.Slot) ? template.Slot : data?.Slot;

			var go = ResourceLoader.InstantiatePrefab(slot, prefabId, mountPoint, false);
			if (go == null)
			{
				Debug.LogError($"[WeaponBuilder] Failed to instantiate weapon prefab '{prefabId}' (Slot='{slot}')");
				return null;
			}

			var weapon = go.GetComponent<WeaponBase>();
			weapon.WeaponTemplateId = !string.IsNullOrEmpty(template.Id) ? template.Id : templateId;

			var rarityId = !string.IsNullOrEmpty(item?.Rarity) ? item.Rarity : data?.Rarity;
			var rarity = FindRarity(template, rarityId);
			var stats = BuildStatsFromRarity(rarity);
			if (stats == null)
			{
				Debug.LogError($"[WeaponBuilder] Missing stats for weapon '{template.Id}' (rarity '{rarityId ?? "Common"}')");
				return null;
			}

			weapon.Init(stats);
			weapon.Model.BaseStats = stats.Clone();
			weapon.Model.Size = template.Size;
			weapon.Model.Tags = EnumParsingHelpers.ParseTags(template.Tags);
			weapon.FireArcDeg = template.FireArcDeg <= 0 ? 360f : template.FireArcDeg;
			weapon.Owner = owner;
			weapon.Model.IsAutoFire = true;
			if (TryResolveDamageType(data, template, out var damageTag))
			{
				weapon.Model.HasDamageType = true;
				weapon.Model.DamageType = damageTag;
			}

			if (rarity?.Effects != null)
			{
				foreach (var eff in rarity.Effects)
				{
					var runtime = EffectFactory.Create(eff);
					if (runtime != null)
						weapon.Model.AddEffect(runtime);
				}
			}

			return weapon;
		}

		private static bool ResolveIsAutoFire(WeaponLoadData data, WeaponTemplate template)
		{
			return true;
		}

		private static bool TryResolveDamageType(WeaponLoadData data, WeaponTemplate template, out TagType tag)
		{
			if (TryParseDamageType(data?.DamageType, out tag))
				return true;

			if (TryParseDamageType(template?.DamageType, out tag))
				return true;

			if (data?.TagValues != null)
			{
				for (var i = 0; i < data.TagValues.Length; i++)
				{
					if (IsDamageTag(data.TagValues[i]))
					{
						tag = data.TagValues[i];
						return true;
					}
				}
			}

			if (template?.Tags != null && template.Tags.Length > 0)
			{
				var tagValues = EnumParsingHelpers.ParseTags(template.Tags);
				for (var i = 0; i < tagValues.Length; i++)
				{
					if (IsDamageTag(tagValues[i]))
					{
						tag = tagValues[i];
						return true;
					}
				}
			}

			tag = default;
			return false;
		}

		private static bool TryParseDamageType(string value, out TagType tag)
		{
			if (string.IsNullOrEmpty(value))
			{
				tag = default;
				return false;
			}

			if (!Enum.TryParse(value, true, out tag))
				return false;

			return IsDamageTag(tag);
		}

		private static bool IsDamageTag(TagType tag)
		{
			return tag == Tags.Kinetic || tag == Tags.Thermal || tag == Tags.Energy;
		}

		private static WeaponTemplate.RarityEntry FindRarity(WeaponTemplate template, string rarityId)
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

		private static Stats BuildStatsFromRarity(WeaponTemplate.RarityEntry rarity)
		{
			if (rarity?.Stats == null || rarity.Stats.Length == 0)
				return null;

			var stats = new Stats();
			for (var i = 0; i < rarity.Stats.Length; i++)
			{
				var entry = rarity.Stats[i];
				if (entry == null || string.IsNullOrEmpty(entry.Name))
					continue;

				if (!Enum.TryParse(entry.Name, true, out StatType statType))
					continue;

				stats.AddStat(new Stat(statType, entry.Value));
			}

			return stats;
		}
	}

	[Serializable]
	public class WeaponLoadData : ISerializationCallbackReceiver
	{
		public string ItemId;
		public string TemplateId;
		public string Name;
		public string Rarity;
		public string Slot;
		public string DamageType;
		[SerializeField] public string[] Tags;
		[NonSerialized] public TagType[] TagValues;
		public string Size;

		public float EnergyCost = 0f;

		public int GridWidth = 1;
		public int GridHeight = 1;
		[SerializeField] public string[] AllowedGridTypes;
		[NonSerialized] public ShipGridType[] AllowedGridTypeValues;
		public float FireArcDeg = 360f;

		public List<StatData> Stats = new();
		public List<EffectValue> Effects = new();

		public void OnBeforeSerialize()
		{
			Tags = EnumParsingHelpers.NormalizeStrings(Tags);
			AllowedGridTypes = EnumParsingHelpers.NormalizeStrings(AllowedGridTypes);
		}

		public void OnAfterDeserialize()
		{
			TagValues = EnumParsingHelpers.ParseTags(Tags);
			AllowedGridTypeValues = EnumParsingHelpers.ParseGridTypes(AllowedGridTypes);
		}
	}

	[System.Serializable]
	public class StatData
	{
		public string Name;
		public float Value;
	}
}
