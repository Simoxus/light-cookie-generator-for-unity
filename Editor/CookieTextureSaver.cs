using System.IO;
using UnityEditor;
using UnityEngine;

public class CookieTextureSaver
{
    private CookieGeneratorSettings _settings;

    public CookieTextureSaver(CookieGeneratorSettings settings)
    {
        _settings = settings;
    }

    public bool SaveCookie(Texture2D texture, string customFileName = null, bool silent = false)
    {
        string originalName = _settings.baseName;
        if (!string.IsNullOrEmpty(customFileName))
        {
            _settings.baseName = customFileName;
        }

        if (!EnsureFolderExists())
        {
            _settings.baseName = originalName;
            return false;
        }

        string fullPath = GetFullPath();
        if (!silent && File.Exists(fullPath))
        {
            if (!EditorUtility.DisplayDialog("File Exists",
                $"'{_settings.baseName}.png' already exists. Overwrite?", "Overwrite", "Cancel"))
            { _settings.baseName = originalName; return false; }
        }

        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        AssetDatabase.Refresh();
        ConfigureTextureImporter(fullPath);

        if (!silent) ShowSuccessDialog(fullPath);

        _settings.baseName = originalName;
        return true;
    }

    public bool SaveCookieToPath(Texture2D texture, string savePath, string fileName, bool silent = false)
    {
        string originalPath = _settings.savePath;
        string originalName = _settings.baseName;

        _settings.savePath = savePath;
        _settings.baseName = fileName;
        bool result = SaveCookie(texture, null, silent);
        _settings.savePath = originalPath;
        _settings.baseName = originalName;

        return result;
    }

    private string GetFullPath() => $"{_settings.savePath}/{_settings.baseName}.png";

    private void ConfigureTextureImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Cookie;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = _settings.resolution;
        importer.npotScale = TextureImporterNPOTScale.None;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private void ShowSuccessDialog(string path)
    {
        EditorUtility.DisplayDialog("Success", $"Cookie saved to:\n{path}", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public string SelectSavePath()
    {
        string selected = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
        if (string.IsNullOrEmpty(selected)) return null;

        if (selected.StartsWith(Application.dataPath))
        {
            return "Assets" + selected.Substring(Application.dataPath.Length);
        }

        EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside your Assets folder.", "OK");
        return null;
    }

    // Used by the batch generator
    public static bool GenerateCookieForLight(
        Light light, CookieGeneratorSettings sourceSettings,
        string savePath, string baseName, bool autoAssign)
    {
        if (light == null) return false;

        var tempSettings = CookieGeneratorSettings.From(sourceSettings, light);
        tempSettings.baseName = baseName;
        tempSettings.savePath = savePath;

        return BakeAndSave(light, tempSettings, savePath, baseName, autoAssign);
    }

    // Used by the light cookie data editor
    public static bool GenerateCookieForLight(
        Light light, LightCookieData data,
        string savePath, string baseName, bool autoAssign)
    {
        if (light == null || data == null) return false;

        if (data.automaticNaming && data.useParentName)
        {
            string parentName = ResolveParentName(light.transform, data.parentLevel);
            if (!string.IsNullOrEmpty(parentName))
            {
                savePath = $"{savePath}/{parentName}";
            }
        }

        var tempSettings = CookieGeneratorSettings.From(data, light);
        tempSettings.baseName = baseName;
        tempSettings.savePath = savePath;

        if (light.type == LightType.Point)
        {
            return BakeAndSavePoint(light, data, tempSettings, savePath, baseName, autoAssign);
        }

        return BakeAndSave(light, tempSettings, savePath, baseName, autoAssign);
    }

    private static bool BakeAndSave(
        Light light, CookieGeneratorSettings tempSettings,
        string savePath, string fileName, bool autoAssign)
    {
        var renderer = new CookieRenderer(tempSettings);
        var texture = renderer.RenderCookie(tempSettings.resolution);
        renderer.Dispose();
        if (texture == null) { Object.DestroyImmediate(tempSettings); return false; }

        var saver = new CookieTextureSaver(tempSettings);
        bool saved = saver.SaveCookieToPath(texture, savePath, fileName, true);

        if (saved && autoAssign)
        {
            string assetPath = $"{savePath}/{fileName}.png";
            var cookieTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (cookieTexture != null)
            {
                Undo.RecordObject(light, "Assign Light Cookie");
                light.cookie = cookieTexture;
                EditorUtility.SetDirty(light);
            }
        }

        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(tempSettings);
        return saved;
    }

    private static bool BakeAndSavePoint(
        Light light, LightCookieData data,
        CookieGeneratorSettings tempSettings,
        string savePath, string fileName, bool autoAssign)
    {
        var renderer = new CookieRenderer(tempSettings);
        var cubemap = renderer.RenderPointCookie(tempSettings.resolution);
        renderer.Dispose();
        if (cubemap == null) { Object.DestroyImmediate(tempSettings); return false; }

        string assetPath = $"{savePath}/{fileName}.cubemap";
        EnsureFolderExists(savePath);
        AssetDatabase.CreateAsset(cubemap, assetPath); // Cubemaps can't go through the normal pipeline
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (autoAssign)
        {
            var cookieAsset = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            if (cookieAsset != null)
            {
                Undo.RecordObject(light, "Assign Point Light Cookie");
                light.cookie = cookieAsset;
                EditorUtility.SetDirty(light);
            }
        }

        if (data != null)
        {
            Undo.RecordObject(data, "Bake Point Light Cookie");
            data.lastBakedPointCookie = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            EditorUtility.SetDirty(data);
        }

        Object.DestroyImmediate(tempSettings);
        return true;
    }

    private static string ResolveParentName(Transform transform, int parentLevel)
    {
        for (int i = 0; i < parentLevel; i++)
        {
            if (transform.parent != null)
            {
                transform = transform.parent;
            }
        }

        return transform.name;
    }

    private static void EnsureFolderExists(string savePath)
    {
        string[] parts = savePath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private bool EnsureFolderExists()
    {
        EnsureFolderExists(_settings.savePath);
        return true;
    }
}