using System.IO;
using UnityEngine;

namespace Ships
{
	public abstract class WeaponBase : MonoBehaviour
	{
		private float _nextFireTime;
		public string WeaponTemplateId;
		public WeaponModel Model;

		[Header("Prefab Parts")]
		[Tooltip("Неподвижная часть оружия (база). Если не задано, используется root.")]
		public Transform BaseTransform;
		[Tooltip("Вращающаяся часть (турель). Если не задано, используется root.")]
		public Transform TurretTransform;

		public bool IsActive = true;
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
			TryInitFromTemplate();
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
			if (!Owner)
			{
				Owner = GetComponentInParent<ShipBase>();
			}
		}

		public void TryFire(ITargetable target)
		{
			if (!IsActive)
				return;

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
			return Random.Range(
				Model.Stats.GetStat(StatType.MinDamage).Current,
				Model.Stats.GetStat(StatType.MaxDamage).Current
			);
		}

		private void TryInitFromTemplate()
		{
			if (Model?.Stats != null)
				return;

			if (string.IsNullOrEmpty(WeaponTemplateId))
				return;

			var templateFile = WeaponTemplateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
				? WeaponTemplateId
				: WeaponTemplateId + ".json";

			var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateFile);
			if (!ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
			{
				Debug.LogWarning($"[WeaponBase] Weapon template not found: {templatePath}");
				return;
			}

			var stats = BuildStatsFromTemplate(template);
			if (stats == null)
				return;

			Init(stats);
			Model.BaseStats = stats.Clone();
			Model.Size = template.Size;
			Model.Tags = EnumParsingHelpers.ParseTags(template.Tags);
			FireArcDeg = template.FireArcDeg <= 0 ? 360f : template.FireArcDeg;
			ApplyTemplateMeta(Model, template);
		}

		private static Stats BuildStatsFromTemplate(WeaponTemplate template)
		{
			if (template == null || template.Rarities == null || template.Rarities.Length == 0)
				return null;

			var rarity = template.Rarities[0];
			var statEntries = rarity?.Stats?.Entries;
			if (statEntries == null || statEntries.Length == 0)
				return null;

			var stats = new Stats();
			for (var i = 0; i < statEntries.Length; i++)
			{
				var entry = statEntries[i];
				if (entry == null || string.IsNullOrEmpty(entry.Name))
					continue;

				if (!System.Enum.TryParse(entry.Name, true, out StatType statType))
					continue;

				var value = Random.Range(entry.Min, entry.Max);
				if (Mathf.Abs(value) > 10f)
					value = Mathf.Round(value);
				else
					value = (float)System.Math.Round(value, 2);

				stats.AddStat(new Stat(statType, value));
			}

			return stats;
		}

		private static void ApplyTemplateMeta(WeaponModel model, WeaponTemplate template)
		{
			if (model == null || template == null)
				return;

			model.IsAutoFire = ResolveIsAutoFire(template);

			if (TryResolveDamageType(template, out var damageTag))
			{
				model.HasDamageType = true;
				model.DamageType = damageTag;
			}
		}

		private static bool ResolveIsAutoFire(WeaponTemplate template)
		{
			if (template?.Tags == null || template.Tags.Length == 0)
				return false;

			var tags = EnumParsingHelpers.ParseTags(template.Tags);
			for (var i = 0; i < tags.Length; i++)
			{
				if (tags[i] == Tags.Automatic)
					return true;
			}

			return false;
		}

		private static bool TryResolveDamageType(WeaponTemplate template, out Tags tag)
		{
			if (template == null)
			{
				tag = default;
				return false;
			}

			if (TryParseDamageType(template.DamageType, out tag))
				return true;

			if (template.Tags != null && template.Tags.Length > 0)
			{
				var tags = EnumParsingHelpers.ParseTags(template.Tags);
				for (var i = 0; i < tags.Length; i++)
				{
					if (IsDamageTag(tags[i]))
					{
						tag = tags[i];
						return true;
					}
				}
			}

			tag = default;
			return false;
		}

		private static bool TryParseDamageType(string value, out Tags tag)
		{
			if (string.IsNullOrEmpty(value))
			{
				tag = default;
				return false;
			}

			if (!System.Enum.TryParse(value, true, out tag))
				return false;

			return IsDamageTag(tag);
		}

		private static bool IsDamageTag(Tags tag)
		{
			return tag == Tags.Kinetic || tag == Tags.Thermal || tag == Tags.Energy;
		}
	}
}
