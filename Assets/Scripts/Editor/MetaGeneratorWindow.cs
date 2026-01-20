using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Ships;

public class MetaGeneratorWindow : EditorWindow
{
	private enum ItemType
	{
		Weapon,
		Module
	}

	private sealed class FitSlotInfo
	{
		public string Id;
		public ShipGridType Type;
		public string Label;
	}

	private sealed class HullEntry
	{
		public string Id;
		public string Label;
	}

	private ItemType _selectedType = ItemType.Weapon;

	private List<string> _weaponTemplates = new();
	private List<string> _moduleTemplates = new();
	private List<HullEntry> _hulls = new();

	private int _selectedWeaponTemplate;
	private int _selectedModuleTemplate;
	private int _selectedHullIndex;
	private int _selectedFitSlotIndex;
	private int _selectedFitItemIndex;
	private int _selectedFitPlacementIndex;
	private int _selectedShipFitIndex;

	private readonly string[] _rarities = { "Random", "Common", "Uncommon", "Rare", "Epic", "Legendary" };
	private int _selectedRarity;
	private int _count = 1;

	private MetaState _state;
	private Vector2 _scroll;
	private bool _dirty;
	private string _status;
	private string[] _statOptions;

	// ===================== MENU =====================
	[MenuItem("Tools/Meta/Meta Generator")]
	public static void Open()
	{
		var w = GetWindow<MetaGeneratorWindow>();
		w.titleContent = new GUIContent("Meta Generator");
		w.Show();
	}

	// ===================== INIT =====================
	private void OnEnable()
	{
		_statOptions = Enum.GetNames(typeof(StatType));
		RefreshTemplates();
		LoadMeta();
	}

	// ===================== GUI =====================
	private void OnGUI()
	{
		if (_state == null)
			LoadMeta();

		using (new GUILayout.VerticalScope())
		{
			DrawHeader();

			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			DrawInventorySection();
			GUILayout.Space(8);
			DrawShipsSection();
			GUILayout.Space(8);
			DrawMetaStatsSection();
			GUILayout.Space(8);
			DrawFitSection();
			EditorGUILayout.EndScrollView();
		}
	}

	private void DrawHeader()
	{
		GUILayout.Label("Meta Generator", EditorStyles.boldLabel);

		using (new GUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Reload Meta"))
				LoadMeta();
			if (GUILayout.Button("Save Meta"))
				SaveMeta();
			if (GUILayout.Button("Refresh Lists"))
				RefreshTemplates();
			if (GUILayout.Button("Open Persistent Folder"))
				EditorUtility.RevealInFinder(ResourceLoader.GetPersistentPath(""));
		}

		if (_dirty)
			EditorGUILayout.HelpBox("Unsaved changes detected.", MessageType.Warning);
		if (!string.IsNullOrEmpty(_status))
			EditorGUILayout.HelpBox(_status, MessageType.Info);
	}

	// ============================================================
	// INVENTORY
	// ============================================================

	private void DrawInventorySection()
	{
		using (new GUILayout.VerticalScope("box"))
		{
			GUILayout.Label("Inventory (from templates)", EditorStyles.boldLabel);

			_selectedType = (ItemType)EditorGUILayout.EnumPopup("Type", _selectedType);
			DrawTemplateSelection();

			_selectedRarity = EditorGUILayout.Popup("Rarity", _selectedRarity, _rarities);
			_count = EditorGUILayout.IntField("Count", Mathf.Max(1, _count));

			using (new GUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Add To Inventory", GUILayout.Height(30)))
					AddToInventory();
				if (GUILayout.Button("Remove From Inventory", GUILayout.Height(30)))
					RemoveFromInventory();
			}

			DrawInventoryList();
		}
	}

	private void DrawTemplateSelection()
	{
		if (_selectedType == ItemType.Weapon)
		{
			if (_weaponTemplates.Count == 0)
			{
				EditorGUILayout.HelpBox("No weapon config files found.", MessageType.Info);
				return;
			}

			_selectedWeaponTemplate = EditorGUILayout.Popup("Weapon Template", _selectedWeaponTemplate, _weaponTemplates.ToArray());
			return;
		}

		if (_moduleTemplates.Count == 0)
		{
			EditorGUILayout.HelpBox("No module config files found.", MessageType.Info);
			return;
		}

		_selectedModuleTemplate = EditorGUILayout.Popup("Module Template", _selectedModuleTemplate, _moduleTemplates.ToArray());
	}

	private void DrawInventoryList()
	{
		if (_state?.InventoryModel?.InventoryUniqueItems == null)
			return;

		GUILayout.Space(6);
		GUILayout.Label("Inventory Items:", EditorStyles.boldLabel);

		if (_state.InventoryModel.InventoryUniqueItems.Count == 0)
		{
			GUILayout.Label("Inventory is empty.");
			return;
		}

		for (var i = 0; i < _state.InventoryModel.InventoryUniqueItems.Count; i++)
		{
			var item = _state.InventoryModel.InventoryUniqueItems[i];
			if (item == null)
				continue;

			var id = InventoryUtils.ResolveItemId(item);
			GUILayout.Label($"{id} | Rarity: {item.Rarity} | Count: {item.Count} | Equipped: {item.EquippedCount}");
		}
	}

	private void AddToInventory()
	{
		if (_state?.InventoryModel == null)
			return;

		var templateFile = GetSelectedTemplateFile();
		if (string.IsNullOrEmpty(templateFile))
			return;

		var rarity = _rarities[_selectedRarity];
		switch (_selectedType)
		{
			case ItemType.Weapon:
				var weapon = ItemGenerator.GenerateWeapon(templateFile, rarity);
				if (weapon == null)
				{
					_status = "Failed to build weapon from template.";
					return;
				}

				InventoryUtils.AddOrIncrease(_state.InventoryModel, weapon.TemplateId, weapon.Rarity, _count);
				break;

			case ItemType.Module:
				var module = ItemGenerator.GenerateModule(templateFile, rarity);
				if (module == null)
				{
					_status = "Failed to build module from template.";
					return;
				}

				InventoryUtils.AddOrIncrease(_state.InventoryModel, module.TemplateId, module.Rarity, _count);
				break;
		}

		SaveMeta("Items added to inventory.");
	}

	private void RemoveFromInventory()
	{
		if (_state?.InventoryModel == null)
			return;

		var templateFile = GetSelectedTemplateFile();
		if (string.IsNullOrEmpty(templateFile))
			return;

		var templateId = NormalizeTemplateId(Path.GetFileNameWithoutExtension(templateFile));
		var removed = ReduceInventory(templateId, _count);
		if (!removed)
			_status = "Item not found in inventory.";
		else
			SaveMeta("Items removed from inventory.");
	}

	// ============================================================
	// SHIPS
	// ============================================================

	private void DrawShipsSection()
	{
		using (new GUILayout.VerticalScope("box"))
		{
			GUILayout.Label("Ships", EditorStyles.boldLabel);

			DrawShipAdd();
			GUILayout.Space(6);
			DrawShipList();
		}
	}

	private void DrawShipAdd()
	{
		if (_hulls.Count == 0)
		{
			EditorGUILayout.HelpBox("No hull configs found.", MessageType.Info);
			return;
		}

		var labels = BuildHullLabels(_hulls);
		_selectedHullIndex = Mathf.Clamp(_selectedHullIndex, 0, labels.Length - 1);
		_selectedHullIndex = EditorGUILayout.Popup("Hull", _selectedHullIndex, labels);

		if (GUILayout.Button("Add Ship"))
		{
			var id = _hulls[_selectedHullIndex].Id;
			if (string.IsNullOrEmpty(id))
				return;

			if (_state.PlayerShipFits.Find(f => f != null && string.Equals(f.ShipId, id, StringComparison.OrdinalIgnoreCase)) == null)
				_state.PlayerShipFits.Add(new ShipFitModel { ShipId = id });

			if (string.IsNullOrEmpty(_state.SelectedShipId))
				_state.SelectedShipId = id;
			if (_state.Fit == null)
				_state.Fit = new ShipFitModel();
			if (string.IsNullOrEmpty(_state.Fit.ShipId))
				_state.Fit.ShipId = id;

			SaveMeta("Ship added.");
		}
	}

	private void DrawShipList()
	{
		if (_state?.PlayerShipFits == null)
			return;

		var shipIds = BuildShipFitIds(_state.PlayerShipFits);
		if (shipIds.Length == 0)
		{
			GUILayout.Label("No ships available.");
			return;
		}

		_selectedShipFitIndex = Mathf.Clamp(_selectedShipFitIndex, 0, shipIds.Length - 1);
		_selectedShipFitIndex = EditorGUILayout.Popup("Available Ships", _selectedShipFitIndex, shipIds);

		using (new GUILayout.HorizontalScope())
		{
			if (GUILayout.Button("Set Active"))
				SetActiveShip(shipIds[_selectedShipFitIndex]);
			if (GUILayout.Button("Remove Ship"))
				RemoveShip(shipIds[_selectedShipFitIndex]);
		}
	}

	// ============================================================
	// META STATS
	// ============================================================

	private void DrawMetaStatsSection()
	{
		if (_state?.MainStatEffects == null)
			return;

		using (new GUILayout.VerticalScope("box"))
		{
			GUILayout.Label("Meta Stats", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			for (var i = 0; i < _state.MainStatEffects.Count; i++)
			{
				var effect = _state.MainStatEffects[i];
				if (effect == null)
					continue;

				using (new GUILayout.HorizontalScope())
				{
					var statIndex = GetStatIndex(effect.Stat);
					statIndex = EditorGUILayout.Popup(statIndex, _statOptions, GUILayout.Width(180));
					effect.Stat = _statOptions[statIndex];

					effect.Operation = (StatEffectOperation)EditorGUILayout.EnumPopup(effect.Operation, GUILayout.Width(120));
					effect.Target = (StatModifierTarget)EditorGUILayout.EnumPopup(effect.Target, GUILayout.Width(100));
					effect.Value = EditorGUILayout.FloatField(effect.Value);

					if (GUILayout.Button("X", GUILayout.Width(24)))
					{
						_state.MainStatEffects.RemoveAt(i);
						i--;
					}
				}
			}

			if (EditorGUI.EndChangeCheck())
				_dirty = true;

			if (GUILayout.Button("Add Stat Effect"))
			{
				_state.MainStatEffects.Add(new StatEffectModel
				{
					Stat = _statOptions.Length > 0 ? _statOptions[0] : "HitPoint",
					Operation = StatEffectOperation.Add,
					Target = StatModifierTarget.Maximum,
					Value = 0f
				});
				_dirty = true;
			}
		}
	}

	// ============================================================
	// FITS
	// ============================================================

	private void DrawFitSection()
	{
		using (new GUILayout.VerticalScope("box"))
		{
			GUILayout.Label("Ship Fit", EditorStyles.boldLabel);

			DrawActiveShipSelector();
			GUILayout.Space(6);
			DrawFitPlacementEditor();
		}
	}

	private void DrawActiveShipSelector()
	{
		var shipIds = BuildShipFitIds(_state?.PlayerShipFits);
		if (shipIds.Length == 0)
		{
			GUILayout.Label("No ships available.");
			return;
		}

		var current = string.IsNullOrEmpty(_state.SelectedShipId) ? shipIds[0] : _state.SelectedShipId;
		var currentIndex = Array.FindIndex(shipIds, id => string.Equals(id, current, StringComparison.OrdinalIgnoreCase));
		if (currentIndex < 0)
			currentIndex = 0;

		EditorGUI.BeginChangeCheck();
		var newIndex = EditorGUILayout.Popup("Active Ship", currentIndex, shipIds);
		if (EditorGUI.EndChangeCheck())
			SetActiveShip(shipIds[newIndex]);
	}

	private void DrawFitPlacementEditor()
	{
		if (_state?.Fit == null)
			return;

		var slots = BuildFitSlots(_state.Fit.ShipId);
		if (slots.Count == 0)
		{
			EditorGUILayout.HelpBox("No sockets/grids found in the hull config.", MessageType.Info);
			DrawPlacementsList();
			return;
		}

		var slotLabels = BuildFitSlotLabels(slots);
		_selectedFitSlotIndex = Mathf.Clamp(_selectedFitSlotIndex, 0, slotLabels.Length - 1);
		_selectedFitSlotIndex = EditorGUILayout.Popup("Slot", _selectedFitSlotIndex, slotLabels);

		var availableItems = BuildAvailableInventoryItems(_state.InventoryModel);
		if (availableItems.Count == 0)
		{
			EditorGUILayout.HelpBox("No available items in inventory.", MessageType.Info);
			DrawPlacementsList();
			return;
		}

		var itemLabels = BuildInventoryItemLabels(availableItems);
		_selectedFitItemIndex = Mathf.Clamp(_selectedFitItemIndex, 0, itemLabels.Length - 1);
		_selectedFitItemIndex = EditorGUILayout.Popup("Item", _selectedFitItemIndex, itemLabels);

		if (GUILayout.Button("Place Item"))
		{
			var slot = slots[_selectedFitSlotIndex];
			var item = availableItems[_selectedFitItemIndex];
			EquipItemToSlot(slot, item);
		}

		DrawPlacementsList();
	}

	private void DrawPlacementsList()
	{
		if (_state?.Fit?.GridPlacements == null)
			return;

		GUILayout.Space(6);
		GUILayout.Label("Placements:", EditorStyles.boldLabel);

		if (_state.Fit.GridPlacements.Count == 0)
		{
			GUILayout.Label("No placements.");
			return;
		}

		var placementLabels = BuildPlacementLabels(_state.Fit.GridPlacements);
		_selectedFitPlacementIndex = Mathf.Clamp(_selectedFitPlacementIndex, 0, placementLabels.Length - 1);
		_selectedFitPlacementIndex = EditorGUILayout.Popup("Placement", _selectedFitPlacementIndex, placementLabels);

		if (GUILayout.Button("Remove Placement"))
		{
			var placement = _state.Fit.GridPlacements[_selectedFitPlacementIndex];
			RemovePlacement(placement);
		}
	}

	// ============================================================
	// STATE MANAGEMENT
	// ============================================================

	private void LoadMeta()
	{
		_state = MetaSaveSystem.Load();
		EnsureState();
		_dirty = false;
		_status = null;
	}

	private void SaveMeta(string message = null)
	{
		if (_state == null)
			return;

		EnsureState();
		MetaSaveSystem.Save(_state);
		_dirty = false;
		_status = message;
	}

	private void EnsureState()
	{
		if (_state == null)
			_state = new MetaState();
		if (_state.PlayerShipFits == null)
			_state.PlayerShipFits = new List<ShipFitModel>();
		if (_state.Fit == null)
			_state.Fit = new ShipFitModel();
		if (_state.Fit.GridPlacements == null)
			_state.Fit.GridPlacements = new List<ShipFitModel.GridPlacement>();
		if (_state.BattleShipSlots == null)
			_state.BattleShipSlots = new List<string>();
		if (_state.InventoryModel == null)
			_state.InventoryModel = new PlayerInventoryModel();
		if (_state.InventoryModel.InventoryUniqueItems == null)
			_state.InventoryModel.InventoryUniqueItems = new List<InventoryItem>();
		if (_state.InventoryModel.InventoryStackItems == null)
			_state.InventoryModel.InventoryStackItems = new List<InventoryItem>();
		if (_state.MainStatEffects == null)
			_state.MainStatEffects = new List<StatEffectModel>();

		if (!string.IsNullOrEmpty(_state.Fit.ShipId))
			_state.SelectedShipId = _state.Fit.ShipId;
		if (string.IsNullOrEmpty(_state.Fit.ShipId) && !string.IsNullOrEmpty(_state.SelectedShipId))
			_state.Fit.ShipId = _state.SelectedShipId;

		if (!string.IsNullOrEmpty(_state.Fit.ShipId))
		{
			var exists = _state.PlayerShipFits.Find(f => f != null &&
			                                             string.Equals(f.ShipId, _state.Fit.ShipId, StringComparison.OrdinalIgnoreCase));
			if (exists == null)
				_state.PlayerShipFits.Add(_state.Fit);
			else
				_state.Fit = exists;
		}
	}

	private void RefreshTemplates()
	{
		_weaponTemplates = ItemGenerator.LoadWeaponFiles();
		_moduleTemplates = ItemGenerator.LoadModuleFiles();
		_hulls = LoadHulls();
		_selectedWeaponTemplate = Mathf.Clamp(_selectedWeaponTemplate, 0, _weaponTemplates.Count - 1);
		_selectedModuleTemplate = Mathf.Clamp(_selectedModuleTemplate, 0, _moduleTemplates.Count - 1);
		_selectedHullIndex = Mathf.Clamp(_selectedHullIndex, 0, _hulls.Count - 1);
	}

	// ============================================================
	// HELPERS
	// ============================================================

	private string GetSelectedTemplateFile()
	{
		if (_selectedType == ItemType.Weapon)
		{
			if (_weaponTemplates.Count == 0)
				return null;
			return _weaponTemplates[_selectedWeaponTemplate];
		}

		if (_moduleTemplates.Count == 0)
			return null;

		return _moduleTemplates[_selectedModuleTemplate];
	}

	private static string NormalizeTemplateId(string id)
	{
		if (string.IsNullOrEmpty(id))
			return id;
		return id.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
			? id.Substring(0, id.Length - ".json".Length)
			: id;
	}

	private bool ReduceInventory(string templateId, int count)
	{
		if (_state?.InventoryModel == null || string.IsNullOrEmpty(templateId) || count <= 0)
			return false;

		var item = InventoryUtils.FindByItemId(_state.InventoryModel, templateId);
		if (item == null)
			return false;

		item.Count = Mathf.Max(0, item.Count - count);
		item.EquippedCount = Mathf.Min(item.EquippedCount, item.Count);

		if (item.Count <= 0)
			_state.InventoryModel.InventoryUniqueItems.Remove(item);

		return true;
	}

	private void SetActiveShip(string shipId)
	{
		if (string.IsNullOrEmpty(shipId) || _state == null)
			return;

		var fit = _state.PlayerShipFits.Find(f => f != null &&
		                                          string.Equals(f.ShipId, shipId, StringComparison.OrdinalIgnoreCase));
		if (fit == null)
		{
			fit = new ShipFitModel { ShipId = shipId };
			_state.PlayerShipFits.Add(fit);
		}

		_state.SelectedShipId = shipId;
		_state.Fit = fit;
		SaveMeta($"Active ship: {shipId}");
	}

	private void RemoveShip(string shipId)
	{
		if (string.IsNullOrEmpty(shipId) || _state?.PlayerShipFits == null)
			return;

		var removedFit = _state.PlayerShipFits.Find(f => f != null &&
		                                                 string.Equals(f.ShipId, shipId, StringComparison.OrdinalIgnoreCase));
		if (removedFit != null)
			ReturnFitItems(removedFit);

		_state.PlayerShipFits.RemoveAll(f => f != null &&
		                                     string.Equals(f.ShipId, shipId, StringComparison.OrdinalIgnoreCase));

		if (_state.BattleShipSlots != null)
		{
			for (var i = 0; i < _state.BattleShipSlots.Count; i++)
			{
				if (string.Equals(_state.BattleShipSlots[i], shipId, StringComparison.OrdinalIgnoreCase))
					_state.BattleShipSlots[i] = string.Empty;
			}
		}

		if (string.Equals(_state.SelectedShipId, shipId, StringComparison.OrdinalIgnoreCase))
			_state.SelectedShipId = _state.PlayerShipFits.Count > 0 ? _state.PlayerShipFits[0].ShipId : string.Empty;

		if (_state.Fit != null && string.Equals(_state.Fit.ShipId, shipId, StringComparison.OrdinalIgnoreCase))
		{
			_state.Fit = _state.PlayerShipFits.Count > 0 ? _state.PlayerShipFits[0] : new ShipFitModel();
			_state.SelectedShipId = _state.Fit.ShipId;
		}

		SaveMeta($"Ship removed: {shipId}");
	}

	private void ReturnFitItems(ShipFitModel fit)
	{
		if (fit?.GridPlacements == null || _state?.InventoryModel == null)
			return;

		for (var i = 0; i < fit.GridPlacements.Count; i++)
		{
			var placement = fit.GridPlacements[i];
			if (placement == null || string.IsNullOrEmpty(placement.ItemId))
				continue;

			InventoryUtils.ReturnToInventory(_state.InventoryModel, placement.ItemId, 1);
		}
	}

	private static string[] BuildHullLabels(IReadOnlyList<HullEntry> hulls)
	{
		var labels = new string[hulls.Count];
		for (var i = 0; i < hulls.Count; i++)
			labels[i] = hulls[i].Label;
		return labels;
	}

	private static string[] BuildShipFitIds(IReadOnlyList<ShipFitModel> fits)
	{
		if (fits == null)
			return Array.Empty<string>();

		var ids = new List<string>();
		for (var i = 0; i < fits.Count; i++)
		{
			var fit = fits[i];
			if (fit == null || string.IsNullOrEmpty(fit.ShipId))
				continue;
			if (!ids.Contains(fit.ShipId))
				ids.Add(fit.ShipId);
		}

		return ids.ToArray();
	}

	private static List<HullEntry> LoadHulls()
	{
		var result = new List<HullEntry>();
		foreach (var file in ResourceLoader.GetStreamingFiles(PathConstant.HullsConfigs, "*.json"))
		{
			var path = Path.Combine(PathConstant.HullsConfigs, file);
			var id = Path.GetFileNameWithoutExtension(file);
			if (ResourceLoader.TryLoadStreamingJson(path, out HullModel hull) && !string.IsNullOrEmpty(hull?.id))
				id = hull.id;

			if (string.IsNullOrEmpty(id))
				continue;

			result.Add(new HullEntry { Id = id, Label = id });
		}

		result.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
		return result;
	}

	private int GetStatIndex(string stat)
	{
		if (_statOptions == null || _statOptions.Length == 0)
			return 0;

		for (var i = 0; i < _statOptions.Length; i++)
		{
			if (string.Equals(_statOptions[i], stat, StringComparison.OrdinalIgnoreCase))
				return i;
		}

		return 0;
	}

	private static List<FitSlotInfo> BuildFitSlots(string shipId)
	{
		var result = new List<FitSlotInfo>();
		if (string.IsNullOrEmpty(shipId))
			return result;

		var hull = HullLoader.Load(shipId);
		if (hull == null)
			return result;

		if (hull.sockets != null)
		{
			for (var i = 0; i < hull.sockets.Count; i++)
			{
				var socket = hull.sockets[i];
				if (socket == null || string.IsNullOrEmpty(socket.id))
					continue;

				result.Add(new FitSlotInfo
				{
					Id = socket.id,
					Type = socket.type,
					Label = $"{socket.id} ({socket.type}, {socket.size})"
				});
			}
		}

		if (hull.grids != null)
		{
			for (var i = 0; i < hull.grids.Count; i++)
			{
				var grid = hull.grids[i];
				if (grid == null || string.IsNullOrEmpty(grid.id))
					continue;

				result.Add(new FitSlotInfo
				{
					Id = grid.id,
					Type = grid.type,
					Label = $"{grid.id} ({grid.type}, {grid.width}x{grid.height})"
				});
			}
		}

		return result;
	}

	private static string[] BuildFitSlotLabels(IReadOnlyList<FitSlotInfo> slots)
	{
		var labels = new string[slots.Count];
		for (var i = 0; i < slots.Count; i++)
			labels[i] = slots[i].Label;
		return labels;
	}

	private static List<InventoryItem> BuildAvailableInventoryItems(PlayerInventoryModel inventory)
	{
		var result = new List<InventoryItem>();
		if (inventory?.InventoryUniqueItems == null)
			return result;

		for (var i = 0; i < inventory.InventoryUniqueItems.Count; i++)
		{
			var item = inventory.InventoryUniqueItems[i];
			if (item == null || item.AvailableCount <= 0)
				continue;

			result.Add(item);
		}

		return result;
	}

	private static string[] BuildInventoryItemLabels(IReadOnlyList<InventoryItem> items)
	{
		var labels = new string[items.Count];
		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			var id = InventoryUtils.ResolveItemId(item);
			labels[i] = $"{id} x{item.AvailableCount} ({item.Rarity})";
		}

		return labels;
	}

	private static string[] BuildPlacementLabels(IReadOnlyList<ShipFitModel.GridPlacement> placements)
	{
		var labels = new string[placements.Count];
		for (var i = 0; i < placements.Count; i++)
		{
			var placement = placements[i];
			if (placement == null)
			{
				labels[i] = "<null>";
				continue;
			}

			var item = string.IsNullOrEmpty(placement.ItemId) ? "<empty>" : placement.ItemId;
			labels[i] = $"{placement.GridId} -> {item}";
		}

		return labels;
	}

	private void EquipItemToSlot(FitSlotInfo slot, InventoryItem item)
	{
		if (slot == null || item == null || _state?.Fit == null)
			return;

		var itemId = InventoryUtils.ResolveItemId(item);
		if (string.IsNullOrEmpty(itemId))
			return;

		if (!InventoryUtils.TryConsume(_state.InventoryModel, itemId, 1))
		{
			_status = "Not enough items to place.";
			return;
		}

		var existing = _state.Fit.GridPlacements.Find(p => p != null && p.GridId == slot.Id);
		if (existing != null && !string.IsNullOrEmpty(existing.ItemId))
			InventoryUtils.ReturnToInventory(_state.InventoryModel, existing.ItemId, 1);

		_state.Fit.GridPlacements.RemoveAll(p => p != null && p.GridId == slot.Id);
		_state.Fit.GridPlacements.Add(new ShipFitModel.GridPlacement
		{
			GridId = slot.Id,
			GridType = slot.Type,
			ItemId = itemId,
			X = 0,
			Y = 0,
			Width = 1,
			Height = 1,
			Position = Vector2.zero,
			RotationDeg = 0f,
			LocalPosition = Vector3.zero,
			LocalEuler = Vector3.zero,
			HasLocalPose = false
		});

		SaveMeta("Item placed in fit.");
	}

	private void RemovePlacement(ShipFitModel.GridPlacement placement)
	{
		if (placement == null || _state?.Fit == null)
			return;

		_state.Fit.GridPlacements.Remove(placement);
		if (!string.IsNullOrEmpty(placement.ItemId))
			InventoryUtils.ReturnToInventory(_state.InventoryModel, placement.ItemId, 1);

		SaveMeta("Placement removed.");
	}
}
