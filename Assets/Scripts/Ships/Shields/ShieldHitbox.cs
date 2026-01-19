using UnityEngine;

namespace Ships
{
	public class ShieldHitbox : MonoBehaviour
	{
	
		public ShieldController Controller;

		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent<Projectile>(out var proj))
				return;

			if (!HitRules.CanHit(proj.HitMask, Controller._ship.Team) || Controller.ShipShield.ShieldHP.Current <= 0)
				return;

			Vector3 hitPoint = proj.transform.position;
			
			var calc = DamageCalculator.CalculateHit(
				projectileDamage: proj.Damage,
				hitPoint:     hitPoint,
				sourceWeapon: proj.SourceWeapon,
				target:       Controller._ship,
				wasShieldHit: true
			);

			GameEvent.TakeDamage(calc);
			Controller.OnShieldHit(calc); 
			proj.DestroySelf();
		}
	}
}
