using UnityEngine;

namespace Ships
{
	using UnityEngine;

	public class Projectile : MonoBehaviour
	{
		private Transform target;
		private float speed;
		private float damage;
		private float armorPierce;
		private WeaponBase sourceWeapon;

		public void Init(Transform target, float dmg, float spd, float ap, WeaponBase source)
		{
			this.target = target;
			damage = dmg;
			speed = spd;
			armorPierce = ap;
			sourceWeapon = source;
		}

		void Update()
		{
			if (target == null)
			{
				Destroy(gameObject);
				return;
			}

			Vector2 dir = (target.position - transform.position).normalized;
			transform.position += (Vector3)(dir * speed * Time.deltaTime);

			if (Vector2.Distance(transform.position, target.position) < 0.2f)
				HitTarget();
		}

		private void HitTarget()
		{
			if (target.TryGetComponent<ITargetable>(out var targetable))
			{
				// нанести урон
				if (targetable.TryGetStat(StatType.HitPoint, out var hpStat))
					hpStat.AddToCurrent(-damage);

				// вызвать эффекты
				foreach (var effect in sourceWeapon.Model.Effects)
					effect.Apply(targetable, damage, sourceWeapon);
			}

			Destroy(gameObject);
		}
	}

}