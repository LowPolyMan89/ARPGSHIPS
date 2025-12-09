using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tanks
{
	public class InventoryItemVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private InventoryItem _item;
		private InventoryView _inventoryView;

		[Header("UI")]
		public Image Icon;
		public Text CountLabel;
		public Button Button;

		public void Init(InventoryItem item, InventoryView inventoryView)
		{
			_inventoryView = inventoryView;
			_item = item;
			Button.onClick.RemoveAllListeners();
			Button.onClick.AddListener(ButtonClick);
		}

		public void ButtonClick()
		{
			MetaController.Instance.MetaVisual.ButtonItemClick(_item);
		}
		private void OnDestroy()
		{
			Button.onClick.RemoveAllListeners();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			MetaController.Instance.MetaVisual.ShowItemInfoWindow(_item, eventData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			MetaController.Instance.MetaVisual.HideItemInfoWindow();
		}
	}
}