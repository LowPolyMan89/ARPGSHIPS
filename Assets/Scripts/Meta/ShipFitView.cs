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
		public void UnequipItem(InventoryItem item)
		{
			if (item == null || !item.IsEquipped)
				return;

			var fit = _state.Fit;

			// убрать из фита
			fit.SelectedShipWeapons.RemoveAll(x => x.Id == item.ItemId);
			fit.SelectedHullModules.RemoveAll(x => x.Id == item.ItemId);
			fit.SelectedWeaponModules.RemoveAll(x => x.Id == item.ItemId);
			fit.SelectedActiveModules.RemoveAll(x => x.Id == item.ItemId);

			// снять флаг
			item.EquippedOnFitId = null;
			item.EquippedSlotIndex = -1;
		}
		public void EquipItemToSlot(string fitId, int slotIndex, string itemId, bool isWeapon)
		{
			var inv = _state.InventoryModel;

			// 1. получаем предмет
			var item = InventoryUtils.FindByItemId(inv, itemId);

			// 2. если предмет на другом фите → снимаем
			if (item.IsEquipped && item.EquippedOnFitId != fitId)
			{
				UnequipItem(item);
			}

			// 3. если в слоте уже что-то есть → снять
			if (isWeapon)
			{
				var existing = _state.Fit.SelectedShipWeapons.Find(x => x.SlotIndex == slotIndex);
				if (existing != null)
					UnequipItem(InventoryUtils.FindByItemId(inv, existing.Id));

				_state.Fit.SelectedShipWeapons.RemoveAll(x => x.SlotIndex == slotIndex);
				_state.Fit.SelectedShipWeapons.Add(new ShipFitModel.SelectedShipWeapon
				{
					Id = item.ItemId,
					SlotIndex = slotIndex
				});
			}

			// 4. помечаем предмет
			item.EquippedOnFitId = fitId;
			item.EquippedSlotIndex = slotIndex;

			// 5. UI
			OnSlotChanged?.Invoke(slotIndex.ToString(), isWeapon);
			MetaSaveSystem.Save(_state);
		}

		/// <summary>
		/// Логика установки предмета в слот (если хочешь держать это во View).
		/// </summary>
		public void InstallItem(string slotId, string itemId, bool isWeapon)
		{
			//if (isWeapon)
				//_state.Fit.SetWeapon(slotId, itemId);
			//else
				//_state.Fit.SetModule(slotId, itemId);

			OnSlotChanged?.Invoke(slotId, isWeapon);
		}
	}
}