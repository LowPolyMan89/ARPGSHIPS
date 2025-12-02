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
			ShipStats = new Stats();
			ShipStats.AddStat(new Stat(StatType.HitPoint, 100));
			ShipStats.AddStat(new Stat(StatType.Shield, 50));
			ShipStats.AddStat(new Stat(StatType.MoveSpeed, 10));
			Init();
		}
	}
}