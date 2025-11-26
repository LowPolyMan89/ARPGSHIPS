using System.Collections.Generic;

namespace Ships
{
	public sealed class Stats
	{
		private readonly Dictionary<StatsNames, Stat> _stats = new Dictionary<StatsNames, Stat>();

		public IReadOnlyDictionary<StatsNames, Stat> All => _stats;

		public void AddStat(Stat stat)
		{
			_stats[stat.Name] = stat;
		}

		public bool TryGetStat(StatsNames name, out Stat stat)
		{
			return _stats.TryGetValue(name, out stat);
		}

		public Stat GetStat(StatsNames name)
		{
			_stats.TryGetValue(name, out var stat);
			return stat;
		}

		public float GetCurrent(StatsNames name)
		{
			return _stats.TryGetValue(name, out var stat) ? stat.Current : 0f;
		}

		public float GetMaximum(StatsNames name)
		{
			return _stats.TryGetValue(name, out var stat) ? stat.Maximum : 0f;
		}
		
		public void Tick()
		{
			foreach (var stat in _stats.Values)
			{
				stat.Tick();
			}
		}
	}
}