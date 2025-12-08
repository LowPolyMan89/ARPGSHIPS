using System;
using System.Collections.Generic;

namespace Tanks
{
	[Serializable]
	public class MetaState
	{
		public string SelectedTankId;
		public TankFitModel Fit = new TankFitModel();
		public List<TankFitModel> PlayerShipFits = new ();
		public PlayerInventoryModel InventoryModel = new PlayerInventoryModel();
	}

	[System.Serializable]
	public sealed class PlayerInventoryModel
	{
		public List<InventoryItem> InventoryUniqueItems = new ();
		public List<InventoryItem> InventoryStackItems = new ();
	}
}