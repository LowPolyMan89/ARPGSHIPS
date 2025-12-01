using System;

namespace Ships
{
	using UnityEngine;

	public class ProjectileWeapon : WeaponBase
	{
		public Projectile ProjectilePrefab;
		private bool IsInit;
		private void Update()
		{
			//TODO
			if (Model != null && !IsInit)
			{
				Stats stats = new Stats();
				stats.AddStat(new Stat(StatType.FireRange, 5));
				stats.AddStat(new Stat(StatType.FireRate, 1));
				stats.AddStat(new Stat(StatType.ProjectileSpeed, 3));
				stats.AddStat(new Stat(StatType.MinDamage, 5));
				stats.AddStat(new Stat(StatType.MaxDamage, 10));
				stats.AddStat(new Stat(StatType.Accuracy, 1));
				stats.AddStat(new Stat(StatType.CritChance, 0.05f));
				stats.AddStat(new Stat(StatType.CritMultiplier, 1.2f));
				Model.InjectStat(stats);
				IsInit = true;
			}
		}

		protected override void Shoot(Transform target)
		{
			float damage = RollDamage();
			float speed = Model.ProjectileSpeed;

			if (speed <= 0.01f)
			{
				DoInstantHit(target, damage);
			}
			else
			{
				var proj = Instantiate(ProjectilePrefab, FirePoint.position, FirePoint.rotation);
				proj.Init(target, damage, speed, Model.ArmorPierce, this, Slot.Side);
			}
		}

		private void DoInstantHit(Transform target, float dmg)
		{
			if (target.TryGetComponent<ITargetable>(out var t))
			{
				if (t.TryGetStat(StatType.HitPoint, out var hp))
					hp.AddToCurrent(-dmg);

				foreach (var effect in Model.Effects)
					effect.Apply(t, dmg, this);
			}
		}
	}
}