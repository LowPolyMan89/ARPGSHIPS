using System.Collections.Generic;

namespace Tanks
{
	public class WeaponModel
	{
		public Stats Stats { get; set; }
		private readonly List<IOnHitEffect> _effects = new();

		public void AddEffect(IOnHitEffect effect) => _effects.Add(effect);
		public IReadOnlyList<IOnHitEffect> Effects => _effects;

		public WeaponModel(Stats stats = null)
		{
			Stats = stats;
		}

		public void InjectStat(Stats newStats)
		{
			Stats = newStats;
		}
		
	}



}