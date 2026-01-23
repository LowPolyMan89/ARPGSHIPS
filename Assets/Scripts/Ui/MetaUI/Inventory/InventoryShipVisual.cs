using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class InventoryShipVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler,
		IDragHandler, IEndDragHandler
	{
		private InventoryShip _ship;
		private InventoryView _inventoryView;
		private RectTransform _dragIcon;
		private Canvas _canvas;
		private RectTransform _rectTransform;

		public Image Icon;

		public void Init(InventoryShip ship, InventoryView inventoryView)
		{
			_ship = ship;
			_inventoryView = inventoryView;
			_canvas = GetComponentInParent<Canvas>();
			_rectTransform = GetComponent<RectTransform>();
			ApplyIcon();

		}

		public void OnPointerEnter(PointerEventData eventData)
		{
		}

		public void OnPointerExit(PointerEventData eventData)
		{
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			var shipId = ShipInventoryUtils.ResolveShipId(_ship);
			if (string.IsNullOrEmpty(shipId))
				return;

			ShipSquadDragContext.DraggedInventoryShip = _ship;
			ShipSquadDragContext.SourceSlot = null;

			if (_canvas == null || _rectTransform == null)
				return;

			var dragSprite = ResourceLoader.LoadShipIcon(shipId);
			if (dragSprite == null)
				return;

			var go = new GameObject("ShipDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
			go.transform.SetParent(_canvas.transform, false);
			_dragIcon = (RectTransform)go.transform;
			_dragIcon.anchorMin = new Vector2(0.5f, 0.5f);
			_dragIcon.anchorMax = new Vector2(0.5f, 0.5f);
			_dragIcon.pivot = new Vector2(0.5f, 0.5f);
			_dragIcon.sizeDelta = _rectTransform.rect.size;
			var img = go.GetComponent<Image>();
			img.sprite = dragSprite;
			img.raycastTarget = false;
			go.GetComponent<CanvasGroup>().blocksRaycasts = false;
			_dragIcon.position = eventData.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			UpdateActiveSlot(eventData);

			if (_dragIcon != null)
				_dragIcon.position = eventData.position;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			ShipSquadDragContext.DraggedInventoryShip = null;
			ShipSquadDragContext.SourceSlot = null;
			ShipSquadDragContext.ActiveSlot = null;

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
				Icon = GetComponent<Image>();

			if (Icon == null)
				return;

			var shipId = ShipInventoryUtils.ResolveShipId(_ship);
			Icon.sprite = ResourceLoader.LoadShipIcon(shipId);
			Icon.enabled = Icon.sprite != null;
		}

		private void UpdateActiveSlot(PointerEventData eventData)
		{
			if (EventSystem.current == null)
				return;

			var results = new System.Collections.Generic.List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, results);

			ShipSelectionPanelElement slot = null;
			for (var i = 0; i < results.Count; i++)
			{
				var go = results[i].gameObject;
				if (go == null)
					continue;

				slot = go.GetComponentInParent<ShipSelectionPanelElement>();
				if (slot != null)
					break;
			}

			ShipSquadDragContext.ActiveSlot = slot;
		}
	}
}
