

namespace Ships
{
	using UnityEngine;
	using System.Collections.Generic;

	public class MetaController : MonoBehaviour
	{
		public static MetaController Instance;

		public MetaState State { get; private set; }
		[SerializeField] private MetaVisual _metaVisual;
		[SerializeField] private Camera _metaCamera;
		[SerializeField] private float _cameraPadding = 1.15f;
		[SerializeField] private float _minCameraDistance = 2f;
		[SerializeField] private float _maxCameraDistance = 100f;
		public Transform ShipPodium;
		private InventoryView _inventoryView;
		public MetaVisual MetaVisual => _metaVisual;
		private GameObject _spawnedMetaShip;

		private void Awake()
		{
			if (!Instance)
				Instance = this;
			else
				Destroy(gameObject);

			if (_metaCamera == null)
				_metaCamera = Camera.main;

		    Localization.LoadLocalizationDataFromConfig();
			State = MetaSaveSystem.Load();

			if (string.IsNullOrEmpty(State.SelectedShipId))
				State.SelectedShipId = "hull_flagman_1";
			if (string.IsNullOrEmpty(State.Fit.ShipId))
				State.Fit.ShipId = State.SelectedShipId;

			EnsureShipFits();
			
			if (State.InventoryModel.InventoryUniqueItems.Count == 0)
				MetaSaveSystem.Save(State);

			_inventoryView = new InventoryView();
			_inventoryView.Init(State);

			if (_metaVisual != null && _metaVisual.InventoryVisual != null)
				_metaVisual.InventoryVisual.Init(_inventoryView);

			SpawnMetaShip();
		}

		private void EnsureShipFits()
		{
			if (State.PlayerShipFits == null)
				State.PlayerShipFits = new List<ShipFitModel>();

			if (State.Fit == null)
				State.Fit = new ShipFitModel();

			var selectedId = !string.IsNullOrEmpty(State.SelectedShipId)
				? State.SelectedShipId
				: State.Fit.ShipId;

			if (!string.IsNullOrEmpty(selectedId))
				State.SelectedShipId = selectedId;

			if (string.IsNullOrEmpty(State.Fit.ShipId) && !string.IsNullOrEmpty(selectedId))
				State.Fit.ShipId = selectedId;

			var existing = State.PlayerShipFits.Find(f =>
				f != null &&
				!string.IsNullOrEmpty(f.ShipId) &&
				f.ShipId.Equals(State.Fit.ShipId, System.StringComparison.OrdinalIgnoreCase));
			if (existing == null)
				State.PlayerShipFits.Add(State.Fit);
			else
				State.Fit = existing;
		}

		public ShipFitModel GetOrCreateFit(string shipId)
		{
			if (string.IsNullOrEmpty(shipId))
				return null;

			var fit = State.PlayerShipFits.Find(f =>
				f != null &&
				!string.IsNullOrEmpty(f.ShipId) &&
				f.ShipId.Equals(shipId, System.StringComparison.OrdinalIgnoreCase));
			if (fit != null)
				return fit;

			fit = new ShipFitModel { ShipId = shipId };
			State.PlayerShipFits.Add(fit);
			return fit;
		}

		public void SetActiveShip(string shipId)
		{
			if (string.IsNullOrEmpty(shipId))
				return;

			var fit = GetOrCreateFit(shipId);
			if (fit == null)
				return;

			State.SelectedShipId = shipId;
			State.Fit = fit;
			MetaSaveSystem.Save(State);
			SpawnMetaShip();
		}

		private void SpawnMetaShip()
		{
			if (ShipPodium == null)
				return;

			if (_spawnedMetaShip != null)
			{
				if (Application.isPlaying)
					Destroy(_spawnedMetaShip);
				else
					DestroyImmediate(_spawnedMetaShip);
			}

			var hull = HullLoader.Load(State.SelectedShipId);
			var prefabId = hull != null ? hull.MetaPrefab : null;
			if (string.IsNullOrEmpty(prefabId))
				return;

			_spawnedMetaShip = ResourceLoader.InstantiatePrefabById(prefabId, ShipPodium, false);
			if (_spawnedMetaShip == null)
				return;

			_spawnedMetaShip.transform.localPosition = Vector3.zero;
			_spawnedMetaShip.transform.localRotation = Quaternion.identity;
			_spawnedMetaShip.transform.localScale = Vector3.one;

			ApplyFitToMetaShip(_spawnedMetaShip);
			FitCameraToShip(_spawnedMetaShip);
		}

		private void ApplyFitToMetaShip(GameObject shipGo)
		{
			if (shipGo == null || State?.Fit?.GridPlacements == null)
				return;

			var fitUi = _metaVisual != null ? _metaVisual.FitSlotsController : null;
			if (fitUi != null)
				fitUi.BindShip(shipGo, State);

			var sockets = shipGo.GetComponentsInChildren<ShipSocketVisual>(true);
			if (sockets == null || sockets.Length == 0)
				return;

			var map = new Dictionary<string, ShipSocketVisual>(System.StringComparer.OrdinalIgnoreCase);
			for (var i = 0; i < sockets.Length; i++)
			{
				var s = sockets[i];
				if (s == null)
					continue;

				if (fitUi == null)
					s.ClearMetaItem();
				if (!map.ContainsKey(s.SocketId))
					map.Add(s.SocketId, s);
			}

			var defaultsAdded = EnsureDefaultWeapons(shipGo, sockets);

			var placementsCopy = new List<ShipFitModel.GridPlacement>(State.Fit.GridPlacements);
			var removed = false;
			var updatedPose = false;

			foreach (var placement in placementsCopy)
			{
				if (placement == null || string.IsNullOrEmpty(placement.ItemId))
					continue;

				if (!map.TryGetValue(placement.GridId, out var socket))
				{
					State.Fit.GridPlacements.RemoveAll(p => p == placement);
					removed = true;
					continue;
				}

				socket.GetLocalPose(shipGo.transform, out var localPos, out var localEuler);
				if (placement.LocalPosition != localPos || placement.LocalEuler != localEuler)
				{
					placement.LocalPosition = localPos;
					placement.LocalEuler = localEuler;
					placement.HasLocalPose = true;
					updatedPose = true;
				}

				var item = InventoryUtils.FindByItemId(State.InventoryModel, placement.ItemId);
				if (item == null)
					continue;

				if (fitUi == null)
					socket.SpawnMetaItem(item, shipGo.GetComponent<PlayerShip>());
			}

			if (removed || updatedPose || defaultsAdded)
			{
				InventoryUtils.RebuildEquippedCounts(State);
				MetaSaveSystem.Save(State);
				GameEvent.InventoryUpdated(State.InventoryModel);
				fitUi?.RefreshSlots();
			}
		}

		private bool EnsureDefaultWeapons(GameObject shipGo, ShipSocketVisual[] sockets)
		{
			if (shipGo == null || State?.Fit == null || sockets == null || sockets.Length == 0)
				return false;

			var changed = false;
			for (var i = 0; i < sockets.Length; i++)
			{
				var socket = sockets[i];
				if (socket == null || socket.SocketType != ShipGridType.WeaponGrid)
					continue;

				var placement = State.Fit.GridPlacements.Find(p => p != null && p.GridId == socket.SocketId);
				var hasValidItem = false;
				if (placement != null && !string.IsNullOrEmpty(placement.ItemId))
				{
					var item = InventoryUtils.FindByItemId(State.InventoryModel, placement.ItemId);
					if (item != null)
						hasValidItem = true;
					else if (DefaultWeaponResolver.TryBuildTemplateItem(placement.ItemId, out _))
						hasValidItem = true;
				}

				if (hasValidItem)
					continue;

				if (!DefaultWeaponResolver.TryGetDefaultTemplateId(socket.SocketSize, out var templateId))
					continue;

				State.Fit.GridPlacements.RemoveAll(p => p != null && p.GridId == socket.SocketId);
				socket.GetLocalPose(shipGo.transform, out var localPos, out var localEuler);
				State.Fit.GridPlacements.Add(new ShipFitModel.GridPlacement
				{
					GridId = socket.SocketId,
					GridType = socket.SocketType,
					ItemId = templateId,
					X = 0,
					Y = 0,
					Width = 1,
					Height = 1,
					Position = Vector2.zero,
					RotationDeg = 0f,
					LocalPosition = localPos,
					LocalEuler = localEuler,
					HasLocalPose = true
				});
				changed = true;
			}

			return changed;
		}

		private void FitCameraToShip(GameObject shipGo)
		{
			if (shipGo == null || _metaCamera == null)
				return;

			if (!TryGetShipBounds(shipGo, out var bounds))
				return;

			var camTransform = _metaCamera.transform;
			var center = bounds.center;
			var extents = bounds.extents;
			var aspect = Mathf.Max(0.0001f, _metaCamera.aspect);
			var distance = _minCameraDistance;

			if (_metaCamera.orthographic)
			{
				var sizeByHeight = extents.y;
				var sizeByWidth = extents.x / aspect;
				var size = Mathf.Max(sizeByHeight, sizeByWidth) * _cameraPadding;
				_metaCamera.orthographicSize = Mathf.Max(_metaCamera.orthographicSize, size);
			}
			else
			{
				var halfVert = Mathf.Deg2Rad * _metaCamera.fieldOfView * 0.5f;
				var halfHoriz = Mathf.Atan(Mathf.Tan(halfVert) * aspect);

				var distV = extents.y / Mathf.Tan(halfVert);
				var distH = extents.x / Mathf.Tan(halfHoriz);
				distance = Mathf.Max(distV, distH);
				distance = (distance + extents.z) * _cameraPadding;
				distance = Mathf.Clamp(distance, _minCameraDistance, _maxCameraDistance);
			}

			var forward = camTransform.forward;
			if (forward == Vector3.zero)
				forward = Vector3.forward;

			camTransform.position = center - forward * distance;
			camTransform.LookAt(center, Vector3.up);
		}

		private static bool TryGetShipBounds(GameObject shipGo, out Bounds bounds)
		{
			bounds = default;
			if (shipGo == null)
				return false;

			var renderers = shipGo.GetComponentsInChildren<Renderer>(true);
			var hasBounds = false;
			for (var i = 0; i < renderers.Length; i++)
			{
				var r = renderers[i];
				if (r == null)
					continue;

				if (!hasBounds)
				{
					bounds = r.bounds;
					hasBounds = true;
				}
				else
				{
					bounds.Encapsulate(r.bounds);
				}
			}

			if (hasBounds)
				return true;

			var colliders = shipGo.GetComponentsInChildren<Collider>(true);
			for (var i = 0; i < colliders.Length; i++)
			{
				var c = colliders[i];
				if (c == null)
					continue;

				if (!hasBounds)
				{
					bounds = c.bounds;
					hasBounds = true;
				}
				else
				{
					bounds.Encapsulate(c.bounds);
				}
			}

			return hasBounds;
		}

		private void GiveStarterItems()
		{
		}
	}
}
