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
			LoadShipFromConfig("hull_test_frigate");
			LoadShipFromPrefab();
		}
	}
}

