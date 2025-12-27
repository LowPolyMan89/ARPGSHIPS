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

		public void SelectItem(InventoryItem item)
		{
			GameEvent.ItemSelected(item);
		}
	}
}
