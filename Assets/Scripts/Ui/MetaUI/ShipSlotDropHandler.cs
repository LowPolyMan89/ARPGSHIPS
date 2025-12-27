using UnityEngine;
using UnityEngine.EventSystems;

namespace Ships
{
	/// <summary>
	/// Обработчик дропа предмета из инвентаря на конкретный якорь слота в мета-сцене.
	/// Требует Collider + PhysicsRaycaster на камере.
	/// </summary>
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
			// Можно добавить подсветку слота.
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			// Снять подсветку.
		}

		public void OnDrop(PointerEventData eventData)
		{
			var item = ShipMetaDragContext.DraggedInventoryItem;
			if (item == null || _anchor == null || _ship == null || MetaController.Instance == null)
				return;

			// Сразу спавним префаб оружия в слот.
			var weapon = WeaponBuilder.BuildMeta(item.ItemId, _anchor.MountPoint, _ship);
			if (weapon == null)
			{
				Debug.LogWarning($"[ShipSlotDropHandler] Failed to build weapon '{item.ItemId}' on slot '{_anchor.GridId}'");
				return;
			}

			// Обновляем инвентарь/сейв.
			item.EquippedOnFitId = MetaController.Instance.State.SelectedShipId;
			item.EquippedGridId = _anchor.GridId;
			item.EquippedGridX = Mathf.FloorToInt(_anchor.CellPosition.x);
			item.EquippedGridY = Mathf.FloorToInt(_anchor.CellPosition.y);
			item.EquippedGridPos = _anchor.CellPosition;
			item.EquippedGridRot = _anchor.RotationDeg;

			var fit = MetaController.Instance.State.Fit;
			if (fit != null)
			{
				fit.GridPlacements.RemoveAll(p => p != null && p.ItemId == item.ItemId);
				fit.GridPlacements.Add(new ShipFitModel.GridPlacement
				{
					GridId = _anchor.GridId,
					GridType = _anchor.GridType,
					ItemId = item.ItemId,
					X = item.EquippedGridX,
					Y = item.EquippedGridY,
					Width = 1,
					Height = 1,
					Position = _anchor.CellPosition,
					RotationDeg = _anchor.RotationDeg
				});
			}

			MetaSaveSystem.Save(MetaController.Instance.State);

			// Очистить drag контекст.
			ShipMetaDragContext.DraggedInventoryItem = null;
		}
	}
}
