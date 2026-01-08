using System.Collections.Generic;

namespace Ships
{
	public class WeaponModel
	{
		public Stats Stats { get; set; }
		public Stats BaseStats { get; set; }
		public bool HasDamageType { get; set; }
		public Tags DamageType { get; set; }
		public string Size { get; set; }
		public Tags[] Tags { get; set; }
		private readonly List<IOnHitEffect> _effects = new();
		public bool IsAutoFire = true;
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
