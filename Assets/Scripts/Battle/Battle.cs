using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Ships
{
	public class Battle : MonoBehaviour
	{
		public static Battle Instance;

		[Header("Battlefield Bounds (3D)")]
		public Vector3 MinBounds = new Vector3(-500, 0, -500);
		public Vector3 MaxBounds = new Vector3(500, 10, 500);

		[Header("Runtime")]
		public PlayerShip Player;
		public BattleCamera CameraController;
		public List<ShipBase> AllShips = new();
		public List<ShipBase> PlayerShips = new();
		public List<ShipBase> SelectedShips = new();
		public List<Transform> PlayerSpawnPositions;

		private void Awake()
		{
			if (Instance)
				Destroy(gameObject);
			else
				Instance = this;

			if (GetComponent<BattleSelectionController>() == null)
				gameObject.AddComponent<BattleSelectionController>();
		}

		private void Start()
		{
			RegisterInitialShips();
		}

		private void RegisterInitialShips()
		{
			var ships = FindObjectsByType<ShipBase>(sortMode: FindObjectsSortMode.None);
			for (var i = 0; i < ships.Length; i++)
				RegisterShip(ships[i]);
		}

		public void RegisterShip(ShipBase ship)
		{
			if (ship == null)
				return;

			if (!AllShips.Contains(ship))
				AllShips.Add(ship);

			if (ship.SideType != SideType.Player &&
			    ship.GetComponent<AiShipBrain>() == null &&
			    ship.GetComponent<EnemyNavAgentDriver>() != null)
				ship.gameObject.AddComponent<AiShipBrain>();
			else
				PlayerShips.Add(ship);
		}

		public void UnregisterShip(ShipBase ship)
		{
			if (ship == null)
				return;

			AllShips.Remove(ship);
			SelectedShips.Remove(ship);
		}

		public bool IsShipSelected(ShipBase ship)
		{
			return ship != null && SelectedShips.Contains(ship);
		}

		public void SetSelection(List<ShipBase> ships)
		{
			SelectedShips.Clear();
			if (ships != null && ships.Count > 0)
			{
				var unique = new HashSet<ShipBase>();
				for (var i = 0; i < ships.Count; i++)
				{
					var ship = ships[i];
					if (ship == null || !unique.Add(ship))
						continue;
					SelectedShips.Add(ship);
				}
			}

			for (var i = 0; i < AllShips.Count; i++)
			{
				var s = AllShips[i];
				if (s == null)
					continue;

				s.IsSelected = SelectedShips.Contains(s);
			}

			GameEvent.SelectionChanged(new List<ShipBase>(SelectedShips));
		}

		/// <summary>
		/// Ограничивает позицию в 3D-объёме.
		/// </summary>
		public Vector3 ClampPosition(Vector3 pos)
		{
			return new Vector3(
				Mathf.Clamp(pos.x, MinBounds.x, MaxBounds.x),
				Mathf.Clamp(pos.y, MinBounds.y, MaxBounds.y),
				Mathf.Clamp(pos.z, MinBounds.z, MaxBounds.z)
			);
		}

		/// <summary>
		/// Проверяет, находится ли точка внутри объёма.
		/// </summary>
		public bool IsInside(Vector3 pos)
		{
			return pos.x >= MinBounds.x && pos.x <= MaxBounds.x &&
			       pos.y >= MinBounds.y && pos.y <= MaxBounds.y &&
			       pos.z >= MinBounds.z && pos.z <= MaxBounds.z;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;

			Vector3 center = (MinBounds + MaxBounds) * 0.5f;
			Vector3 size = MaxBounds - MinBounds;

			Gizmos.DrawWireCube(center, size);
		}
#endif
	}
}
