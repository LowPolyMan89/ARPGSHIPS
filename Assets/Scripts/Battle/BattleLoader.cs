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
			StatEffectApplier.ApplyAll(ship.ShipStats, fit.MainStatEffects, StatModifierSource.Main, fit);

			InstallFit(ship, fit.Fit, hull);
		}

		private void InstallFit(PlayerShip ship, ShipFitModel fit, HullModel hull)
		{
			if (ship == null || fit == null)
				return;

			if (fit.GridPlacements == null || fit.GridPlacements.Count == 0)
				return;

			foreach (var placement in fit.GridPlacements)
			{
				if (placement == null ||
				    string.IsNullOrEmpty(placement.ItemId))
					continue;

				var mount = CreatePlacementMount(ship.transform, placement);
				if (mount == null)
					continue;

				if (placement.GridType == ShipGridType.WeaponGrid)
					WeaponBuilder.BuildBattle(placement.ItemId, mount, ship);
				// TODO: modules when implemented.
			}
		}

		private static Transform CreatePlacementMount(Transform shipRoot, ShipFitModel.GridPlacement placement)
		{
			var mountGo = new GameObject($"Mount_{placement.GridId}_{placement.ItemId}");
			var mount = mountGo.transform;
			mount.SetParent(shipRoot, worldPositionStays: false);

			var pos = placement.Position;
			mount.localPosition = new Vector3(pos.x, pos.y, 0f);
			mount.localRotation = Quaternion.Euler(0f, 0f, placement.RotationDeg);
			mount.localScale = Vector3.one;
			return mount;
		}

	}

}
