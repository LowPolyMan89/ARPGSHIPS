using System;
using System.IO;
using UnityEngine;

namespace Ships
{
	public static class ModuleBuilder
	{
		public static GameObject BuildBattle(string moduleItemId, Transform mountPoint, ShipBase owner)
		{
			return BuildInternal(moduleItemId, mountPoint, useMetaPrefab: false);
		}

		public static GameObject BuildMeta(string moduleItemId, Transform mountPoint, ShipBase owner)
		{
			return BuildInternal(moduleItemId, mountPoint, useMetaPrefab: true);
		}

		public static bool TryLoadModuleData(string itemId, out ModuleLoadData data)
		{
			data = null;
			if (string.IsNullOrEmpty(itemId))
				return false;

			var relativePath = Path.Combine(PathConstant.Inventory, itemId + ".json");
			if (!ResourceLoader.TryLoadPersistentJson(relativePath, out ModuleLoadData loaded))
				return false;

			if (!IsModuleData(loaded))
				return false;

			data = loaded;
			return true;
		}

		public static bool TryLoadModuleTemplate(string templateId, out ModuleTemplate template)
		{
			template = null;
			if (string.IsNullOrEmpty(templateId))
				return false;

			var file = templateId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? templateId
				: templateId + ".json";
			var path = Path.Combine(PathConstant.ModulesConfigs, file);
			return ResourceLoader.TryLoadStreamingJson(path, out template);
		}

		private static bool IsModuleData(ModuleLoadData data)
		{
			if (data == null)
				return false;

			if (string.Equals(data.Slot, "Module", StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		private static GameObject BuildInternal(string moduleItemId, Transform mountPoint, bool useMetaPrefab)
		{
			if (!TryLoadModuleData(moduleItemId, out var data))
			{
				Debug.LogWarning($"[ModuleBuilder] Module data not found for item '{moduleItemId}'");
				return null;
			}

			if (!TryLoadModuleTemplate(data.TemplateId, out var template))
			{
				Debug.LogWarning($"[ModuleBuilder] Module template not found for '{data.TemplateId}'");
				return null;
			}

			var prefabId = useMetaPrefab
				? (!string.IsNullOrEmpty(template.MetaPrefab) ? template.MetaPrefab : template.Prefab)
				: (!string.IsNullOrEmpty(template.BattlePrefab) ? template.BattlePrefab : template.Prefab);
			if (string.IsNullOrEmpty(prefabId))
				return null;

			var go = ResourceLoader.InstantiatePrefab(template.Slot, prefabId, mountPoint, false);
			if (go == null)
			{
				Debug.LogWarning($"[ModuleBuilder] Failed to instantiate module prefab '{prefabId}'");
				return null;
			}

			return go;
		}
	}
}
