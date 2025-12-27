using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class InventoryItemVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private InventoryItem _item;
		private InventoryView _inventoryView;
		private RectTransform _dragIcon;
		private Canvas _canvas;

		[Header("UI")]
		public Image Icon;
		public Text CountLabel;
		public Button Button;

		public void Init(InventoryItem item, InventoryView inventoryView)
		{
			_inventoryView = inventoryView;
			_item = item;
			_canvas = GetComponentInParent<Canvas>();
			ApplyIcon();

			Button.onClick.RemoveAllListeners();
			Button.onClick.AddListener(ButtonClick);
		}

		public void ButtonClick()
		{
			_inventoryView.SelectItem(_item);
		}

		private void OnDestroy()
		{
			Button.onClick.RemoveAllListeners();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (MetaController.Instance?.MetaVisual != null)
				MetaController.Instance.MetaVisual.ShowItemInfoWindow(_item, eventData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (MetaController.Instance?.MetaVisual != null)
				MetaController.Instance.MetaVisual.HideItemInfoWindow();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (_item == null)
				return;

			ShipMetaDragContext.DraggedInventoryItem = _item;
			MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
			ShipSocketVisual.HighlightSockets(_item);

			if (_canvas == null || Icon == null)
				return;

			var go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
			go.transform.SetParent(_canvas.transform, false);
			_dragIcon = (RectTransform)go.transform;
			_dragIcon.anchorMin = new Vector2(0.5f, 0.5f);
			_dragIcon.anchorMax = new Vector2(0.5f, 0.5f);
			_dragIcon.pivot = new Vector2(0.5f, 0.5f);
			_dragIcon.sizeDelta = ((RectTransform)Icon.transform).rect.size;
			var img = go.GetComponent<Image>();
			img.sprite = ResourceLoader.LoadItemIcon(_item, ItemIconContext.Drag);
			img.raycastTarget = false;
			go.GetComponent<CanvasGroup>().blocksRaycasts = false;

			_dragIcon.position = eventData.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			UpdateActiveGrid(eventData);

			if (_dragIcon == null)
				return;

			if (ShipMetaDragContext.ActiveSocket != null &&
			    ShipMetaDragContext.ActiveSocket.TryGetSnappedScreenPosition(eventData, out var snapped))
			{
				UpdateDragIconSize(ShipMetaDragContext.ActiveSocket);
				_dragIcon.position = snapped;
			}
			else
			{
				_dragIcon.position = eventData.position;
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			ShipMetaDragContext.DraggedInventoryItem = null;
			ShipMetaDragContext.ActiveSocket = null;
			ShipSocketVisual.ClearHighlights();
			if (_dragIcon != null)
				Destroy(_dragIcon.gameObject);
			_dragIcon = null;
		}

		private void OnDisable()
		{
			if (_dragIcon != null)
			{
				Destroy(_dragIcon.gameObject);
				_dragIcon = null;
			}
		}

		private void ApplyIcon()
		{
			if (Icon == null)
				return;

			var sprite = ResourceLoader.LoadItemIcon(_item, ItemIconContext.Inventory);
			Icon.sprite = sprite;
			Icon.enabled = sprite != null;
			Icon.preserveAspect = true;
		}

		private void UpdateActiveGrid(PointerEventData eventData)
		{
			if (EventSystem.current == null)
				return;

			var results = new System.Collections.Generic.List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, results);

			ShipSocketVisual socket = null;
			for (var i = 0; i < results.Count; i++)
			{
				var go = results[i].gameObject;
				if (go == null)
					continue;

				socket = go.GetComponentInParent<ShipSocketVisual>();
				if (socket != null)
					break;
			}

			ShipMetaDragContext.ActiveSocket = socket;
		}

		private void UpdateDragIconSize(ShipSocketVisual socket)
		{
			if (_dragIcon == null || socket == null || _item == null)
				return;

			_dragIcon.sizeDelta = new Vector2(socket.GetVisualSize(), socket.GetVisualSize());
		}

	}
}
