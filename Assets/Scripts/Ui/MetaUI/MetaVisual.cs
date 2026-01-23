using UnityEngine;
using UnityEngine.EventSystems;

namespace Ships
{
	public class MetaVisual : MonoBehaviour
	{
		public InventoryVisual InventoryVisual;
		public InventoryShipListVisual ShipInventoryVisual;
		[SerializeField] private ShipFitSlotsController _fitSlotsController;
		[SerializeField] private ItemSelectionVisual _itemSelectionVisual;
		[SerializeField] private ShipUiMetaStatVisual _uiMetaStatVisualPrefab;
		[SerializeField] private Transform _uiMetaStatVisualPrefabsRoot;
		[SerializeField] private MetaStatsController _metaStatsController;
		[SerializeField] private bool _autoRefreshStatsOnStart = true;

		public void ShowItemInfoWindow(InventoryItem item, PointerEventData pointerEventData)
		{
			if (_itemSelectionVisual != null)
				_itemSelectionVisual.Show(item, pointerEventData);
		}

		public void HideItemInfoWindow()
		{
			if (_itemSelectionVisual != null)
				_itemSelectionVisual.Hide();
		}

		public ShipUiMetaStatVisual StatPrefab => _uiMetaStatVisualPrefab;
		public Transform StatRoot => _uiMetaStatVisualPrefabsRoot;
		public MetaStatsController StatsController => _metaStatsController;
		public ShipFitSlotsController FitSlotsController => _fitSlotsController;

		private void Start()
		{
			if (_autoRefreshStatsOnStart && _metaStatsController != null)
				_metaStatsController.Refresh();
		}
	}
}
