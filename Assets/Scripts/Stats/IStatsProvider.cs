using System.Collections.Generic;

namespace Ships
{
	public interface IStatsProvider
	{
		bool TryGetStat(StatsNames name, out IStat stat);
		IStat GetStat(StatsNames name);
		IEnumerable<IStat> GetAllStats();
	}
}