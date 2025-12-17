using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ships
{
	public class ShipFitView
	{
		private MetaState _state;

		public event Action OnFitChanged;

		public void Init(MetaState state)
		{
			_state = state;
		}

		public void UnequipItem(InventoryItem item)
		{
			if (item == null || !item.IsEquipped)
				return;

			_state.Fit.GridPlacements.RemoveAll(x => x.ItemId == item.ItemId);

			item.EquippedOnFitId = null;
			item.EquippedGridId = null;
			item.EquippedGridX = -1;
			item.EquippedGridY = -1;
		}

		public bool TryPlaceWeaponToGrid(string gridId, int gridWidth, int gridHeight, int x, int y, InventoryItem item)
		{
			if (item == null)
				return false;

			if (item.IsEquipped && item.EquippedOnFitId != _state.Fit.ShipId)
				UnequipItem(item);

			if (!TryResolveWeaponGridSize(item, out var w, out var h, out var allowed))
			{
				Debug.LogWarning($"[ShipFitView] Can't resolve grid size for item '{item.ItemId}' (TemplateId='{item.TemplateId}')");
				return false;
			}

			if (allowed != null && allowed.Length > 0)
			{
				var ok = false;
				for (var i = 0; i < allowed.Length; i++)
				{
					if (allowed[i] == ShipGridType.WeaponGrid)
					{
						ok = true;
						break;
					}
				}

				if (!ok)
				{
					Debug.LogWarning($"[ShipFitView] Item '{item.ItemId}' is not allowed in WeaponGrid");
					return false;
				}
			}

			var existing = _state.Fit.GridPlacements.FindAll(p => p.GridId == gridId);
			if (!GridPlacementUtility.CanPlaceRect(gridWidth, gridHeight, existing, x, y, w, h, ignoreItemId: item.ItemId))
			{
				var reason = DescribePlacementFailure(gridWidth, gridHeight, existing, x, y, w, h, item.ItemId);
				Debug.LogWarning(
					$"[ShipFitView] Can't place '{item.ItemId}' (size {w}x{h}) to '{gridId}' {gridWidth}x{gridHeight} at ({x},{y}): {reason}");
				return false;
			}

			_state.Fit.GridPlacements.RemoveAll(p => p.ItemId == item.ItemId);

			_state.Fit.GridPlacements.Add(new ShipFitModel.GridPlacement
			{
				GridId = gridId,
				GridType = ShipGridType.WeaponGrid,
				ItemId = item.ItemId,
				X = x,
				Y = y,
				Width = w,
				Height = h
			});

			item.EquippedOnFitId = _state.Fit.ShipId;
			item.EquippedGridId = gridId;
			item.EquippedGridX = x;
			item.EquippedGridY = y;

			OnFitChanged?.Invoke();
			MetaSaveSystem.Save(_state);
			return true;
		}

		private static string DescribePlacementFailure(
			int gridWidth,
			int gridHeight,
			IReadOnlyList<ShipFitModel.GridPlacement> existing,
			int x,
			int y,
			int width,
			int height,
			string ignoreItemId)
		{
			if (width <= 0 || height <= 0)
				return "invalid item size";

			if (x < 0 || y < 0)
				return "negative coordinates";

			if (x + width > gridWidth || y + height > gridHeight)
				return "out of bounds";

			for (var i = 0; i < existing.Count; i++)
			{
				var p = existing[i];
				if (!string.IsNullOrEmpty(ignoreItemId) && p.ItemId == ignoreItemId)
					continue;

				var aRight = x + width;
				var aTop = y + height;
				var bRight = p.X + p.Width;
				var bTop = p.Y + p.Height;
				var overlap = x < bRight && aRight > p.X && y < bTop && aTop > p.Y;
				if (overlap)
					return $"overlaps '{p.ItemId}' at ({p.X},{p.Y}) size {p.Width}x{p.Height}";
			}

			return "unknown";
		}

		private static bool TryResolveWeaponGridSize(InventoryItem item, out int width, out int height, out ShipGridType[] allowedGridTypes)
		{
			width = 1;
			height = 1;
			allowedGridTypes = null;

			// 1) Prefer generated runtime item (persistentDataPath/Inventory/{ItemId}.json)
			if (!string.IsNullOrEmpty(item.ItemId))
			{
				var generatedPath = Path.Combine(ItemGenerator.OutputPath, item.ItemId + ".json");
				if (File.Exists(generatedPath))
				{
					var json = File.ReadAllText(generatedPath);
					var weapon = JsonUtility.FromJson<GeneratedWeaponItem>(json);
					if (weapon != null)
					{
						width = weapon.GridWidth > 0 ? weapon.GridWidth : 1;
						height = weapon.GridHeight > 0 ? weapon.GridHeight : 1;
						allowedGridTypes = weapon.AllowedGridTypes;
						return true;
					}
				}
			}

			// 2) Fallback to template in StreamingAssets/Configs/Weapons/{TemplateId}.json
			if (string.IsNullOrEmpty(item.TemplateId))
				return false;

			var templateId = item.TemplateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? item.TemplateId
				: item.TemplateId + ".json";

			var templatePath = Path.Combine(ItemGenerator.WeaponConfigsPath, templateId);
			if (!File.Exists(templatePath))
				return false;

			var templateJson = File.ReadAllText(templatePath);
			var template = JsonUtility.FromJson<WeaponTemplate>(templateJson);
			if (template == null)
				return false;

			width = template.GridWidth > 0 ? template.GridWidth : 1;
			height = template.GridHeight > 0 ? template.GridHeight : 1;
			allowedGridTypes = template.AllowedGridTypes;
			return true;
		}
	}
}
