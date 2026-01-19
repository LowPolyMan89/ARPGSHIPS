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
			var dir = fp.forward;
			var spreadAngle = GetSpreadAngleForShot();
			if (spreadAngle > 0f)
			{
				var offset = UnityEngine.Random.insideUnitCircle * spreadAngle;
				var spreadRot = fp.rotation * Quaternion.Euler(offset.y, offset.x, 0f);
				dir = spreadRot * Vector3.forward;
			}

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
