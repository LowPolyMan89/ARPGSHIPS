using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class ShipSelectionPanel : MonoBehaviour
	{
		public List<ShipSelectionPanelElement> Elements = new List<ShipSelectionPanelElement>();

		private MetaState _state;

		private void Start()
		{
			_state = MetaController.Instance != null ? MetaController.Instance.State : null;
			EnsureSlots();
			ShipInventoryUtils.EnsureInventory(_state);
			InitElements();
		}

		private void EnsureSlots()
		{
			if (_state == null)
				return;

			if (_state.BattleShipSlots == null)
				_state.BattleShipSlots = new List<string>();

			while (_state.BattleShipSlots.Count < Elements.Count)
				_state.BattleShipSlots.Add(string.Empty);
		}

		private void InitElements()
		{
			for (var i = 0; i < Elements.Count; i++)
			{
				var element = Elements[i];
				if (element != null)
					element.Init(this, i);
			}
		}

		public string GetSlotShipId(int slotIndex)
		{
			if (_state == null || _state.BattleShipSlots == null)
				return null;
			if (slotIndex < 0 || slotIndex >= _state.BattleShipSlots.Count)
				return null;

			return _state.BattleShipSlots[slotIndex];
		}

		public bool SetSlotShipId(int slotIndex, string shipId)
		{
			return TryAssignShipById(slotIndex, shipId, false, false);
		}

		public void ClearSlot(int slotIndex)
		{
			TryClearSlot(slotIndex, false);
		}

		public bool TryAssignShipFromInventory(int slotIndex, InventoryShip ship, bool isFlagshipSlot)
		{
			var shipId = ShipInventoryUtils.ResolveShipId(ship);
			if (string.IsNullOrEmpty(shipId))
				return false;

			if (!ShipInventoryUtils.ContainsShip(_state, shipId))
				return false;

			return TryAssignShipById(slotIndex, shipId, isFlagshipSlot, true);
		}

		public bool TryClearSlot(int slotIndex, bool isFlagshipSlot)
		{
			if (_state == null || _state.BattleShipSlots == null)
				return false;
			if (slotIndex < 0 || slotIndex >= _state.BattleShipSlots.Count)
				return false;
			if (isFlagshipSlot)
				return false;

			var removedId = _state.BattleShipSlots[slotIndex];
			if (string.IsNullOrEmpty(removedId))
				return false;

			_state.BattleShipSlots[slotIndex] = string.Empty;
			UnequipShipFit(removedId);
			ShipInventoryUtils.AddShip(_state, removedId);
			MetaSaveSystem.Save(_state);

			if (!string.IsNullOrEmpty(removedId) &&
			    string.Equals(_state.SelectedShipId, removedId, System.StringComparison.OrdinalIgnoreCase))
				SelectFallbackShip();

			RefreshAll();
			GameEvent.ShipInventoryUpdated(_state.ShipInventory);
			return true;
		}

		private void UnequipShipFit(string shipId)
		{
			if (string.IsNullOrEmpty(shipId) || _state?.PlayerShipFits == null || _state.InventoryModel == null)
				return;

			var fit = _state.PlayerShipFits.Find(f =>
				f != null && !string.IsNullOrEmpty(f.ShipId) &&
				f.ShipId.Equals(shipId, System.StringComparison.OrdinalIgnoreCase));
			if (fit?.GridPlacements == null || fit.GridPlacements.Count == 0)
				return;

			for (var i = 0; i < fit.GridPlacements.Count; i++)
			{
				var placement = fit.GridPlacements[i];
				if (placement == null || string.IsNullOrEmpty(placement.ItemId))
					continue;

				InventoryUtils.ReturnToInventory(_state.InventoryModel, placement.ItemId, 1);
			}

			fit.GridPlacements.Clear();
			InventoryUtils.RebuildEquippedCounts(_state);
			GameEvent.InventoryUpdated(_state.InventoryModel);
		}

		public void SelectSlot(int slotIndex)
		{
			var shipId = GetSlotShipId(slotIndex);
			if (string.IsNullOrEmpty(shipId))
				return;

			MetaController.Instance?.SetActiveShip(shipId);
			RefreshAll();
		}

		private void SelectFallbackShip()
		{
			var fallback = GetSlotShipId(0);
			if (!string.IsNullOrEmpty(fallback))
				MetaController.Instance?.SetActiveShip(fallback);
		}

		public List<string> GetAvailableShipIds(int slotIndex, bool flagshipOnly)
		{
			var result = new List<string>();
			if (_state == null || _state.ShipInventory == null)
				return result;

			for (var i = 0; i < _state.ShipInventory.Count; i++)
			{
				var ship = _state.ShipInventory[i];
				var id = ShipInventoryUtils.ResolveShipId(ship);
				if (string.IsNullOrEmpty(id))
					continue;
				if (flagshipOnly && !IsFlagship(id))
					continue;

				result.Add(id);
			}

			return result;
		}

		public Sprite LoadShipIcon(string shipId)
		{
			return ResourceLoader.LoadShipIcon(shipId);
		}

		private bool IsFlagship(string shipId)
		{
			var hull = HullLoader.Load(shipId);
			if (hull == null || string.IsNullOrEmpty(hull.shipClass))
				return false;

			return hull.shipClass.Equals("Flagship", System.StringComparison.OrdinalIgnoreCase);
		}

		private void RefreshAll()
		{
			for (var i = 0; i < Elements.Count; i++)
			{
				var element = Elements[i];
				if (element != null)
					element.Refresh();
			}
		}

		private bool TryAssignShipById(int slotIndex, string shipId, bool isFlagshipSlot, bool removeFromInventory)
		{
			if (_state == null || _state.BattleShipSlots == null || string.IsNullOrEmpty(shipId))
				return false;
			if (slotIndex < 0 || slotIndex >= _state.BattleShipSlots.Count)
				return false;

			shipId = ShipInventoryUtils.NormalizeShipId(shipId);
			if (string.IsNullOrEmpty(shipId))
				return false;

			if (isFlagshipSlot && !IsFlagship(shipId))
				return false;

			for (var i = 0; i < _state.BattleShipSlots.Count; i++)
			{
				if (i == slotIndex)
					continue;
				if (string.Equals(_state.BattleShipSlots[i], shipId, System.StringComparison.OrdinalIgnoreCase))
					return false;
			}

			var existingId = _state.BattleShipSlots[slotIndex];
			if (string.Equals(existingId, shipId, System.StringComparison.OrdinalIgnoreCase))
				return false;

			if (!string.IsNullOrEmpty(existingId))
			{
				UnequipShipFit(existingId);
				ShipInventoryUtils.AddShip(_state, existingId);
			}

			if (removeFromInventory && !ShipInventoryUtils.RemoveShip(_state, shipId))
				return false;

			_state.BattleShipSlots[slotIndex] = shipId;

			MetaController.Instance?.SetActiveShip(shipId);

			MetaSaveSystem.Save(_state);
			RefreshAll();
			GameEvent.ShipInventoryUpdated(_state.ShipInventory);
			return true;
		}
	}
}
