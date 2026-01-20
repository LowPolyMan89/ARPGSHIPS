using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		private static readonly Dictionary<string, GameObject> IconPrefabCache = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, GameObject> PrefabCache = new(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<string, Sprite> ShipIconCache = new(StringComparer.OrdinalIgnoreCase);

		public static string GetStreamingPath(params string[] segments) =>
			CombinePath(Application.streamingAssetsPath, segments);

		public static string GetPersistentPath(params string[] segments) =>
			CombinePath(Application.persistentDataPath, segments);

		public static IReadOnlyList<string> GetStreamingFiles(string relativeFolder, string searchPattern = "*.*")
		{
			var folder = GetStreamingPath(relativeFolder);
			if (!Directory.Exists(folder))
				return Array.Empty<string>();

			return Directory.GetFiles(folder, searchPattern)
				.Select(Path.GetFileName)
				.Where(f => !string.IsNullOrEmpty(f))
				.ToArray();
		}

		public static IReadOnlyList<string> GetPersistentFiles(string relativeFolder, string searchPattern = "*.*")
		{
			var folder = GetPersistentPath(relativeFolder);
			if (!Directory.Exists(folder))
				return Array.Empty<string>();

			return Directory.GetFiles(folder, searchPattern)
				.Select(Path.GetFileName)
				.Where(f => !string.IsNullOrEmpty(f))
				.ToArray();
		}

		public static bool StreamingFileExists(string relativePath) =>
			File.Exists(GetStreamingPath(relativePath));

		public static bool PersistentFileExists(string relativePath) =>
			File.Exists(GetPersistentPath(relativePath));

		public static bool TryReadStreamingText(string relativePath, out string content) =>
			TryReadText(GetStreamingPath(relativePath), out content);

		public static bool TryReadPersistentText(string relativePath, out string content) =>
			TryReadText(GetPersistentPath(relativePath), out content);

		public static bool TryLoadStreamingJson<T>(string relativePath, out T data) =>
			TryLoadJson(GetStreamingPath(relativePath), out data);

		public static bool TryLoadPersistentJson<T>(string relativePath, out T data) =>
			TryLoadJson(GetPersistentPath(relativePath), out data);

		public static void SavePersistentJson<T>(string relativePath, T data, bool prettyPrint = true)
		{
			var fullPath = GetPersistentPath(relativePath);
			EnsureDirectoryForFile(fullPath);

			var json = JsonUtility.ToJson(data, prettyPrint);
			File.WriteAllText(fullPath, json);
		}

		public static void EnsurePersistentFolder(string relativeFolder)
		{
			var path = GetPersistentPath(relativeFolder);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		public static Sprite LoadItemIcon(InventoryItem item)
		{
			return LoadItemIcon(item, ItemIconContext.Inventory);
		}

		public static Sprite LoadItemIcon(InventoryItem item, ItemIconContext context)
		{
			var info = ResolveItemAssetInfo(item);
			var iconId = info.GetIconId(context);
			if (string.IsNullOrEmpty(iconId))
				return null;

			var root = GetIconRoot(info.Slot);
			var key = BuildPath(root, iconId);

			if (IconCache.TryGetValue(key, out var cached))
				return cached;

			var sprite = Resources.Load<Sprite>(key);
			if (sprite == null)
				Debug.LogWarning($"[ResourceLoader] Sprite not found at Resources/{key} (Slot='{info.Slot}')");

			IconCache[key] = sprite;
			return sprite;
		}

		public static GameObject LoadItemIconPrefab(InventoryItem item, ItemIconContext context)
		{
			var info = ResolveItemAssetInfo(item);
			var iconId = info.GetIconId(context);
			if (string.IsNullOrEmpty(iconId))
				return null;

			var root = GetIconRoot(info.Slot);
			var key = BuildPath(root, iconId);

			if (IconPrefabCache.TryGetValue(key, out var cached))
				return cached;

			var prefab = Resources.Load<GameObject>(key);
			if (prefab == null)
				Debug.LogWarning($"[ResourceLoader] Icon prefab not found at Resources/{key} (Slot='{info.Slot}')");

			IconPrefabCache[key] = prefab;
			return prefab;
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

		public static GameObject InstantiatePrefabById(string prefabId, Transform parent = null, bool worldPositionStays = false)
		{
			if (string.IsNullOrEmpty(prefabId))
				return null;

			if (PrefabCache.TryGetValue(prefabId, out var cached))
				return UnityEngine.Object.Instantiate(cached, parent, worldPositionStays);

			var prefab = LoadByIdWithFallback(prefabId);
			if (prefab == null)
			{
				Debug.LogWarning($"[ResourceLoader] Prefab not found at Resources/{prefabId}");
				return null;
			}

			PrefabCache[prefabId] = prefab;
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

		private static GameObject LoadByIdWithFallback(string id)
		{
			// Try exact id, then common folders (Ships/*) if no folder specified.
			var prefab = Resources.Load<GameObject>(id);
			if (prefab != null)
				return prefab;

			if (!id.Contains("/"))
			{
				prefab = Resources.Load<GameObject>($"Ships/{id}");
				if (prefab != null)
					return prefab;
			}

			return null;
		}

		private static ItemAssetInfo ResolveItemAssetInfo(InventoryItem item)
		{
			var meta = LoadTemplateMeta(item);

			var iconInventory = ResolveIcon(meta?.IconInventory, meta?.Icon);
			// Для drag не используем фолбэки: если IconOnDrag пустой, вернём null.
			var iconOnDrag = string.IsNullOrEmpty(meta?.IconOnDrag) ? null : meta.IconOnDrag;
			var iconOnFit = ResolveIcon(meta?.IconOnFit, iconOnDrag, iconInventory);
			var prefabId = !string.IsNullOrEmpty(meta?.MetaPrefab) ? meta.MetaPrefab : meta?.Prefab;

			return new ItemAssetInfo
			{
				Slot = !string.IsNullOrEmpty(meta?.Slot) ? meta.Slot : "Weapon",
				IconInventory = iconInventory,
				IconOnDrag = iconOnDrag,
				IconOnFit = iconOnFit,
				PrefabId = prefabId
			};
		}

		private static string ResolveIcon(params string[] values)
		{
			for (var i = 0; i < values.Length; i++)
			{
				if (!string.IsNullOrEmpty(values[i]))
					return values[i];
			}

			return null;
		}

		public static Sprite LoadShipIcon(string hullId)
		{
			if (string.IsNullOrEmpty(hullId))
				return null;

			if (ShipIconCache.TryGetValue(hullId, out var cached))
				return cached;

			var hull = HullLoader.Load(hullId);
			var iconId = hull != null ? hull.ShipIcon : null;
			if (string.IsNullOrEmpty(iconId))
				return null;

			var key = $"Sprites/Ships/{iconId}";
			var sprite = Resources.Load<Sprite>(key);
			if (sprite == null)
				Debug.LogWarning($"[ResourceLoader] Ship icon not found at Resources/{key}");

			ShipIconCache[hullId] = sprite;
			return sprite;
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
				if (TryLoadJson(path, out BasicItemMeta meta))
					return meta;
			}

			return null;
		}

		private static IEnumerable<string> GetTemplateDirectories()
		{
			yield return ItemGenerator.WeaponConfigsPath;
			yield return ItemGenerator.ModulesConfigsPath;
		}

		private static bool TryReadText(string fullPath, out string content)
		{
			content = null;

			if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
				return false;

			try
			{
				content = File.ReadAllText(fullPath);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[ResourceLoader] Failed to read file '{fullPath}': {ex.Message}");
				return false;
			}
		}

		private static bool TryLoadJson<T>(string fullPath, out T data)
		{
			data = default;
			if (!TryReadText(fullPath, out var json))
				return false;

			try
			{
				data = JsonUtility.FromJson<T>(json);
				return data != null;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[ResourceLoader] Failed to parse JSON at '{fullPath}': {ex.Message}");
				return false;
			}
		}

		private static void EnsureDirectoryForFile(string fullPath)
		{
			var dir = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}

		private static string CombinePath(string root, params string[] segments)
		{
			if (segments == null || segments.Length == 0)
				return root;

			var result = root;
			for (var i = 0; i < segments.Length; i++)
			{
				if (string.IsNullOrEmpty(segments[i]))
					continue;

				result = Path.Combine(result, segments[i]);
			}

			return result;
		}

		private sealed class ItemAssetInfo
		{
			public string Slot;
			public string IconInventory;
			public string IconOnDrag;
			public string IconOnFit;
			public string PrefabId;

			public string GetIconId(ItemIconContext context)
			{
				switch (context)
				{
					case ItemIconContext.Drag:
						// Если иконка для драга не указана, вернём null чтобы можно было показать мета-префаб.
						if (!string.IsNullOrEmpty(IconOnDrag))
							return IconOnDrag;
						return null;

					case ItemIconContext.Fit:
						if (!string.IsNullOrEmpty(IconOnFit))
							return IconOnFit;
						if (!string.IsNullOrEmpty(IconOnDrag))
							return IconOnDrag;
						return IconInventory;

					case ItemIconContext.Inventory:
					default:
						return IconInventory;
				}
			}
		}

		[Serializable]
		private sealed class BasicItemMeta
		{
			public string Slot;
			public string Icon;
			public string IconInventory;
			public string IconOnDrag;
			public string IconOnFit;
			public string Prefab;
			public string BattlePrefab;
			public string MetaPrefab;
		}
	}

	public enum ItemIconContext
	{
		Inventory,
		Drag,
		Fit
	}
}
