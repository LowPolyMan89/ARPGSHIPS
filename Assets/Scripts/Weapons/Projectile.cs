using UnityEngine;
using UnityEngine.EventSystems;

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
		public ShipBase Owner;
		public TeamMask HitMask;
		[SerializeField] private Vector2 moveDir;

		public void Init(Vector2 direction, float dmg, float spd, float ap, WeaponBase source)
		{
			damage = dmg;
			speed = spd;
			armorPierce = ap;
			SourceWeapon = source;
			moveDir = direction.normalized;
			HitMask = SourceWeapon.Slot.HitMask;
		}

		void Update()
		{
			transform.position += (Vector3)(moveDir * speed * Time.deltaTime);
		}

		public float Damage => damage;

		public void DestroySelf()
		{
			Destroy(gameObject);
		}
		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.TryGetComponent<ITargetable>(out var targetable))
				return;
			if (!HitRules.CanHit(HitMask, targetable.Team))
				return;
		
			var calc = DamageCalculator.CalculateHit(
				projectileDamage: damage,
				armorPierce: armorPierce,
				hitPoint: transform.position,
				sourceWeapon: SourceWeapon,
				target: targetable,
				wasShieldHit: false
			);
			
			GameEvent.TakeDamage(calc);
			targetable.TakeDamage(calc);
			Destroy(gameObject);
		}
	}

}