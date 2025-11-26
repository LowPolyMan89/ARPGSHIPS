using System.Collections.Generic;

namespace Ships
{
	public class WeaponModel
	{
		private Stats Stats { get; }
		public List<IOnHitEffect> OnHitEffects = new();

		public WeaponModel(Stats stats)
		{
			Stats = stats;
		}

		public float FireRate        => Stats.GetCurrent(StatType.FireRate);
		public float MinDamage       => Stats.GetCurrent(StatType.MinDamage);
		public float MaxDamage       => Stats.GetCurrent(StatType.MaxDamage);
		public float CritChance      => Stats.GetCurrent(StatType.CritChance);
		public float CritMultiplier  => Stats.GetCurrent(StatType.CritMultiplier);
		public float ProjectileSpeed => Stats.GetCurrent(StatType.ProjectileSpeed);
		public float ArmorPierce     => Stats.GetCurrent(StatType.ArmorPierce);
		public float FireRange       => Stats.GetCurrent(StatType.FireRange);
		public float Accuracy        => Stats.GetCurrent(StatType.Accuracy);
	}


}