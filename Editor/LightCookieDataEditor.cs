using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightCookieData))]
[CanEditMultipleObjects]
public class LightCookieDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (target == null) return;

        if (!CookieGenerator.RenderPipelineInfo.IsSupportedPipeline())
        {
            EditorGUILayout.HelpBox("Cookie baking is not supported for this pipeline.", MessageType.Error);
            return;
        }

        serializedObject.Update();

        var data = (LightCookieData)target;
        var light = data.GetComponent<Light>();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("occluders"), true);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Naming", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Resolved Name", data.ResolveBaseName());
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("automaticNaming"));
        if (data.automaticNaming)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useParentName"));
            if (data.useParentName)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("parentLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("putCookiesInParentFolder"));
            }
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseName"));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nameSuffix"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Render", EditorStyles.boldLabel);
        var shadowProp = serializedObject.FindProperty("shadowOpacity");
        shadowProp.floatValue = EditorGUILayout.Slider("Shadow Opacity", shadowProp.floatValue, 0f, 1f);
        var brightnessProp = serializedObject.FindProperty("cookieBrightness");
        brightnessProp.floatValue = EditorGUILayout.Slider("Cookie Brightness", brightnessProp.floatValue, 0f, 1f);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Blur", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("blurMethod"));
        if (CookieGeneratorDefaults.SHOW_STUPID_STUFF)
        {
            var currentMethod = (CookieTextureProcessor.SmoothingMethod)serializedObject.FindProperty("blurMethod").enumValueIndex;
            string blurDesc = currentMethod switch
            {
                CookieTextureProcessor.SmoothingMethod.None => "Straight out of the oven",
                CookieTextureProcessor.SmoothingMethod.Gaussian => "Don't bring this to parties it's boring",
                CookieTextureProcessor.SmoothingMethod.Kawase => "Supposed to be dubai chocolate but it looks pretty much the same as Gaussian",
                CookieTextureProcessor.SmoothingMethod.Spiral => "This is the only one you're allowed to use",
                _ => ""
            };
            GUILayout.Label(blurDesc, EditorStyles.helpBox);
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("blurRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("blurIterations"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Camera", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spotNearClip"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spotFarClip"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("orthographicSize"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUILayout.Label("Output", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("savePath"));
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                {
                    serializedObject.FindProperty("savePath").stringValue =
                        "Assets" + selected.Substring(Application.dataPath.Length);
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Path",
                        "Please select a folder inside your Assets folder.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resolution"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();

        string cookieWord = targets.Length == 1 ? "Cookie" : "Cookies";
        string label = targets.Length > 1 ? $"Bake {targets.Length} {cookieWord}" : $"Bake {cookieWord}";

        if (GUILayout.Button(label))
        {
            var usedNames = new HashSet<string>();
            var targetList = new List<LightCookieData>();
            foreach (var t in targets) targetList.Add((LightCookieData)t);

            CookieGenerator.RunWithProgress("Baking Cookies", "Preparing...", "Preheating oven...", targetList, (lightCookieData, i) =>
            {
                var targetLight = lightCookieData.GetComponent<Light>();
                if (targetLight == null) return;
                string baseName = $"Cookie_{lightCookieData.ResolveBaseName()}_{targetLight.type}";
                string safeName = CookieGenerator.ResolveSafeName(baseName, lightCookieData.savePath, lightCookieData.lastBakedName, usedNames);
                usedNames.Add(safeName);
                BakeCookie(lightCookieData, targetLight, safeName);
            });
        }

        if (GUI.changed)
        {
            Repaint();
        }
    }

    private void BakeCookie(LightCookieData data, Light light, string safeName)
    {
        Undo.RecordObject(light, "Bake Light Cookie");
        Undo.RecordObject(data, "Bake Light Cookie");

        bool saved = CookieTextureSaver.GenerateCookieForLight(light, data, data.savePath, safeName, true);
        if (!saved) return;

        var previewSettings = CookieGeneratorSettings.From(data, light);
        var renderer = new CookieRenderer(previewSettings);
        data.lastBakedCookie = light.type == LightType.Point
            ? renderer.RenderFace(Quaternion.LookRotation(Vector3.forward, Vector3.down), 128)
            : renderer.RenderCookie(128);
        renderer.Dispose();
        DestroyImmediate(previewSettings);

        data.lastBakedName = safeName;
        EditorUtility.SetDirty(data);
    }
}