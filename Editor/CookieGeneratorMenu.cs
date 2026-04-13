using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CookieGeneratorMenu
{
    private const string SELECT_COOKIE_DATA = "GameObject/Light Cookies/Select Cookie Data in Children";
    private const string BAKE_COOKIE_DATA = "GameObject/Light Cookies/Bake Cookies in Children";

    [MenuItem(SELECT_COOKIE_DATA, false, 4)]
    private static void SelectCookieDataInChildren()
    {
        var collected = new List<GameObject>();
        foreach (GameObject selected in Selection.gameObjects)
        {
            LightCookieData[] datas = selected.GetComponentsInChildren<LightCookieData>(includeInactive: true);
            foreach (LightCookieData data in datas)
            {
                if (!collected.Contains(data.gameObject))
                {
                    collected.Add(data.gameObject);
                }
            }
        }
        Selection.objects = collected.ToArray();
    }

    [MenuItem(SELECT_COOKIE_DATA, true)]
    private static bool SelectCookieDataInChildren_Validate()
    {
        return Selection.gameObjects.Length > 0;
    }

    [MenuItem(BAKE_COOKIE_DATA, false, 4)]
    private static void BakeCookiesInChildren(MenuCommand menuCommand)
    {
        EditorApplication.delayCall -= ExecuteBake;
        EditorApplication.delayCall += ExecuteBake;
    }

    private static void ExecuteBake()
    {
        EditorApplication.delayCall -= ExecuteBake;
        var usedNames = new HashSet<string>();
        var targetList = new List<LightCookieData>();
        foreach (GameObject selected in Selection.gameObjects)
        {
            LightCookieData[] datas = selected.GetComponentsInChildren<LightCookieData>(includeInactive: true);
            foreach (LightCookieData data in datas)
            {
                if (!targetList.Contains(data))
                    targetList.Add(data);
            }
        }
        if (targetList.Count == 0)
        {
            EditorUtility.DisplayDialog("No Cookie Data Found",
                "No LightCookieData components found in the selected objects or their children.", "OK");
            return;
        }
        AssetDatabase.StartAssetEditing();
        try
        {
            CookieGenerator.RunWithProgress("Baking Cookies", "Preparing...", "Preheating oven...", targetList, (data, i) =>
            {
                Light light = data.GetComponent<Light>();
                if (light == null) return;
                string baseName = $"Cookie_{data.ResolveBaseName()}_{light.type}";
                string safeName = CookieGenerator.ResolveSafeName(baseName, data.savePath, data.lastBakedName, usedNames);
                usedNames.Add(safeName);
                Undo.RecordObject(light, "Bake Light Cookie");
                Undo.RecordObject(data, "Bake Light Cookie");
                bool saved = CookieTextureSaver.GenerateCookieForLight(light, data, data.savePath, safeName, true);
                if (!saved) return;
                data.lastBakedName = safeName;
                EditorUtility.SetDirty(data);
            });
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();
    }

    [MenuItem(BAKE_COOKIE_DATA, true)]
    private static bool BakeCookiesInChildren_Validate()
    {
        if (Selection.gameObjects.Length == 0) return false;
        foreach (GameObject selected in Selection.gameObjects)
        {
            LightCookieData[] datas = selected.GetComponentsInChildren<LightCookieData>(includeInactive: true);
            foreach (LightCookieData data in datas)
            {
                Light light = data.GetComponent<Light>();
                if (light != null && data.occluders.Count > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }
}