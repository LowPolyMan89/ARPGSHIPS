using System;
using System.Collections.Generic;

namespace Ships
{
	public static class ShipInventoryUtils
	{
		public static string ResolveShipId(InventoryShip ship)
		{
			if (ship == null)
				return null;

			if (!string.IsNullOrEmpty(ship.ShipId))
				return NormalizeShipId(ship.ShipId);

			if (!string.IsNullOrEmpty(ship.TemplateId))
				return NormalizeShipId(ship.TemplateId);

			return null;
		}

		public static string NormalizeShipId(string shipId)
		{
			if (string.IsNullOrEmpty(shipId))
				return shipId;

			return shipId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? shipId.Substring(0, shipId.Length - ".json".Length)
				: shipId;
		}

		public static bool ContainsShip(MetaState state, string shipId)
		{
			if (state?.ShipInventory == null || string.IsNullOrEmpty(shipId))
				return false;

			var normalized = NormalizeShipId(shipId);
			for (var i = 0; i < state.ShipInventory.Count; i++)
			{
				var entry = state.ShipInventory[i];
				var id = ResolveShipId(entry);
				if (!string.IsNullOrEmpty(id) &&
				    string.Equals(id, normalized, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		public static bool AddShip(MetaState state, string shipId)
		{
			if (state == null || string.IsNullOrEmpty(shipId))
				return false;

			shipId = NormalizeShipId(shipId);
			if (string.IsNullOrEmpty(shipId))
				return false;

			if (state.ShipInventory == null)
				state.ShipInventory = new List<InventoryShip>();

			if (ContainsShip(state, shipId))
				return false;

			state.ShipInventory.Add(new InventoryShip
			{
				ShipId = shipId,
				TemplateId = shipId
			});

			return true;
		}

		public static bool RemoveShip(MetaState state, string shipId)
		{
			if (state?.ShipInventory == null || string.IsNullOrEmpty(shipId))
				return false;

			shipId = NormalizeShipId(shipId);
			for (var i = 0; i < state.ShipInventory.Count; i++)
			{
				var entry = state.ShipInventory[i];
				var id = ResolveShipId(entry);
				if (!string.IsNullOrEmpty(id) &&
				    string.Equals(id, shipId, StringComparison.OrdinalIgnoreCase))
				{
					state.ShipInventory.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		public static void EnsureInventory(MetaState state)
		{
			if (state == null)
				return;

			if (state.ShipInventory == null)
				state.ShipInventory = new List<InventoryShip>();

			var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (state.BattleShipSlots != null)
			{
				for (var i = 0; i < state.BattleShipSlots.Count; i++)
				{
					var id = NormalizeShipId(state.BattleShipSlots[i]);
					if (!string.IsNullOrEmpty(id))
						assigned.Add(id);
				}
			}

			var normalizedInventory = new List<InventoryShip>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (state.ShipInventory != null)
			{
				for (var i = 0; i < state.ShipInventory.Count; i++)
				{
					var ship = state.ShipInventory[i];
					var id = ResolveShipId(ship);
					if (string.IsNullOrEmpty(id))
						continue;
					if (assigned.Contains(id))
						continue;
					if (!seen.Add(id))
						continue;

					normalizedInventory.Add(new InventoryShip
					{
						ShipId = id,
						TemplateId = ship?.TemplateId
					});
				}
			}

			var shouldFillFromFits = normalizedInventory.Count == 0;
			if (shouldFillFromFits && state.PlayerShipFits != null)
			{
				for (var i = 0; i < state.PlayerShipFits.Count; i++)
				{
					var fit = state.PlayerShipFits[i];
					if (fit == null || string.IsNullOrEmpty(fit.ShipId))
						continue;

					var id = NormalizeShipId(fit.ShipId);
					if (string.IsNullOrEmpty(id))
						continue;
					if (assigned.Contains(id))
						continue;
					if (!seen.Add(id))
						continue;

					normalizedInventory.Add(new InventoryShip
					{
						ShipId = id,
						TemplateId = id
					});
				}
			}

			state.ShipInventory = normalizedInventory;
		}
	}
}
