using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ships
{
	[System.Serializable]
	public class WeaponController
	{
		public List<WeaponSlot> Weapons = new List<WeaponSlot>();
		//TODO must remove
		

		public void OnUpdate()
		{
			foreach (var weapon in Weapons)
			{
				weapon.WeaponTargeting.SetEnemies(Battle.Instance.EnemyList.Select(x => x.GetComponent<ITargetable>()).ToList());
			}
		}
	}
}