using System;

namespace Ships
{
	public class ShipFitView
	{
		private MetaState _state;
		private InventoryView _inventory;

		// слот поменялся (для визуалов)
		public event Action<string, bool> OnSlotChanged; // slotId, isWeapon

		public void Init(MetaState state, InventoryView inventory)
		{
			_state = state;
			_inventory = inventory;
		}

		/// <summary>
		/// Вызывается визуалом слота при клике.
		/// </summary>
		public void OnSlotClicked(string slotId, bool isWeaponSlot)
		{
			// вместо старого InventoryView.ShowInstallMenu(...)
			// просто говорим InventoryView: "мы собираемся ставить в этот слот"
			_inventory.BeginInstallToSlot(slotId, isWeaponSlot);
		}

		/// <summary>
		/// Логика установки предмета в слот (если хочешь держать это во View).
		/// </summary>
		public void InstallItem(string slotId, string itemId, bool isWeapon)
		{
			if (isWeapon)
				_state.Fit.SetWeapon(slotId, itemId);
			else
				_state.Fit.SetModule(slotId, itemId);

			OnSlotChanged?.Invoke(slotId, isWeapon);
		}
	}
}