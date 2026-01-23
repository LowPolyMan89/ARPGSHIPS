using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class ShipFitSlotUi : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		private static readonly System.Collections.Generic.List<ShipFitSlotUi> AllSlots = new();

		[Header("UI")]
		[SerializeField] private Image _icon;
		[SerializeField] private Image _background;

		[Header("Colors")]
		[SerializeField] private Color _idleColor = new Color(1f, 1f, 1f, 0.08f);
		[SerializeField] private Color _hoverColor = new Color(1f, 1f, 0f, 0.25f);
		[SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.3f);
		[SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.3f);

		private ShipSocketVisual _socket;
		private MetaState _state;
		private PlayerShip _ship;
		private Transform _shipRoot;
		private bool _isHighlighted;
		private Canvas _canvas;
		private RectTransform _rectTransform;
		private RectTransform _dragIcon;
		private Transform _dragWorld;
		private float _dragWorldDepth;

		private void Awake()
		{
			_canvas = GetComponentInParent<Canvas>();
			_rectTransform = GetComponent<RectTransform>();
			SetIdle();
		}

		private void OnEnable()
		{
			if (!AllSlots.Contains(this))
				AllSlots.Add(this);
		}

		private void OnDisable()
		{
			AllSlots.Remove(this);
		}

		public void Bind(ShipSocketVisual socket, MetaState state, PlayerShip ship, Transform shipRoot)
		{
			_socket = socket;
			_state = state;
			_ship = ship;
			_shipRoot = shipRoot;
			Refresh();
		}

		public void Refresh()
		{
			if (_socket == null || _state?.Fit?.GridPlacements == null)
			{
				SetIcon(null);
				return;
			}

			var placement = _state.Fit.GridPlacements.Find(p => p != null && p.GridId == _socket.SocketId);
			if (placement == null || string.IsNullOrEmpty(placement.ItemId))
			{
				if (TryEnsureDefaultWeapon(out var defaultItem))
				{
					SetIcon(defaultItem);
					_socket.SpawnMetaItem(defaultItem, _ship);
				}
				else
				{
					SetIcon(null);
					_socket.ClearMetaItem();
				}
				return;
			}

			var item = InventoryUtils.FindByItemId(_state.InventoryModel, placement.ItemId);
			if (item == null && TryEnsureDefaultWeapon(out var fallbackItem))
				item = fallbackItem;

			SetIcon(item);
			if (item != null)
				_socket.SpawnMetaItem(item, _ship);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			ShipMetaDragContext.ActiveSlot = this;
			SetHover();

			if (TryGetEquippedItem(out var item, out _))
				MetaController.Instance?.MetaVisual?.ShowItemInfoWindow(item, eventData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (ShipMetaDragContext.ActiveSlot == this)
				ShipMetaDragContext.ActiveSlot = null;
			SetIdle();
			MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
		}

		public void OnDrop(PointerEventData eventData)
		{
			var item = ShipMetaDragContext.DraggedInventoryItem;
			if (item == null || _socket == null || _state == null)
				return;

			if (!_socket.CanAccept(item))
				return;

			Equip(item);
			ShipMetaDragContext.DraggedInventoryItem = null;
			ClearHighlights();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Right)
				return;

			if (!TryGetEquippedItem(out var item, out var isFromInventory) || !isFromInventory)
				return;

			Unequip(item);
			MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			if (!TryGetEquippedItem(out var item, out var isFromInventory) || !isFromInventory)
				return;

			ShipMetaDragContext.DraggedInventoryItem = item;
			MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
			HighlightSlots(item);

			if (_canvas == null || _rectTransform == null)
				return;

			var forceWorldPrefab = false;
			var dragSprite = ResourceLoader.LoadItemIcon(item, ItemIconContext.Drag);

			if (!forceWorldPrefab)
			{
				var go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
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

				if (img.sprite == null)
				{
					img.enabled = false;
					forceWorldPrefab = true;
				}

				if (!forceWorldPrefab)
					_dragIcon.position = eventData.position;
			}

			if (forceWorldPrefab)
			{
				TryAttachDragPrefabWorld(item, eventData);
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			UpdateActiveGrid(eventData);

			if (_dragWorld != null)
			{
				UpdateDragWorldPosition(eventData);
				return;
			}

			if (_dragIcon != null)
				_dragIcon.position = eventData.position;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			var draggedItem = ShipMetaDragContext.DraggedInventoryItem;
			var activeSlot = ShipMetaDragContext.ActiveSlot;
			ShipMetaDragContext.DraggedInventoryItem = null;
			ShipMetaDragContext.ActiveSlot = null;
			ClearHighlights();

			if (_dragIcon != null)
				Destroy(_dragIcon.gameObject);
			_dragIcon = null;

			if (_dragWorld != null)
				Destroy(_dragWorld.gameObject);
			_dragWorld = null;

			if (draggedItem != null)
			{
				if (activeSlot == null)
					Unequip(draggedItem);
				else
					Refresh();
				return;
			}

			Refresh();
		}

		private void Equip(InventoryItem item)
		{
			if (_state?.Fit == null)
				return;

			var itemId = InventoryUtils.ResolveItemId(item);
			if (string.IsNullOrEmpty(itemId))
				return;

			if (_state.Fit.GridPlacements != null)
			{
				var existing = _state.Fit.GridPlacements.Find(p => p != null && p.GridId == _socket.SocketId);
				if (existing != null && !string.IsNullOrEmpty(existing.ItemId))
				{
					if (string.Equals(existing.ItemId, itemId, System.StringComparison.OrdinalIgnoreCase))
						return;

					InventoryUtils.ReturnToInventory(_state.InventoryModel, existing.ItemId, 1);
				}

				_state.Fit.GridPlacements.RemoveAll(p => p != null && p.GridId == _socket.SocketId);
			}

			if (!InventoryUtils.TryConsume(_state.InventoryModel, itemId, 1))
				return;

			_socket.GetLocalPose(_shipRoot, out var localPos, out var localEuler);

			_state.Fit.GridPlacements.Add(new ShipFitModel.GridPlacement
			{
				GridId = _socket.SocketId,
				GridType = _socket.SocketType,
				ItemId = itemId,
				X = 0,
				Y = 0,
				Width = 1,
				Height = 1,
				Position = Vector2.zero,
				RotationDeg = 0f,
				LocalPosition = localPos,
				LocalEuler = localEuler,
				HasLocalPose = true
			});

			_socket.SpawnMetaItem(item, _ship);
			SetIcon(item);

			MetaSaveSystem.Save(_state);
			GameEvent.InventoryUpdated(_state.InventoryModel);

			var statsCtrl = MetaController.Instance?.MetaVisual?.StatsController;
			if (statsCtrl != null)
				statsCtrl.Refresh();
		}

		private void Unequip(InventoryItem item)
		{
			if (item == null || _state?.Fit == null || _socket == null)
				return;

			var placement = _state.Fit.GridPlacements.Find(p => p != null && p.GridId == _socket.SocketId);
			if (placement == null || string.IsNullOrEmpty(placement.ItemId))
				return;

			_state.Fit.GridPlacements.RemoveAll(p => p != null && p.GridId == _socket.SocketId);
			InventoryUtils.ReturnToInventory(_state.InventoryModel, placement.ItemId, 1);

			_socket.ClearMetaItem();
			MetaSaveSystem.Save(_state);
			GameEvent.InventoryUpdated(_state.InventoryModel);

			var statsCtrl = MetaController.Instance?.MetaVisual?.StatsController;
			if (statsCtrl != null)
				statsCtrl.Refresh();

			Refresh();
		}

		private void SetIcon(InventoryItem item)
		{
			if (_icon == null)
				return;

			if (item == null)
			{
				_icon.enabled = false;
				_icon.sprite = null;
				return;
			}

			_icon.sprite = ResourceLoader.LoadItemIcon(item, ItemIconContext.Fit);
			_icon.enabled = _icon.sprite != null;
		}

		private bool TryEnsureDefaultWeapon(out InventoryItem defaultItem)
		{
			defaultItem = null;
			if (_socket == null || _state?.Fit == null || _state.InventoryModel == null)
				return false;
			if (_socket.SocketType != ShipGridType.WeaponGrid)
				return false;

			var placement = _state.Fit.GridPlacements.Find(p => p != null && p.GridId == _socket.SocketId);
			if (placement != null && !string.IsNullOrEmpty(placement.ItemId))
			{
				var existing = InventoryUtils.FindByItemId(_state.InventoryModel, placement.ItemId);
				if (existing != null)
					return false;

				if (DefaultWeaponResolver.TryBuildTemplateItem(placement.ItemId, out defaultItem))
					return true;
			}

			if (!DefaultWeaponResolver.TryGetDefaultTemplateId(_socket.SocketSize, out var templateId))
				return false;
			if (!DefaultWeaponResolver.TryBuildTemplateItem(templateId, out defaultItem))
				return false;

			_state.Fit.GridPlacements.RemoveAll(p => p != null && p.GridId == _socket.SocketId);

			_socket.GetLocalPose(_shipRoot, out var localPos, out var localEuler);
			_state.Fit.GridPlacements.Add(new ShipFitModel.GridPlacement
			{
				GridId = _socket.SocketId,
				GridType = _socket.SocketType,
				ItemId = templateId,
				X = 0,
				Y = 0,
				Width = 1,
				Height = 1,
				Position = Vector2.zero,
				RotationDeg = 0f,
				LocalPosition = localPos,
				LocalEuler = localEuler,
				HasLocalPose = true
			});

			MetaSaveSystem.Save(_state);

			var statsCtrl = MetaController.Instance?.MetaVisual?.StatsController;
			if (statsCtrl != null)
				statsCtrl.Refresh();

			return true;
		}

		private void SetHover()
		{
			if (_background != null && !_isHighlighted)
				_background.color = _hoverColor;
		}

		private void SetIdle()
		{
			if (_background != null && !_isHighlighted)
				_background.color = _idleColor;
		}

		private void SetHighlight(bool canPlace)
		{
			_isHighlighted = true;
			if (_background != null)
				_background.color = canPlace ? _validColor : _invalidColor;
		}

		private void ClearHighlightState()
		{
			_isHighlighted = false;
			SetIdle();
		}

		private bool TryGetEquippedItem(out InventoryItem item, out bool isFromInventory)
		{
			item = null;
			isFromInventory = false;

			if (_socket == null || _state?.Fit?.GridPlacements == null || _state.InventoryModel == null)
				return false;

			var placement = _state.Fit.GridPlacements.Find(p => p != null && p.GridId == _socket.SocketId);
			if (placement == null || string.IsNullOrEmpty(placement.ItemId))
				return false;

			item = InventoryUtils.FindByItemId(_state.InventoryModel, placement.ItemId);
			if (item != null)
			{
				isFromInventory = true;
				return true;
			}

			if (DefaultWeaponResolver.TryBuildTemplateItem(placement.ItemId, out var defaultItem))
			{
				item = defaultItem;
				return true;
			}

			return false;
		}

		private void UpdateActiveGrid(PointerEventData eventData)
		{
			if (EventSystem.current == null)
				return;

			var results = new System.Collections.Generic.List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, results);

			ShipFitSlotUi slot = null;
			for (var i = 0; i < results.Count; i++)
			{
				var go = results[i].gameObject;
				if (go == null)
					continue;

				slot = go.GetComponentInParent<ShipFitSlotUi>();
				if (slot != null)
					break;
			}

			ShipMetaDragContext.ActiveSlot = slot;
		}

		private void UpdateDragWorldPosition(PointerEventData eventData)
		{
			var cam = eventData.pressEventCamera ?? eventData.enterEventCamera ?? Camera.main;
			if (cam == null || _dragWorld == null)
				return;

			var sp = new Vector3(eventData.position.x, eventData.position.y, _dragWorldDepth);
			var wp = cam.ScreenToWorldPoint(sp);
			_dragWorld.position = wp;
		}

		private bool TryAttachDragPrefabWorld(InventoryItem item, PointerEventData eventData)
		{
			if (item == null)
				return false;

			Transform parent = null;
			if (MetaController.Instance != null)
				parent = MetaController.Instance.ShipPodium;

			var go = ResourceLoader.InstantiateItemPrefab(item, parent, false);
			if (go == null)
				return false;

			foreach (var col in go.GetComponentsInChildren<Collider>(true))
				col.enabled = false;
			foreach (var col2d in go.GetComponentsInChildren<Collider2D>(true))
				col2d.enabled = false;

			_dragWorld = go.transform;
			var cam = eventData.pressEventCamera ?? eventData.enterEventCamera ?? Camera.main;
			_dragWorldDepth = cam != null ? Mathf.Abs(_dragWorld.position.z - cam.transform.position.z) : 0f;
			UpdateDragWorldPosition(eventData);
			return true;
		}

		public static void HighlightSlots(InventoryItem item)
		{
			foreach (var slot in AllSlots)
			{
				if (slot == null || slot._socket == null)
					continue;

				var canPlace = slot._socket.CanAccept(item);
				slot.SetHighlight(canPlace);
			}
		}

		public static void ClearHighlights()
		{
			foreach (var slot in AllSlots)
			{
				if (slot == null)
					continue;
				slot.ClearHighlightState();
			}
		}
	}
}
