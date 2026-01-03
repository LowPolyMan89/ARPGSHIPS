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
			var dmg = RollDamage();
			var spd = Model.Stats.GetStat(StatType.ProjectileSpeed).Current;

			var fp = FirePoint != null ? FirePoint : transform;
			var plane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XY;
			var dir = plane == Battle.WorldPlane.XY ? fp.up : fp.forward;
			if (plane == Battle.WorldPlane.XY)
				dir.z = 0f;
			else
				dir.y = 0f;

			var proj = Instantiate(ProjectilePrefab, fp.position, fp.rotation);

			proj.Init(
				direction: dir,
				dmg,
				spd,
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
