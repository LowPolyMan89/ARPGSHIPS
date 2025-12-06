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
			_view.OnInventoryUpdated += UpdateVisual;
			_view.OnItemSelected += OnItemSelected;
		}

		private void UpdateVisual(List<InventoryItem> items)
		{
			foreach (Transform child in ListRoot)
				Destroy(child.gameObject);

			foreach (var item in items)
			{
				var visual = Instantiate(ItemPrefab, ListRoot);
				visual.Init(item, OnItemClicked);
			}
		}

		private void OnItemClicked(InventoryItem item)
		{
			_view.InstallFromInventory(item);
		}

		private void OnItemSelected(InventoryItem item)
		{
			// Будущее: отображение описания предмета
			// UI: показываем stats, описание и т.д.
		}
	}


}