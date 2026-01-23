using UnityEngine;

namespace Ships
{
	public class InventoryShipListVisual : MonoBehaviour
	{
		private InventoryView _view;

		public Transform ListRoot;
		public InventoryShipVisual ItemPrefab;

		public void Init(InventoryView view)
		{
			_view = view;

			GameEvent.OnShipInventoryUpdated += UpdateVisual;
			UpdateVisual(_view.GetShipInventory());
		}

		private void OnDestroy()
		{
			GameEvent.OnShipInventoryUpdated -= UpdateVisual;
		}

		private void UpdateVisual(System.Collections.Generic.List<InventoryShip> ships)
		{
			if (ListRoot == null)
				return;

			foreach (Transform child in ListRoot)
				Destroy(child.gameObject);

			if (ships == null || ItemPrefab == null)
				return;

			for (var i = 0; i < ships.Count; i++)
			{
				var ship = ships[i];
				var shipId = ShipInventoryUtils.ResolveShipId(ship);
				if (string.IsNullOrEmpty(shipId))
					continue;

				var visual = Instantiate(ItemPrefab, ListRoot);
				visual.Init(ship, _view);
			}
		}
	}
}
