using System.Collections.Generic;

namespace Ships
{
	public class PlayerShip : ShipBase
	{
		
		private void Awake()
		{
			ShipStats = new Stats();
			ShipStats.AddStat(new Stat(StatType.MaxHP, 100));
			ShipStats.AddStat(new Stat(StatType.Shield, 50));
			ShipStats.AddStat(new Stat(StatType.MoveSpeed, 10));
		}
		
	}
}