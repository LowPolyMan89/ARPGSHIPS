using UnityEngine;

namespace Tanks
{
	public class ShieldHitbox : MonoBehaviour
	{
		public ShieldSide Side;
		public ShieldController Controller;

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.TryGetComponent<Projectile>(out var proj))
				return;

			if (!HitRules.CanHit(proj.HitMask, Controller._tank.Team))
				return;

			Vector2 hitPoint = proj.transform.position;
			
			Stat s;
			proj.SourceWeapon.Model.Stats.TryGetStat(StatType.ArmorPierce, out s);
			var calc = DamageCalculator.CalculateHit(
				projectileDamage: proj.Damage,
				armorPierce:  s?.Current ?? 0,
				hitPoint:     hitPoint,
				sourceWeapon: proj.SourceWeapon,
				target:       Controller._tank,
				wasShieldHit: true
			);

			GameEvent.TakeDamage(calc);
			Controller.OnShieldHit(Side, calc); 
			proj.DestroySelf();
		}
	}
}