using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public abstract class ShipBase : MonoBehaviour, IStatsProvider
	{
		public ShipVisual _visual;
		public Stats ShipStats;

		public void Init()
		{
			if (_visual != null)
			{
				_visual.Unload();
				_visual.Load();
			}
			else
			{
				_visual = new ShipVisual();
				_visual.Load();
			}
		}
		public bool TryGetStat(StatType name, out IStat stat)
		{
			bool result = ShipStats.TryGetStat(name, out var s);
			stat = s;
			return result;
		}

		public IStat GetStat(StatType name) => ShipStats.GetStat(name);

		public IEnumerable<IStat> GetAllStats() => ShipStats.All.Values;
	}
}