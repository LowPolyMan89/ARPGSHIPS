using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Tanks;

public class ItemGeneratorWindow : EditorWindow
{
    private enum ItemType
    {
        Weapon,
        Module
    }

    private ItemType _selectedType = ItemType.Weapon;

    private List<string> _weaponTemplates = new();
    private List<string> _moduleTemplates = new();

    private int _selectedWeaponTemplate = 0;
    private int _selectedModuleTemplate = 0;

    private string[] _rarities = { "Random", "Common", "Uncommon", "Rare", "Epic", "Legendary" };
    private int _selectedRarity = 0;

    private List<string> _lootTables = new();
    private int _selectedLootTable = 0;

    private int _count = 1;

    // ===================== MENU =====================
    [MenuItem("Tools/Item Generator/Item Generator")]
    public static void Open()
    {
        var w = GetWindow<ItemGeneratorWindow>();
        w.titleContent = new GUIContent("Item Generator");
        w.Show();
    }

    // ===================== INIT =====================
    private void OnEnable()
    {
        _weaponTemplates = ItemGenerator.LoadWeaponFiles();
        _moduleTemplates = ItemGenerator.LoadModuleFiles();
        _lootTables = LoadLootTables();
    }

    // ===================== GUI =====================
    private void OnGUI()
    {
        GUILayout.Label("Item Generator", EditorStyles.boldLabel);

        DrawItemTypeSelection();
        GUILayout.Space(10);

        DrawLootTableSelection();
        GUILayout.Space(10);

        DrawTemplateSelection();
        GUILayout.Space(10);

        DrawRaritySelection();
        GUILayout.Space(10);

        DrawCount();
        GUILayout.Space(15);

        DrawGenerateButton();
        DrawOpenFolderButton();
    }

    // ============================================================
    // UI BLOCKS
    // ============================================================

    private void DrawItemTypeSelection()
    {
        GUILayout.Label("Item Type:", EditorStyles.boldLabel);
        _selectedType = (ItemType)EditorGUILayout.EnumPopup("Type", _selectedType);
    }

    private void DrawLootTableSelection()
    {
        GUILayout.Label("Loot Table (optional):", EditorStyles.boldLabel);

        if (_lootTables.Count == 0)
        {
            GUILayout.Label("No LootTables found");
            return;
        }

        _selectedLootTable = EditorGUILayout.Popup("Loot Table", _selectedLootTable, _lootTables.ToArray());
    }

    private void DrawTemplateSelection()
    {
        GUILayout.Label("Template:", EditorStyles.boldLabel);

        if (_selectedType == ItemType.Weapon)
        {
            if (_weaponTemplates.Count == 0)
            {
                GUILayout.Label("No weapon config files found.");
                return;
            }

            _selectedWeaponTemplate = EditorGUILayout.Popup("Weapon Template", _selectedWeaponTemplate, _weaponTemplates.ToArray());
        }
        else if (_selectedType == ItemType.Module)
        {
            if (_moduleTemplates.Count == 0)
            {
                GUILayout.Label("No module config files found.");
                return;
            }

            _selectedModuleTemplate = EditorGUILayout.Popup("Module Template", _selectedModuleTemplate, _moduleTemplates.ToArray());
        }
    }

    private void DrawRaritySelection()
    {
        GUILayout.Label("Rarity:", EditorStyles.boldLabel);
        _selectedRarity = EditorGUILayout.Popup("Rarity", _selectedRarity, _rarities);
    }

    private void DrawCount()
    {
        _count = EditorGUILayout.IntField("Count", Mathf.Max(1, _count));
    }

    private void DrawGenerateButton()
    {
        if (GUILayout.Button("Generate", GUILayout.Height(35)))
        {
            string selectedLootTable = _lootTables.Count > 0
                ? _lootTables[_selectedLootTable]
                : null;

            for (int i = 0; i < _count; i++)
            {
                switch (_selectedType)
                {
                    case ItemType.Weapon:
                        GenerateWeapon(selectedLootTable);
                        break;

                    case ItemType.Module:
                        GenerateModule(selectedLootTable);
                        break;
                }
            }
        }
    }

    private void DrawOpenFolderButton()
    {
        if (GUILayout.Button("Open Inventory Folder"))
            EditorUtility.RevealInFinder(ItemGenerator.OutputPath);
    }

    // ============================================================
    // GENERATION CALLS
    // ============================================================

    private void GenerateWeapon(string lootTable)
    {
        if (lootTable != null && lootTable != "None")
        {
            ItemGenerator.GenerateWeaponFromLoot(lootTable);
            return;
        }

        // Генерация без LootTable
        ItemGenerator.GenerateWeapon(
            _weaponTemplates[_selectedWeaponTemplate],
            _rarities[_selectedRarity]
        );
    }

    private void GenerateModule(string lootTable)
    {
        if (lootTable != null && lootTable != "None")
        {
            ItemGenerator.GenerateModuleFromLoot(lootTable);
            return;
        }

        ItemGenerator.GenerateModule(
            _moduleTemplates[_selectedModuleTemplate],
            _rarities[_selectedRarity]
        );
    }

    // ============================================================
    // LOAD LOOT TABLES
    // ============================================================

    private List<string> LoadLootTables()
    {
        var result = new List<string>();

        string path = Path.Combine(Application.streamingAssetsPath, "Configs/Loot/LootTables");

        if (!Directory.Exists(path))
            return result;

        var files = Directory.GetFiles(path, "*.json");
        foreach (var f in files)
            result.Add(Path.GetFileNameWithoutExtension(f));

        result.Insert(0, "None"); // выбор "без таблицы"

        return result;
    }
}
