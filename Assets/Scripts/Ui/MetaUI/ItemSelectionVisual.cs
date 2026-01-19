using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Ships
{
	public class ItemSelectionVisual : MonoBehaviour
	{
		[SerializeField] private StatInfoElementVisual _mainStatElementVisualPrefab;
		[SerializeField] private ItemVisualInfoPanel _selectedItemPanel;

		private const float OffsetX = 20f;
		private const float OffsetY = -20f;

		// ? задержка тултипа
		[SerializeField] private float _tooltipDelay = 0.4f;
		private Coroutine _tooltipRoutine;

		private static readonly Dictionary<string, WeaponStatDescriptor> WeaponStatMap =
			new Dictionary<string, WeaponStatDescriptor>(System.StringComparer.OrdinalIgnoreCase)
			{
				{ "MinDamage", new WeaponStatDescriptor("MinDamage", "damage", WeaponStatFormat.DamageRange, pairedWith: "MaxDamage") },
				{ "FireRate", new WeaponStatDescriptor("FireRate", "firerate", WeaponStatFormat.Value, unit: "r/m") },
				{ "RotationSpeed", new WeaponStatDescriptor("RotationSpeed", "rotationspeed", WeaponStatFormat.Value) },
				{ "CritChance", new WeaponStatDescriptor("CritChance", "critchance", WeaponStatFormat.Percent) },
				{ "CritMultiplier", new WeaponStatDescriptor("CritMultiplier", "critmultiplier", WeaponStatFormat.Percent) },
				{ "FireRange", new WeaponStatDescriptor("FireRange", "firerange", WeaponStatFormat.Value, unit: "m") },
				{ "ReloadTime", new WeaponStatDescriptor("ReloadTime", "reloadtime", WeaponStatFormat.Value) },
				{ "ProjectileSpeed", new WeaponStatDescriptor("ProjectileSpeed", "projectilespeed", WeaponStatFormat.Value) },
				{ "RocketSpeed", new WeaponStatDescriptor("RocketSpeed", "rocketspeed", WeaponStatFormat.Value) },
				{ "ExplosionRadius", new WeaponStatDescriptor("ExplosionRadius", "explosionradius", WeaponStatFormat.Value, unit: "m") },
				{ "Accuracy", new WeaponStatDescriptor("Accuracy", "accuracy", WeaponStatFormat.Percent) },
				{ "Penetration", new WeaponStatDescriptor("Penetration", "penetration", WeaponStatFormat.Percent) },
				{ "AmmoCount", new WeaponStatDescriptor("AmmoCount", "ammocount", WeaponStatFormat.Value) },
				{ "Spread", new WeaponStatDescriptor("Spread", "spread", WeaponStatFormat.Value) },
			};
		private void Start()
		{
			_selectedItemPanel.PanelGameObject.SetActive(false);
		}

		// ?? вызывается из инвентаря при наведении
		public void Show(InventoryItem item, PointerEventData pointerEventData)
		{
			// если уже была корутина - сбиваем
			if (_tooltipRoutine != null)
				StopCoroutine(_tooltipRoutine);

			_tooltipRoutine = StartCoroutine(ShowWithDelay(item, pointerEventData));
		}

		public void Hide()
		{
			// отменяем задержку, если была
			if (_tooltipRoutine != null)
			{
				StopCoroutine(_tooltipRoutine);
				_tooltipRoutine = null;
			}

			_selectedItemPanel.PanelGameObject.SetActive(false);
		}

		private IEnumerator ShowWithDelay(InventoryItem item, PointerEventData eventData)
		{
			yield return new WaitForSeconds(_tooltipDelay);

			// если мышь за это время ушла - тултип не нужен
			if (!IsPointerStillOver(eventData))
				yield break;

			PopulateSelectedItemPanel(item);
			_selectedItemPanel.PanelGameObject.SetActive(true);

			PositionTooltip(eventData.position, _selectedItemPanel.PanelRect);

			_tooltipRoutine = null;
		}

		// ?? проверяем, что курсор всё ещё над объектом
		private bool IsPointerStillOver(PointerEventData data)
		{
			return data.pointerEnter != null;
		}

		// -------------------------------------------
		// твой метод позиции
		// -------------------------------------------

		public void PositionTooltip(Vector2 screenPos, RectTransform tooltip)
		{
			Canvas canvas = tooltip.GetComponentInParent<Canvas>();
			RectTransform canvasRect = canvas.GetComponent<RectTransform>();

			Vector2 localPoint;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				screenPos,
				canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
				out localPoint
			);

			localPoint += new Vector2(OffsetX, OffsetY);

			Vector2 tooltipSize = tooltip.sizeDelta;
			Vector2 canvasSize = canvasRect.rect.size;

			float minX = -canvasSize.x * 0.5f;
			float maxX = canvasSize.x * 0.5f - tooltipSize.x;

			float minY = -canvasSize.y * 0.5f;
			float maxY = canvasSize.y * 0.5f - tooltipSize.y;

			localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
			localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

			tooltip.position = canvasRect.TransformPoint(localPoint);
		}

		private void PopulateSelectedItemPanel(InventoryItem item)
		{
			ClearRoot(_selectedItemPanel.MainStatRoot);
			ClearRoot(_selectedItemPanel.EffectStatRoot);

			if (item == null)
				return;

			if (!TryLoadWeaponData(item, out var weapon, out _))
				return;

			_selectedItemPanel.ItemName.text = InventoryUtils.ResolveItemId(item);

			var baseStatsDict = BuildStatsDictionary(weapon.Stats);
			var statsDict = baseStatsDict;
			if (TryBuildComposedWeaponStats(item, weapon, out var composed))
				statsDict = composed;

			var showBase = IsAltPressed();
			PopulateMainStats(statsDict, baseStatsDict, weapon.DamageType, showBase);
			if (weapon.Effects != null && weapon.Effects.Count > 0)
				AddStatElement(_mainStatElementVisualPrefab, _selectedItemPanel.MainStatRoot, "------------");
			PopulateEffectStats(weapon);
		}

		private void PopulateMainStats(
			Dictionary<string, float> stats,
			Dictionary<string, float> baseStats,
			string damageType,
			bool showBase)
		{
			if (stats == null || stats.Count == 0)
				return;

			var damageColor = GetDamageColorHex(damageType);

			foreach (var descriptor in WeaponStatMap.Values)
			{
				if (descriptor.Format == WeaponStatFormat.DamageRange)
				{
					if (!descriptor.HasPair || !TryGetStat(stats, descriptor.Name, out var min) ||
					    !TryGetStat(stats, descriptor.PairedWith, out var max))
						continue;

					if (min <= 0f && max <= 0f)
						continue;

					var baseSuffix = string.Empty;
					if (showBase && baseStats != null &&
					    TryGetStat(baseStats, descriptor.Name, out var baseMin) &&
					    TryGetStat(baseStats, descriptor.PairedWith, out var baseMax))
					{
						baseSuffix = $" ({Mathf.RoundToInt(baseMin)} - {Mathf.RoundToInt(baseMax)})";
					}

					var text = $"{descriptor.Label}: <b><color={damageColor}>{Mathf.RoundToInt(min)}</color></b> - <b><color={damageColor}>{Mathf.RoundToInt(max)}</color></b>{baseSuffix}";
					AddStatElement(_mainStatElementVisualPrefab, _selectedItemPanel.MainStatRoot, text);
					continue;
				}

				if (!TryGetStat(stats, descriptor.Name, out var value) || value <= 0f)
					continue;

				var formatted = FormatStatValue(descriptor, value, true);
				if (string.IsNullOrEmpty(formatted))
					continue;

				var baseSuffixValue = string.Empty;
				if (showBase && baseStats != null && TryGetStat(baseStats, descriptor.Name, out var baseValue))
				{
					var baseText = FormatStatValue(descriptor, baseValue, false);
					if (!string.IsNullOrEmpty(baseText))
						baseSuffixValue = $" ({baseText})";
				}

				AddStatElement(_mainStatElementVisualPrefab, _selectedItemPanel.MainStatRoot, $"{descriptor.Label}: {formatted}{baseSuffixValue}");
			}
		}

		private void PopulateEffectStats(GeneratedWeaponItem weapon)
		{
			if (weapon?.Effects == null || weapon.Effects.Count == 0)
				return;

			foreach (var effect in weapon.Effects)
			{
				if (effect?.Stats == null || effect.Stats.Count == 0)
					continue;

				var text = FormatEffectText(effect);
				if (string.IsNullOrEmpty(text))
					continue;

				AddEffectElement(_mainStatElementVisualPrefab, _selectedItemPanel.MainStatRoot, text);
			}
		}

		private static string FormatStatValue(WeaponStatDescriptor descriptor, float value, bool bold)
		{
			var prefix = bold ? "<b>" : string.Empty;
			var suffix = bold ? "</b>" : string.Empty;

			switch (descriptor.Format)
			{
				case WeaponStatFormat.Percent:
					return $"{prefix}{Mathf.RoundToInt(value * 100f)}%{suffix}";
				case WeaponStatFormat.Value:
					return descriptor.HasUnit
						? $"{prefix}{Mathf.RoundToInt(value)}{suffix} {descriptor.Unit}"
						: $"{prefix}{Mathf.RoundToInt(value)}{suffix}";
				default:
					return null;
			}
		}

		private static string FormatEffectText(EffectValue effect)
		{
			var stats = BuildStatsDictionary(effect.Stats);
			if (stats.Count == 0)
				return null;

			TryGetStat(stats, "DamagePerTick", out var damagePerTick);
			TryGetStat(stats, "MinDamage", out var minDmg);
			TryGetStat(stats, "MaxDamage", out var maxDmg);
			TryGetStat(stats, "Chance", out var chance);
			TryGetStat(stats, "Duration", out var duration);

			var parts = new List<string>();
			if (damagePerTick > 0f)
				parts.Add($"Damage per tick: <b>{Mathf.RoundToInt(damagePerTick)}</b>");
			else if (minDmg > 0f || maxDmg > 0f)
				parts.Add($"Наносит <b>{Mathf.RoundToInt(minDmg)}</b> - <b>{Mathf.RoundToInt(maxDmg)}</b> урона");

			if (chance > 0f)
				parts.Add($"с шансом <b>{Mathf.RoundToInt(chance * 100f)}%</b>");

			if (duration > 0f)
				parts.Add($"в течение <b>{Mathf.RoundToInt(duration)}</b>");

			// Добавим прочие статы, если есть
			foreach (var kv in stats)
			{
				if (kv.Key.Equals("DamagePerTick", System.StringComparison.OrdinalIgnoreCase) ||
				    kv.Key.Equals("MinDamage", System.StringComparison.OrdinalIgnoreCase) ||
				    kv.Key.Equals("MaxDamage", System.StringComparison.OrdinalIgnoreCase) ||
				    kv.Key.Equals("Chance", System.StringComparison.OrdinalIgnoreCase) ||
				    kv.Key.Equals("Duration", System.StringComparison.OrdinalIgnoreCase))
					continue;

				if (kv.Value <= 0f)
					continue;

				var isChance = kv.Key.IndexOf("chance", System.StringComparison.OrdinalIgnoreCase) >= 0;
				var valueText = isChance
					? $"<b>{Mathf.RoundToInt(kv.Value * 100f)}%</b>"
					: $"<b>{Mathf.RoundToInt(kv.Value)}</b>";

				parts.Add($"{kv.Key}: {valueText}");
			}

			if (parts.Count == 0)
				return null;

			return $"{effect.Name}: {string.Join(" ", parts)}";
		}

		private static bool TryLoadWeaponData(InventoryItem item, out GeneratedWeaponItem weapon, out WeaponTemplate template)
		{
			weapon = null;
			template = null;

			if (item == null)
				return false;

			var templateId = InventoryUtils.ResolveItemId(item);
			if (!string.IsNullOrEmpty(templateId))
			{
				templateId = templateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)
					? templateId
					: templateId + ".json";
			}

			if (!string.IsNullOrEmpty(templateId))
			{
				var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateId);
				if (ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate loadedTemplate))
				{
					template = loadedTemplate;
				}
			}

			if (template != null)
			{
				var rarityId = !string.IsNullOrEmpty(item.Rarity) ? item.Rarity : DefaultWeaponResolver.DefaultRarity;
				var rarity = FindRarity(template, rarityId);
				var resolvedRarity = rarity?.Rarity ?? rarityId ?? DefaultWeaponResolver.DefaultRarity;

				weapon = BuildWeaponFromTemplate(template, rarity, resolvedRarity);
			}

			return weapon != null;
		}

		private static WeaponTemplate.RarityEntry FindRarity(WeaponTemplate template, string rarityId)
		{
			if (template?.Rarities == null || template.Rarities.Length == 0)
				return null;

			if (!string.IsNullOrEmpty(rarityId))
			{
				for (var i = 0; i < template.Rarities.Length; i++)
				{
					var entry = template.Rarities[i];
					if (entry != null && entry.Rarity != null &&
					    entry.Rarity.Equals(rarityId, System.StringComparison.OrdinalIgnoreCase))
						return entry;
				}
			}

			return template.Rarities[0];
		}

		private static GeneratedWeaponItem BuildWeaponFromTemplate(
			WeaponTemplate template,
			WeaponTemplate.RarityEntry rarity,
			string rarityId)
		{
			return new GeneratedWeaponItem
			{
				TemplateId = template?.Id,
				Name = template?.Name,
				Rarity = string.IsNullOrEmpty(rarityId) ? DefaultWeaponResolver.DefaultRarity : rarityId,
				DamageType = template?.DamageType,
				Size = template?.Size,
				Tags = EnumParsingHelpers.NormalizeStrings(template?.Tags),
				TagValues = EnumParsingHelpers.ParseTags(template?.Tags),
				Stats = rarity?.Stats ?? Array.Empty<StatValue>(),
				Effects = rarity?.Effects != null ? new List<EffectValue>(rarity.Effects) : new List<EffectValue>()
			};
		}

		private static bool TryBuildComposedWeaponStats(
			InventoryItem item,
			GeneratedWeaponItem weapon,
			out Dictionary<string, float> composed)
		{
			composed = null;
			if (item == null || weapon == null)
				return false;

			var baseStats = WeaponStatComposer.BuildBaseStats(weapon.Stats);
			if (baseStats == null)
				return false;

			var state = MetaController.Instance != null ? MetaController.Instance.State : null;
			var includeModules = item.IsEquipped;
			var includeHull = item.IsEquipped;
			var includeMeta = true;

			var shipBuild = ShipStatBuilder.Build(state, includeHull, includeModules, includeMeta);
			var model = BuildWeaponModelForPreview(weapon, baseStats);

			var composedStats = WeaponStatComposer.Compose(baseStats, model, shipBuild.ShipStats, shipBuild.WeaponEffects);
			composed = BuildStatsDictionary(composedStats);
			return composed.Count > 0;
		}

		private static WeaponModel BuildWeaponModelForPreview(GeneratedWeaponItem weapon, Stats baseStats)
		{
			var model = new WeaponModel(baseStats)
			{
				BaseStats = baseStats,
				Size = weapon.Size,
				Tags = weapon.TagValues ?? Array.Empty<Tags>()
			};

			if (!string.IsNullOrEmpty(weapon.DamageType) && Enum.TryParse(weapon.DamageType, true, out Tags tag))
			{
				model.HasDamageType = true;
				model.DamageType = tag;
			}

			return model;
		}

		private static Dictionary<string, float> BuildStatsDictionary(IEnumerable<StatValue> stats)
		{
			var dict = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
			if (stats == null)
				return dict;

			foreach (var s in stats)
			{
				if (s == null || dict.ContainsKey(s.Name))
					continue;

				dict[s.Name] = s.Value;
			}

			return dict;
		}

		private static Dictionary<string, float> BuildStatsDictionary(Stats stats)
		{
			var dict = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
			if (stats == null)
				return dict;

			foreach (var kvp in stats.All)
			{
				dict[kvp.Key.ToString()] = kvp.Value.Maximum;
			}

			return dict;
		}

		private static bool TryGetStat(Dictionary<string, float> stats, string key, out float value)
		{
			if (stats != null && !string.IsNullOrEmpty(key))
				return stats.TryGetValue(key, out value);

			value = 0f;
			return false;
		}

		private void AddStatElement(StatInfoElementVisual prefab, RectTransform root, string text)
		{
			if (prefab == null || root == null || string.IsNullOrEmpty(text))
				return;

			var element = Instantiate(prefab, root);
			if (element.StatText != null)
				element.StatText.text = text;
		}
		private void AddEffectElement(StatInfoElementVisual prefab, RectTransform root, string text)
		{
			if (prefab == null || root == null || string.IsNullOrEmpty(text))
				return;

			var element = Instantiate(prefab, root);
			var elementRect = element.GetComponent<RectTransform>();
			if (elementRect != null)
				elementRect.sizeDelta = new Vector2(elementRect.sizeDelta.x, 70f);
			if (element.StatText != null)
				element.StatText.text = text;
		}

		private static void ClearRoot(RectTransform root)
		{
			if (root == null)
				return;

			for (var i = root.childCount - 1; i >= 0; i--)
			{
				var child = root.GetChild(i);
				if (Application.isPlaying)
					UnityEngine.Object.Destroy(child.gameObject);
				else
					UnityEngine.Object.DestroyImmediate(child.gameObject);
			}
		}

		private static bool IsAltPressed()
		{
			var keyboard = Keyboard.current;
			if (keyboard == null)
				return false;

			return (keyboard.leftAltKey != null && keyboard.leftAltKey.isPressed) ||
			       (keyboard.rightAltKey != null && keyboard.rightAltKey.isPressed);
		}

		private static string GetDamageColorHex(string damageType)
		{
			if (string.IsNullOrEmpty(damageType))
				return "#FFFFFF";

			switch (damageType.ToLowerInvariant())
			{
				case "kinetic":
					return "#FFD54F"; // жёлтый
				case "thermal":
					return "#FF6B6B"; // красный
				case "energy":
				case "energetic":
					return "#64B5F6"; // синий
				default:
					return "#FFFFFF";
			}
		}
	}

	[System.Serializable]
	public class ItemVisualInfoPanel
	{
		public GameObject PanelGameObject;
		public TMP_Text ItemName;
		public RectTransform MainStatRoot;
		public RectTransform EffectStatRoot;
		public Button EquipButton;
		public Button UnEquipButton;
		public RectTransform PanelRect;
	}

	public enum WeaponStatFormat
	{
		Value,
		Percent,
		DamageRange
	}

	public readonly struct WeaponStatDescriptor
	{
		public readonly string Name;
		public readonly string LocId;
		public readonly WeaponStatFormat Format;
		public readonly string Unit;
		public readonly string PairedWith;

		public string Label => Localization.GetLoc(LocId);
		public bool HasUnit => !string.IsNullOrEmpty(Unit);
		public bool HasPair => !string.IsNullOrEmpty(PairedWith);

		public WeaponStatDescriptor(string name, string locId, WeaponStatFormat format, string unit = null, string pairedWith = null)
		{
			Name = name;
			LocId = string.IsNullOrEmpty(locId) ? name : locId;
			Format = format;
			Unit = unit;
			PairedWith = pairedWith;
		}
	}
}

