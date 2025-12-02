using System.Collections.Generic;

namespace Ships
{
	public class PlayerShip : ShipBase
	{
		
		private void Awake()
		{
			ShipStats = new Stats();
			ShipStats.AddStat(new Stat(StatType.HitPoint, 100));
			ShipStats.AddStat(new Stat(StatType.Shield, 50));
			ShipStats.AddStat(new Stat(StatType.MoveSpeed, 4));
			ShipStats.AddStat(new Stat(StatType.BackSpeed, 2));
			ShipStats.AddStat(new Stat(StatType.TurnSpeed, 3));
			ShipStats.AddStat(new Stat(StatType.BrakePower, 5));
			ShipStats.AddStat(new Stat(StatType.Acceleration, 8));
			ShipStats.AddStat(new Stat(StatType.KineticResist, 5));
			ShipStats.AddStat(new Stat(StatType.ExplosionResist, 5));
			Init();
		}
		
	}
}