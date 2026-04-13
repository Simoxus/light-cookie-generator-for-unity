using System.Collections.Generic;
using UnityEngine;

public class CookieGeneratorSettings : ScriptableObject
{
    // Render
    public float shadowOpacity = CookieGeneratorDefaults.SHADOW_OPACITY;
    public float cookieBrightness = CookieGeneratorDefaults.COOKIE_BRIGHTNESS;

    // Blur
    public CookieTextureProcessor.SmoothingMethod blurMethod = CookieGeneratorDefaults.BLUR_METHOD;
    public int blurRadius = CookieGeneratorDefaults.BLUR_RADIUS;
    public int blurIterations = CookieGeneratorDefaults.BLUR_ITERATIONS;

    // Camera
    public Vector3 rotationOffset = CookieGeneratorDefaults.ROTATION_OFFSET;
    public float spotNearClip = CookieGeneratorDefaults.SPOT_NEAR_CLIP;
    public float spotFarClip = CookieGeneratorDefaults.SPOT_FAR_CLIP;
    public float orthographicSize = CookieGeneratorDefaults.ORTHOGRAPHIC_SIZE;
    public List<OccluderEntry> occluders = new List<OccluderEntry>();

    // Per-generation
    public Transform cameraTransform;
    public Light referenceLight;

    // Output
    public string baseName = CookieGeneratorDefaults.BASE_NAME;
    public int resolution = CookieGeneratorDefaults.RESOLUTION;
    public string savePath = CookieGeneratorDefaults.SAVE_PATH;

    public bool CanGenerate()
    {
        return cameraTransform != null && occluders.Count > 0;
    }

    public void Reset()
    {
        shadowOpacity = CookieGeneratorDefaults.SHADOW_OPACITY;
        cookieBrightness = CookieGeneratorDefaults.COOKIE_BRIGHTNESS;
        blurMethod = CookieGeneratorDefaults.BLUR_METHOD;
        blurRadius = CookieGeneratorDefaults.BLUR_RADIUS;
        blurIterations = CookieGeneratorDefaults.BLUR_ITERATIONS;
        rotationOffset = CookieGeneratorDefaults.ROTATION_OFFSET;
        spotNearClip = CookieGeneratorDefaults.SPOT_NEAR_CLIP;
        spotFarClip = CookieGeneratorDefaults.SPOT_FAR_CLIP;
        orthographicSize = CookieGeneratorDefaults.ORTHOGRAPHIC_SIZE;
        occluders.Clear();
        cameraTransform = null;
        referenceLight = null;
        baseName = CookieGeneratorDefaults.BASE_NAME;
        resolution = CookieGeneratorDefaults.RESOLUTION;
        savePath = CookieGeneratorDefaults.SAVE_PATH;
    }

    public static CookieGeneratorSettings From(LightCookieData data, Light light)
    {
        var s = CreateInstance<CookieGeneratorSettings>();
        s.cameraTransform = light.transform;
        s.referenceLight = light;
        s.shadowOpacity = data.shadowOpacity;
        s.cookieBrightness = data.cookieBrightness;
        s.blurMethod = data.blurMethod;
        s.blurRadius = data.blurRadius;
        s.blurIterations = data.blurIterations;
        s.rotationOffset = data.rotationOffset;
        s.orthographicSize = data.orthographicSize;
        s.spotNearClip = data.spotNearClip;
        s.spotFarClip = data.spotFarClip;
        s.resolution = data.resolution;
        s.savePath = data.savePath;
        s.occluders = new List<OccluderEntry>(data.occluders);
        return s;
    }

    public static CookieGeneratorSettings From(CookieGeneratorSettings src, Light light)
    {
        var s = CreateInstance<CookieGeneratorSettings>();
        s.cameraTransform = light.transform;
        s.referenceLight = light;
        s.shadowOpacity = src.shadowOpacity;
        s.cookieBrightness = src.cookieBrightness;
        s.blurMethod = src.blurMethod;
        s.blurRadius = src.blurRadius;
        s.blurIterations = src.blurIterations;
        s.rotationOffset = src.rotationOffset;
        s.orthographicSize = src.orthographicSize;
        s.spotNearClip = src.spotNearClip;
        s.spotFarClip = src.spotFarClip;
        s.resolution = src.resolution;
        s.savePath = src.savePath;
        s.occluders = new List<OccluderEntry>(src.occluders);
        return s;
    }
}