using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	[System.Serializable]
	public class WeaponController
	{
		public List<WeaponSlot> Weapons = new List<WeaponSlot>();
		//TODO must remove
		public List<GameObject> EnemyList = new List<GameObject>();

		public void OnUpdate()
		{
			List<ITargetable> _targetables = new List<ITargetable>();
			foreach (var eGameObject in EnemyList)
			{
				_targetables.Add(eGameObject.GetComponent<ITargetable>());
			}
			foreach (var weapon in Weapons)
			{
				weapon.WeaponTargeting.SetEnemies(_targetables);
			}
		}
	}
}