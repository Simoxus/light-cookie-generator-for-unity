using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Rendering/Light Cookie Data")]
public class LightCookieData : MonoBehaviour
{
    [Space]
    public List<OccluderEntry> occluders = new();

    // Naming
    public bool automaticNaming = CookieGeneratorDefaults.AUTOMATIC_NAMING;
    public bool useParentName = CookieGeneratorDefaults.USE_PARENT_NAME;
    public int parentLevel = CookieGeneratorDefaults.PARENT_LEVEL;
    public bool putCookiesInParentFolder = CookieGeneratorDefaults.PUT_COOKIES_IN_PARENT_FOLDER;
    public string nameSuffix = CookieGeneratorDefaults.NAME_SUFFIX;

    // Render
    [Range(0f, 1f)] public float shadowOpacity = CookieGeneratorDefaults.SHADOW_OPACITY;
    [Range(0f, 1f)] public float cookieBrightness = CookieGeneratorDefaults.COOKIE_BRIGHTNESS;

    // Blur
    public CookieTextureProcessor.SmoothingMethod blurMethod = CookieGeneratorDefaults.BLUR_METHOD;
    [Range(1, 10)] public int blurRadius = CookieGeneratorDefaults.BLUR_RADIUS;
    [Range(1, 5)] public int blurIterations = CookieGeneratorDefaults.BLUR_ITERATIONS;

    // Camera
    public Vector3 rotationOffset = CookieGeneratorDefaults.ROTATION_OFFSET;
    public float spotNearClip = CookieGeneratorDefaults.SPOT_NEAR_CLIP;
    public float spotFarClip = CookieGeneratorDefaults.SPOT_FAR_CLIP;
    public float orthographicSize = CookieGeneratorDefaults.ORTHOGRAPHIC_SIZE;

    // Output
    public string savePath = CookieGeneratorDefaults.SAVE_PATH;
    public string baseName = CookieGeneratorDefaults.BASE_NAME;
    public int resolution = CookieGeneratorDefaults.RESOLUTION;

    [HideInInspector] public Texture2D lastBakedCookie;
    [HideInInspector] public Cubemap lastBakedPointCookie;
    [HideInInspector] public string lastBakedName = "";
    [HideInInspector] public string componentGuid = "";

#if UNITY_EDITOR
    private void Reset()
    {
        RegenerateGuid();

        Light light = GetComponent<Light>();
        if (light == null) return;

        // Try placing LightCookieData just below the Light component; u can remove this if you'd like
        int maxMoves = 20;
        while (maxMoves-- > 0)
        {
            UnityEditorInternal.ComponentUtility.MoveComponentUp(this);
            Component[] components = GetComponents<Component>();
            int componentIndex = System.Array.IndexOf(components, this);
            if (componentIndex > 0 && components[componentIndex - 1] is Light) break;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(componentGuid))
        {
            RegenerateGuid();
            return;
        }

        foreach (var other in FindObjectsByType<LightCookieData>(FindObjectsSortMode.None))
        {
            if (other != this && other.componentGuid == componentGuid)
            {
                RegenerateGuid();
                break;
            }
        }
    }

    [ContextMenu("Regenerate GUID for Cookie")]
    private void RegenerateGuid()
    {
        componentGuid = System.Guid.NewGuid().ToString();
        lastBakedName = "";
        lastBakedCookie = null;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public string ResolveBaseName()
    {
        string name;

        if (!automaticNaming)
        {
            name = string.IsNullOrEmpty(baseName) ? gameObject.name : baseName;
        }
        else if (useParentName)
        {
            Transform current = transform;
            for (int i = 0; i < parentLevel; i++)
            {
                if (current.parent == null) break;
                current = current.parent;
            }
            name = current.gameObject.name;
        }
        else
        {
            name = gameObject.name;
        }

        return string.IsNullOrEmpty(nameSuffix) ? name : $"{name}{nameSuffix}";
    }
}