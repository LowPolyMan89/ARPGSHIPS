using System.Collections.Generic;

namespace Tanks
{
	public interface IStatsProvider
	{
		bool TryGetStat(StatType name, out IStat stat);
		IStat GetStat(StatType name);
		IEnumerable<IStat> GetAllStats();
	}
}