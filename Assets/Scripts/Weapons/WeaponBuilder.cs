using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

namespace Tanks
{
	public static class WeaponBuilder
	{
		public static WeaponBase Build(string weaponId, WeaponSlot slot)
		{
			var data = JsonUtility.FromJson<WeaponLoadData>(File.ReadAllText(ItemGenerator.OutputPath + "/" + weaponId + ".json"));
			var weaponBase = GameObject.Instantiate(Resources.Load($"Weapons/{data.Prefab}") as GameObject, slot.MountPoint, false);
			var stats = new Stats();
			foreach (var statData in data.Stats)
			{
				stats.AddStat(new Stat(Enum.Parse<StatType>(statData.Name), statData.Value));
			}
			weaponBase.GetComponent<WeaponBase>().Init(slot, stats);
			return null;
		}
	}

	[System.Serializable]
	public class WeaponLoadData
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
		public List<StatData> Stats = new List<StatData>();
		public List<string> Effects = new List<string>();
	}

	[System.Serializable]
	public class StatData
	{
		public string Name;
		public float Value;
	}
	
}