namespace Ships
{
	using UnityEngine;

	public class WeaponSlot : MonoBehaviour
	{
		public float AllowedAngle = 45f; 
		public bool IsTurret = false;    
		public Transform Muzzle;    
		
		public bool IsTargetWithinSector(Vector2 dir)
		{
			Vector2 forward = transform.right; 
			var angle = Vector2.Angle(forward, dir);
			return angle <= AllowedAngle;
		}
	}

}