using System;

namespace Ships
{
	using UnityEngine;

	public class ProjectileWeapon : WeaponBase
	{
		public Projectile ProjectilePrefab;
		private bool IsInit;

		protected override void Shoot(Transform target)
		{
			float damage = RollDamage();
			float speed = Model.Stats.GetStat(StatType.ProjectileSpeed).Current;

			if (speed <= 0.01f)
			{
				DoInstantHit(target, damage);
			}
			else
			{
				var proj = Instantiate(ProjectilePrefab, FirePoint.position, FirePoint.rotation);
				proj.Init((Vector2)FirePoint.right, damage, speed, Model.Stats.GetStat(StatType.ArmorPierce).Current, this);
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