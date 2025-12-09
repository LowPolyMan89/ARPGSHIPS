using UnityEngine;
using UnityEditor;

public class UniversalBarEditor : ShaderGUI
{
    private MaterialProperty SafeFind(string name, MaterialProperty[] props)
    {
        try
        {
            return FindProperty(name, props);
        }
        catch
        {
            Debug.LogError("[UniversalBarEditor] Missing property: " + name);
            return null;
        }
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        // ---- Safely fetch properties ----
        var MainTex    = SafeFind("_MainTex", props);
        var OverlayTex = SafeFind("_OverlayTex", props);
        var GradientTex= SafeFind("_GradientTex", props);

        var Fill       = SafeFind("_Fill", props);
        var InvertFill = SafeFind("_InvertFill", props);
        var FillMode   = SafeFind("_FillMode", props);
        var CutAxis    = SafeFind("_CutAxis", props);

        var UVMinX = SafeFind("_UVMinX", props);
        var UVMaxX = SafeFind("_UVMaxX", props);
        var UVMinY = SafeFind("_UVMinY", props);
        var UVMaxY = SafeFind("_UVMaxY", props);
        var CenterX = SafeFind("_CenterX", props);
        var CenterY = SafeFind("_CenterY", props);

        var GradientDir = SafeFind("_GradientDir", props);

        var PulseEnabled   = SafeFind("_PulseEnabled", props);
        var PulseSpeed     = SafeFind("_PulseSpeed", props);
        var PulseAmplitude = SafeFind("_PulseAmplitude", props);
        var PulseThreshold = SafeFind("_PulseThreshold", props);

        var Tint = SafeFind("_Tint", props);

        EditorGUI.BeginChangeCheck();

        // ============================================
        // BASE TEXTURES
        // ============================================

        EditorGUILayout.LabelField("Base Textures", EditorStyles.boldLabel);

        if (MainTex != null)    editor.TextureProperty(MainTex, "Base");
        if (OverlayTex != null) editor.TextureProperty(OverlayTex, "Overlay");
        if (GradientTex != null)editor.TextureProperty(GradientTex, "Gradient");

        EditorGUILayout.Space(6);

        // ============================================
        // FILL SETTINGS
        // ============================================

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Fill Settings", EditorStyles.boldLabel);

        if (Fill != null)       editor.ShaderProperty(Fill, "Fill Amount");
        if (InvertFill != null) editor.ShaderProperty(InvertFill, "Invert Fill");
        if (CutAxis != null)    editor.ShaderProperty(CutAxis, "Cut Axis");

        if (FillMode != null)
        {
            string[] options = { "Both Sides", "From Min", "From Max" };
            int f = Mathf.RoundToInt(FillMode.floatValue);
            f = EditorGUILayout.Popup("Fill Mode", f, options);
            FillMode.floatValue = f;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // ============================================
        // UV AREA
        // ============================================

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("UV Area", EditorStyles.boldLabel);

        if (UVMinX != null) editor.ShaderProperty(UVMinX, "UV Min X");
        if (UVMaxX != null) editor.ShaderProperty(UVMaxX, "UV Max X");
        if (UVMinY != null) editor.ShaderProperty(UVMinY, "UV Min Y");
        if (UVMaxY != null) editor.ShaderProperty(UVMaxY, "UV Max Y");

        if (CenterX != null) editor.ShaderProperty(CenterX, "Center X");
        if (CenterY != null) editor.ShaderProperty(CenterY, "Center Y");

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);

        // ============================================
        // GRADIENT
        // ============================================

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Gradient", EditorStyles.boldLabel);
        if (GradientDir != null) editor.ShaderProperty(GradientDir, "Gradient Direction");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // ============================================
        // PULSE
        // ============================================

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Pulse", EditorStyles.boldLabel);

        if (PulseEnabled != null) editor.ShaderProperty(PulseEnabled, "Enabled");

        if (PulseEnabled != null && PulseEnabled.floatValue > 0.5f)
        {
            if (PulseSpeed != null)     editor.ShaderProperty(PulseSpeed, "Speed");
            if (PulseAmplitude != null) editor.ShaderProperty(PulseAmplitude, "Amplitude");
            if (PulseThreshold != null) editor.ShaderProperty(PulseThreshold, "Threshold");
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);

        // ============================================
        // TINT
        // ============================================

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Color Tint", EditorStyles.boldLabel);
        if (Tint != null) editor.ShaderProperty(Tint, "Tint");
        EditorGUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
            foreach (Material m in editor.targets)
                EditorUtility.SetDirty(m);
    }
}