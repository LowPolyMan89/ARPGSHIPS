using Ships;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShipSelectionPanelElement : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] private Button _selectButton;
	[SerializeField] private Button _removeButton;
	[SerializeField] private Image _currentShipImage;
	[SerializeField] private bool _isFlagship;
	[SerializeField] private Sprite _nonShipSprite;

	private ShipSelectionPanel _panel;
	private int _slotIndex;
	private Canvas _canvas;
	private RectTransform _rectTransform;
	private RectTransform _dragIcon;

	private void Awake()
	{
		_canvas = GetComponentInParent<Canvas>();
		_rectTransform = GetComponent<RectTransform>();
	}

	public void Init(ShipSelectionPanel panel, int slotIndex)
	{
		_panel = panel;
		_slotIndex = slotIndex;

		if (_selectButton != null)
		{
			_selectButton.onClick.RemoveAllListeners();
			_selectButton.onClick.AddListener(OnSelect);
		}

		if (_removeButton != null)
		{
			_removeButton.onClick.RemoveAllListeners();
			_removeButton.onClick.AddListener(OnRemove);
			_removeButton.interactable = !_isFlagship;
		}

		Refresh();
	}

	public void Refresh()
	{
		if (_panel == null)
			return;

		var shipId = _panel.GetSlotShipId(_slotIndex);
		var icon = _panel.LoadShipIcon(shipId);
		if (_currentShipImage != null)
		{
			_currentShipImage.sprite = icon != null ? icon : _nonShipSprite;
			_currentShipImage.enabled = _currentShipImage.sprite != null;
		}
	}

	private void OnSelect()
	{
		if (_panel == null)
			return;

		_panel.SelectSlot(_slotIndex);
	}

	private void OnRemove()
	{
		if (_panel == null)
			return;

		_panel.TryClearSlot(_slotIndex, _isFlagship);
	}

	public void OnDrop(PointerEventData eventData)
	{
		if (_panel == null)
			return;

		if (ShipSquadDragContext.SourceSlot != null)
			return;

		var ship = ShipSquadDragContext.DraggedInventoryShip;
		if (ship == null)
			return;

		if (_panel.TryAssignShipFromInventory(_slotIndex, ship, _isFlagship))
			ShipSquadDragContext.DraggedInventoryShip = null;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (_panel == null || _isFlagship)
			return;

		var shipId = _panel.GetSlotShipId(_slotIndex);
		if (string.IsNullOrEmpty(shipId))
			return;

		ShipSquadDragContext.DraggedInventoryShip = new InventoryShip
		{
			ShipId = shipId,
			TemplateId = shipId
		};
		ShipSquadDragContext.SourceSlot = this;

		if (_canvas == null || _rectTransform == null)
			return;

		var dragSprite = ResourceLoader.LoadShipIcon(shipId);
		if (dragSprite == null)
			return;

		var go = new GameObject("ShipSlotDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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
		var dragged = ShipSquadDragContext.DraggedInventoryShip;
		var activeSlot = ShipSquadDragContext.ActiveSlot;
		var wasSource = ShipSquadDragContext.SourceSlot == this;

		ShipSquadDragContext.DraggedInventoryShip = null;
		ShipSquadDragContext.ActiveSlot = null;
		ShipSquadDragContext.SourceSlot = null;

		if (_dragIcon != null)
			Destroy(_dragIcon.gameObject);
		_dragIcon = null;

		if (dragged != null && wasSource)
		{
			if (activeSlot == null)
				_panel?.TryClearSlot(_slotIndex, _isFlagship);
			else
				Refresh();
		}
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
