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
		public WeaponBase SourceWeapon;
		public SideType Side;

		public void Init(Transform target, float dmg, float spd, float ap, WeaponBase source, SideType side)
		{
			this.target = target;
			damage = dmg;
			speed = spd;
			armorPierce = ap;
			SourceWeapon = source;
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
		}

		public float Damage => damage;

		public void DestroySelf()
		{
			Destroy(gameObject);
		}
		private void OnTriggerEnter2D(Collider2D other)
		{
			// Попали в корпус
			if (other.gameObject.TryGetComponent<ShipBase>(out var ship))
			{
				if(Side == ship.SideType)
					return;
				ship.TakeDamage(damage, transform.position, SourceWeapon);
				Destroy(gameObject);
			}
		}
	}

}