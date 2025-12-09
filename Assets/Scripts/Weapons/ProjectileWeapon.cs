using System;

namespace Tanks
{
	using UnityEngine;

	public class ProjectileWeapon : WeaponBase
	{
		public Projectile ProjectilePrefab;
		private bool IsInit;

		protected override void Shoot(Transform target)
		{
			var dmg = RollDamage();
			var spd = Model.Stats.GetStat(StatType.ProjectileSpeed).Current;
			var ap  = Model.Stats.GetStat(StatType.ArmorPierce).Current;

			var proj = Instantiate(ProjectilePrefab, FirePoint.position, FirePoint.rotation);

			proj.Init(
				direction: FirePoint.forward,   // ВАЖНО: 3D НАПРАВЛЕНИЕ
				dmg,
				spd,
				ap,
				this
			);
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