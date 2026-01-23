using System;
using TMPro;
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
		private Transform _dragWorld;
		private float _dragWorldDepth;
		private Canvas _canvas;
		private RectTransform _rectTransform;

		[Header("UI")]
		public Image Icon;
		public TMP_Text CountLabel;
		public TMP_Text EnergyLabel;
		public Button Button;

		public void Init(InventoryItem item, InventoryView inventoryView)
		{
			_inventoryView = inventoryView;
			_item = item;
			_canvas = GetComponentInParent<Canvas>();
			_rectTransform = GetComponent<RectTransform>();
			ApplyIcon();
			UpdateCountLabel();
			UpdateEnergyLabel();

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
			ShipFitSlotUi.HighlightSlots(_item);

			if (_canvas == null || _rectTransform == null)
				return;

			var forceWorldPrefab = false;
			var dragSprite = ResourceLoader.LoadItemIcon(_item, ItemIconContext.Drag);

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
				TryAttachDragPrefabWorld(_item, eventData);
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
			{
				_dragIcon.position = eventData.position;
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			ShipMetaDragContext.DraggedInventoryItem = null;
			ShipMetaDragContext.ActiveSlot = null;
			ShipFitSlotUi.ClearHighlights();

			if (_dragIcon != null)
				Destroy(_dragIcon.gameObject);
			_dragIcon = null;

			if (_dragWorld != null)
				Destroy(_dragWorld.gameObject);
			_dragWorld = null;
		}

		private void OnDisable()
		{
			if (_dragIcon != null)
			{
				Destroy(_dragIcon.gameObject);
				_dragIcon = null;
			}
			if (_dragWorld != null)
			{
				Destroy(_dragWorld.gameObject);
				_dragWorld = null;
			}
		}

		private void ApplyIcon()
		{
			if (Icon == null)
				Icon = GetComponent<Image>();

			if (Icon == null)
				return;

			var sprite = ResourceLoader.LoadItemIcon(_item, ItemIconContext.Inventory);
			Icon.sprite = sprite;
			Icon.enabled = sprite != null;
		}

		private void UpdateCountLabel()
		{
			if (CountLabel == null)
				return;

			var count = _item != null ? _item.AvailableCount : 0;
			CountLabel.text = count.ToString();
			CountLabel.gameObject.SetActive(count > 0);
		}

		private void UpdateEnergyLabel()
		{
			if (EnergyLabel == null)
				return;

			var energy = ResolveEnergy();
			if (energy <= 0f)
			{
				EnergyLabel.gameObject.SetActive(false);
				return;
			}

			EnergyLabel.text = Mathf.RoundToInt(energy).ToString();
			EnergyLabel.gameObject.SetActive(true);
		}

		private float ResolveEnergy()
		{
			if (_item == null)
				return 0f;

			if (ModuleBuilder.TryBuildModuleData(_item.ResolvedId, _item.Rarity, out var module))
				return module.EnergyCost;

			return EnergyCostResolver.ResolveEnergyCost(_item);
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
	}
}
