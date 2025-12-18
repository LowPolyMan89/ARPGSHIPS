using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ships
{
	/// <summary>
	/// Centralized loading/spawning of assets (sprites, prefabs) for inventory items.
	/// Decides folders based on item slot/type from config or generated data.
	/// </summary>
	public static class ResourceLoader
	{
		private const string DefaultIconRoot = "Sprites";
		private const string DefaultPrefabRoot = "";

		// Map Slot/Type -> Resources folder
		private static readonly Dictionary<string, string> IconRoots = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "Weapon", "Sprites/Wepons" }, // keep existing folder naming
			{ "Module", "Sprites/Modules" }
		};

		private static readonly Dictionary<string, string> PrefabRoots = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "Weapon", "Weapons" },
			{ "Module", "Modules" }
		};

		private static readonly Dictionary<string, Sprite> IconCache = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, GameObject> PrefabCache = new(StringComparer.OrdinalIgnoreCase);

		public static Sprite LoadItemIcon(InventoryItem item)
		{
			var info = ResolveItemAssetInfo(item);
			if (string.IsNullOrEmpty(info.IconId))
				return null;

			var root = GetIconRoot(info.Slot);
			var key = BuildPath(root, info.IconId);

			if (IconCache.TryGetValue(key, out var cached))
				return cached;

			var sprite = Resources.Load<Sprite>(key);
			if (sprite == null)
				Debug.LogWarning($"[ResourceLoader] Sprite not found at Resources/{key} (Slot='{info.Slot}')");

			IconCache[key] = sprite;
			return sprite;
		}

		public static GameObject LoadItemPrefab(InventoryItem item)
		{
			var info = ResolveItemAssetInfo(item);
			return LoadPrefab(info.Slot, info.PrefabId);
		}

		public static GameObject InstantiateItemPrefab(InventoryItem item, Transform parent = null, bool worldPositionStays = false)
		{
			var prefab = LoadItemPrefab(item);
			if (prefab == null)
				return null;

			return UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
		}

		public static GameObject InstantiatePrefab(string slot, string prefabId, Transform parent = null, bool worldPositionStays = false)
		{
			var prefab = LoadPrefab(slot, prefabId);
			if (prefab == null)
				return null;

			return UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
		}

		private static GameObject LoadPrefab(string slot, string prefabId)
		{
			if (string.IsNullOrEmpty(prefabId))
				return null;

			var root = GetPrefabRoot(slot);
			var key = BuildPath(root, prefabId);

			if (PrefabCache.TryGetValue(key, out var cached))
				return cached;

			var prefab = Resources.Load<GameObject>(key);
			if (prefab == null)
				Debug.LogWarning($"[ResourceLoader] Prefab not found at Resources/{key} (Slot='{slot}')");

			PrefabCache[key] = prefab;
			return prefab;
		}

		private static string GetIconRoot(string slot)
		{
			if (!string.IsNullOrEmpty(slot) && IconRoots.TryGetValue(slot, out var root))
				return root;
			return DefaultIconRoot;
		}

		private static string GetPrefabRoot(string slot)
		{
			if (!string.IsNullOrEmpty(slot) && PrefabRoots.TryGetValue(slot, out var root))
				return root;
			return DefaultPrefabRoot;
		}

		private static string BuildPath(string root, string id)
		{
			return string.IsNullOrEmpty(root) ? id : $"{root}/{id}";
		}

		private static ItemAssetInfo ResolveItemAssetInfo(InventoryItem item)
		{
			var meta = LoadGeneratedMeta(item) ?? LoadTemplateMeta(item);

			return new ItemAssetInfo
			{
				Slot = !string.IsNullOrEmpty(meta?.Slot) ? meta.Slot : "Weapon",
				IconId = meta?.Icon,
				PrefabId = meta?.Prefab
			};
		}

		private static BasicItemMeta LoadGeneratedMeta(InventoryItem item)
		{
			if (item == null || string.IsNullOrEmpty(item.ItemId))
				return null;

			var generatedPath = Path.Combine(ItemGenerator.OutputPath, item.ItemId + ".json");
			if (!File.Exists(generatedPath))
				return null;

			var json = File.ReadAllText(generatedPath);
			return JsonUtility.FromJson<BasicItemMeta>(json);
		}

		private static BasicItemMeta LoadTemplateMeta(InventoryItem item)
		{
			if (item == null || string.IsNullOrEmpty(item.TemplateId))
				return null;

			var templateId = item.TemplateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? item.TemplateId
				: item.TemplateId + ".json";

			foreach (var dir in GetTemplateDirectories())
			{
				var path = Path.Combine(dir, templateId);
				if (!File.Exists(path))
					continue;

				var json = File.ReadAllText(path);
				return JsonUtility.FromJson<BasicItemMeta>(json);
			}

			return null;
		}

		private static IEnumerable<string> GetTemplateDirectories()
		{
			yield return ItemGenerator.WeaponConfigsPath;
			yield return ItemGenerator.ModulesConfigsPath;
		}

		private sealed class ItemAssetInfo
		{
			public string Slot;
			public string IconId;
			public string PrefabId;
		}

		[Serializable]
		private sealed class BasicItemMeta
		{
			public string Slot;
			public string Icon;
			public string Prefab;
		}
	}
}
