using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ships.Mobs
{
	public class DummyTarget : ShipBase
	{
		private void Start()
		{
			LoadShipFromConfig("pirate_ship_s_1");
			//LoadShipFromPrefab();
			Init();
		}
	}
}

