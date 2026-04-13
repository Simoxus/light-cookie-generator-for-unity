using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CookieGenerator : EditorWindow
{
    public static class RenderPipelineInfo
    {
        public static RenderPipelineAsset renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;

        public static bool IsSupportedPipeline()
        {
            return IsUniversalRenderPipeline() || IsHighDefinitionRenderPipeline();
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
    }
    private CookieGeneratorSettings settings;
    private SerializedObject serializedSettings;
    private CookieGeneratorWindow mainWindow;
    private CookieRenderer renderer;
    private CookieTextureSaver saver;
    private CookieGizmoDrawer gizmoDrawer;
    private Dictionary<Light, Texture2D> batchPreviewTextures = new Dictionary<Light, Texture2D>();

    [MenuItem("Tools/Light Cookie Generator")]
    public static void ShowWindow()
    {
        GetWindow<CookieGenerator>("Cookie Generator");
    }

    private void OnEnable()
    {
        if (settings == null)
        {
            settings = CreateInstance<CookieGeneratorSettings>();
            settings.LoadFromEditorPrefs();
        }

        serializedSettings = new SerializedObject(settings);
        mainWindow = new CookieGeneratorWindow();
        mainWindow.Initialize(serializedSettings);
        renderer = new CookieRenderer(settings);
        saver = new CookieTextureSaver(settings);
        gizmoDrawer = new CookieGizmoDrawer(settings);

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        if (settings != null)
        {
            settings.SaveToEditorPrefs();
        }

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

        mainWindow.DrawHeader();
        mainWindow.DrawVisibilityError();
        mainWindow.DrawValidationWarning();

        mainWindow.DrawLoadSettings(settings);

        mainWindow.DrawLightsList();
        mainWindow.DrawRenderingSettings(settings);
        mainWindow.DrawShadowSettings(settings);
        mainWindow.DrawBlurSettings(settings);
        mainWindow.DrawCameraSettings(settings);
        mainWindow.DrawOutputSettings(settings, saver);

        mainWindow.DrawPreview(batchPreviewTextures);
        mainWindow.DrawGenerateButtons(settings, GeneratePreviews, GenerateAllCookies);
        mainWindow.DrawResetButton(settings);

        mainWindow.EndDraw();

        if (GUI.changed)
        {
            settings.SaveToEditorPrefs();
            gizmoDrawer.SetLights(mainWindow.GetLights());

            SceneView.RepaintAll();
        }
    }

    private void ClearBatchPreviews()
    {
        foreach (var texture in batchPreviewTextures.Values)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }

        batchPreviewTextures.Clear();
    }

    private void GeneratePreviews()
    {
        ClearBatchPreviews();

        List<Light> lights = mainWindow.GetLights();
        if (lights.Count == 0) return;

        EditorUtility.DisplayProgressBar("Generating Previews", "Preparing...", 0f);

        try
        {
            for (int i = 0; i < lights.Count; i++)
            {
                Light light = lights[i];
                if (light == null) continue;

                float progress = (float)i / lights.Count;
                EditorUtility.DisplayProgressBar("Generating Previews",
                    $"Processing {light.name} ({i + 1}/{lights.Count})",
                    progress);

                CookieGeneratorSettings tempSettings = CreateInstance<CookieGeneratorSettings>();

                // Copy all settings
                tempSettings.baseName = settings.baseName;
                tempSettings.resolution = settings.resolution;
                tempSettings.useSpotlightRange = settings.useSpotlightRange;
                tempSettings.shadowPlaneDistance = settings.shadowPlaneDistance;
                tempSettings.shadowOpacity = settings.shadowOpacity;
                tempSettings.cookieBrightness = settings.cookieBrightness;
                tempSettings.shadowSampleRadius = settings.shadowSampleRadius;
                tempSettings.shadowSamples = settings.shadowSamples;
                tempSettings.blurMethod = settings.blurMethod;
                tempSettings.blurRadius = settings.blurRadius;
                tempSettings.blurIterations = settings.blurIterations;
                tempSettings.rotationOffset = settings.rotationOffset;
                tempSettings.orthographicSize = settings.orthographicSize;
                tempSettings.savePath = settings.savePath;

                // Set light-specific settings
                tempSettings.cameraTransform = light.transform;
                tempSettings.referenceLight = light;

                // Create renderer with temp settings
                CookieRenderer tempRenderer = new CookieRenderer(tempSettings);
                Texture2D preview = tempRenderer.RenderCookie(256);
                batchPreviewTextures[light] = preview;

                DestroyImmediate(tempSettings);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Repaint();
    }

    private void GenerateAllCookies()
    {
        mainWindow.GenerateAllCookies(settings, renderer, saver);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        gizmoDrawer.DrawSceneGizmos();
    }
}