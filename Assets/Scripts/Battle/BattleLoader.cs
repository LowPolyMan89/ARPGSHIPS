namespace Ships
{
	using UnityEngine;
	using UnityEngine.Serialization;
	using System.Collections.Generic;
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
			Battle.Instance?.RegisterShip(ship);

			ship.LoadShipFromConfig(fit.SelectedShipId);
			ship.Init();
			StatEffectApplier.ApplyAll(ship.ShipStats, fit.MainStatEffects, StatModifierSource.Main, fit);

			var moduleWeaponEffects = ApplyModuleEffects(ship, fit);
			ShipStatBonusApplier.Apply(ship.ShipStats);
			InstallFit(ship, fit, hull, moduleWeaponEffects);
		}

		private void InstallFit(
			PlayerShip ship,
			MetaState state,
			HullModel hull,
			List<WeaponStatEffectModel> moduleWeaponEffects)
		{
			if (ship == null || state == null)
				return;

			var fit = state.Fit;
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
					var item = InventoryUtils.FindByItemId(state.InventoryModel, placement.ItemId);
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

		private static List<WeaponStatEffectModel> ApplyModuleEffects(PlayerShip ship, MetaState state)
		{
			var weaponEffects = new List<WeaponStatEffectModel>();
			if (ship == null || state?.Fit?.GridPlacements == null)
				return weaponEffects;

			foreach (var placement in state.Fit.GridPlacements)
			{
				if (placement == null ||
				    placement.GridType != ShipGridType.ModuleGrid ||
				    string.IsNullOrEmpty(placement.ItemId))
					continue;

				var item = InventoryUtils.FindByItemId(state.InventoryModel, placement.ItemId);
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
