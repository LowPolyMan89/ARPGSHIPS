using UnityEngine;

namespace Ships
{
	public class ShieldHitbox : MonoBehaviour
	{
		public ShieldSide Side;
		public ShieldController Controller;

		private void OnTriggerEnter2D(Collider2D other)
		{
			if (!other.TryGetComponent<Projectile>(out var proj) || proj.Side == Controller.Ship.SideType)
				return;

			Vector2 hitPoint = other.ClosestPoint(transform.position);
			Controller.OnShieldHit(Side, hitPoint, proj);
			proj.DestroySelf();
		}
	}
}