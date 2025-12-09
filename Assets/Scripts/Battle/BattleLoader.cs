namespace Tanks
{
	using UnityEngine;

	public class BattleLoader : MonoBehaviour
	{
		public GameObject PlayerTankPrefab;

		private void Start()
		{
			LoadPlayerTankFromFit();
		}

		private void LoadPlayerTankFromFit()
		{
			var fit = MetaBattleBridge.LastFit;
			if (fit == null) return;

			var hull = HullLoader.Load(fit.SelectedTankId);
			var go = Instantiate(PlayerTankPrefab, Vector3.zero, Quaternion.identity);
			var tank = go.GetComponent<PlayerTank>();

			tank.LoadShipFromConfig(fit.SelectedTankId);
			tank.Init();

			InstallFit(tank, fit.Fit);
		}

		private void InstallFit(PlayerTank tank, TankFitModel fit)
		{
			foreach (var slot in tank.WeaponController.Weapons)
			{
				//if (!fit.WeaponSlots.TryGetValue(slot.name, out var itemId))
				//	continue;

				//if (itemId != null)
				//	InstallWeapon(slot, itemId);
			}

			// когда появятся ModuleSlots — аналогично
		}

		private void InstallWeapon(WeaponSlot slot, string itemId)
		{
			// читаем JSON оружия, инстансим WeaponBase, подменяем статы
		}
	}

}