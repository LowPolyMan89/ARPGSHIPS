using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class InventoryVisual : MonoBehaviour
	{
		private InventoryView _view;

		public Transform ListRoot;
		public InventoryItemVisual ItemPrefab;

		public void Init(InventoryView view)
		{
			_view = view;

			// Подписываемся
			GameEvent.OnInventoryUpdated += UpdateVisual;
			GameEvent.OnItemSelected += OnItemSelected;
			UpdateVisual(_view.GetInventory());
		}

		private void OnDestroy()
		{
			GameEvent.OnInventoryUpdated -= UpdateVisual;
			GameEvent.OnItemSelected -= OnItemSelected;
		}

		private void UpdateVisual(PlayerInventoryModel inventory)
		{
			Debug.Log("Update inventory, items: " + inventory.InventoryUniqueItems.Count);
			foreach (Transform child in ListRoot)
				Destroy(child.gameObject);

			foreach (var item in inventory.InventoryUniqueItems)
			{
				// Hide items that are already equipped in a grid.
				if (item.IsEquipped && !string.IsNullOrEmpty(item.EquippedGridId))
					continue;

				Debug.Log($"Load item: {item.ItemId}");
				var visual = Instantiate(ItemPrefab, ListRoot);
				visual.Init(item, _view);
			}
		}

		private void OnItemClicked(InventoryItem item)
		{
			_view.SelectItem(item);
		}

		private void OnItemSelected(InventoryItem item)
		{
			// Будущее: отображение описания предмета
			// UI: показываем stats, описание и т.д.
		}
	}


}
