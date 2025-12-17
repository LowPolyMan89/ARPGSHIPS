namespace Ships
{
	using UnityEngine;
	using UnityEngine.Serialization;

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

			InstallFit(ship, fit.Fit);
		}

		private void InstallFit(PlayerShip ship, ShipFitModel fit)
		{
		}

		private void InstallWeapon(WeaponSlot slot, string itemId)
		{
			// читаем JSON оружия, инстансим WeaponBase, подменяем статы
		}
	}

}
