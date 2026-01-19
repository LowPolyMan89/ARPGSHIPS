using System;
using System.Collections.Generic;

namespace Ships
{
	public class PlayerShip : ShipBase
	{
		private void Awake()
		{
			LoadShipFromConfig("hull_flagman_1");
			Init();
			Battle.Instance.Player = this;
		}
	}
}
