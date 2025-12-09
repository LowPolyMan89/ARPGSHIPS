using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Tanks;

public class ShipDebuggerWindow : EditorWindow
{
    private Vector2 scroll;
    GUIStyle redBold;
    GUIStyle header;
    GUIStyle box;
    [MenuItem("Tools/ARPG SHIPS/Ship Debugger")]
    public static void Open()
    {
        GetWindow<ShipDebuggerWindow>("Ship Debugger");
    }

    private void EnsureStyles()
    {
        if (redBold == null)
        {
            redBold = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.25f, 0.25f) },
                fontStyle = FontStyle.Bold
            };
        }

        if (header == null)
        {
            header = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };
        }

        if (box == null)
        {
            box = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!Application.isPlaying)
            return;

        scroll = EditorGUILayout.BeginScrollView(scroll);

        var ships = FindObjectsByType<TankBase>(FindObjectsSortMode.None);

        foreach (var ship in ships)
        {
            DrawShip(ship);
            GUILayout.Space(15);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawShip(TankBase tank)
    {
        GUILayout.BeginVertical(box);
        EditorGUILayout.LabelField(tank.name, header);
        GUILayout.BeginHorizontal();
        GUILayout.Box("ICON", GUILayout.Width(80), GUILayout.Height(80));
        GUILayout.BeginVertical();
        DrawStats(tank);
        GUILayout.Space(5);
        DrawActiveEffects(tank);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        DrawWeapons(tank);
        GUILayout.EndVertical();
    }

    private void DrawStats(TankBase tank)
    {
        foreach (var kv in tank.TankStats.All)
        {
            Stat stat = kv.Value;
            EditorGUILayout.LabelField($"{kv.Key}: {stat.Current}/{stat.Maximum}");
        }

        foreach (var kv in tank.TankStats.All)
        {
            Stat stat = kv.Value;

            if (stat.Modifiers.Count == 0)
                continue;

            foreach (var mod in stat.Modifiers)
            {
                string txt = FormatModifier(stat.Name, mod);

                if (mod.Periodicity == StatModifierPeriodicity.Timed)
                    txt += $" ({mod.RemainingTicks}s)";

                EditorGUILayout.LabelField(txt, redBold);
            }
        }
    }

    private string FormatModifier(StatType type, StatModifier mod)
    {
        string sign = mod.Value >= 0 ? "+" : "-";
        float val = Mathf.Abs(mod.Value);

        return mod.Type switch
        {
            StatModifierType.Flat        => $"Buff {sign}{val} {type}",
            StatModifierType.PercentAdd  => $"Buff {sign}{val * 100}% {type}",
            StatModifierType.PercentMult => $"x{1 + mod.Value} {type}",
            StatModifierType.Set         => $"Set {type} = {mod.Value}",
            _ => "Modifier"
        };
    }

    private void DrawActiveEffects(TankBase tank)
    {
        if (tank.ActiveEffects.Count == 0)
        {
            EditorGUILayout.LabelField("No active effects", EditorStyles.miniLabel);
            return;
        }

        foreach (var eff in tank.ActiveEffects)
        {
            EditorGUILayout.LabelField(
                $"{eff.EffectId} x{eff.Stacks} ({eff.Remaining:F1}/{eff.Duration:F1}s)",
                redBold
            );
        }
    }

    private void DrawWeapons(TankBase tank)
    {
        if (tank.WeaponController == null)
        {
            EditorGUILayout.LabelField("No weapons", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.LabelField("Weapons:", header);

        foreach (var slot in tank.WeaponController.Weapons)
        {
            if (slot == null || slot.MountedWeapon == null)
            {
                EditorGUILayout.LabelField("Slot empty");
                continue;
            }

            var w = slot.MountedWeapon;

            GUILayout.BeginVertical(box);
            EditorGUILayout.LabelField(w.name, EditorStyles.boldLabel);

            if (w.Model != null)
            {
                EditorGUILayout.LabelField(
                    $"FireRate {w.Model.Stats.GetStat(StatType.FireRate).Current}/s " +
                    $"| Speed {w.Model.Stats.GetStat(StatType.ProjectileSpeed).Current} " +
                    $"| Range {w.Model.Stats.GetStat(StatType.FireRange).Current}");

                EditorGUILayout.LabelField(
                    $"Damage {w.Model.Stats.GetStat(StatType.MinDamage).Current}" +
                    $"-{w.Model.Stats.GetStat(StatType.MaxDamage).Current}" +
                    $" | Crit {w.Model.Stats.GetStat(StatType.CritChance).Current * 100}% " +
                    $"x{w.Model.Stats.GetStat(StatType.CritMultiplier).Current}");

                EditorGUILayout.LabelField($"Accuracy {w.Model.Stats.GetStat(StatType.Accuracy).Current}");
            }

            DrawWeaponTargeting(slot);

            GUILayout.EndVertical();
        }
    }

    private void DrawWeaponTargeting(WeaponSlot slot)
    {
        if (slot.WeaponTargeting == null)
            return;

        var wt = slot.WeaponTargeting;

        var f = wt.GetType().GetField("currentTarget",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        var t = f?.GetValue(wt) as ITargetable;

        EditorGUILayout.LabelField(
            "Status: " + (t != null ? "firing" : "idle"),
            t != null ? redBold : EditorStyles.miniLabel);
    }
}