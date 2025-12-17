namespace Ships
{
	using UnityEngine;
	using UnityEngine.Serialization;
	using System.Linq;

	public class BattleLoader : MonoBehaviour
	{
		[FormerlySerializedAs("PlayerTankPrefab")] public GameObject PlayerShipPrefab;

		private void Start()
		{
			LoadPlayerShipFromFit();
		}

		private void LoadPlayerShipFromFit()
		{
			var fit = MetaBattleBridge.LastFit;
			if (fit == null) return;

			var hull = HullLoader.Load(fit.SelectedShipId);
			var go = Instantiate(PlayerShipPrefab, Vector3.zero, Quaternion.identity);
			var ship = go.GetComponent<PlayerShip>();

			ship.LoadShipFromConfig(fit.SelectedShipId);
			ship.Init();

			InstallFit(ship, fit.Fit, hull);
		}

		private void InstallFit(PlayerShip ship, ShipFitModel fit, HullModel hull)
		{
			if (ship == null || fit == null)
				return;

			if (fit.GridPlacements == null || fit.GridPlacements.Count == 0)
				return;

			if (hull == null || hull.grids == null || hull.grids.Count == 0)
				return;

			foreach (var placement in fit.GridPlacements)
			{
				if (placement == null ||
				    placement.GridType != ShipGridType.WeaponGrid ||
				    string.IsNullOrEmpty(placement.ItemId))
					continue;

				var grid = hull.grids.FirstOrDefault(g => g != null && g.id == placement.GridId);
				if (grid == null || grid.type != ShipGridType.WeaponGrid)
					continue;

				var mount = CreateGridMount(ship.transform, hull, grid, placement);
				WeaponBuilder.Build(placement.ItemId, mount, ship);
			}
		}

		private static Transform CreateGridMount(Transform shipRoot, HullModel hull, HullGridModel grid, ShipFitModel.GridPlacement placement)
		{
			var mountGo = new GameObject($"GridMount_{grid.id}_{placement.X}_{placement.Y}");
			var mount = mountGo.transform;
			mount.SetParent(shipRoot, worldPositionStays: false);

			var cell = hull != null && hull.gridCellSize > 0f ? hull.gridCellSize : 0.25f;
			var center = new Vector2(
				(placement.X + placement.Width * 0.5f) * cell,
				(placement.Y + placement.Height * 0.5f) * cell
			);

			var rot = Quaternion.Euler(0f, 0f, grid.rotationDeg);
			var rotated = rot * new Vector3(center.x, center.y, 0f);

			mount.localPosition = new Vector3(grid.origin.x + rotated.x, grid.origin.y + rotated.y, 0f);
			mount.localRotation = rot;
			mount.localScale = Vector3.one;
			return mount;
		}

	}

}
