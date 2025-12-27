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
			foreach (Transform child in ListRoot)
				Destroy(child.gameObject);

			foreach (var item in inventory.InventoryUniqueItems)
			{
				if (item.IsEquipped)
					continue;

				var visual = Instantiate(ItemPrefab, ListRoot);
				visual.Init(item, _view);
			}
		}

		private void OnItemSelected(InventoryItem item)
		{
			// Здесь можно показывать описание предмета или подсветку.
		}
	}
}
