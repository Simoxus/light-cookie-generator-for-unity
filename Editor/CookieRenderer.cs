using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CookieRenderer
{
    private CookieGeneratorSettings settings;
    private Material _blackMaterial;

    public void Dispose()
    {
        if (_blackMaterial != null)
        {
            Object.DestroyImmediate(_blackMaterial);
            _blackMaterial = null;
        }
    }

    private Material BlackMaterial
    {
        get
        {
            if (_blackMaterial == null)
            {
                string shaderName = GetBlackShaderName();
                if (shaderName == null) return null;

                _blackMaterial = new Material(Shader.Find(GetBlackShaderName()));
                if (CookieGenerator.RenderPipelineInfo.IsBuiltInRenderPipeline())
                {
                    _blackMaterial.SetColor("_Color", Color.black);
                }
                else
                {
                    _blackMaterial.SetColor("_BaseColor", Color.black);
                }
            }
            return _blackMaterial;
        }
    }

    public CookieRenderer(CookieGeneratorSettings settings)
    {
        this.settings = settings;
    }

    public Texture2D RenderCookie(int renderResolution)
    {
        Texture2D texture = RenderOccluders(renderResolution);

        if (settings.blurMethod != CookieTextureProcessor.SmoothingMethod.None)
        {
            Texture2D smoothed = CookieTextureProcessor.SmoothCookie(texture, settings.blurMethod, settings.blurRadius, settings.blurIterations);
            Object.DestroyImmediate(texture);
            texture = smoothed;
        }

        return texture;
    }

    public Texture2D RenderOccluders(int renderResolution)
    {
        var cameraObj = new GameObject("TempCookieCamera");
        var camera = cameraObj.AddComponent<Camera>();
        if (CookieGenerator.RenderPipelineInfo.IsUniversalRenderPipeline())
        {
            var cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
        }
        else if (CookieGenerator.RenderPipelineInfo.IsHighDefinitionRenderPipeline())
        {
            CookieGenerator.RenderPipelineInfo.AddHDCameraData(cameraObj);
        }
        camera.enabled = false;

        cameraObj.transform.position = settings.cameraTransform.position;
        cameraObj.transform.rotation = settings.cameraTransform.rotation * Quaternion.Euler(settings.rotationOffset);

        bool isSpot = settings.referenceLight != null && settings.referenceLight.type == LightType.Spot;

        if (isSpot)
        {
            camera.orthographic = false;
            camera.fieldOfView = settings.referenceLight.spotAngle;
            camera.nearClipPlane = settings.spotNearClip;
            camera.farClipPlane = settings.spotFarClip;
        }
        else
        {
            camera.orthographic = true;
            camera.orthographicSize = settings.orthographicSize;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = settings.spotFarClip;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.aspect = 1f;
        camera.cullingMask = 1 << CookieGeneratorDefaults.OCCLUDER_LAYER;

        float[] composite = BuildComposite(camera, renderResolution);

        Color[] result = new Color[composite.Length];
        for (int i = 0; i < composite.Length; i++) // The composite starts fully white, and then each occluder acts a sort of layer, adding darkness to it
            result[i] = new Color(composite[i], composite[i], composite[i], 1f);

        var texture = new Texture2D(renderResolution, renderResolution, TextureFormat.RGB24, false);
        texture.SetPixels(result);
        texture.Apply();

        Object.DestroyImmediate(cameraObj);

        ApplyIntensitySettings(texture);
        return texture;
    }

    public Texture2D RenderSingleOccluder(MeshRenderer mr, Camera cam, int resolution)
    {
        var blackMat = BlackMaterial;
        var originalMats = mr.sharedMaterials;
        var originalLayer = mr.gameObject.layer;

        mr.gameObject.layer = CookieGeneratorDefaults.OCCLUDER_LAYER;
        var blackMats = new Material[mr.sharedMaterials.Length];
        for (int i = 0; i < blackMats.Length; i++) blackMats[i] = blackMat;
        mr.sharedMaterials = blackMats;

        var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        mr.sharedMaterials = originalMats;
        mr.gameObject.layer = originalLayer;

        rt.Release();
        Object.DestroyImmediate(rt);

        return tex;
    }

    public Cubemap RenderPointCookie(int renderResolution)
    {
        var cubemap = new Cubemap(renderResolution, TextureFormat.RGB24, false);

        (CubemapFace face, Quaternion rot)[] faces = {
        (CubemapFace.PositiveX, Quaternion.LookRotation(Vector3.right,   Vector3.down)),
        (CubemapFace.NegativeX, Quaternion.LookRotation(Vector3.left,    Vector3.down)),
        (CubemapFace.PositiveY, Quaternion.LookRotation(Vector3.up,      Vector3.forward)),
        (CubemapFace.NegativeY, Quaternion.LookRotation(Vector3.down,    Vector3.back)),
        (CubemapFace.PositiveZ, Quaternion.LookRotation(Vector3.forward, Vector3.down)),
        (CubemapFace.NegativeZ, Quaternion.LookRotation(Vector3.back,    Vector3.down)),
    };

        foreach (var (face, rot) in faces)
        {
            Texture2D faceTexture = RenderFace(rot, renderResolution);
            cubemap.SetPixels(faceTexture.GetPixels(), face);
            Object.DestroyImmediate(faceTexture);
        }

        cubemap.Apply();
        return cubemap;
    }

    public Texture2D RenderFace(Quaternion rotation, int resolution)
    {
        var cameraObj = new GameObject("TempCookieCamera");
        var camera = cameraObj.AddComponent<Camera>();
        if (CookieGenerator.RenderPipelineInfo.IsUniversalRenderPipeline())
        {
            var cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
        }
        else if (CookieGenerator.RenderPipelineInfo.IsHighDefinitionRenderPipeline())
        {
            CookieGenerator.RenderPipelineInfo.AddHDCameraData(cameraObj);
        }
        camera.enabled = false;

        camera.transform.position = settings.cameraTransform.position;
        camera.transform.rotation = rotation;
        camera.orthographic = false;
        camera.fieldOfView = 90f;
        camera.nearClipPlane = settings.spotNearClip;
        camera.farClipPlane = settings.referenceLight != null ? settings.referenceLight.range : settings.spotFarClip;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.aspect = 1f;
        camera.cullingMask = 1 << CookieGeneratorDefaults.OCCLUDER_LAYER;

        float[] composite = BuildComposite(camera, resolution);

        Color[] result = new Color[composite.Length];
        for (int i = 0; i < composite.Length; i++)
            result[i] = new Color(composite[i], composite[i], composite[i], 1f);

        var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        texture.SetPixels(result);
        texture.Apply();

        Object.DestroyImmediate(cameraObj);
        ApplyIntensitySettings(texture);
        return texture;
    }

    private float[] BuildComposite(Camera cam, int resolution)
    {
        float[] composite = new float[resolution * resolution];
        for (int i = 0; i < composite.Length; i++) composite[i] = 1f;

        foreach (var entry in settings.occluders)
        {
            if (entry == null || entry.renderer == null || !entry.enabled) continue;

            Texture2D mask = RenderSingleOccluder(entry.renderer, cam, resolution);

            if (entry.dilateRadius > 0)
            {
                Texture2D dilated = CookieTextureProcessor.Dilate(mask, entry.dilateRadius);
                Object.DestroyImmediate(mask);
                mask = dilated;
            }

            if (entry.erodeRadius > 0)
            {
                Texture2D eroded = CookieTextureProcessor.Erode(mask, entry.erodeRadius);
                Object.DestroyImmediate(mask);
                mask = eroded;
            }

            Color[] pixels = mask.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float value = pixels[i].grayscale;
                if (entry.invert) value = 1f - value;
                float occluded = Mathf.Min(composite[i], value);
                composite[i] = Mathf.Lerp(composite[i], occluded, entry.opacity);
            }

            Object.DestroyImmediate(mask);
        }

        return composite;
    }

    private void ApplyIntensitySettings(Texture2D texture)
    {
        if (settings.shadowOpacity >= 1f && settings.cookieBrightness <= 0f) return;

        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float lum = pixels[i].grayscale;

            if (settings.shadowOpacity < 1f)
            {
                float shadowMask = 1f - lum;
                lum = Mathf.Lerp(lum, 1f, shadowMask * (1f - settings.shadowOpacity));
            }

            if (settings.cookieBrightness > 0f)
            {
                lum = Mathf.Lerp(lum, 1f, settings.cookieBrightness);
            }

            pixels[i] = new Color(lum, lum, lum, 1f);
        }
        texture.SetPixels(pixels);
        texture.Apply();
    }

    private static string GetBlackShaderName()
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
}