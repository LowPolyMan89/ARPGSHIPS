using System;
using System.Collections.Generic;

namespace Ships
{
	[Serializable]
	public class ShipFitModel
	{
		public string ShipId;
		public List<SelectedShipWeapon> SelectedShipWeapons = new ();
		public List<SelectedHullModule> SelectedHullModules = new ();
		public List<SelectedWeaponModule> SelectedWeaponModules = new ();
		public List<SelectedActiveModule> SelectedActiveModules = new ();
		
		[System.Serializable]
		public class SelectedShipWeapon
		{
			public string Id;
			public int SlotIndex;
		}
		[System.Serializable]
		public class SelectedHullModule
		{
			public string Id;
			public int SlotIndex;
		}
		[System.Serializable]
		public class SelectedWeaponModule
		{
			public string Id;
			public int SlotIndex;
		}
		[System.Serializable]
		public class SelectedActiveModule
		{
			public string Id;
			public int SlotIndex;
		}
		
	}

}

