using UnityEngine;
using System.IO;

namespace Ships
{
	public enum ShipSocketSize
	{
		Small,
		Medium,
		Large
	}

	/// <summary>
	/// 3D socket on the ship model used for module/weapon mounting.
	/// </summary>
	public class ShipSocketVisual : MonoBehaviour
	{
		[Header("Socket")]
		public string SocketId = "socket_01";
		public ShipGridType SocketType = ShipGridType.WeaponGrid;
		public ShipSocketSize SocketSize = ShipSocketSize.Small;

		private GameObject _metaItemInstance;
		private string _metaItemId;

		public Transform MountPoint => transform;

		public void ClearMetaItem()
		{
			_metaItemId = null;
			if (_metaItemInstance == null)
				return;

			if (Application.isPlaying)
				Destroy(_metaItemInstance);
			else
				DestroyImmediate(_metaItemInstance);

			_metaItemInstance = null;
		}

		public void SpawnMetaItem(InventoryItem item, PlayerShip ship)
		{
			if (item == null)
			{
				ClearMetaItem();
				return;
			}

			var itemId = InventoryUtils.ResolveItemId(item);
			if (_metaItemId == itemId && _metaItemInstance != null)
				return;

			ClearMetaItem();

			if (IsModuleItem(item))
			{
				var module = ModuleBuilder.BuildMeta(itemId, MountPoint, ship);
				_metaItemInstance = module != null ? module.gameObject : null;
			}
			else if (SocketType == ShipGridType.WeaponGrid)
			{
				var weapon = WeaponBuilder.BuildMeta(itemId, MountPoint, ship, item);
				_metaItemInstance = weapon != null ? weapon.gameObject : null;
			}
			else if (SocketType == ShipGridType.ModuleGrid)
			{
				var module = ModuleBuilder.BuildMeta(itemId, MountPoint, ship);
				_metaItemInstance = module != null ? module.gameObject : null;
			}

			_metaItemId = itemId;
		}

		public void GetLocalPose(Transform shipRoot, out Vector3 localPosition, out Vector3 localEuler)
		{
			if (shipRoot == null)
			{
				localPosition = transform.localPosition;
				localEuler = transform.localEulerAngles;
				return;
			}

			localPosition = shipRoot.InverseTransformPoint(transform.position);
			var localRot = Quaternion.Inverse(shipRoot.rotation) * transform.rotation;
			localEuler = localRot.eulerAngles;
		}

		public bool CanAccept(InventoryItem item)
		{
			if (item == null)
				return false;
			if (!IsTypeAllowed(item))
				return false;
			if (TryResolveSize(item, out var size) && !IsSizeAllowed(size))
				return false;
			return true;
		}

		private bool IsSizeAllowed(ShipSocketSize itemSize)
		{
			return itemSize <= SocketSize;
		}

		private bool IsTypeAllowed(InventoryItem item)
		{
			if (!TryResolveTypes(item, out var allowed))
				return true;
			if (allowed == null || allowed.Length == 0)
				return true;
			return System.Array.IndexOf(allowed, SocketType) >= 0;
		}

		private static bool IsModuleItem(InventoryItem item)
		{
			if (item == null)
				return false;

			var templateId = InventoryUtils.ResolveItemId(item);
			if (!string.IsNullOrEmpty(templateId))
				return ModuleBuilder.TryLoadModuleTemplate(templateId, out _);

			return false;
		}

		private static bool TryResolveTypes(InventoryItem item, out ShipGridType[] allowed)
		{
			allowed = null;
			if (item == null)
				return false;

			var templateId = InventoryUtils.ResolveItemId(item);
			if (!string.IsNullOrEmpty(templateId))
			{
				var templateFile = templateId.EndsWith(".json") ? templateId : templateId + ".json";
				if (ModuleBuilder.TryLoadModuleTemplate(templateFile, out var moduleTemplate))
				{
					allowed = EnumParsingHelpers.ParseGridTypes(moduleTemplate.AllowedGridTypes);
					return true;
				}

				var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateFile);
				if (ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
				{
					allowed = EnumParsingHelpers.ParseGridTypes(template.AllowedGridTypes);
					return true;
				}
			}

			return false;
		}

		private static bool TryResolveSize(InventoryItem item, out ShipSocketSize size)
		{
			size = ShipSocketSize.Small;
			if (item == null)
				return false;

			var templateId = InventoryUtils.ResolveItemId(item);
			if (!string.IsNullOrEmpty(templateId))
			{
				var templateFile = templateId.EndsWith(".json") ? templateId : templateId + ".json";
				if (ModuleBuilder.TryLoadModuleTemplate(templateFile, out var moduleTemplate))
				{
					if (TryParseSize(moduleTemplate.Size, out size))
						return true;

					return TryMapGridToSize(moduleTemplate.GridWidth, moduleTemplate.GridHeight, out size);
				}

				var templatePath = Path.Combine(PathConstant.WeaponsConfigs, templateFile);
				if (ResourceLoader.TryLoadStreamingJson(templatePath, out WeaponTemplate template))
				{
					if (TryParseSize(template.Size, out size))
						return true;

					return TryMapGridToSize(template.GridWidth, template.GridHeight, out size);
				}
			}

			return false;
		}

		private static bool TryParseSize(string source, out ShipSocketSize size)
		{
			size = ShipSocketSize.Small;
			if (string.IsNullOrEmpty(source))
				return false;

			if (System.Enum.TryParse(source, true, out ShipSocketSize parsed))
			{
				size = parsed;
				return true;
			}

			return false;
		}

		private static bool TryMapGridToSize(int w, int h, out ShipSocketSize size)
		{
			size = ShipSocketSize.Small;
			var max = Mathf.Max(w, h);
			if (max <= 1)
			{
				size = ShipSocketSize.Small;
				return true;
			}

			if (max == 2)
			{
				size = ShipSocketSize.Medium;
				return true;
			}

			if (max >= 3)
			{
				size = ShipSocketSize.Large;
				return true;
			}

			return false;
		}
	}
}
