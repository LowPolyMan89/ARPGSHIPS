

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
				State.SelectedShipId = "hull_test_frigate";
			if (string.IsNullOrEmpty(State.Fit.ShipId))
				State.Fit.ShipId = State.SelectedShipId;
			
			if (State.InventoryModel.InventoryUniqueItems.Count == 0)
			{
				GiveStarterItems();
				MetaSaveSystem.Save(State);
			}

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

			var sockets = shipGo.GetComponentsInChildren<ShipSocketVisual>(true);
			if (sockets == null || sockets.Length == 0)
				return;

			var map = new Dictionary<string, ShipSocketVisual>(System.StringComparer.OrdinalIgnoreCase);
			for (var i = 0; i < sockets.Length; i++)
			{
				var s = sockets[i];
				if (s != null && !map.ContainsKey(s.SocketId))
					map.Add(s.SocketId, s);
			}

			var placementsCopy = new List<ShipFitModel.GridPlacement>(State.Fit.GridPlacements);
			var removed = false;

			foreach (var placement in placementsCopy)
			{
				if (placement == null || string.IsNullOrEmpty(placement.ItemId))
					continue;

				if (!map.TryGetValue(placement.GridId, out var socket))
				{
					// Сокет не найден – возвращаем предмет в инвентарь и чистим плейсмент.
					var itemMissing = InventoryUtils.FindByItemId(State.InventoryModel, placement.ItemId);
					if (itemMissing != null)
					{
						itemMissing.EquippedOnFitId = null;
						itemMissing.EquippedGridId = null;
						itemMissing.EquippedGridX = -1;
						itemMissing.EquippedGridY = -1;
						itemMissing.EquippedGridPos = Vector2.zero;
						itemMissing.EquippedGridRot = 0f;
					}

					State.Fit.GridPlacements.RemoveAll(p => p == placement);
					removed = true;
					continue;
				}

				var item = InventoryUtils.FindByItemId(State.InventoryModel, placement.ItemId);
				if (item == null)
					continue;

				socket.EquipFromSave(item);
			}

			if (removed)
			{
				MetaSaveSystem.Save(State);
				GameEvent.InventoryUpdated(State.InventoryModel);
			}
		}

		private void GiveStarterItems()
		{
			var w = ItemGenerator.GenerateWeapon("weapon_p_small_bolter_1.json", "Common");

			State.InventoryModel.InventoryUniqueItems.Add(new InventoryItem
			{
				ItemId = w.ItemId,
				TemplateId = w.TemplateId
			});
		}
	}
}
