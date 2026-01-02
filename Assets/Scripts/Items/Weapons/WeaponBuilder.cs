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

			var prefabId = useMetaPrefab
				? (!string.IsNullOrEmpty(data.MetaPrefab) ? data.MetaPrefab : data.Prefab)
				: (!string.IsNullOrEmpty(data.BattlePrefab) ? data.BattlePrefab : data.Prefab);

			var go = ResourceLoader.InstantiatePrefab(data.Slot, prefabId, mountPoint, false);
			if (go == null)
			{
				Debug.LogError($"[WeaponBuilder] Failed to instantiate weapon prefab '{prefabId}' (Slot='{data.Slot}')");
				return null;
			}

			var weapon = go.GetComponent<WeaponBase>();
			weapon.WeaponTemplateId = data.TemplateId;

			// ---------- Stats ----------
			var stats = new Stats();
			foreach (var s in data.Stats)
			{
				var statType = Enum.Parse<StatType>(s.Name);
				stats.AddStat(new Stat(statType, s.Value));
			}

			weapon.Init(stats);
			weapon.FireArcDeg = data.FireArcDeg <= 0 ? 360f : data.FireArcDeg;
			weapon.Owner = owner;

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
		public string Icon;
		public string Prefab;
		public string BattlePrefab;
		public string MetaPrefab;

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
