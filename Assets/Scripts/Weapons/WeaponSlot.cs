using System;

namespace Ships
{
	using UnityEngine;

	public class WeaponSlot : MonoBehaviour
	{
		public float AllowedAngle = 45f; 
		public bool IsTurret = false;
		public WeaponBase MountedWeapon;
		public WeaponTargeting WeaponTargeting;
		

		private void Start()
		{
			if (transform.childCount > 0)
			{
				MountedWeapon = transform.GetComponentInChildren<WeaponBase>();
				WeaponTargeting = transform.GetComponentInChildren<WeaponTargeting>();
			}

			if (MountedWeapon)
			{
				MountedWeapon.Init(this);
			}
				
		}

		public bool IsTargetWithinSector(Vector2 dir)
		{
			Vector2 forward = transform.right; 
			var angle = Vector2.Angle(forward, dir);
			return angle <= AllowedAngle;
		}
	}

}