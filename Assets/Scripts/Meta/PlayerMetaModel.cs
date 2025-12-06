using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Ships
{
	[System.Serializable]
	public class PlayerMetaModel
	{
		public PlayerShipModel CurrentShipModel;
		public List<PlayerShipModel> PlayerShipModels = new ();
		public PlayerInventoryModel InventoryModel;
	}

	[System.Serializable]
	public sealed class PlayerInventoryModel
	{
		public List<InventoryUniqueItem> InventoryUniqueItems = new ();
		public List<InventoryStackItem> InventoryStackItems = new ();
		
		[System.Serializable]
		public class InventoryUniqueItem
		{
			public string Id;
		}
		[System.Serializable]
		public class InventoryStackItem
		{
			public string Id;
			public int Value;
		}
	}
	//JSON DATA
	[System.Serializable]
	public sealed class PlayerShipModel
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