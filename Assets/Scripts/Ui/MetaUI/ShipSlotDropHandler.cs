using UnityEngine;
using UnityEngine.EventSystems;

namespace Ships
{
	public class ShipSlotDropHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
	{
		[SerializeField] private ShipSlotAnchor _anchor;
		[SerializeField] private PlayerShip _ship;

		private void Reset()
		{
			if (_anchor == null)
				_anchor = GetComponent<ShipSlotAnchor>();
			if (_ship == null)
				_ship = GetComponentInParent<PlayerShip>();
		}

		private void Awake()
		{
			if (_anchor == null)
				_anchor = GetComponent<ShipSlotAnchor>();
			if (_ship == null)
				_ship = GetComponentInParent<PlayerShip>();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
		}

		public void OnPointerExit(PointerEventData eventData)
		{
		}

		public void OnDrop(PointerEventData eventData)
		{
			var item = ShipMetaDragContext.DraggedInventoryItem;
			if (item == null || _anchor == null || _ship == null || MetaController.Instance == null)
				return;

			var itemId = InventoryUtils.ResolveItemId(item);
			if (string.IsNullOrEmpty(itemId))
				return;

			var state = MetaController.Instance.State;
			if (!InventoryUtils.TryConsume(state.InventoryModel, itemId, 1))
				return;

			var weapon = WeaponBuilder.BuildMeta(itemId, _anchor.MountPoint, _ship, item);
			if (weapon == null)
			{
				InventoryUtils.ReturnToInventory(state.InventoryModel, itemId, 1);
				Debug.LogWarning($"[ShipSlotDropHandler] Failed to build weapon '{itemId}' on slot '{_anchor.GridId}'");
				return;
			}

			var fit = state.Fit;
			if (fit != null)
			{
				var existing = fit.GridPlacements.Find(p => p != null && p.GridId == _anchor.GridId);
				if (existing != null && !string.IsNullOrEmpty(existing.ItemId))
					InventoryUtils.ReturnToInventory(state.InventoryModel, existing.ItemId, 1);

				fit.GridPlacements.RemoveAll(p => p != null && p.GridId == _anchor.GridId);
				fit.GridPlacements.Add(new ShipFitModel.GridPlacement
				{
					GridId = _anchor.GridId,
					GridType = _anchor.GridType,
					ItemId = itemId,
					X = Mathf.FloorToInt(_anchor.CellPosition.x),
					Y = Mathf.FloorToInt(_anchor.CellPosition.y),
					Width = 1,
					Height = 1,
					Position = _anchor.CellPosition,
					RotationDeg = _anchor.RotationDeg
				});
			}

			MetaSaveSystem.Save(state);
			GameEvent.InventoryUpdated(state.InventoryModel);

			ShipMetaDragContext.DraggedInventoryItem = null;
		}
	}
}
