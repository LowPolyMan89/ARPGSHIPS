using System;
using System.Collections.Generic;

namespace Ships
{
	[Serializable]
	public class MetaState
	{
		public string SelectedShipId;
		public ShipFitModel Fit = new ShipFitModel();
		public List<ShipFitModel> PlayerShipFits = new ();
		public PlayerInventoryModel InventoryModel = new PlayerInventoryModel();
	}

	[System.Serializable]
	public sealed class PlayerInventoryModel
	{
		public List<InventoryItem> InventoryUniqueItems = new ();
		public List<InventoryItem> InventoryStackItems = new ();
	}
}