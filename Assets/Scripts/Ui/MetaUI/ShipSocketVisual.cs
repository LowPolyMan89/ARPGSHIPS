using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;

namespace Ships
{
	public enum ShipSocketSize
	{
		Small,
		Medium,
		Large
	}

	/// <summary>
	/// World-space UI сокет на мета-префабе корабля. Принимает драг/дроп предмета, проверяет тип/размер и спавнит мета-префаб.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(Image))]
	public class ShipSocketVisual : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
	{
		private static readonly System.Collections.Generic.List<ShipSocketVisual> AllSockets = new();

		[Header("Socket")]
		public string SocketId = "socket_01";
		public ShipGridType SocketType = ShipGridType.WeaponGrid;
		public ShipSocketSize SocketSize = ShipSocketSize.Small;
		public float BaseSize = 64f;
		public float MediumFactor = 1.5f;
		public float LargeFactor = 2f;
		public float RotationDeg;

		[Header("Visuals")]
		[SerializeField] private Color _idleColor = new Color(1f, 1f, 1f, 0.08f);
		[SerializeField] private Color _hoverColor = new Color(1f, 1f, 0f, 0.25f);
		[SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.3f);
		[SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.3f);

		private RectTransform _rect;
		private Image _image;
		private EquippedItemHandle _currentHandle;
		private bool _isHighlighted;

		private void Awake()
		{
			_rect = GetComponent<RectTransform>();
			_image = GetComponent<Image>();
			SetupSize();
			SetIdle();
			Register();
		}

		private void OnValidate()
		{
			if (_rect == null)
				_rect = GetComponent<RectTransform>();
			if (_image == null)
				_image = GetComponent<Image>();

			SetupSize();
			SetIdle();
		}

		private void OnEnable()
		{
			Register();
		}

		private void OnDisable()
		{
			Unregister();
		}

		private void OnDestroy()
		{
			Unregister();
		}

		private void Register()
		{
			if (!AllSockets.Contains(this))
				AllSockets.Add(this);
		}

		private void Unregister()
		{
			AllSockets.Remove(this);
		}

		private void SetupSize()
		{
			if (_rect == null)
				return;

			var factor = SocketSize switch
			{
				ShipSocketSize.Small => 1f,
				ShipSocketSize.Medium => MediumFactor,
				ShipSocketSize.Large => LargeFactor,
				_ => 1f
			};

			var size = BaseSize * factor;
			_rect.sizeDelta = new Vector2(size, size);
			_rect.pivot = new Vector2(0.5f, 0.5f);
			_rect.localRotation = Quaternion.Euler(0f, 0f, RotationDeg);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			ShipMetaDragContext.ActiveSocket = this;
			SetHover();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (ShipMetaDragContext.ActiveSocket == this)
				ShipMetaDragContext.ActiveSocket = null;
			SetIdle();
		}

		public void OnDrop(PointerEventData eventData)
		{
			var item = ShipMetaDragContext.DraggedInventoryItem;
			if (item == null || MetaController.Instance == null)
				return;

			if (!IsTypeAllowed(item))
				return;

			if (!TryResolveSize(item, out var itemSize))
				return;

			if (!IsSizeAllowed(itemSize))
				return;

			EquipInternal(item, save: true, fireEvents: true, updateFitList: true);
			ShipMetaDragContext.DraggedInventoryItem = null;
			ClearHighlights();
		}

		public void EquipFromSave(InventoryItem item)
		{
			// При загрузке из сейва не трогаем список размещений (он уже заполнен).
			EquipInternal(item, save: false, fireEvents: false, updateFitList: false);
		}

		/// <summary>
		/// Вернуть предмет в слот (например, при отменённом драге) с восстановлением плейсмента и сохранением.
		/// </summary>
		public void ReequipItem(InventoryItem item)
		{
			EquipInternal(item, save: true, fireEvents: true, updateFitList: true);
		}

		private void EquipInternal(InventoryItem item, bool save, bool fireEvents, bool updateFitList)
		{
			var state = MetaController.Instance.State;
			if (state == null)
				return;

			// Снимаем то, что уже висит на сокете.
			if (_currentHandle != null)
				_currentHandle.Unequip();

			var fit = state.Fit;
			if (updateFitList && fit != null && fit.GridPlacements != null)
			{
				fit.GridPlacements.RemoveAll(p => p != null && (p.ItemId == item.ItemId || p.GridId == SocketId));
				fit.GridPlacements.Add(new ShipFitModel.GridPlacement
				{
					GridId = SocketId,
					GridType = SocketType,
					ItemId = item.ItemId,
					X = 0,
					Y = 0,
					Width = 1,
					Height = 1,
					Position = new Vector2(transform.localPosition.x, transform.localPosition.y),
					RotationDeg = RotationDeg
				});
			}

			item.EquippedOnFitId = state.SelectedShipId;
			item.EquippedGridId = SocketId;
			item.EquippedGridX = 0;
			item.EquippedGridY = 0;
			item.EquippedGridPos = Vector2.zero;
			item.EquippedGridRot = RotationDeg;

			SpawnItem(item);

			if (save)
			{
				MetaSaveSystem.Save(state);
				if (fireEvents)
					GameEvent.InventoryUpdated(state.InventoryModel);
			}

			RefreshStatsUi();
		}

		private void SpawnItem(InventoryItem item)
		{
			if (_currentHandle != null)
				Destroy(_currentHandle.gameObject);

			var mountGo = new GameObject($"SocketMount_{SocketId}_{item.ItemId}");
			var mount = mountGo.transform;
			var parent = transform.parent != null ? transform.parent : transform;

			// Крепим к родителю сокета, но ставим по позиции/повороту сокета.
			mount.SetParent(parent, worldPositionStays: false);
			mount.position = transform.position;
			mount.rotation = transform.rotation;
			mount.localScale = Vector3.one;

			var ship = GetComponentInParent<PlayerShip>();

			if (SocketType == ShipGridType.WeaponGrid)
				WeaponBuilder.BuildMeta(item.ItemId, mount, ship);
			// TODO: Module builder when появится.

			EnsurePointerCollider(mount);

			_currentHandle = mount.gameObject.AddComponent<EquippedItemHandle>();
			_currentHandle.Init(this, item.ItemId);
			SetRaycastEnabled(false);
		}

		private void EnsurePointerCollider(Transform mount)
		{
			// Нужен коллайдер, чтобы Physics/Physics2D Raycaster смог поймать клик ПКМ.
			if (mount.GetComponentInChildren<Collider2D>() != null || mount.GetComponentInChildren<Collider>() != null)
				return;

			var renderer = mount.GetComponentInChildren<Renderer>();
			var size = renderer != null ? renderer.bounds.size : new Vector3(GetVisualSize() * 0.01f, GetVisualSize() * 0.01f, 0.1f);

			var colGo = renderer != null ? renderer.gameObject : mount.gameObject;
			var box = colGo.AddComponent<BoxCollider2D>();
			box.isTrigger = true;
			box.size = new Vector2(size.x, size.y);
		}

		private void RefreshStatsUi()
		{
			var statsCtrl = MetaController.Instance?.MetaVisual?.StatsController;
			if (statsCtrl != null)
				statsCtrl.Refresh();
		}

		public bool TryGetSnappedScreenPosition(PointerEventData eventData, out Vector2 screenPos)
		{
			screenPos = default;
			if (_rect == null)
				return false;

			var dragged = ShipMetaDragContext.DraggedInventoryItem;
			if (dragged != null)
			{
				if (!IsTypeAllowed(dragged))
					return false;
				if (TryResolveSize(dragged, out var itemSize) && !IsSizeAllowed(itemSize))
					return false;
			}

			var cam = eventData.pressEventCamera
			          ?? eventData.pointerPressRaycast.module?.eventCamera
			          ?? eventData.enterEventCamera
			          ?? Camera.main;

			screenPos = RectTransformUtility.WorldToScreenPoint(cam, _rect.position);
			return true;
		}

		public float GetVisualSize()
		{
			var factor = SocketSize switch
			{
				ShipSocketSize.Small => 1f,
				ShipSocketSize.Medium => MediumFactor,
				ShipSocketSize.Large => LargeFactor,
				_ => 1f
			};
			return BaseSize * factor;
		}

		public bool TryGetSnappedWorldPosition(out Vector3 worldPos)
		{
			worldPos = Vector3.zero;
			var dragged = ShipMetaDragContext.DraggedInventoryItem;
			if (dragged != null && !CanAccept(dragged))
				return false;

			if (_rect == null)
				return false;

			worldPos = _rect.position;
			return true;
		}

		private bool IsTypeAllowed(InventoryItem item)
		{
			if (!TryResolveTypes(item, out var allowed))
				return true;
			if (allowed == null || allowed.Length == 0)
				return true;
			return System.Array.IndexOf(allowed, SocketType) >= 0;
		}

		private static bool TryResolveTypes(InventoryItem item, out ShipGridType[] allowed)
		{
			allowed = null;
			if (item == null)
				return false;

			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var relativePath = System.IO.Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
				if (ResourceLoader.TryLoadPersistentJson(relativePath, out WeaponLoadData weapon))
				{
					allowed = weapon.AllowedGridTypeValues;
					return true;
				}
			}

			if (!string.IsNullOrEmpty(item.TemplateId))
			{
				var templateId = item.TemplateId.EndsWith(".json") ? item.TemplateId : item.TemplateId + ".json";
				var templatePath = System.IO.Path.Combine(PathConstant.WeaponsConfigs, templateId);
				if (ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
				{
					allowed = EnumParsingHelpers.ParseGridTypes(template.AllowedGridTypes);
					return true;
				}
			}

			return false;
		}

		private static bool TryResolveSize(InventoryItem item, out ShipSocketSize size)
		{
			size = ShipSocketSize.Small;
			if (item == null)
				return false;

			// 1) Пробуем поле Size из айтема.
			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var relativePath = System.IO.Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
				if (ResourceLoader.TryLoadPersistentJson(relativePath, out WeaponLoadData weapon))
				{
					if (TryParseSize(weapon.Size, out size))
						return true;

					return TryMapGridToSize(weapon.GridWidth, weapon.GridHeight, out size);
				}
			}

			// 2) Из шаблона.
			if (!string.IsNullOrEmpty(item.TemplateId))
			{
				var templateId = item.TemplateId.EndsWith(".json") ? item.TemplateId : item.TemplateId + ".json";
				var templatePath = System.IO.Path.Combine(PathConstant.WeaponsConfigs, templateId);
				if (ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
				{
					if (TryParseSize(template.Size, out size))
						return true;

					return TryMapGridToSize(template.GridWidth, template.GridHeight, out size);
				}
			}

			return false;
		}

		private static bool TryParseSize(string source, out ShipSocketSize size)
		{
			size = ShipSocketSize.Small;
			if (string.IsNullOrEmpty(source))
				return false;

			if (System.Enum.TryParse(source, true, out ShipSocketSize parsed))
			{
				size = parsed;
				return true;
			}

			return false;
		}

		private static bool TryMapGridToSize(int w, int h, out ShipSocketSize size)
		{
			size = ShipSocketSize.Small;
			var max = Mathf.Max(w, h);
			if (max <= 1)
			{
				size = ShipSocketSize.Small;
				return true;
			}

			if (max == 2)
			{
				size = ShipSocketSize.Medium;
				return true;
			}

			if (max >= 3)
			{
				size = ShipSocketSize.Large;
				return true;
			}

			return false;
		}

		private bool IsSizeAllowed(ShipSocketSize itemSize)
		{
			// Маленький предмет можно в больший сокет; обратное запрещаем.
			return itemSize <= SocketSize;
		}

		private bool CanAccept(InventoryItem item)
		{
			if (item == null)
				return false;
			if (!IsTypeAllowed(item))
				return false;
			if (TryResolveSize(item, out var size) && !IsSizeAllowed(size))
				return false;
			return true;
		}

		private void SetHover()
		{
			if (_image != null && !_isHighlighted)
				_image.color = _hoverColor;
		}

		private void SetIdle()
		{
			if (_image != null && !_isHighlighted)
				_image.color = _idleColor;
		}

		private void SetHighlight(bool canPlace)
		{
			_isHighlighted = true;
			if (_image != null)
				_image.color = canPlace ? _validColor : _invalidColor;
		}

		private void ClearHighlightState()
		{
			_isHighlighted = false;
			SetIdle();
		}

		public static void HighlightSockets(InventoryItem item)
		{
			foreach (var socket in AllSockets)
			{
				if (socket == null)
					continue;

				var canPlace = socket.IsTypeAllowed(item) &&
				               (!TryResolveSize(item, out var size) || socket.IsSizeAllowed(size));
				socket.SetHighlight(canPlace);
			}
		}

		public static void ClearHighlights()
		{
			foreach (var socket in AllSockets)
			{
				if (socket == null)
					continue;
				socket.ClearHighlightState();
			}
		}

		private void SetRaycastEnabled(bool enabled)
		{
			if (_image != null)
				_image.raycastTarget = enabled;
		}

		private class EquippedItemHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
		{
			private ShipSocketVisual _socket;
			private string _itemId;
			private RectTransform _dragIcon;
			private Canvas _canvas;
			private Transform _dragWorld;
			private float _dragWorldDepth;

			public void Init(ShipSocketVisual socket, string itemId)
			{
				_socket = socket;
				_itemId = itemId;
				_canvas = GetComponentInParent<Canvas>(true);
			}

			public void OnPointerEnter(PointerEventData eventData)
			{
				var item = ResolveItem();
				if (item != null)
					MetaController.Instance?.MetaVisual?.ShowItemInfoWindow(item, eventData);
			}

			public void OnPointerExit(PointerEventData eventData)
			{
				MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
			}

			public void OnPointerClick(PointerEventData eventData)
			{
				if (eventData.button != PointerEventData.InputButton.Right)
					return;

				MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
				Unequip();
			}

			public void Unequip()
			{
				if (_socket == null || MetaController.Instance == null)
					return;

				ClearPlacement(save: true, fireEvents: true);
				_socket._currentHandle = null;
				Destroy(gameObject);
			}

			public void OnBeginDrag(PointerEventData eventData)
			{
				if (_socket == null || MetaController.Instance == null || string.IsNullOrEmpty(_itemId))
					return;

				var state = MetaController.Instance.State;
				var item = ResolveItem();
				if (item == null)
					return;

				ShipMetaDragContext.DraggedInventoryItem = item;

				// Освобождаем слот без сохранения (сохранит новый слот или отмена).
				ClearPlacement(save: false, fireEvents: false);
				_socket._currentHandle = null;

				if (_canvas == null)
					_canvas = MetaController.Instance.MetaVisual.GetComponent<Canvas>();

				if (_canvas != null)
					CreateDragIcon(eventData, item);

				ShipSocketVisual.HighlightSockets(item);
				MetaController.Instance?.MetaVisual?.HideItemInfoWindow();
			}

			public void OnDrag(PointerEventData eventData)
			{
				UpdateActiveSocket(eventData);

				if (_dragWorld != null)
				{
					if (ShipMetaDragContext.ActiveSocket != null &&
					    ShipMetaDragContext.ActiveSocket.TryGetSnappedWorldPosition(out var snappedWorld))
					{
						_dragWorld.position = snappedWorld;
					}
					else
					{
						UpdateDragWorldPosition(eventData);
					}
					return;
				}

				if (_dragIcon == null)
					return;

				if (ShipMetaDragContext.ActiveSocket != null &&
				    ShipMetaDragContext.ActiveSocket.TryGetSnappedScreenPosition(eventData, out var snapped))
				{
					_dragIcon.position = snapped;
				}
				else
				{
					_dragIcon.position = eventData.position;
				}
			}

			public void OnEndDrag(PointerEventData eventData)
			{
				if (_dragIcon != null)
					Destroy(_dragIcon.gameObject);
				_dragIcon = null;
				if (_dragWorld != null)
					Destroy(_dragWorld.gameObject);
				_dragWorld = null;

				var targetSocket = ShipMetaDragContext.ActiveSocket;
				ShipMetaDragContext.ActiveSocket = null;

				if (ShipMetaDragContext.DraggedInventoryItem != null)
				{
					var item = ShipMetaDragContext.DraggedInventoryItem;
					ShipMetaDragContext.DraggedInventoryItem = null;

					if (targetSocket != null && targetSocket != _socket && targetSocket.CanAccept(item))
					{
						targetSocket.ReequipItem(item);
					}
					else if (targetSocket == null)
					{
						ClearPlacement(save: true, fireEvents: true);
					}
					else
					{
						_socket?.ReequipItem(item);
					}
				}

				ShipSocketVisual.ClearHighlights();
				_socket?.SetRaycastEnabled(_socket._currentHandle == null);
				Destroy(gameObject);
			}

			private void CreateDragIcon(PointerEventData eventData, InventoryItem item)
			{
				var go = new GameObject("DragIcon_Equipped", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
				go.transform.SetParent(_canvas.transform, false);
				_dragIcon = (RectTransform)go.transform;
				_dragIcon.anchorMin = new Vector2(0.5f, 0.5f);
				_dragIcon.anchorMax = new Vector2(0.5f, 0.5f);
				_dragIcon.pivot = new Vector2(0.5f, 0.5f);
				_dragIcon.sizeDelta = new Vector2(_socket.GetVisualSize(), _socket.GetVisualSize());
				var img = go.GetComponent<Image>();
				img.sprite = ResourceLoader.LoadItemIcon(item, ItemIconContext.Drag);
				img.raycastTarget = false;
				go.GetComponent<CanvasGroup>().blocksRaycasts = false;
				_dragIcon.position = eventData.position;

				if (img.sprite == null)
				{
					img.enabled = false;
					TryAttachDragPrefabWorld(item, eventData);
				}
			}

			private void UpdateActiveSocket(PointerEventData eventData)
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

			private InventoryItem ResolveItem()
			{
				if (MetaController.Instance == null || string.IsNullOrEmpty(_itemId))
					return null;

				return InventoryUtils.FindByItemId(MetaController.Instance.State.InventoryModel, _itemId);
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

				var relativePath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
				if (!ResourceLoader.TryLoadPersistentJson(relativePath, out WeaponLoadData data))
					return false;

				var prefabId = !string.IsNullOrEmpty(data.MetaPrefab) ? data.MetaPrefab : data.Prefab;
				if (string.IsNullOrEmpty(prefabId))
					return false;

				Transform parent = null;
				if (MetaController.Instance != null)
					parent = MetaController.Instance.ShipPodium;

				var go = ResourceLoader.InstantiatePrefab(data.Slot, prefabId, parent, false);
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

			private void ClearPlacement(bool save, bool fireEvents)
			{
				var state = MetaController.Instance.State;
				if (state?.Fit?.GridPlacements != null)
					state.Fit.GridPlacements.RemoveAll(p => p != null && p.ItemId == _itemId);

				var item = InventoryUtils.FindByItemId(state.InventoryModel, _itemId);
				if (item != null)
				{
					item.EquippedOnFitId = null;
					item.EquippedGridId = null;
					item.EquippedGridX = -1;
					item.EquippedGridY = -1;
					item.EquippedGridPos = Vector2.zero;
					item.EquippedGridRot = 0f;
				}

				if (save)
				{
					MetaSaveSystem.Save(state);
					if (fireEvents)
						GameEvent.InventoryUpdated(state.InventoryModel);
				}

				var statsCtrl = MetaController.Instance?.MetaVisual?.StatsController;
				if (statsCtrl != null)
					statsCtrl.Refresh();

				_socket?.SetRaycastEnabled(true);
			}
		}
	}
}
