using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using TagType = Ships.Tags;

namespace Ships
{
	public static class WeaponBuilder
	{
		public static WeaponBase BuildBattle(string weaponId, Transform mountPoint, ShipBase owner)
		{
			return BuildInternal(weaponId, mountPoint, owner, useMetaPrefab: false);
		}

		public static WeaponBase BuildMeta(string weaponId, Transform mountPoint, ShipBase owner)
		{
			return BuildInternal(weaponId, mountPoint, owner, useMetaPrefab: true);
		}

		private static WeaponBase BuildInternal(string weaponId, Transform mountPoint, ShipBase owner, bool useMetaPrefab)
		{
			var relativePath = Path.Combine(PathConstant.Inventory, weaponId + ".json");
			if (!ResourceLoader.TryLoadPersistentJson(relativePath, out WeaponLoadData data))
			{
				Debug.LogError($"[WeaponBuilder] Weapon data not found: {relativePath}");
				return null;
			}

			if (string.IsNullOrEmpty(data.TemplateId))
			{
				Debug.LogError($"[WeaponBuilder] TemplateId missing for weapon item '{weaponId}'");
				return null;
			}

			var templateFile = data.TemplateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? data.TemplateId
				: data.TemplateId + ".json";
			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateFile);
			if (!ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
			{
				Debug.LogError($"[WeaponBuilder] Weapon template not found: {templatePath}");
				return null;
			}

			var prefabId = useMetaPrefab
				? (!string.IsNullOrEmpty(template.MetaPrefab) ? template.MetaPrefab : template.Prefab)
				: (!string.IsNullOrEmpty(template.BattlePrefab) ? template.BattlePrefab : template.Prefab);
			var slot = !string.IsNullOrEmpty(template.Slot) ? template.Slot : data.Slot;

			var go = ResourceLoader.InstantiatePrefab(slot, prefabId, mountPoint, false);
			if (go == null)
			{
				Debug.LogError($"[WeaponBuilder] Failed to instantiate weapon prefab '{prefabId}' (Slot='{slot}')");
				return null;
			}

			var weapon = go.GetComponent<WeaponBase>();
			weapon.WeaponTemplateId = data.TemplateId;

			// ---------- Stats ----------
			var stats = new Stats();
			foreach (var s in data.Stats)
			{
				if (!Enum.TryParse(s.Name, true, out StatType statType))
					continue;
				stats.AddStat(new Stat(statType, s.Value));
			}

			weapon.Init(stats);
			weapon.Model.BaseStats = stats.Clone();
			weapon.Model.Size = data.Size;
			weapon.Model.Tags = data.TagValues;
			weapon.FireArcDeg = data.FireArcDeg <= 0 ? 360f : data.FireArcDeg;
			weapon.Owner = owner;
			weapon.Model.IsAutoFire = ResolveIsAutoFire(data, template);
			if (TryResolveDamageType(data, template, out var damageTag))
			{
				weapon.Model.HasDamageType = true;
				weapon.Model.DamageType = damageTag;
			}

			// ---------- Effects ----------
			if (data.Effects != null)
			{
				foreach (var eff in data.Effects)
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
			if (data?.TagValues != null && data.TagValues.Length > 0)
				return Array.IndexOf(data.TagValues, Tags.Automatic) >= 0;

			if (template?.Tags == null || template.Tags.Length == 0)
				return false;

			var tagValues = EnumParsingHelpers.ParseTags(template.Tags);
			return Array.IndexOf(tagValues, Tags.Automatic) >= 0;
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
