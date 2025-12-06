using System;
using System.Collections.Generic;

namespace Ships
{
	public class InventoryView
	{
		private MetaState _state;

		// в какой слот сейчас ставим предмет
		private string _currentSlotId;
		private bool _currentIsWeapon;

		public event Action<List<InventoryItem>> OnInventoryUpdated;
		public event Action<InventoryItem> OnItemSelected;

		public void Init(MetaState state)
		{
			_state = state;
			OnInventoryUpdated?.Invoke(_state.Inventory);
		}

		/// <summary>
		/// ShipFitView сообщает, что пользователь кликнул слот
		/// и теперь выбрать предмет нужно "в этот слот".
		/// </summary>
		public void BeginInstallToSlot(string slotId, bool isWeaponSlot)
		{
			_currentSlotId = slotId;
			_currentIsWeapon = isWeaponSlot;

			// тут можно сделать фильтрацию по типу/размеру, а пока просто обновим список
			OnInventoryUpdated?.Invoke(_state.Inventory);
		}

		public void SelectItem(InventoryItem item)
		{
			OnItemSelected?.Invoke(item);
		}

		/// <summary>
		/// Вызывается из InventoryVisual, когда игрок кликает по предмету.
		/// </summary>
		public void InstallFromInventory(InventoryItem item)
		{
			if (string.IsNullOrEmpty(_currentSlotId))
				return;

			var entry = _state.Inventory.Find(i => i.Id == item.Id);
			if (entry != null)
				entry.Count--;

			if (_currentIsWeapon)
				_state.Fit.SetWeapon(_currentSlotId, item.Id);
			else
				_state.Fit.SetModule(_currentSlotId, item.Id);

			OnInventoryUpdated?.Invoke(_state.Inventory);
		}
	}
}