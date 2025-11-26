using UnityEngine;

namespace Ships
{
	using UnityEngine;

	public abstract class WeaponBase : MonoBehaviour
	{

		protected float nextFireTime;

		public WeaponSlot Slot;
		public WeaponModel Model;

		public void Init(Stats stats)
		{
			Model = new WeaponModel(stats);
		}

		public void TickWeapon(Transform target)
		{
			if (Model == null || target == null)
				return;

			Vector2 dir = (target.position - Slot.transform.position);

			RotateToTarget(dir);

			if (Time.time >= nextFireTime)
			{
				nextFireTime = Time.time + 1f / Model.FireRate;
				Shoot(target);
			}
		}

		// NEW: Tick with aim point (using lead + accuracy)
		public void TickWeaponPosition(Vector2 aimPoint)
		{
			if (Model == null)
				return;

			Vector2 dir = aimPoint - (Vector2)Slot.transform.position;
			RotateToTarget(dir);
		}

		protected virtual void RotateToTarget(Vector2 direction)
		{
			if (!Slot.IsTurret)
				return;

			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			Quaternion rot = Quaternion.Euler(0, 0, angle);
			Slot.transform.rotation = Quaternion.Lerp(Slot.transform.rotation, rot, Time.deltaTime * 10f);
		}

		protected abstract void Shoot(Transform target);

		protected float RollDamage()
		{
			float dmg = Random.Range(Model.MinDamage, Model.MaxDamage);

			if (Random.value < Model.CritChance)
				dmg *= Model.CritMultiplier;

			return dmg;
		}
	}

}
