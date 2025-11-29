using System.Collections.Generic;

namespace Ships
{
	public class WeaponModel
	{
		private Stats Stats { get; set; }
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

		// --- SAFE GETTER ---
		private float Safe(StatType t)
		{
			if (Stats == null) return 0;
			return Stats.GetCurrent(t);
		}

		public float FireRate        => Safe(StatType.FireRate);
		public float MinDamage       => Safe(StatType.MinDamage);
		public float MaxDamage       => Safe(StatType.MaxDamage);
		public float CritChance      => Safe(StatType.CritChance);
		public float CritMultiplier  => Safe(StatType.CritMultiplier);
		public float ProjectileSpeed => Safe(StatType.ProjectileSpeed);
		public float ArmorPierce     => Safe(StatType.ArmorPierce);
		public float FireRange       => Safe(StatType.FireRange);
		public float Accuracy        => Safe(StatType.Accuracy);
	}



}