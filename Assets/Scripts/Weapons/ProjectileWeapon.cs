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
				proj.Init(target, damage, speed, Model.ArmorPierce);
			}
		}

		private void DoInstantHit(Transform target, float dmg)
		{
			//var hp = target.GetComponent<HealthComponent>();
			//if (hp != null)
			//	hp.ApplyDamage(dmg, Model.ArmorPierce);

			// TODO: визуальный лазер-эффект
		}
	}
}