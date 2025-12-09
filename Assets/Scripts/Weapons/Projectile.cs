using UnityEngine;
using UnityEngine.EventSystems;

namespace Tanks
{
	using UnityEngine;

	public class Projectile : MonoBehaviour
	{
		private Transform _target;
		private float _speed;
		private float _damage;
		private float _pierce;
		public WeaponBase SourceWeapon;
		public TankBase Owner;
		public TeamMask HitMask;
		[SerializeField] private Vector3 _moveDir;
		public float Damage => _damage;

		public void Init(Vector3 direction, float dmg, float spd, float armorPierce, WeaponBase source)
		{
			_moveDir = direction.normalized;
			_speed = spd;
			_damage = dmg;
			_pierce = armorPierce;
			SourceWeapon = source;
			HitMask = source.Slot.HitMask;
		}

		private void Update()
		{
			transform.position += _moveDir * _speed * Time.deltaTime;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent<ITargetable>(out var t))
				return;

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