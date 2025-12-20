using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class ShipGridPlacedItemVisual : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[Header("UI")]
		public Image Icon;

		private InventoryItem _item;
		private ShipFitModel.GridPlacement _placement;
		private ShipGridVisual _grid;

		public void Init(ShipFitModel.GridPlacement placement, ShipGridVisual grid)
		{
			_placement = placement;
			_grid = grid;

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
				var sprite = ResourceLoader.LoadItemIcon(_item);
				Icon.sprite = sprite;
				Icon.enabled = sprite != null;
				Icon.preserveAspect = true;
			}
		}

		// Right click removes item (temporary UX until full drag-from-grid is implemented)
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Right)
				return;

			var state = MetaController.Instance != null ? MetaController.Instance.State : null;
			if (state == null)
				return;

			state.Fit.GridPlacements.RemoveAll(p => p.ItemId == _placement.ItemId);
			var invItem = InventoryUtils.FindByItemId(state.InventoryModel, _placement.ItemId);
			if (invItem != null)
			{
				invItem.EquippedOnFitId = null;
				invItem.EquippedGridId = null;
				invItem.EquippedGridX = -1;
				invItem.EquippedGridY = -1;
			}

			MetaSaveSystem.Save(state);
			GameEvent.InventoryUpdated(state.InventoryModel);
			_grid.Refresh();
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
	}
}
