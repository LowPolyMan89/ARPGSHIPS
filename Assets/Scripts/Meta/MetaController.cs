

namespace Ships
{
	using UnityEngine;
	using System.Collections.Generic;

	public class MetaController : MonoBehaviour
	{
		public static MetaController Instance;

		public MetaState State { get; private set; }
		[SerializeField] private MetaVisual _metaVisual;
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

		    Localization.LoadLocalizationDataFromConfig();
			State = MetaSaveSystem.Load();

			if (string.IsNullOrEmpty(State.SelectedShipId))
				State.SelectedShipId = "hull_flagman_1";
			if (string.IsNullOrEmpty(State.Fit.ShipId))
				State.Fit.ShipId = State.SelectedShipId;
			
			if (State.InventoryModel.InventoryUniqueItems.Count == 0)
				MetaSaveSystem.Save(State);

			_inventoryView = new InventoryView();
			_inventoryView.Init(State);

			if (_metaVisual != null && _metaVisual.InventoryVisual != null)
				_metaVisual.InventoryVisual.Init(_inventoryView);

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

		private void GiveStarterItems()
		{
		}
	}
}
