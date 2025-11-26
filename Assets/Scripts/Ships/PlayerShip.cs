using System.Collections.Generic;

namespace Ships
{
	public class PlayerShip : ShipBase
	{
		
		private void Awake()
		{
			ShipStats = new Stats();
			ShipStats.AddStat(new Stat(StatsNames.Hull, 100));
			ShipStats.AddStat(new Stat(StatsNames.Shield, 50));
			ShipStats.AddStat(new Stat(StatsNames.Speed, 10));
		}
		
	}
}