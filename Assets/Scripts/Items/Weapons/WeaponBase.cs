using UnityEngine;

namespace Ships
{
	public abstract class WeaponBase : MonoBehaviour
	{
		private float _nextFireTime;

		public WeaponModel Model;

		[Header("Prefab Parts")]
		[Tooltip("Неподвижная часть оружия (база). Если не задано, используется root.")]
		public Transform BaseTransform;
		[Tooltip("Вращающаяся часть (турель). Если не задано, используется root.")]
		public Transform TurretTransform;

		public Transform FirePoint;
		private int _ammo;
		private bool _isReloading;
		private float _reloadFinishTime;
		public ShipBase Owner { get; set; }

		[Header("Firing")]
		[Tooltip("Разрешённый сектор стрельбы (в градусах). 360 = без ограничения.")]
		public float FireArcDeg = 360f;

		public float NextFireTime => _nextFireTime;
		public float ReloadFinishTime
		{
			get => _reloadFinishTime;
			set => _reloadFinishTime = value;
		}
		public bool IsReloading => _isReloading;
		public int Ammo => _ammo;

		protected virtual void Awake()
		{
			WirePrefabParts();
		}

		private void WirePrefabParts()
		{
			if (BaseTransform == null)
				BaseTransform = transform;

			if (TurretTransform == null)
				TurretTransform = FindDeepChild(transform, "WeaponTurret") ?? transform;

			if (FirePoint == null)
				FirePoint = FindDeepChild(transform, "WeaponFirepoint") ?? FindDeepChild(transform, "WeaponFirePoint");
		}

		private static Transform FindDeepChild(Transform parent, string name)
		{
			if (parent == null)
				return null;

			for (var i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				if (child.name == name)
					return child;

				var result = FindDeepChild(child, name);
				if (result != null)
					return result;
			}

			return null;
		}

		public void Init(Stats stats)
		{
			Model = new WeaponModel();
			Model.InjectStat(stats);
			_ammo = GetMaxAmmo();
		}

		public void TryFire(ITargetable target)
		{
			if (!CanFire())
				return;

			if (target != null && LineOfSightUtility.HasLOS(transform.position, target.Transform.position, default))
				Shoot(target.Transform);
			else
				ShootWithoutTarget();

			AfterShot();
		}

		private bool CanFire()
		{
			if (_isReloading)
			{
				if (Time.time >= _reloadFinishTime)
				{
					_isReloading = false;
					_ammo = GetMaxAmmo();
				}
				else
				{
					return false;
				}
			}

			if (Time.time < _nextFireTime)
				return false;

			if (_ammo <= 0)
				return false;

			return true;
		}

		private void AfterShot()
		{
			_ammo--;
			_nextFireTime = Time.time + 1f / Model.Stats.GetStat(StatType.FireRate).Current;

			if (_ammo <= 0)
				StartReload();
		}

		private int GetMaxAmmo()
		{
			if (Model.Stats.TryGetStat(StatType.AmmoCount, out var a))
				return Mathf.RoundToInt(a.Maximum);

			return 0;
		}

		private void StartReload()
		{
			_isReloading = true;
			_reloadFinishTime = Time.time + Model.Stats.GetStat(StatType.ReloadTime).Maximum;
		}

		// стрельба по конкретной цели
		protected abstract void Shoot(Transform target);

		// стрельба "вперёд", когда цели нет
		protected virtual void ShootWithoutTarget()
		{
			var t = FirePoint ? FirePoint : transform;
			Shoot(t);
		}

		protected float RollDamage()
		{
			var dmg = Random.Range(
				Model.Stats.GetStat(StatType.MinDamage).Current,
				Model.Stats.GetStat(StatType.MaxDamage).Current
			);

			if (Random.value < Model.Stats.GetStat(StatType.CritChance).Current)
				dmg *= Model.Stats.GetStat(StatType.CritMultiplier).Current;

			return dmg;
		}
	}
}
