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

			if (!HitRules.CanHit(proj.HitMask, Controller._ship.Team))
				return;

			Vector2 hitPoint = proj.transform.position;
			
			Stat s;
			proj.SourceWeapon.Model.Stats.TryGetStat(StatType.ArmorPierce, out s);
			var calc = DamageCalculator.CalculateHit(
				projectileDamage: proj.Damage,
				armorPierce:  s?.Current ?? 0,
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
