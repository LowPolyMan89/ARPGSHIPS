namespace Ships
{
	using UnityEngine;
	using UnityEngine.Serialization;
	using System.Collections.Generic;

	public class BattleLoader : MonoBehaviour
	{
		[FormerlySerializedAs("PlayerTankPrefab")] public GameObject PlayerShipPrefab;

		private void Start()
		{
			LoadPlayerShipFromFit();
		}

		private void LoadPlayerShipFromFit()
		{
			var state = MetaBattleBridge.LastFit;
			if (state == null)
				return;

			var shipIds = BuildShipIds(state);
			if (shipIds.Count == 0)
				return;

			var spawnPositions = Battle.Instance != null ? Battle.Instance.PlayerSpawnPositions : null;
			for (var i = 0; i < shipIds.Count; i++)
			{
				var shipId = shipIds[i];
				if (string.IsNullOrEmpty(shipId))
					continue;

				var hull = HullLoader.Load(shipId);
				if (hull == null)
					continue;

				var position = Vector3.zero;
				var rotation = Quaternion.identity;
				if (spawnPositions != null && i < spawnPositions.Count && spawnPositions[i] != null)
				{
					position = spawnPositions[i].position;
					rotation = spawnPositions[i].rotation;
				}

				var go = InstantiateShip(hull, position, rotation);
				var ship = go.GetComponent<PlayerShip>();
				Battle.Instance?.RegisterShip(ship);
				if (Battle.Instance != null && Battle.Instance.Player == null)
					Battle.Instance.Player = ship;

				ship.LoadShipFromConfig(shipId);
				ship.Init();
				StatEffectApplier.ApplyAll(ship.ShipStats, state.MainStatEffects, StatModifierSource.Main, state);

				var fit = GetFitForShip(state, shipId);
				var moduleWeaponEffects = ApplyModuleEffects(ship, fit, state.InventoryModel);
				ShipStatBonusApplier.Apply(ship.ShipStats);
				InstallFit(ship, fit, hull, moduleWeaponEffects, state.InventoryModel);
			}
		}

		private GameObject InstantiateShip(HullModel hull, Vector3 position, Quaternion rotation)
		{
			if (hull != null && !string.IsNullOrEmpty(hull.BattlePrefab))
			{
				var go = ResourceLoader.InstantiatePrefabById(hull.BattlePrefab, null, false);
				if (go != null)
				{
					go.transform.SetPositionAndRotation(position, rotation);
					return go;
				}
			}

			return Instantiate(PlayerShipPrefab, position, rotation);
		}

		private void InstallFit(
			PlayerShip ship,
			ShipFitModel fit,
			HullModel hull,
			List<WeaponStatEffectModel> moduleWeaponEffects,
			PlayerInventoryModel inventory)
		{
			if (ship == null || fit == null)
				return;

			if (fit?.GridPlacements == null || fit.GridPlacements.Count == 0)
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
				{
					var item = InventoryUtils.FindByItemId(inventory, placement.ItemId);
					var weapon = WeaponBuilder.BuildBattle(placement.ItemId, mount, ship, item);
					if (weapon != null)
						ApplyWeaponComposition(weapon, ship, moduleWeaponEffects);
				}
				else if (placement.GridType == ShipGridType.ModuleGrid)
				{
					ModuleBuilder.BuildBattle(placement.ItemId, mount, ship);
				}
			}
		}


		private static List<string> BuildShipIds(MetaState state)
		{
			var result = new List<string>();
			if (state?.BattleShipSlots != null)
			{
				for (var i = 0; i < state.BattleShipSlots.Count; i++)
				{
					var id = state.BattleShipSlots[i];
					if (!string.IsNullOrEmpty(id))
						result.Add(id);
				}
			}

			if (result.Count == 0 && !string.IsNullOrEmpty(state?.SelectedShipId))
				result.Add(state.SelectedShipId);

			return result;
		}

		private static ShipFitModel GetFitForShip(MetaState state, string shipId)
		{
			if (state == null || string.IsNullOrEmpty(shipId))
				return null;

			if (state.Fit != null && string.Equals(state.Fit.ShipId, shipId, System.StringComparison.OrdinalIgnoreCase))
				return state.Fit;

			var fit = state.PlayerShipFits?.Find(f =>
				f != null &&
				!string.IsNullOrEmpty(f.ShipId) &&
				string.Equals(f.ShipId, shipId, System.StringComparison.OrdinalIgnoreCase));

			return fit ?? new ShipFitModel { ShipId = shipId };
		}

		private static List<WeaponStatEffectModel> ApplyModuleEffects(
			PlayerShip ship,
			ShipFitModel fit,
			PlayerInventoryModel inventory)
		{
			var weaponEffects = new List<WeaponStatEffectModel>();
			if (ship == null || fit?.GridPlacements == null)
				return weaponEffects;

			foreach (var placement in fit.GridPlacements)
			{
				if (placement == null ||
				    placement.GridType != ShipGridType.ModuleGrid ||
				    string.IsNullOrEmpty(placement.ItemId))
					continue;

				var item = InventoryUtils.FindByItemId(inventory, placement.ItemId);
				if ((item == null || !ModuleBuilder.TryLoadModuleData(item, out var module)) &&
				    !ModuleBuilder.TryLoadModuleData(placement.ItemId, out module))
					continue;

				StatEffectApplier.ApplyAll(ship.ShipStats, module.ShipStatEffects, StatModifierSource.Module, module);

				if (module.WeaponStatEffects != null && module.WeaponStatEffects.Count > 0)
					weaponEffects.AddRange(module.WeaponStatEffects);
			}

			return weaponEffects;
		}

		private static void ApplyWeaponComposition(
			WeaponBase weapon,
			ShipBase ship,
			List<WeaponStatEffectModel> moduleWeaponEffects)
		{
			if (weapon?.Model == null || ship?.ShipStats == null)
				return;

			var baseStats = weapon.Model.BaseStats ?? weapon.Model.Stats;
			var composed = WeaponStatComposer.Compose(baseStats, weapon.Model, ship.ShipStats, moduleWeaponEffects);
			weapon.Model.Stats = composed;
		}

		private static Transform CreatePlacementMount(Transform shipRoot, ShipFitModel.GridPlacement placement)
		{
			var mountGo = new GameObject($"Mount_{placement.GridId}_{placement.ItemId}");
			var mount = mountGo.transform;
			mount.SetParent(shipRoot, worldPositionStays: false);

			var localPos = placement.LocalPosition;
			var localEuler = placement.LocalEuler;

			if (!placement.HasLocalPose)
			{
				var pos2 = placement.Position;
				localPos = new Vector3(pos2.x, 0f, pos2.y);
				localEuler = new Vector3(0f, placement.RotationDeg, 0f);
			}

			mount.localPosition = localPos;
			mount.localRotation = Quaternion.Euler(localEuler);
			mount.localScale = Vector3.one;
			return mount;
		}

	}

}
