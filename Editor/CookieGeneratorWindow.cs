using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

public class CookieGeneratorWindow
{
    private Texture2D _headerIcon;
    private Vector2 _scrollPosition;
    private List<Light> _lights = new List<Light>();
    private Dictionary<Light, Editor> _lightDataEditors = new Dictionary<Light, Editor>();
    private Dictionary<Light, bool> _lightDataFoldouts = new Dictionary<Light, bool>();
    private bool _showPreviews = false;

    public void BeginDraw()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
    }

    public void EndDraw()
    {
        EditorGUILayout.EndScrollView();
    }

    public void DrawHeader()
    {
        if (_headerIcon == null)
        {
            string[] guids = AssetDatabase.FindAssets("CookieGenerator-cover t:Texture2D");
            if (guids.Length > 0)
            {
                _headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (_headerIcon != null)
        {
            GUILayout.Label(_headerIcon, GUILayout.Width(216), GUILayout.Height(144));
        }
        else
        {
            GUILayout.Label("Light Cookie Generator", EditorStyles.whiteLargeLabel);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    public bool DrawPipelineError()
    {
        if (!CookieGenerator.RenderPipelineInfo.IsSupportedPipeline())
        {
            EditorGUILayout.HelpBox("Cookie baking only supports URP and HDRP. :(", MessageType.Error);
            return true;
        }
        return false;
    }

    public void DrawValidationWarning()
    {
        if (_lights.Count == 0)
        {
            EditorGUILayout.HelpBox("Add at least one Light to generate cookies.", MessageType.Warning);
        }
        foreach (Light light in _lights)
        {
            if (light != null && light.GetComponent<LightCookieData>() == null)
            {
                EditorGUILayout.HelpBox($"{light.gameObject.name} has no LightCookieData component.", MessageType.Warning);
            }
        }
    }

    public void DrawLightsList()
    {
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"Lights ({_lights.Count})", EditorStyles.boldLabel);

        for (int i = _lights.Count - 1; i >= 0; i--)
        {
            if (_lights[i] == null)
            {
                _lights.RemoveAt(i);
                continue;
            }

            Light light = _lights[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(light, typeof(Light), true);
            EditorGUI.EndDisabledGroup();
            GUILayout.Label($"{light.type}", GUILayout.Width(100));
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                _lights.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        Light newLight = EditorGUILayout.ObjectField("Add Light", null, typeof(Light), true) as Light;
        if (newLight != null && !_lights.Contains(newLight))
        {
            if (CookieGenerator.IsValidLightType(newLight))
            {
                _lights.Add(newLight);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Selected Lights")) AddSelectedLights();
        if (GUILayout.Button("Clear All Lights")) _lights.Clear();

        EditorGUILayout.Space();

        if (GUILayout.Button("Add All Lights With Cookie Data")) AddLightsWithCookieData(enabledOnly: false);
        if (GUILayout.Button("Add Enabled Lights With Cookie Data")) AddLightsWithCookieData(enabledOnly: true);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    public void DrawLightDataEditors()
    {
        CleanupStaleEditors();

        var usedNames = new HashSet<string>();
        var resolvedNames = new Dictionary<Light, string>();
        foreach (Light light in _lights)
        {
            LightCookieData data = light.GetComponent<LightCookieData>();
            if (data == null) continue;
            string baseName = $"Cookie_{data.ResolveBaseName()}_{light.type}";
            string uniqueName = CookieGenerator.ResolveSafeName(baseName, data.savePath, null, usedNames);
            usedNames.Add(uniqueName);
            resolvedNames[light] = uniqueName;
        }

        foreach (Light light in _lights)
        {
            LightCookieData data = light.GetComponent<LightCookieData>();
            if (data == null) continue;

            if (!_lightDataEditors.TryGetValue(light, out Editor editor) || editor == null)
            {
                _lightDataEditors[light] = editor = Editor.CreateEditor(data);
            }

            if (!_lightDataFoldouts.ContainsKey(light))
            {
                _lightDataFoldouts[light] = false;
            }

            string displayName = resolvedNames.TryGetValue(light, out string n) ? n : data.ResolveBaseName();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                _lightDataFoldouts[light] = EditorGUILayout.Foldout(_lightDataFoldouts[light], displayName, true, EditorStyles.foldout);
                if (_lightDataFoldouts[light])
                {
                    editor.OnInspectorGUI();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space();
        }
    }

    public void DrawPreview(Dictionary<Light, Texture2D> previews)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        _showPreviews = EditorGUILayout.Foldout(_showPreviews, $"Preview ({previews.Count})", true);

        if (_showPreviews && previews.Count > 0)
        {
            EditorGUILayout.Space();
            foreach (var kvp in previews)
            {
                if (kvp.Key == null || kvp.Value == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label(kvp.Key.name, EditorStyles.boldLabel);
                GUILayout.Box(kvp.Value, GUILayout.Width(128), GUILayout.Height(128));

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    public List<Light> GetLights() => _lights;

    public void DrawGenerateButtons(CookieGeneratorSettings settings, Action onGeneratePreviews, Action onGenerateAll)
    {
        EditorGUILayout.BeginHorizontal();

        bool allReady = _lights.Count > 0 && _lights.TrueForAll(
            light => light != null && light.GetComponent<LightCookieData>()?.occluders.Count > 0);

        GUI.enabled = allReady;

        string lightWord = _lights.Count == 1 ? "Light" : "Lights";
        string cookieWord = _lights.Count == 1 ? "Cookie" : "Cookies";

        if (GUILayout.Button($"Preview {_lights.Count} {lightWord}", GUILayout.Height(40)))
        {
            onGeneratePreviews?.Invoke();
        }
        if (GUILayout.Button($"Generate {_lights.Count} {cookieWord}", GUILayout.Height(40)))
        {
            onGenerateAll?.Invoke();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    public void GenerateAllCookies()
    {
        if (_lights.Count == 0) return;

        var usedNames = new HashSet<string>();
        CookieGenerator.RunWithProgress("Generating Cookies", "Preparing...", "Preheating oven...", _lights, (light, i) =>
        {
            LightCookieData data = light.GetComponent<LightCookieData>();
            if (data == null) return;
            string baseName = $"Cookie_{data.ResolveBaseName()}_{light.type}";
            string safeName = CookieGenerator.ResolveSafeName(baseName, data.savePath, data.lastBakedName, usedNames);
            usedNames.Add(safeName);
            CookieTextureSaver.GenerateCookieForLight(light, data, data.savePath, safeName, true);
            data.lastBakedName = safeName;
            EditorUtility.SetDirty(data);
        });

        AssetDatabase.Refresh();
    }

    private void AddSelectedLights()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Select one or more GameObjects with Light components.", "OK");
            return;
        }

        int added = 0;
        foreach (GameObject gameObject in Selection.gameObjects)
        {
            Light light = gameObject.GetComponent<Light>();
            if (light == null || _lights.Contains(light)) continue;
            if (CookieGenerator.IsValidLightType(light))
            {
                _lights.Add(light);
                added++;
            }
        }

        if (added == 0)
        {
            EditorUtility.DisplayDialog("No Lights Found", "No valid lights found in selection.", "OK");
        }
    }

    private void AddLightsWithCookieData(bool enabledOnly)
    {
        foreach (var data in Object.FindObjectsByType<LightCookieData>(FindObjectsSortMode.None))
        {
            if (enabledOnly && (!data.enabled)) continue;
            Light light = data.GetComponent<Light>();
            if (light == null || (enabledOnly && !light.enabled) || _lights.Contains(light)) continue;
            if (CookieGenerator.IsValidLightType(light))
            {
                _lights.Add(light);
            }
        }
    }

    private void CleanupStaleEditors()
    {
        var removedKeys = new List<Light>();
        foreach (var key in _lightDataEditors.Keys)
            if (key == null || !_lights.Contains(key))
                removedKeys.Add(key);

        foreach (var key in removedKeys)
        {
            if (_lightDataEditors.TryGetValue(key, out var editor) && editor != null)
                Object.DestroyImmediate(editor);
            _lightDataEditors.Remove(key);
            _lightDataFoldouts.Remove(key);
        }
    }
}