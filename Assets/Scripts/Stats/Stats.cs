using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public sealed class Stats
	{
		private readonly Dictionary<StatType, Stat> _stats = new Dictionary<StatType, Stat>();

		public IReadOnlyDictionary<StatType, Stat> All => _stats;

		public void AddStat(Stat stat)
		{
			_stats[stat.Name] = stat;
		}

		public bool TryGetStat(StatType name, out Stat stat)
		{
			return _stats.TryGetValue(name, out stat);
		}

		public Stat GetStat(StatType name)
		{
			_stats.TryGetValue(name, out var stat);
			return stat;
		}

		public float GetCurrent(StatType name)
		{
			if (!_stats.TryGetValue(name, out var stat))
			{
				Debug.LogWarning($"Stat {name} not found in Stats!");
				return 0f;
			}
			return stat.Current;
		}

		public float GetMaximum(StatType name)
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
