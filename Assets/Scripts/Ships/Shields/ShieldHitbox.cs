using UnityEngine;

namespace Ships
{
	public class ShieldHitbox : MonoBehaviour
	{
		public ShieldSide Side;
		public ShieldController Controller;

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.TryGetComponent<Projectile>(out var proj))
				return;

			if (!HitRules.CanHit(proj.HitMask, Controller.Ship.Team))
				return;

			Vector2 hitPoint = other.ClosestPoint((Vector2)transform.position);
			Controller.OnShieldHit(Side, hitPoint, proj);
			proj.DestroySelf();
		}
	}
}