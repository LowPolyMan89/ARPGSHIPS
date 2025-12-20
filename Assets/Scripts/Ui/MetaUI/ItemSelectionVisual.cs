using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ships
{
	public class ItemSelectionVisual : MonoBehaviour
	{
		[SerializeField] private StatInfoElementVisual _mainStatElementVisualPrefab;
		[SerializeField] private StatInfoElementVisual _effectStatElementVisualPrefab;
		[SerializeField] private ItemVisualInfoPanel _selectedItemPanel;
		[SerializeField] private ItemVisualInfoPanel _compareItemPanel;

		private const float OffsetX = 20f;
		private const float OffsetY = -20f;

		// ? задержка тултипа
		[SerializeField] private float _tooltipDelay = 0.4f;

		private Coroutine _tooltipRoutine;

		private static readonly Dictionary<string, WeaponStatDescriptor> WeaponStatMap =
			new Dictionary<string, WeaponStatDescriptor>(System.StringComparer.OrdinalIgnoreCase)
			{
				{ "MinDamage", new WeaponStatDescriptor("MinDamage", "Урон", WeaponStatFormat.DamageRange, pairedWith: "MaxDamage") },
				{ "FireRate", new WeaponStatDescriptor("FireRate", "Скорострельность", WeaponStatFormat.Value, unit: "в/м") },
				{ "RotationSpeed", new WeaponStatDescriptor("RotationSpeed", "Угол поворота", WeaponStatFormat.Value) },
				{ "CritChance", new WeaponStatDescriptor("CritChance", "Шанс крита", WeaponStatFormat.Percent) },
				{ "CritMultiplier", new WeaponStatDescriptor("CritMultiplier", "Размер крита", WeaponStatFormat.Percent) },
				{ "FireRange", new WeaponStatDescriptor("FireRange", "Радиус стрельбы", WeaponStatFormat.Value, unit: "м") },
			};

		private void Start()
		{
			_selectedItemPanel.PanelGameObject.SetActive(false);
			_compareItemPanel.PanelGameObject.SetActive(false);
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
			_compareItemPanel.PanelGameObject.SetActive(false);
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

			if (_selectedItemPanel.ItemImage != null)
			{
				var sprite = ResourceLoader.LoadItemIcon(item);
				_selectedItemPanel.ItemImage.sprite = sprite;
				_selectedItemPanel.ItemImage.enabled = sprite != null;
			}

			var statsDict = BuildStatsDictionary(weapon.Stats);
			PopulateMainStats(statsDict, weapon.DamageType);
			PopulateEffectStats(weapon);
		}

		private void PopulateMainStats(Dictionary<string, float> stats, string damageType)
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

					var text = $"{descriptor.Label}: <b><color={damageColor}>{Mathf.RoundToInt(min)}</color></b> - <b><color={damageColor}>{Mathf.RoundToInt(max)}</color></b>";
					AddStatElement(_mainStatElementVisualPrefab, _selectedItemPanel.MainStatRoot, text);
					continue;
				}

				if (!TryGetStat(stats, descriptor.Name, out var value) || value <= 0f)
					continue;

				var formatted = FormatStatValue(descriptor, value);
				if (string.IsNullOrEmpty(formatted))
					continue;

				AddStatElement(_mainStatElementVisualPrefab, _selectedItemPanel.MainStatRoot, $"{descriptor.Label}: {formatted}");
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

				AddStatElement(_effectStatElementVisualPrefab, _selectedItemPanel.EffectStatRoot, text);
			}
		}

		private static string FormatStatValue(WeaponStatDescriptor descriptor, float value)
		{
			switch (descriptor.Format)
			{
				case WeaponStatFormat.Percent:
					return $"<b>{Mathf.RoundToInt(value * 100f)}%</b>";
				case WeaponStatFormat.Value:
					return descriptor.HasUnit
						? $"<b>{Mathf.RoundToInt(value)}</b> {descriptor.Unit}"
						: $"<b>{Mathf.RoundToInt(value)}</b>";
				default:
					return null;
			}
		}

		private static string FormatEffectText(EffectValue effect)
		{
			var stats = BuildStatsDictionary(effect.Stats);
			if (stats.Count == 0)
				return null;

			TryGetStat(stats, "MinDamage", out var minDmg);
			TryGetStat(stats, "MaxDamage", out var maxDmg);
			TryGetStat(stats, "Chance", out var chance);
			TryGetStat(stats, "Duration", out var duration);

			var parts = new List<string>();
			if (minDmg > 0f || maxDmg > 0f)
				parts.Add($"Наносит <b>{Mathf.RoundToInt(minDmg)}</b> - <b>{Mathf.RoundToInt(maxDmg)}</b> урона");

			if (chance > 0f)
				parts.Add($"с шансом <b>{Mathf.RoundToInt(chance * 100f)}%</b>");

			if (duration > 0f)
				parts.Add($"в течение <b>{Mathf.RoundToInt(duration)}</b>");

			// Добавим прочие статы, если есть
			foreach (var kv in stats)
			{
				if (kv.Key.Equals("MinDamage", System.StringComparison.OrdinalIgnoreCase) ||
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

			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var generatedPath = Path.Combine(ItemGenerator.OutputPath, item.ItemId + ".json");
				if (File.Exists(generatedPath))
				{
					var json = File.ReadAllText(generatedPath);
					weapon = JsonUtility.FromJson<GeneratedWeaponItem>(json);
				}
			}

			var templateId = !string.IsNullOrEmpty(item.TemplateId)
				? (item.TemplateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase) ? item.TemplateId : item.TemplateId + ".json")
				: weapon?.TemplateId + ".json";

			if (!string.IsNullOrEmpty(templateId))
			{
				var templatePath = Path.Combine(ItemGenerator.WeaponConfigsPath, templateId);
				if (File.Exists(templatePath))
				{
					var templateJson = File.ReadAllText(templatePath);
					template = JsonUtility.FromJson<WeaponTemplate>(templateJson);
				}
			}

			if (weapon == null && template != null)
			{
				var rarity = template.Rarities != null && template.Rarities.Length > 0
					? template.Rarities[0]
					: null;

				weapon = new GeneratedWeaponItem
				{
					DamageType = template.DamageType,
					Stats = ConvertRangesToValues(rarity?.Stats),
					Effects = new List<EffectValue>()
				};
			}

			return weapon != null;
		}

		private static StatValue[] ConvertRangesToValues(StatRangeList ranges)
		{
			if (ranges?.Entries == null || ranges.Entries.Length == 0)
				return System.Array.Empty<StatValue>();

			var result = new StatValue[ranges.Entries.Length];
			for (var i = 0; i < ranges.Entries.Length; i++)
			{
				var entry = ranges.Entries[i];
				var avg = (entry.Min + entry.Max) * 0.5f;
				result[i] = new StatValue { Name = entry.Name, Value = avg };
			}

			return result;
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

		private static void ClearRoot(RectTransform root)
		{
			if (root == null)
				return;

			for (var i = root.childCount - 1; i >= 0; i--)
			{
				var child = root.GetChild(i);
				if (Application.isPlaying)
					Object.Destroy(child.gameObject);
				else
					Object.DestroyImmediate(child.gameObject);
			}
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
		public Image ItemImage;
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
		public readonly string Label;
		public readonly WeaponStatFormat Format;
		public readonly string Unit;
		public readonly string PairedWith;

		public bool HasUnit => !string.IsNullOrEmpty(Unit);
		public bool HasPair => !string.IsNullOrEmpty(PairedWith);

		public WeaponStatDescriptor(string name, string label, WeaponStatFormat format, string unit = null, string pairedWith = null)
		{
			Name = name;
			Label = label;
			Format = format;
			Unit = unit;
			PairedWith = pairedWith;
		}
	}
}
