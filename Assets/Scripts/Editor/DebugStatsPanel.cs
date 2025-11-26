using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using Ships;                     // твои статы
using Ships.HitEffect;          // эффекты
using Object = UnityEngine.Object;

public class DebugStatsPanel : EditorWindow
{
    private ShipBase targetShip;

    private Vector2 scrollStats;
    private Vector2 scrollEffects;

    private List<Type> effectTypes = new List<Type>();

    [MenuItem("Tools/Debug/Stats & Effects Panel")]
    public static void Open()
    {
        GetWindow<DebugStatsPanel>("Debug Stats Panel");
    }

    private void OnEnable()
    {
        RefreshEffects();
    }

    private void RefreshEffects()
    {
        effectTypes.Clear();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract) continue;
                if (!typeof(IOnHitEffect).IsAssignableFrom(type)) continue;

                effectTypes.Add(type);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        DrawTargetSelector();

        EditorGUILayout.Space(8);

        if (targetShip != null)
        {
            DrawStats();
            EditorGUILayout.Space(12);
            DrawEffects();
        }
        else
        {
            EditorGUILayout.HelpBox("Select a ShipBase target.", MessageType.Info);
        }
    }

    private void DrawTargetSelector()
    {
        EditorGUILayout.LabelField("Target Ship", EditorStyles.boldLabel);
        targetShip = (ShipBase)EditorGUILayout.ObjectField(targetShip, typeof(ShipBase), true);

        if (GUILayout.Button("Refresh Effect List"))
        {
            RefreshEffects();
        }
    }

    private void DrawStats()
    {
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        scrollStats = EditorGUILayout.BeginScrollView(scrollStats, GUILayout.Height(250));

        foreach (var statPair in targetShip.ShipStats.All)
        {
            var stat = statPair.Value;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{stat.Name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Base:    {stat.BaseCurrent} / {stat.BaseMaximum}");
            EditorGUILayout.LabelField($"Current: {stat.Current} / {stat.Maximum}");

            if (stat.Modifiers.Count > 0)
            {
                EditorGUILayout.LabelField("Modifiers:");

                foreach (var mod in stat.Modifiers)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"Type: {mod.Type}");
                    EditorGUILayout.LabelField($"Target: {mod.Target}");
                    EditorGUILayout.LabelField($"Value: {mod.Value}");
                    EditorGUILayout.LabelField($"Periodicity: {mod.Periodicity}");
                    EditorGUILayout.LabelField($"Ticks left: {mod.RemainingTicks}");
                    EditorGUILayout.LabelField($"Source: {mod.Source}");
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No modifiers.");
            }
            EditorGUILayout.LabelField("Active Effects", EditorStyles.boldLabel);

            foreach (var e in targetShip.ActiveEffects)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Effect: {e.EffectName}");
                EditorGUILayout.LabelField($"Remaining: {e.Remaining}/{e.Duration}");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEffects()
    {
        EditorGUILayout.LabelField("Available Hit Effects", EditorStyles.boldLabel);
        scrollEffects = EditorGUILayout.BeginScrollView(scrollEffects, GUILayout.Height(300));

        foreach (var type in effectTypes)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);

            ConstructorInfo ctor = type.GetConstructors().FirstOrDefault();

            if (ctor == null)
            {
                EditorGUILayout.HelpBox("No public constructor found!", MessageType.Error);
                EditorGUILayout.EndVertical();
                continue;
            }

            var parameters = ctor.GetParameters();
            object[] args = new object[parameters.Length];

            // Draw editable fields for constructor parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];

                if (p.ParameterType == typeof(float))
                {
                    args[i] = EditorGUILayout.FloatField(p.Name, PlayerPrefs.GetFloat($"{type.Name}_{p.Name}", 1f));
                    PlayerPrefs.SetFloat($"{type.Name}_{p.Name}", (float)args[i]);
                }
                else if (p.ParameterType == typeof(int))
                {
                    args[i] = EditorGUILayout.IntField(p.Name, PlayerPrefs.GetInt($"{type.Name}_{p.Name}", 1));
                    PlayerPrefs.SetInt($"{type.Name}_{p.Name}", (int)args[i]);
                }
                else if (p.ParameterType.IsEnum)
                {
                    args[i] = EditorGUILayout.EnumPopup(p.Name, (Enum)Enum.ToObject(p.ParameterType, 0));
                }
                else
                {
                    EditorGUILayout.LabelField($"{p.Name}: unsupported type {p.ParameterType.Name}");
                }
            }

            // APPLY EFFECT
            if (GUILayout.Button("Apply To Target"))
            {
                var effect = (IOnHitEffect)ctor.Invoke(args);

                var target = (ITargetable)targetShip;

                effect.Apply(target, damage: 0, sourceWeapon: null);

                Debug.Log($"[Debug Panel] Applied {type.Name} to {targetShip.name}");
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }
}
