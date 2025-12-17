using System;
using System.Collections.Generic;
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

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (_item == null)
				return;

			ShipMetaDragContext.DraggedInventoryItem = _item;
			ShipMetaDragContext.ActiveGrid = null;

			if (_canvas == null || Icon == null)
				return;

			var go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
			go.transform.SetParent(_canvas.transform, false);
			_dragIcon = (RectTransform)go.transform;
			_dragIcon.anchorMin = new Vector2(0, 0);
			_dragIcon.anchorMax = new Vector2(0, 0);
			_dragIcon.pivot = new Vector2(0, 0);
			_dragIcon.sizeDelta = ((RectTransform)Icon.transform).rect.size;
			var img = go.GetComponent<Image>();
			img.sprite = Icon.sprite;
			img.raycastTarget = false;
			go.GetComponent<CanvasGroup>().blocksRaycasts = false;

			_dragIcon.position = eventData.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			UpdateActiveGrid(eventData);

			if (_dragIcon == null)
				return;

			if (ShipMetaDragContext.ActiveGrid != null &&
			    ShipMetaDragContext.ActiveGrid.TryGetSnappedDragPosition(eventData, out var snapped, out _, out _))
			{
				UpdateDragIconSize(ShipMetaDragContext.ActiveGrid);
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
			ShipMetaDragContext.ActiveGrid = null;
			if (_dragIcon != null)
				Destroy(_dragIcon.gameObject);
			_dragIcon = null;
		}

		private void UpdateActiveGrid(PointerEventData eventData)
		{
			if (EventSystem.current == null)
				return;

			var results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, results);

			ShipGridVisual grid = null;
			for (var i = 0; i < results.Count; i++)
			{
				var go = results[i].gameObject;
				if (go == null)
					continue;

				grid = go.GetComponentInParent<ShipGridVisual>();
				if (grid != null)
					break;
			}

			ShipMetaDragContext.ActiveGrid = grid;
		}

		private void UpdateDragIconSize(ShipGridVisual grid)
		{
			if (_dragIcon == null || grid == null)
				return;

			if (!TryResolveWeaponGridSize(_item, out var w, out var h))
				return;

			_dragIcon.sizeDelta = new Vector2(w * grid.CellSize, h * grid.CellSize);
		}

		private static bool TryResolveWeaponGridSize(InventoryItem item, out int width, out int height)
		{
			width = 1;
			height = 1;

			if (item == null)
				return false;

			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var generatedPath = Path.Combine(ItemGenerator.OutputPath, item.ItemId + ".json");
				if (File.Exists(generatedPath))
				{
					var json = File.ReadAllText(generatedPath);
					var weapon = JsonUtility.FromJson<GeneratedWeaponItem>(json);
					if (weapon != null)
					{
						width = weapon.GridWidth > 0 ? weapon.GridWidth : 1;
						height = weapon.GridHeight > 0 ? weapon.GridHeight : 1;
						return true;
					}
				}
			}

			if (string.IsNullOrEmpty(item.TemplateId))
				return false;

			var templateId = item.TemplateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? item.TemplateId
				: item.TemplateId + ".json";

			var templatePath = Path.Combine(ItemGenerator.WeaponConfigsPath, templateId);
			if (!File.Exists(templatePath))
				return false;

			var templateJson = File.ReadAllText(templatePath);
			var template = JsonUtility.FromJson<WeaponTemplate>(templateJson);
			if (template == null)
				return false;

			width = template.GridWidth > 0 ? template.GridWidth : 1;
			height = template.GridHeight > 0 ? template.GridHeight : 1;
			return true;
		}
	}
}
