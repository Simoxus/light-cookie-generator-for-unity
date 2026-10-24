using System;
using System.Collections.Generic;
using System.IO;
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

        public static string GetBlackShaderName()
        {
            if (CookieGenerator.RenderPipelineInfo.IsBuiltInRenderPipeline())
            {
                return "Unlit/Color";
            }
            if (CookieGenerator.RenderPipelineInfo.IsUniversalRenderPipeline())
            {
                return "Universal Render Pipeline/Unlit";
            }
            if (CookieGenerator.RenderPipelineInfo.IsHighDefinitionRenderPipeline())
            {
                return "HDRP/Unlit";
            }

            return null;
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

    // Picks a file name in the folder the cookie actually gets saved to, so baking never replaces another light's cookie.
    // usedThisBatch holds full asset paths, and the chosen path is added to it.
    public static string ResolveSafeName(LightCookieData data, Light light, HashSet<string> usedThisBatch)
    {
        string baseName = $"Cookie_{data.ResolveBaseName()}_{light.type}";
        string folder = CookieTextureSaver.ResolveSaveFolder(data, light);
        string extension = CookieTextureSaver.GetExtension(light);
        string PathOf(string name) => $"{folder}/{name}.{extension}";

        string ownedName = data.lastBakedName;
        if (!string.IsNullOrEmpty(ownedName) && !usedThisBatch.Contains(PathOf(ownedName)))
        {
            UnityEngine.Object owned = light.type == LightType.Point ? data.lastBakedPointCookie : data.lastBakedCookie;
            if (IsOwnedCookie(PathOf(ownedName), owned))
            {
                usedThisBatch.Add(PathOf(ownedName));
                return ownedName;
            }
        }

        string candidate = baseName;
        int counter = 1;
        while (usedThisBatch.Contains(PathOf(candidate)) || File.Exists(PathOf(candidate)))
        {
            candidate = $"{baseName} ({counter++})";
        }

        usedThisBatch.Add(PathOf(candidate));
        return candidate;
    }

    private static bool IsOwnedCookie(string path, UnityEngine.Object owned)
    {
        UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
        if (existing == null) return false;

        // Cookies baked before the asset was tracked only have their name to go on
        if (owned == null || !EditorUtility.IsPersistent(owned)) return true;

        return existing == owned;
    }

    public static void RunWithProgress<T>(string title, string info, string funnyInfo, List<T> items, Action<T, int> process)
    {
        static string Msg(string normal, string funny) => LightCookieGeneratorProjectSettings.instance.showStupidStuff ? funny : normal;

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