using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class ShipGridVisual : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[Header("Grid")]
		public string GridId = "weapon_left";
		public ShipGridType GridType = ShipGridType.WeaponGrid;
		public int Width = 4;
		public int Height = 3;
		public float CellSize = 32f;

		[Header("UI")]
		public RectTransform GridRoot;
		public Image CellPrefab;
		public ShipGridPlacedItemVisual PlacedItemPrefab;

		private ShipFitView _view;
		private readonly List<Image> _cells = new();
		private readonly List<ShipGridPlacedItemVisual> _placed = new();

		public void Init(ShipFitView view)
		{
			_view = view;
			RebuildGrid();

			if (_view != null)
			{
				_view.OnFitChanged -= Refresh;
				_view.OnFitChanged += Refresh;
			}

			Refresh();
		}

		private void OnDestroy()
		{
			if (_view != null)
				_view.OnFitChanged -= Refresh;
		}

		public void RebuildGrid()
		{
			if (!GridRoot)
				GridRoot = (RectTransform)transform;

			// Ensure there's a raycastable rect covering the whole grid so drop/snapping works everywhere.
			var bg = GridRoot.GetComponent<Image>();
			if (bg == null)
				bg = GridRoot.gameObject.AddComponent<Image>();
			bg.color = new Color(1f, 1f, 1f, 0f);
			bg.raycastTarget = true;

			foreach (var c in _cells)
				if (c) Destroy(c.gameObject);
			_cells.Clear();

			GridRoot.sizeDelta = new Vector2(Width * CellSize, Height * CellSize);

			if (!CellPrefab)
				return;

			for (var y = 0; y < Height; y++)
			{
				for (var x = 0; x < Width; x++)
				{
					var cell = Instantiate(CellPrefab, GridRoot);
					// Cells should not block drop/pointer events; the grid is the drop target.
					cell.raycastTarget = false;
					var rt = (RectTransform)cell.transform;
					rt.anchorMin = new Vector2(0, 0);
					rt.anchorMax = new Vector2(0, 0);
					rt.pivot = new Vector2(0, 0);
					rt.anchoredPosition = new Vector2(x * CellSize, y * CellSize);
					rt.sizeDelta = new Vector2(CellSize, CellSize);
					_cells.Add(cell);
				}
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (ShipMetaDragContext.DraggedInventoryItem != null)
				ShipMetaDragContext.ActiveGrid = this;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (ShipMetaDragContext.ActiveGrid == this)
				ShipMetaDragContext.ActiveGrid = null;
		}

		public bool TryGetSnappedDragPosition(PointerEventData eventData, out Vector2 screenPosition, out int x, out int y)
		{
			screenPosition = default;
			x = -1;
			y = -1;

			if (GridRoot == null)
				return false;

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
				    GridRoot,
				    eventData.position,
				    eventData.pressEventCamera,
				    out var local))
				return false;

			var pivotOffset = new Vector2(GridRoot.rect.width * GridRoot.pivot.x, GridRoot.rect.height * GridRoot.pivot.y);
			var point = local + pivotOffset;

			x = Mathf.FloorToInt(point.x / CellSize);
			y = Mathf.FloorToInt(point.y / CellSize);

			x = Mathf.Clamp(x, 0, Mathf.Max(0, Width - 1));
			y = Mathf.Clamp(y, 0, Mathf.Max(0, Height - 1));

			// Snap to bottom-left of the chosen cell (drag icon uses bottom-left pivot).
			var snappedBottomLeft = new Vector2(x * CellSize, y * CellSize);
			var snappedLocal = snappedBottomLeft - pivotOffset;
			var world = GridRoot.TransformPoint(snappedLocal);
			screenPosition = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, world);
			return true;
		}

		public void Refresh()
		{
			if (_view == null || PlacedItemPrefab == null || GridRoot == null)
				return;

			for (var i = 0; i < _placed.Count; i++)
				if (_placed[i]) Destroy(_placed[i].gameObject);
			_placed.Clear();

			var fit = MetaController.Instance != null ? MetaController.Instance.State.Fit : null;
			if (fit == null || fit.GridPlacements == null)
				return;

			foreach (var p in fit.GridPlacements)
			{
				if (p.GridId != GridId)
					continue;

				var vis = Instantiate(PlacedItemPrefab, GridRoot);
				vis.Init(p, this);
				_placed.Add(vis);
			}
		}

		public void OnDrop(PointerEventData eventData)
		{
			if (_view == null)
				return;

			var item = ShipMetaDragContext.DraggedInventoryItem;
			if (item == null)
				return;

			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
				    GridRoot,
				    eventData.position,
				    eventData.pressEventCamera,
				    out var local))
				return;

			// ScreenPointToLocalPointInRectangle gives local coords relative to GridRoot pivot.
			// Our grid placement uses bottom-left origin, so convert to bottom-left space.
			var pivotOffset = new Vector2(GridRoot.rect.width * GridRoot.pivot.x, GridRoot.rect.height * GridRoot.pivot.y);
			var point = local + pivotOffset;

			var x = Mathf.FloorToInt(point.x / CellSize);
			var y = Mathf.FloorToInt(point.y / CellSize);

			var placed = _view.TryPlaceWeaponToGrid(GridId, Width, Height, x, y, item);
			if (!placed)
				Debug.Log($"[ShipGridVisual] Can't place item '{item.ItemId}' to grid '{GridId}' at ({x},{y})");
		}
	}
}
