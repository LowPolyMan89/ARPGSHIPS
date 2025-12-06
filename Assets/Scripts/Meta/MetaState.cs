using System.Collections.Generic;

namespace Ships
{
	[System.Serializable]
	public class MetaState
	{
		public string SelectedShipId;
		public ShipFitModel Fit = new ShipFitModel();
		public List<InventoryItem> Inventory = new();
	}
}