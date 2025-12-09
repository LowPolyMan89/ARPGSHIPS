using System;
using System.Collections.Generic;

namespace Tanks
{
	[Serializable]
	public class TankFitModel
	{
		public string TankId;
		public List<SelectedTankWeapon> SelectedShipWeapons = new ();
		public List<SelectedHullModule> SelectedHullModules = new ();
		public List<SelectedWeaponModule> SelectedWeaponModules = new ();
		public List<SelectedActiveModule> SelectedActiveModules = new ();
		
		[System.Serializable]
		public class SelectedTankWeapon
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