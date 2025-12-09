using System;
using System.Collections.Generic;

namespace Tanks
{
	public class InventoryView
	{
		private MetaState _state;
		public PlayerInventoryModel GetInventory() => _state.InventoryModel;
		// в какой слот сейчас ставим предмет
		private string _currentSlotId;
		private bool _currentIsWeapon;

		public void Init(MetaState state)
		{
			_state = state;
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
			GameEvent.InventoryUpdated(_state.InventoryModel);
		}

		public void SelectItem(InventoryItem item)
		{
			GameEvent.ItemSelected(item);
		}

		/// <summary>
		/// Вызывается из InventoryVisual, когда игрок кликает по предмету.
		/// </summary>
		public void InstallFromInventory(InventoryItem item)
		{
			if (string.IsNullOrEmpty(_currentSlotId))
				return;

			int slotIndex = int.Parse(_currentSlotId);

			MetaController.Instance.TankFitView.EquipItemToSlot(
				_state.Fit.TankId,
				slotIndex,
				item.ItemId,
				_currentIsWeapon
			);

			GameEvent.InventoryUpdated(_state.InventoryModel);
		}

		public void Open()
		{
		}
	}
}