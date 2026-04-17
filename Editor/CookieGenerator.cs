using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CookieGenerator : EditorWindow
{
    public static class RenderPipelineInfo
    {
        public static RenderPipelineAsset renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;

        public static bool IsSupportedPipeline()
        {
            return IsBuiltInRenderPipeline() || IsUniversalRenderPipeline() || IsHighDefinitionRenderPipeline();
        }

        public static bool IsBuiltInRenderPipeline()
        {
            return renderPipelineAsset == null;
        }

        public static bool IsUniversalRenderPipeline()
        {
            return renderPipelineAsset is UniversalRenderPipelineAsset urpAsset;
        }

        public static bool IsHighDefinitionRenderPipeline()
        {
            string typeName = renderPipelineAsset?.GetType().FullName;
            return typeName != null && typeName.Contains("HDRenderPipelineAsset"); // Might br considered a little hacky but it works
        }

        public static Component AddHDCameraData(GameObject target)
        {
            var type = Type.GetType(
                "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, " +
                "Unity.RenderPipelines.HighDefinition.Runtime");
            if (type == null) return null;

            var component = target.AddComponent(type);

            // Disable post-processing (probably??? i have no idea)
            var prop = type.GetProperty("renderPostProcessing");
            prop?.SetValue(component, false);

            return component;
        }
    }

    public static bool IsValidLightType(Light light)
    {
        return light.type == LightType.Spot || light.type == LightType.Directional || light.type == LightType.Point;
    }

    public static string ResolveSafeName(string baseName, string savePath, string ownedName, HashSet<string> usedThisBatch)
    {
        if (!string.IsNullOrEmpty(ownedName) && !usedThisBatch.Contains(ownedName))
        {
            // Only reuse the owned name if the fiel actually exists
            if (AssetDatabase.LoadAssetAtPath<Texture2D>($"{savePath}/{ownedName}.png") != null)
            {
                return ownedName;
            }
        }

        string candidate = baseName;
        int counter = 1;
        while (usedThisBatch.Contains(candidate) || AssetDatabase.LoadAssetAtPath<Texture2D>($"{savePath}/{candidate}.png") != null)
        {
            candidate = $"{baseName} ({counter++})";
        }

        return candidate;
    }

    public static void RunWithProgress<T>(string title, string info, string funnyInfo, List<T> items, Action<T, int> process)
    {
        static string Msg(string normal, string funny) => CookieGeneratorDefaults.SHOW_STUPID_STUFF ? funny : normal;

        EditorUtility.DisplayProgressBar(title, Msg(info, funnyInfo), 0f);
        try
        {
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item == null) continue;
                EditorUtility.DisplayProgressBar(title,
                    Msg($"Processing {i + 1} of {items.Count}...", $"Baking {i + 1} of {items.Count}..."),
                    (float)i / items.Count);
                process(item, i);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private CookieGeneratorSettings settings;
    private SerializedObject serializedSettings;
    private CookieGeneratorWindow mainWindow;
    private Dictionary<Light, Texture2D> batchPreviewTextures = new Dictionary<Light, Texture2D>();

    [MenuItem("Window/Rendering/Light Cookie Generator")]
    public static void ShowWindow() => GetWindow<CookieGenerator>("Cookie Generator");

    private void OnEnable()
    {
        if (settings == null)
        {
            settings = CreateInstance<CookieGeneratorSettings>();
        }

        serializedSettings = new SerializedObject(settings);
        mainWindow = new CookieGeneratorWindow();

        string[] guids = AssetDatabase.FindAssets("CookieGenerator-windowtitle-icon t:Texture2D");
        if (guids.Length > 0)
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            titleContent = new GUIContent("Cookie Generator", icon);
        }
    }

    private void OnDisable()
    {
        ClearBatchPreviews();
    }

    private void OnGUI()
    {
        if (settings == null || serializedSettings == null || serializedSettings.targetObject == null)
        {
            OnEnable();
            return;
        }

        mainWindow.BeginDraw();
        try
        {
            mainWindow.DrawHeader();
            if (mainWindow.DrawPipelineError()) return;
            mainWindow.DrawValidationWarning();
            mainWindow.DrawLightsList();
            mainWindow.DrawLightDataEditors();
            mainWindow.DrawPreview(batchPreviewTextures);
            mainWindow.DrawGenerateButtons(settings, GeneratePreviews, GenerateAllCookies);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            mainWindow.EndDraw();
        }

        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    private void ClearBatchPreviews()
    {
        foreach (var texture in batchPreviewTextures.Values)
        {
            if (texture != null) DestroyImmediate(texture);
        }
        batchPreviewTextures.Clear();
    }

    private void GeneratePreviews()
    {
        ClearBatchPreviews();
        List<Light> lights = mainWindow.GetLights();
        if (lights.Count == 0) return;

        RunWithProgress("Generating Previews", "Preparing previews...", "Turning on the oven light...", lights, (light, i) =>
        {
            {
                LightCookieData componentData = light.GetComponent<LightCookieData>();
                var tempSettings = componentData != null
                    ? CookieGeneratorSettings.From(componentData, light)
                    : CookieGeneratorSettings.From(settings, light);
                var renderer = new CookieRenderer(tempSettings);
                batchPreviewTextures[light] = renderer.RenderCookie(256);
                renderer.Dispose();
                DestroyImmediate(tempSettings);
            }
        });

        Repaint();
    }

    private void GenerateAllCookies()
    {
        mainWindow.GenerateAllCookies();
    }
}