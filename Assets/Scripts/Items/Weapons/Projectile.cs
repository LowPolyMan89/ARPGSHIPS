using Unity.VisualScripting;
using UnityEngine;

namespace Ships
{
	public class Projectile : MonoBehaviour
	{
		private float _speed;
		private float _damage;

		public WeaponBase SourceWeapon { get; private set; }
		public ShipBase Owner { get; private set; }
		public TeamMask HitMask { get; private set; }

		[SerializeField] private Vector3 _moveDir;

		public float Damage => _damage;

		public void Init(Vector3 direction, float dmg, float spd, WeaponBase source)
		{
			_moveDir = direction.normalized;
			_speed = spd;
			_damage = dmg;

			SourceWeapon = source;
			Owner = source != null ? source.Owner : null;
			HitMask = Owner != null ? Owner.HitMask : default;
		}

		private void Update()
		{
			transform.position += _moveDir * _speed * Time.deltaTime;
			if (Battle.Instance != null && !Battle.Instance.IsInside(transform.position))
			{
				Destroy(gameObject);
			}
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.TryGetComponent<ITargetable>(out var t))
				return;

			if (!HitRules.CanHit(HitMask, t.Team))
				return;

			var calc = DamageCalculator.CalculateHit(
				projectileDamage: _damage,
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
