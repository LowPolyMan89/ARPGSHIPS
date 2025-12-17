using UnityEngine;

namespace Ships
{
	public class Projectile : MonoBehaviour
	{
		private float _speed;
		private float _damage;
		private float _pierce;

		public WeaponBase SourceWeapon { get; private set; }
		public ShipBase Owner { get; private set; }
		public TeamMask HitMask { get; private set; }

		[SerializeField] 
		private Vector3 _moveDir;

		public float Damage => _damage;

		public void Init(Vector3 direction, float dmg, float spd, float armorPierce, WeaponBase source)
		{
			_moveDir   = direction.normalized;
			_speed     = spd;
			_damage    = dmg;
			_pierce    = armorPierce;

			SourceWeapon = source;
			Owner        = source.Slot.Owner;
			HitMask      = Owner.HitMask;   // ← КОРРЕКТНО!
		}

		private void Update()
		{
			transform.position += _moveDir * _speed * Time.deltaTime;
			if (!Battle.Instance.IsInside(transform.position))
			{
				Destroy(gameObject);
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (Services.IsInLayerMask(other.gameObject, SourceWeapon.Slot.ObstacleLayers))
			{
				Destroy(gameObject);
			}
			if (!other.TryGetComponent<ITargetable>(out var t))
				return;

			// команда владельца решает, можно ли наносить урон
			if (!HitRules.CanHit(HitMask, t.Team))
				return;

			var calc = DamageCalculator.CalculateHit(
				projectileDamage: _damage,
				armorPierce: _pierce,
				hitPoint: transform.position,
				sourceWeapon: SourceWeapon,
				target: t,
				wasShieldHit: false
			);

			GameEvent.TakeDamage(calc);
			t.TakeDamage(calc);
			Destroy(gameObject);
		}

		public void DestroySelf()
		{
			Destroy(gameObject);
		}
	}
}
