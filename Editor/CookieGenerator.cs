using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CookieGenerator
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
            return typeName != null && typeName.Contains("HDRenderPipelineAsset");
        }

        public static Component AddHDCameraData(GameObject target)
        {
            var type = Type.GetType(
                "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, " +
                "Unity.RenderPipelines.HighDefinition.Runtime");
            if (type == null) return null;

            var component = target.AddComponent(type);

            // Disable post-processing (probably)
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
            // Only reuse the owned name if the field actually exists
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
}