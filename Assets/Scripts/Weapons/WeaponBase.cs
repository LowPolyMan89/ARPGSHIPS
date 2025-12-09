using System;
using UnityEngine;

namespace Tanks
{
	using UnityEngine;

	public abstract class WeaponBase : MonoBehaviour
	{
		private float _nextFireTime;

		public WeaponSlot Slot;
		public WeaponModel Model;
		public Transform FirePoint;
		private int _ammo;
		private bool _isReloading;
		private float _reloadFinishTime;
		
		public void Init(WeaponSlot slot, Stats stats)
		{
			Model = new WeaponModel();
			Model.InjectStat(stats);
			Slot = slot;
			_ammo = GetMaxAmmo(); 
		}

		public void TickWeapon(Transform target)
		{
			if (target == null || Model == null)
				return;

			if (_isReloading)
			{
				if (Time.time >= _reloadFinishTime)
				{
					_isReloading = false;
					_ammo = GetMaxAmmo();
				}
				return;
			}

			if (_ammo <= 0)
			{
				StartReload();
				return;
			}

			if (Time.time < _nextFireTime)
				return;

			Shoot(target);

			_ammo--;
			_nextFireTime = Time.time + 1f / Model.Stats.GetStat(StatType.FireRate).Current;

			if (_ammo <= 0)
				StartReload();
		}
		private int GetMaxAmmo()
		{
			return Mathf.RoundToInt(Model.Stats.GetStat(StatType.AmmoCount).Maximum);
		}
		private void StartReload()
		{
			_isReloading = true;
			_reloadFinishTime = Time.time + Model.Stats.GetStat(StatType.ReloadTime).Maximum;
		}
		
		public void TickWeaponPosition(Vector3 aimPoint)
		{
			Vector3 dir = aimPoint - Slot.transform.position;
			Slot.RotateWeaponTowards(dir);
		}

		protected virtual void RotateToDirection(Vector3 worldDir)
		{
			Slot.RotateWeaponTowards(worldDir);
		}

		protected abstract void Shoot(Transform target);

		protected float RollDamage()
		{
			float dmg = Random.Range(Model.Stats.GetStat(StatType.MinDamage).Current, Model.Stats.GetStat(StatType.MaxDamage).Current);

			if (Random.value < Model.Stats.GetStat(StatType.CritChance).Current)
				dmg *= Model.Stats.GetStat(StatType.CritMultiplier).Current;

			return dmg;
		}
	}

}