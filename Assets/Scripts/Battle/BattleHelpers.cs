using System.Collections.Generic;
using Ships;
using UnityEngine;

namespace Ships
{
	public static class BattleHelpers
	{
		private static readonly Collider[] OverlapBuffer = new Collider[16];
		public static List<Squad> ShipSquads = new List<Squad>();
		
		public static ShipBase FindShipInSphere(
			Vector3 origin,
			float radius,
			LayerMask layerMask)
		{
			int count = Physics.OverlapSphereNonAlloc(
				origin,
				radius,
				OverlapBuffer,
				layerMask);

			for (int i = 0; i < count; i++)
			{
				if (OverlapBuffer[i].TryGetComponent(out ShipBase ship))
					return ship;
			}

			return null;
		}

		public static ShipBase GetClosestPlayerShip(
			Vector3 fromPosition,
			float maxRange)
		{
			var battle = Battle.Instance;
			if (battle == null)
				return null;

			var pShips = battle.PlayerShips;
			if (pShips == null || pShips.Count == 0)
				return null;

			float maxRangeSqr = maxRange * maxRange;
			float bestDistSqr = maxRangeSqr;

			ShipBase bestShip = null;

			for (int i = 0; i < pShips.Count; i++)
			{
				var ship = pShips[i];
				if (ship == null || !ship.isActiveAndEnabled)
					continue;

				float distSqr =
					(ship.transform.position - fromPosition).sqrMagnitude;

				if (distSqr > maxRangeSqr)
					continue;

				if (distSqr < bestDistSqr)
				{
					bestDistSqr = distSqr;
					bestShip = ship;
				}
			}

			return bestShip;
		}

		//получение случайной позиции вокруг цели
		public static Vector3 GetRandomPositionInCircleXZ(Vector3 origin, float radius)
		{
			var offset = Random.insideUnitCircle * radius;
			return new Vector3(origin.x + offset.x, 0f, origin.z + offset.y);
		}
		//получение случайной позиции внутри боя
		public static Vector3 GetRandomPositionInBoundXZ(Bounds bounds)
		{
			return new Vector3(
				Random.Range(bounds.min.x, bounds.max.x),
				0f,
				Random.Range(bounds.min.z, bounds.max.z));
		}
		
		//получение ближайшего корабля в команде
		public static ShipBase GetClosestShip(float range, TeamMask teamMask)
		{
			if (range <= 0f)
				return null;

			var battle = Battle.Instance;
			if (battle == null)
				return null;

			var ships = battle.AllShips;
			if (ships == null || ships.Count == 0)
				return null;

			var origins = battle.PlayerShips;
			if (origins == null || origins.Count == 0)
				return null;

			var best = (ShipBase)null;
			var bestDistSqr = range * range;

			for (var i = 0; i < ships.Count; i++)
			{
				var ship = ships[i];
				if (ship == null || !ship.isActiveAndEnabled || !ship.IsAlive)
					continue;
				if ((ship.Team & teamMask) == 0)
					continue;

				var distSqr = GetClosestDistanceSqrToPlayers(ship.transform.position, origins, bestDistSqr);
				if (distSqr < bestDistSqr)
				{
					bestDistSqr = distSqr;
					best = ship;
				}
			}

			return best;
		}
		
		//получение ближайшего c низким здоровьем в команде
		public static ShipBase GetClosestDamagedShip(float range, TeamMask teamMask, float hpAmount)
		{
			if (range <= 0f)
				return null;

			var battle = Battle.Instance;
			if (battle == null)
				return null;

			var ships = battle.AllShips;
			if (ships == null || ships.Count == 0)
				return null;

			var origins = battle.PlayerShips;
			if (origins == null || origins.Count == 0)
				return null;

			var best = (ShipBase)null;
			var bestDistSqr = range * range;

			for (var i = 0; i < ships.Count; i++)
			{
				var ship = ships[i];
				if (ship == null || !ship.isActiveAndEnabled || !ship.IsAlive)
					continue;
				if ((ship.Team & teamMask) == 0)
					continue;

				if (!ship.TryGetStat(StatType.HitPoint, out var hp) || hp == null)
					continue;

				var maxHp = hp.Maximum;
				if (maxHp <= 0f)
					continue;

				var currentHp = hp.Current;
				if (hpAmount <= 1f)
				{
					if (currentHp / maxHp > hpAmount)
						continue;
				}
				else if (currentHp > hpAmount)
				{
					continue;
				}

				var distSqr = GetClosestDistanceSqrToPlayers(ship.transform.position, origins, bestDistSqr);
				if (distSqr < bestDistSqr)
				{
					bestDistSqr = distSqr;
					best = ship;
				}
			}

			return best;
		}
		
		//получение ближайшего корабля класса в команде
		public static ShipBase GetClosestShipByClass(float range, TeamMask teamMask, ShipClass shipClass)
		{
			if (range <= 0f)
				return null;

			var battle = Battle.Instance;
			if (battle == null)
				return null;

			var ships = battle.AllShips;
			if (ships == null || ships.Count == 0)
				return null;

			var origins = battle.PlayerShips;
			if (origins == null || origins.Count == 0)
				return null;

			var best = (ShipBase)null;
			var bestDistSqr = range * range;

			for (var i = 0; i < ships.Count; i++)
			{
				var ship = ships[i];
				if (ship == null || !ship.isActiveAndEnabled || !ship.IsAlive)
					continue;
				if (ship.Class != shipClass)
					continue;
				if ((ship.Team & teamMask) == 0)
					continue;

				var distSqr = GetClosestDistanceSqrToPlayers(ship.transform.position, origins, bestDistSqr);
				if (distSqr < bestDistSqr)
				{
					bestDistSqr = distSqr;
					best = ship;
				}
			}

			return best;
		}

		//сформировать отряд кораблей
		public static void FormShipSquad(List<ShipBase> ships)
		{
			ShipSquads.Clear();
			if (ships == null || ships.Count == 0)
				return;

			var squad = new Squad();
			for (var i = 0; i < ships.Count; i++)
			{
				var ship = ships[i];
				if (ship == null || squad.ShipsInSquad.Contains(ship))
					continue;
				squad.ShipsInSquad.Add(ship);
			}

			if (squad.ShipsInSquad.Count > 0)
				ShipSquads.Add(squad);
		}

		private static float GetClosestDistanceSqrToPlayers(
			Vector3 position,
			List<ShipBase> playerShips,
			float maxRangeSqr)
		{
			var best = maxRangeSqr;
			for (var i = 0; i < playerShips.Count; i++)
			{
				var playerShip = playerShips[i];
				if (playerShip == null || !playerShip.isActiveAndEnabled || !playerShip.IsAlive)
					continue;

				var distSqr = (playerShip.transform.position - position).sqrMagnitude;
				if (distSqr < best)
					best = distSqr;
			}

			return best;
		}
		
	}

	public class Squad
	{
		public List<ShipBase> ShipsInSquad = new List<ShipBase>();
	}
}
