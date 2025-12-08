using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Tanks;

public class LootTableTesterWindow : EditorWindow
{
    private enum GraphType
    {
        Bars,
        Histogram
    }

    private readonly string[] RARITIES =
        { "Common", "Uncommon", "Rare", "Epic", "Legendary" };

    private List<string> _lootTables = new();
    private int _selectedTable = 0;

    private int _simulationCount = 10000;

    private Dictionary<string, int> _itemCounts = new();
    private Dictionary<string, int> _rarityCounts = new();

    private bool _hasResults = false;

    private GraphType _graphType = GraphType.Bars;

    // --------------------------------------------------
    [MenuItem("Tools/Loot/Loot Table Tester")]
    public static void Open()
    {
        var w = GetWindow<LootTableTesterWindow>();
        w.titleContent = new GUIContent("Loot Tester");
        w.Show();
    }

    // --------------------------------------------------
    private void OnEnable()
    {
        LoadLootTables();
    }

    private void LoadLootTables()
    {
        _lootTables.Clear();

        string lootPath = Path.Combine(
            Application.streamingAssetsPath,
            "Configs/Loot/LootTables"
        );

        if (!Directory.Exists(lootPath))
            return;

        foreach (var file in Directory.GetFiles(lootPath, "*.json"))
            _lootTables.Add(Path.GetFileNameWithoutExtension(file));
    }

    // --------------------------------------------------
    private void OnGUI()
    {
        GUILayout.Label("Loot Table Tester", EditorStyles.boldLabel);

        if (_lootTables.Count == 0)
        {
            GUILayout.Label("No LootTables found.");
            return;
        }

        _selectedTable = EditorGUILayout.Popup("LootTable", _selectedTable, _lootTables.ToArray());
        _simulationCount = EditorGUILayout.IntField("Simulations", _simulationCount);

        _graphType = (GraphType)EditorGUILayout.EnumPopup("Graph Type", _graphType);

        if (GUILayout.Button("Simulate", GUILayout.Height(30)))
        {
            RunSimulation();
        }

        if (_hasResults)
        {
            GUILayout.Space(15);
            DrawResults();
        }
    }

    // --------------------------------------------------
    private void RunSimulation()
    {
        string tableId = _lootTables[_selectedTable];
        var table = LootLoader.Load(tableId);

        _itemCounts.Clear();
        _rarityCounts.Clear();
        _hasResults = false;

        if (table == null)
        {
            Debug.LogError("Error: loot table not found!");
            return;
        }

        for (int i = 0; i < _simulationCount; i++)
        {
            var result = LootTableSystem.Roll(table);

            // count items
            if (!_itemCounts.ContainsKey(result.itemId))
                _itemCounts[result.itemId] = 0;
            _itemCounts[result.itemId]++;

            // count rarities
            if (!_rarityCounts.ContainsKey(result.rarity))
                _rarityCounts[result.rarity] = 0;
            _rarityCounts[result.rarity]++;
        }

        _hasResults = true;
    }

    // --------------------------------------------------
    private void DrawResults()
    {
        GUILayout.Label("Results", EditorStyles.boldLabel);

        GUILayout.Label("Rarity Distribution:");

        if (_graphType == GraphType.Bars)
            DrawRarityBars();
        else
            DrawRarityHistogram();

        GUILayout.Space(20);

        GUILayout.Label("Item Distribution:");

        if (_graphType == GraphType.Bars)
            DrawItemBars();
        else
            DrawItemHistogram();
    }

    // --------------------------------------------------
    // COLORS
    private Color GetColorForRarity(string rarity)
    {
        return rarity switch
        {
            "Common" => Color.white,
            "Uncommon" => new Color(0.5f, 1f, 0.5f),
            "Rare" => new Color(0.3f, 0.6f, 1f),
            "Epic" => new Color(0.7f, 0.2f, 1f),
            "Legendary" => new Color(1f, 0.7f, 0f),
            _ => Color.gray
        };
    }

    // --------------------------------------------------
    // BARS (horizontal)
    private void DrawRarityBars()
    {
        foreach (var rarity in RARITIES)
        {
            if (!_rarityCounts.ContainsKey(rarity))
                continue;

            float pct = (float)_rarityCounts[rarity] / _simulationCount;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(rarity, GUILayout.Width(80));

            DrawHorizontalBar(pct, GetColorForRarity(rarity));

            GUILayout.Label($"{pct * 100:F2}% ({_rarityCounts[rarity]})",
                GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawItemBars()
    {
        foreach (var pair in _itemCounts)
        {
            float pct = (float)pair.Value / _simulationCount;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(pair.Key, GUILayout.Width(220));

            DrawHorizontalBar(pct, Color.green);

            GUILayout.Label($"{pct * 100:F2}% ({pair.Value})",
                GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawHorizontalBar(float pct, Color color)
    {
        Rect r = GUILayoutUtility.GetRect(300, 18);

        EditorGUI.DrawRect(new Rect(r.x, r.y, 300 * pct, r.height), color);
        EditorGUI.DrawRect(new Rect(r.x + 300 * pct, r.y, 300 - 300 * pct, r.height),
            new Color(0, 0, 0, 0.2f));
    }

    // --------------------------------------------------
    // HISTOGRAM (vertical)
    private void DrawRarityHistogram()
    {
        Rect rect = GUILayoutUtility.GetRect(300, 180);
        DrawHistogramBackground(rect);

        float barWidth = rect.width / RARITIES.Length;

        for (int i = 0; i < RARITIES.Length; i++)
        {
            string rarity = RARITIES[i];

            if (!_rarityCounts.ContainsKey(rarity))
                continue;

            float pct = (float)_rarityCounts[rarity] / _simulationCount;

            DrawHistogramBar(rect, i, barWidth, pct, GetColorForRarity(rarity), rarity);
        }
    }

    private void DrawItemHistogram()
    {
        Rect rect = GUILayoutUtility.GetRect(300, 180);
        DrawHistogramBackground(rect);

        float barWidth = rect.width / _itemCounts.Count;

        int i = 0;
        foreach (var kvp in _itemCounts)
        {
            float pct = (float)kvp.Value / _simulationCount;

            DrawHistogramBar(rect, i, barWidth, pct, Color.green, kvp.Key);
            i++;
        }
    }

    // --------------------------------------------------
    // Histogram Helpers
    private void DrawHistogramBackground(Rect rect)
    {
        // фиксированный фон — ограничивает всё
        GUI.BeginClip(rect);
        EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height),
            new Color(0.15f, 0.15f, 0.15f));
        GUI.EndClip();
    }

    private void DrawHistogramBar(Rect rect, int index, float barWidth, float pct,
        Color color, string label)
    {
        // ограничиваем область
        GUI.BeginClip(rect);

        // рисуем бар строго внутри 0..rect.height
        float height = rect.height * pct;
        float x = index * barWidth;
        float y = rect.height - height;

        EditorGUI.DrawRect(
            new Rect(x, y, barWidth - 4, height),
            color
        );

        // подпись идет вне clip — иначе обрежется
        GUI.EndClip();

        // рисуем подпись уже в нормальных координатах
        GUI.Label(
            new Rect(rect.x + index * barWidth, rect.yMax + 2, barWidth, 18),
            label,
            EditorStyles.centeredGreyMiniLabel
        );
    }
}
