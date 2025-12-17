using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

namespace Ships
{
	public static class WeaponBuilder
	{
		public static WeaponBase Build(string weaponId, WeaponSlot slot)
		{
			var path = Path.Combine(ItemGenerator.OutputPath, weaponId + ".json");
			var json = File.ReadAllText(path);

			var data = JsonUtility.FromJson<WeaponLoadData>(json);

			var prefab = Resources.Load<GameObject>($"Weapons/{data.Prefab}");
			var go = GameObject.Instantiate(prefab, slot.MountPoint, false);

			var weapon = go.GetComponent<WeaponBase>();

			// ---------- Stats ----------
			var stats = new Stats();
			foreach (var s in data.Stats)
			{
				var statType = Enum.Parse<StatType>(s.Name);
				stats.AddStat(new Stat(statType, s.Value));
			}

			weapon.Init(slot, stats);

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

		public List<StatData> Stats = new();
		public List<EffectValue> Effects = new();
	}

	[System.Serializable]
	public class StatData
	{
		public string Name;
		public float Value;
	}
	
}
