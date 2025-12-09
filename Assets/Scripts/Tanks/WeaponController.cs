using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tanks
{
	[System.Serializable]
	public class WeaponController
	{
		public List<WeaponSlot> Weapons = new List<WeaponSlot>();
		
		public void Init(SideType sideType)
		{
			foreach (var weapon in Weapons)
			{
				weapon.Init(sideType);
			}
		}
		public void OnUpdate()
		{
			var tanks = Battle.Instance.AllTanks;

			foreach (var slot in Weapons)
			{
				if (slot?.WeaponTargeting == null)
					continue;

				slot.WeaponTargeting.UpdateTargetList(tanks);
			}
		}
	}
}