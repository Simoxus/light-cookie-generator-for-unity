using UnityEditor;
using UnityEngine;

[FilePath("ProjectSettings/LightCookieGeneratorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public class LightCookieGeneratorProjectSettings : ScriptableSingleton<LightCookieGeneratorProjectSettings>
{
    // Editor
    public bool showStupidStuff = CookieGeneratorDefaults.SHOW_STUPID_STUFF;

    // Output
    public string savePath = CookieGeneratorDefaults.SAVE_PATH;
    public string baseName = CookieGeneratorDefaults.BASE_NAME;
    public int resolution = CookieGeneratorDefaults.RESOLUTION;

    public void Save() => Save(true);

    public void ApplyOutputDefaults(LightCookieData data)
    {
        data.savePath = savePath;
        data.baseName = baseName;
        data.resolution = resolution;
    }
}

[InitializeOnLoad]
internal static class LightCookieGeneratorProjectDefaults
{
    static LightCookieGeneratorProjectDefaults()
    {
        LightCookieData.applyProjectDefaults = data => LightCookieGeneratorProjectSettings.instance.ApplyOutputDefaults(data);
    }
}

internal static class LightCookieGeneratorProjectSettingsProvider
{
    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        var provider = new SettingsProvider("Project/Light Cookie Generator", SettingsScope.Project)
        {
            label = "Light Cookie Generator",
            guiHandler = (searchContext) =>
            {
                var settings = LightCookieGeneratorProjectSettings.instance;

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(10); // Top

                    using (new GUILayout.VerticalScope())
                    {
                        EditorGUILayout.Space(8); // Left

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
                        settings.showStupidStuff = EditorGUILayout.Toggle(
                            new GUIContent("Show Stupid Stuff", "Shows the oven-themed progress messages and blur descriptions."),
                            settings.showStupidStuff
                        );

                        EditorGUILayout.Space(8);

                        EditorGUILayout.LabelField(
                            new GUIContent("Output Defaults", "Copied into each newly added Light Cookie Data component, which can still override them."),
                            EditorStyles.boldLabel
                        );

                        EditorGUILayout.BeginHorizontal();
                        settings.savePath = EditorGUILayout.TextField("Save Path", settings.savePath);
                        if (GUILayout.Button("Browse", GUILayout.Width(60)))
                        {
                            string selected = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
                            if (!string.IsNullOrEmpty(selected))
                            {
                                if (selected.StartsWith(Application.dataPath))
                                {
                                    settings.savePath = "Assets" + selected.Substring(Application.dataPath.Length);
                                    GUI.changed = true;
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Invalid Path",
                                        "Please select a folder inside your Assets folder.", "OK");
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        settings.baseName = EditorGUILayout.TextField("Base Name", settings.baseName);
                        settings.resolution = Mathf.Max(1, EditorGUILayout.IntField("Resolution", settings.resolution));

                        if (EditorGUI.EndChangeCheck())
                        {
                            settings.Save();
                        }
                    }
                }
            },
            keywords = new[] { "cookie", "light", "save path", "base name", "resolution" }
        };

        return provider;
    }
}
