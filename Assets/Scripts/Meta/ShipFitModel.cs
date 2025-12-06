using System;
using System.Collections.Generic;

namespace Ships
{
	[Serializable]
	public class ShipFitModel
	{
		public Dictionary<string, string> WeaponSlots = new();
		public Dictionary<string, string> ModuleSlots = new();

		public void SetWeapon(string slotId, string itemId)
		{
			WeaponSlots[slotId] = itemId;
		}

		public void SetModule(string slotId, string itemId)
		{
			ModuleSlots[slotId] = itemId;
		}
	}

}