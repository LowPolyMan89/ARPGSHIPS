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
				ShipBase ship = target.GetComponent<ShipBase>();

				float dmg = damage;

				// === ЩИТЫ ==
				if (ship != null && ship.TryGetComponent<ShieldController>(out var shields))
				{
					Vector2 hitDir = (ship.transform.position - transform.position).normalized;
					int sectorIndex = shields.FindSectorIndex(hitDir);

					float left = shields.ApplyDamage(sectorIndex, dmg);

					if (left < dmg)
					{
						shields.OnSectorHit(sectorIndex, transform.position);
						dmg = left;
					}
				}

				// === УРОН В КОРПУС ===
				if (dmg > 0 && targetable.TryGetStat(StatType.HitPoint, out var hpStat))
					hpStat.AddToCurrent(-dmg);

				// === ЭФФЕКТЫ ===
				foreach (var effect in sourceWeapon.Model.Effects)
					effect.Apply(targetable, dmg, sourceWeapon);
			}

			Destroy(gameObject);
		}
	}

}