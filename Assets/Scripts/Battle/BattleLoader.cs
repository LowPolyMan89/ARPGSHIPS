namespace Ships
{
	using UnityEngine;

	public class BattleLoader : MonoBehaviour
	{
		public GameObject PlayerShipPrefab;

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
			foreach (var slot in ship.WeaponController.Weapons)
			{
				if (!fit.WeaponSlots.TryGetValue(slot.name, out var itemId))
					continue;

				if (itemId != null)
					InstallWeapon(slot, itemId);
			}

			// когда появятся ModuleSlots — аналогично
		}

		private void InstallWeapon(WeaponSlot slot, string itemId)
		{
			// читаем JSON оружия, инстансим WeaponBase, подменяем статы
		}
	}

}