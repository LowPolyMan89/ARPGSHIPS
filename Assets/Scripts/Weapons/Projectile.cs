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

		public void Init(Transform target, float dmg, float spd, float ap)
		{
			this.target = target;
			damage = dmg;
			speed = spd;
			armorPierce = ap;
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
			//var hp = target.GetComponent<HealthComponent>();
			//if (hp != null)
			//	hp.ApplyDamage(damage, armorPierce);

			//Destroy(gameObject);
		}
	}

}