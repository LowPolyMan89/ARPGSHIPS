using System;
using System.Collections.Generic;

namespace Tanks
{
	public class PlayerTank : TankBase
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