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
		public TankBase Owner { get; set; }

		public float NextFireTime => _nextFireTime;

		public float ReloadFinishTime
		{
			get => _reloadFinishTime;
			set => _reloadFinishTime = value;
		}

		public bool IsReloading => _isReloading;

		public int Ammo => _ammo;

		public void Init(WeaponSlot slot, Stats stats)
		{
			Model = new WeaponModel();
			Model.InjectStat(stats);
			Slot = slot;
			_ammo = GetMaxAmmo();
		}
		public void TryFire(ITargetable target)
		{
			if (target == null)
				return;
			if (_isReloading)
			{
				if (Time.time >= ReloadFinishTime)
				{
					_isReloading = false;
					_ammo = GetMaxAmmo();
				}
				else
				{
					return;
				}
			}
			if (Time.time < NextFireTime)
				return;
			Shoot(target.Transform);

			_ammo--;
			_nextFireTime = Time.time + 1f / Model.Stats.GetStat(StatType.FireRate).Current;
			if (_ammo <= 0)
				StartReload();
		}

		private int GetMaxAmmo()
		{
			Stat a;
			if (Model.Stats.TryGetStat(StatType.AmmoCount, out a))
			{
				return Mathf.RoundToInt(a.Maximum);
			}
			return 0;
		}
		private void StartReload()
		{
			_isReloading = true;
			ReloadFinishTime = Time.time + Model.Stats.GetStat(StatType.ReloadTime).Maximum;
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