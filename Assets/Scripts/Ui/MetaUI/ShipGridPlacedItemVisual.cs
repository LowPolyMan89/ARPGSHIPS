using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class ShipGridPlacedItemVisual : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler,
		IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		[Header("UI")]
		public Image Icon;

		private InventoryItem _item;
		private ShipFitModel.GridPlacement _placement;
		private ShipGridVisual _grid;
		private RectTransform _dragIcon;
		private Canvas _canvas;
		[SerializeField] private ShipGridArcRenderer _arcRenderer;
		[SerializeField] private TMP_Text _energyCostText;
		public void Init(ShipFitModel.GridPlacement placement, ShipGridVisual grid)
		{
			_placement = placement;
			_grid = grid;
			if (_arcRenderer == null)
				_arcRenderer = GetComponentInChildren<ShipGridArcRenderer>(true);

			var rt = (RectTransform)transform;
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(0, 0);
			rt.pivot = new Vector2(0, 0);
			rt.anchoredPosition = new Vector2(placement.X * grid.CellSize, placement.Y * grid.CellSize);
			rt.sizeDelta = new Vector2(placement.Width * grid.CellSize, placement.Height * grid.CellSize);

			if (MetaController.Instance != null && MetaController.Instance.State != null)
				_item = InventoryUtils.FindByItemId(MetaController.Instance.State.InventoryModel, placement.ItemId);

			if (Icon != null)
			{
				var sprite = ResourceLoader.LoadItemIcon(_item, ItemIconContext.Fit);
				Icon.sprite = sprite;
				Icon.enabled = sprite != null;
				Icon.preserveAspect = true;
			}

			if (_arcRenderer != null)
				_arcRenderer.RenderArc(_item, _grid, _placement);

			ApplyEnergyCost();
		}

		// Right click removes item (temporary UX until full drag-from-grid is implemented)
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Right)
				return;

			UnequipAndRefresh();
		}

		// If this visual blocks raycasts, make sure drop still reaches the grid.
		public void OnDrop(PointerEventData eventData)
		{
			if (_grid != null)
				_grid.OnDrop(eventData);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (ShipMetaDragContext.DraggedInventoryItem != null)
				ShipMetaDragContext.ActiveGrid = _grid;

			if (_item != null && MetaController.Instance != null)
				MetaController.Instance.MetaVisual.ShowItemInfoWindow(_item, eventData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (ShipMetaDragContext.ActiveGrid == _grid)
				ShipMetaDragContext.ActiveGrid = null;

			if (MetaController.Instance != null)
				MetaController.Instance.MetaVisual.HideItemInfoWindow();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (_item == null || _grid == null)
				return;

			ShipMetaDragContext.DraggedInventoryItem = _item;
			ShipMetaDragContext.ActiveGrid = _grid;
			ShipMetaDragContext.DraggingFromFit = true;
			ShipMetaDragContext.DraggedPlacement = ClonePlacement(_placement);

			_canvas = _canvas ?? GetComponentInParent<Canvas>();
			if (_canvas == null || Icon == null)
				return;

			if (Icon.enabled)
				Icon.enabled = false; // hide original while dragging

			var go = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
			go.transform.SetParent(_canvas.transform, false);
			_dragIcon = (RectTransform)go.transform;
			_dragIcon.anchorMin = Vector2.zero;
			_dragIcon.anchorMax = Vector2.zero;
			_dragIcon.pivot = Vector2.zero;
			_dragIcon.sizeDelta = new Vector2(_placement.Width * _grid.CellSize, _placement.Height * _grid.CellSize);
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
			UpdateActiveGrid(eventData);

			var dropGrid = ShipMetaDragContext.ActiveGrid;
			var dropSucceeded = false;

			if (dropGrid != null &&
			    dropGrid.TryGetSnappedDragPosition(eventData, out _, out var x, out var y) &&
			    MetaController.Instance != null)
			{
				dropSucceeded = MetaController.Instance.ShipFitView.TryPlaceWeaponToGrid(
					dropGrid.GridId,
					dropGrid.Width,
					dropGrid.Height,
					x,
					y,
					_item);
			}

			if (!dropSucceeded)
			{
				if (dropGrid == null)
				{
					// Dropped outside any grid -> unequip.
					UnequipAndRefresh();
				}
				else
				{
					RestoreOriginalPlacement();
				}
			}

			if (_dragIcon != null)
				Destroy(_dragIcon.gameObject);
			_dragIcon = null;

			if (Icon != null)
				Icon.enabled = true;

			ShipMetaDragContext.DraggedInventoryItem = null;
			ShipMetaDragContext.ActiveGrid = null;
			ShipMetaDragContext.DraggingFromFit = false;
			ShipMetaDragContext.DraggedPlacement = null;
		}

		private void OnDisable()
		{
			if (_dragIcon != null)
			{
				Destroy(_dragIcon.gameObject);
				_dragIcon = null;
			}

			if (ShipMetaDragContext.DraggingFromFit && ShipMetaDragContext.DraggedInventoryItem == _item)
			{
				ShipMetaDragContext.DraggedInventoryItem = null;
				ShipMetaDragContext.ActiveGrid = null;
				ShipMetaDragContext.DraggingFromFit = false;
				ShipMetaDragContext.DraggedPlacement = null;
			}
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

		private void UnequipAndRefresh()
		{
			if (MetaController.Instance == null || _item == null)
				return;

			MetaController.Instance.ShipFitView.UnequipItem(_item);
		}

		private void RestoreOriginalPlacement()
		{
			if (MetaController.Instance == null || ShipMetaDragContext.DraggedPlacement == null)
			{
				_grid.Refresh();
				return;
			}

			var origin = ShipMetaDragContext.DraggedPlacement;
			MetaController.Instance.ShipFitView.TryPlaceWeaponToGrid(
				origin.GridId,
				_grid.Width,
				_grid.Height,
				origin.X,
				origin.Y,
				_item);
		}

		private void UpdateDragIconSize(ShipGridVisual grid)
		{
			if (_dragIcon == null || grid == null || _placement == null)
				return;

			_dragIcon.sizeDelta = new Vector2(_placement.Width * grid.CellSize, _placement.Height * grid.CellSize);
		}

		private static ShipFitModel.GridPlacement ClonePlacement(ShipFitModel.GridPlacement src)
		{
			if (src == null)
				return null;

			return new ShipFitModel.GridPlacement
			{
				GridId = src.GridId,
				GridType = src.GridType,
				ItemId = src.ItemId,
				X = src.X,
				Y = src.Y,
				Width = src.Width,
				Height = src.Height
			};
		}

		private void ApplyEnergyCost()
		{
			if (_energyCostText == null)
				return;

			var cost = EnergyCostResolver.ResolveEnergyCost(_item);
			if (Mathf.Abs(cost) < 0.001f)
			{
				_energyCostText.gameObject.SetActive(false);
				return;
			}

			_energyCostText.gameObject.SetActive(true);
			_energyCostText.text = Mathf.RoundToInt(cost).ToString();
		}
	}
}
