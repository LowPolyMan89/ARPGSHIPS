using UnityEngine;
using UnityEngine.EventSystems;

namespace Ships
{
	public class MetaVisual : MonoBehaviour
	{
		public ShipFitVisual _tankFitVisual;
		public InventoryVisual InventoryVisual;
		[SerializeField] private ItemSelectionVisual _itemSelectionVisual;

		public void OpenInventory()
		{
			
		}

		public void ButtonItemClick(InventoryItem item)
		{
			
		}

		public void ShowItemInfoWindow(InventoryItem item, PointerEventData pointerEventData)
		{
			_itemSelectionVisual.Show(item, pointerEventData);
		}
		public void HideItemInfoWindow()
		{
			_itemSelectionVisual.Hide();
		}
	}
}
