using System;
using System.Collections.Generic;

namespace Ships
{
	public class PlayerShip : ShipBase
	{
		private void Awake()
		{
			LoadShipFromConfig("hull_test_frigate");
			Init();
			AimSystem = new PlayerTurretAimSystem();
			AimSystem.Init(this);
			AimSystem.Turret = Turret;
		}
	}
}
