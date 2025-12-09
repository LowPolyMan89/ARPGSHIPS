using UnityEngine;
using UnityEngine.EventSystems;

namespace Tanks
{
	public class MetaVisual : MonoBehaviour
	{
		public TankFitVisual _tankFitVisual;
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