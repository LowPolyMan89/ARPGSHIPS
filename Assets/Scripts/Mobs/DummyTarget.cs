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
			TankStats = new Stats();
			TankStats.AddStat(new Stat(StatType.HitPoint, 100));
			TankStats.AddStat(new Stat(StatType.Shield, 50));
			TankStats.AddStat(new Stat(StatType.MoveSpeed, 10));
			Init();
			LoadTankFromPrefab();
		}
	}
}