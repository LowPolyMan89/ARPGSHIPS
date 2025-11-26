namespace Ships
{
	using UnityEngine;

	public class ProjectileWeapon : WeaponBase
	{
		public Projectile ProjectilePrefab;

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
				var proj = Instantiate(ProjectilePrefab, Slot.Muzzle.position, Slot.Muzzle.rotation);
				proj.Init(target, damage, speed, Model.ArmorPierce, this);
			}
		}

		private void DoInstantHit(Transform target, float dmg)
		{
			if (target.TryGetComponent<ITargetable>(out var t))
			{
				if (t.TryGetStat(StatType.HP, out var hp))
					hp.AddToCurrent(-dmg);

				foreach (var effect in Model.Effects)
					effect.Apply(t, dmg, this);
			}
		}
	}
}