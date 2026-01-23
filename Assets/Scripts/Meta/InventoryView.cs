namespace Ships
{
	public class InventoryView
	{
		private MetaState _state;

		public void Init(MetaState state)
		{
			_state = state;
		}

		public PlayerInventoryModel GetInventory() => _state.InventoryModel;
		public System.Collections.Generic.List<InventoryShip> GetShipInventory() => _state.ShipInventory;

		public void SelectItem(InventoryItem item)
		{
			GameEvent.ItemSelected(item);
		}
		public void SelectShip(InventoryShip item)
		{
			GameEvent.ShipSelected(item);
		}
	}
}
