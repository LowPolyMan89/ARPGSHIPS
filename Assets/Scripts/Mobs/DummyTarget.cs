using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Mobs
{
	public class DummyTarget : TankBase
	{
		private void Start()
		{
			LoadShipFromConfig("hull_test_frigate");
			LoadTankFromPrefab();
		}
	}
}