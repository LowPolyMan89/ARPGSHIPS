using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class ShipGridArcRenderer : WeaponArcRenderer
	{
		[SerializeField] private float _pixelsPerRangeUnit = 4f;
		[SerializeField] private float _minRadius = 8f;
		[SerializeField] private float _maxRadius = 256f;

		public void RenderArc(InventoryItem item, ShipGridVisual grid, ShipFitModel.GridPlacement placement)
		{
			if (item == null || grid == null || placement == null)
			{
				ClearAndHide();
				return;
			}

			if (!TryLoadWeaponData(item, out var weapon, out var template))
			{
				ClearAndHide();
				return;
			}

			var arcDeg = weapon.FireArcDeg;
			if (arcDeg <= 0f)
			{
				ClearAndHide();
				return;
			}

			var range = ResolveRange(weapon, template);
			if (range <= 0f)
			{
				ClearAndHide();
				return;
			}

			var radiusPx = Mathf.Clamp(range * _pixelsPerRangeUnit, _minRadius, _maxRadius);
			BuildMesh(arcDeg, radiusPx, placement, grid.CellSize);
			if (!gameObject.activeSelf)
				gameObject.SetActive(true);
		}

		private void BuildMesh(float arcDeg, float radiusPx, ShipFitModel.GridPlacement placement, float cellSize)
		{
			CacheComponents();
			MatchRectToParent();

			var widthPx = placement.Width * cellSize;
			var heightPx = placement.Height * cellSize;
			var center = new Vector3(widthPx * 0.5f, heightPx * 0.5f, 0f);

			RenderArc(arcDeg, radiusPx, center, ArcSpace.Canvas);
		}

		private float ResolveRange(GeneratedWeaponItem weapon, WeaponTemplate template)
		{
			var stats = BuildStatsDictionary(weapon?.Stats);
			if (stats.Count == 0 && template?.Rarities != null && template.Rarities.Length > 0)
			{
				stats = BuildStatsDictionary(ConvertRangesToValues(template.Rarities[0].Stats));
			}

			return TryGetStat(stats, "FireRange", out var range) ? range : 0f;
		}

		private static bool TryLoadWeaponData(InventoryItem item, out GeneratedWeaponItem weapon, out WeaponTemplate template)
		{
			weapon = null;
			template = null;

			if (item == null)
				return false;

			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var generatedPath = Path.Combine(PathConstant.Inventory, item.ItemId + ".json");
				if (ResourceLoader.TryLoadPersistentJson(generatedPath, out GeneratedWeaponItem loadedWeapon))
				{
					weapon = loadedWeapon;
				}
			}

			var templateId = !string.IsNullOrEmpty(item.TemplateId)
				? (item.TemplateId.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase) ? item.TemplateId : item.TemplateId + ".json")
				: weapon?.TemplateId + ".json";

			if (!string.IsNullOrEmpty(templateId))
			{
				var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateId);
				if (ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate loadedTemplate))
				{
					template = loadedTemplate;
				}
			}

			if (weapon == null && template != null)
			{
				var rarity = template.Rarities != null && template.Rarities.Length > 0
					? template.Rarities[0]
					: null;

				weapon = new GeneratedWeaponItem
				{
					FireArcDeg = template.FireArcDeg,
					EnergyCost = template.EnergyCost,
					Stats = ConvertRangesToValues(rarity?.Stats)
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

		private void MatchRectToParent()
		{
			if (_rectTransform == null)
				return;

			if (transform.parent is RectTransform parentRect)
			{
				_rectTransform.anchorMin = new Vector2(0, 0);
				_rectTransform.anchorMax = new Vector2(0, 0);
				_rectTransform.pivot = new Vector2(0, 0);
				_rectTransform.anchoredPosition = Vector2.zero;
				_rectTransform.sizeDelta = parentRect.rect.size;
			}
		}
	}
}
