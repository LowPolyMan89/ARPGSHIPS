using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public enum ModuleEffectTarget
	{
		Ship,
		Weapon,
		Both
	}

	[Serializable]
	public sealed class WeaponEffectFilter
	{
		public string DamageType;
		public string Size;
		[SerializeField] public string[] Tags;
		[NonSerialized] public Tags[] TagValues;

		public void OnAfterDeserialize()
		{
			TagValues = EnumParsingHelpers.ParseTags(Tags);
		}
	}

	[Serializable]
	public sealed class WeaponStatEffectModel : ISerializationCallbackReceiver
	{
		public string Stat;
		public StatEffectOperation Operation = StatEffectOperation.Add;
		public float Value;
		public WeaponEffectFilter Filter;

		public void OnBeforeSerialize()
		{
			if (Filter != null)
				Filter.Tags = EnumParsingHelpers.NormalizeStrings(Filter.Tags);
		}

		public void OnAfterDeserialize()
		{
			if (Filter != null)
				Filter.OnAfterDeserialize();
		}
	}

	[Serializable]
	public sealed class ModuleStatEffectRange
	{
		public string Stat;
		public StatEffectOperation Operation = StatEffectOperation.Add;
		public StatModifierTarget Target = StatModifierTarget.Maximum;
		public float Min;
		public float Max;
	}

	[Serializable]
	public sealed class ModuleWeaponStatEffectRange : ISerializationCallbackReceiver
	{
		public string Stat;
		public StatEffectOperation Operation = StatEffectOperation.Add;
		public float Min;
		public float Max;
		public WeaponEffectFilter Filter;

		public void OnBeforeSerialize()
		{
			if (Filter != null)
				Filter.Tags = EnumParsingHelpers.NormalizeStrings(Filter.Tags);
		}

		public void OnAfterDeserialize()
		{
			if (Filter != null)
				Filter.OnAfterDeserialize();
		}
	}

	[Serializable]
	public sealed class ModuleRarityEntry
	{
		public string Rarity;
		public int DropChance;
		public ModuleStatEffectRange[] ShipStatEffects;
		public ModuleWeaponStatEffectRange[] WeaponStatEffects;
	}

	[Serializable]
	public sealed class ModuleTemplate
	{
		public string Id;
		public string Name;
		public string Icon;
		public string IconInventory;
		public string IconOnDrag;
		public string IconOnFit;
		public string Slot = "Module";
		public string Size;
		public int GridWidth = 1;
		public int GridHeight = 1;
		public string[] AllowedGridTypes;
		public float EnergyCost = 0f;
		public string Prefab;
		public string BattlePrefab;
		public string MetaPrefab;

		public List<StatEffectModel> ShipStatEffects = new();
		public List<WeaponStatEffectModel> WeaponStatEffects = new();
		public List<EffectModel> ActiveEffects = new();
		public ModuleRarityEntry[] Rarities;
	}

	[Serializable]
	public sealed class ModuleLoadData : ISerializationCallbackReceiver
	{
		public string ItemId;
		public string TemplateId;
		public string Name;
		public string Rarity;
		public string Slot;
		public string Size;
		public int GridWidth = 1;
		public int GridHeight = 1;
		[SerializeField] public string[] AllowedGridTypes;
		[NonSerialized] public ShipGridType[] AllowedGridTypeValues;
		public float EnergyCost = 0f;

		public List<StatEffectModel> ShipStatEffects = new();
		public List<WeaponStatEffectModel> WeaponStatEffects = new();
		public List<EffectModel> ActiveEffects = new();

		public void OnBeforeSerialize()
		{
			AllowedGridTypes = EnumParsingHelpers.NormalizeStrings(AllowedGridTypes);
		}

		public void OnAfterDeserialize()
		{
			if (AllowedGridTypes == null || AllowedGridTypes.Length == 0)
				AllowedGridTypeValues = Array.Empty<ShipGridType>();
			else
				AllowedGridTypeValues = EnumParsingHelpers.ParseGridTypes(AllowedGridTypes);
		}
	}
}
