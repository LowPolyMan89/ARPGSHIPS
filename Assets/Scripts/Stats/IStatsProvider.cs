using System.Collections.Generic;

namespace Ships
{
	public interface IStatsProvider
	{
		bool TryGetStat(StatType name, out IStat stat);
		IStat GetStat(StatType name);
		IEnumerable<IStat> GetAllStats();
	}
}