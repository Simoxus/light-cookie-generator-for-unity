using UnityEngine;

public static class CookieTextureProcessor
{
    public enum SmoothingMethod
    {
        None,
        Gaussian,
        Kawase,
        Spiral
    }

    private const int PASS_DILATE_H = 0;
    private const int PASS_DILATE_V = 1;
    private const int PASS_GAUSSIAN_H = 2;
    private const int PASS_GAUSSIAN_V = 3;
    private const int PASS_KAWASE = 4;
    private const int PASS_SPIRAL = 5;

    private static Material _material;

    private static Material Material
    {
        get
        {
            if (_material == null)
            {
                var shader = Shader.Find("Hidden/CookieSmoother");
                if (shader == null) return null;

                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return _material;
        }
    }

    public static Texture2D SmoothCookie(Texture2D source, SmoothingMethod method, int radius, int iterations)
    {
        if (method == SmoothingMethod.None || radius <= 0 || Material == null)
        {
            return Blit2D(source);
        }

        int w = source.width, h = source.height;
        var rt0 = TempRT(w, h);
        var rt1 = TempRT(w, h);

        if (method == SmoothingMethod.Gaussian)
        {
            Graphics.Blit(source, rt0);
            Material.SetInt("_Radius", radius);
            for (int i = 0; i < iterations; i++)
            {
                Graphics.Blit(rt0, rt1, Material, PASS_GAUSSIAN_H);
                Graphics.Blit(rt1, rt0, Material, PASS_GAUSSIAN_V);
            }
            Texture2D gaussResult = ReadBack(rt0, w, h);
            ReleaseRT(rt0); ReleaseRT(rt1);
            return gaussResult;
        }

        if (method == SmoothingMethod.Kawase || method == SmoothingMethod.Spiral)
        {
            int pass = method == SmoothingMethod.Kawase ? PASS_KAWASE : PASS_SPIRAL;
            var ping = rt0;
            var pong = rt1;
            Graphics.Blit(source, ping);
            Material.SetInt("_Radius", radius);
            for (int i = 0; i < iterations; i++)
            {
                Material.SetFloat("_KawaseOffset", i);
                Graphics.Blit(ping, pong, Material, pass);
                (ping, pong) = (pong, ping);
            }
            Texture2D result = ReadBack(ping, w, h);
            ReleaseRT(rt0); ReleaseRT(rt1);
            return result;
        }

        ReleaseRT(rt0); ReleaseRT(rt1);
        return Blit2D(source);
    }

    public static Texture2D Dilate(Texture2D source, int radius)
    {
        if (radius <= 0 || Material == null)
        {
            return Blit2D(source);
        }

        int w = source.width, h = source.height;

        var rt0 = TempRT(w, h);
        var rt1 = TempRT(w, h);

        Graphics.Blit(source, rt0);

        Material.SetInt("_Radius", radius);

        Graphics.Blit(rt0, rt1, Material, PASS_DILATE_H);
        Graphics.Blit(rt1, rt0, Material, PASS_DILATE_V);

        Texture2D result = ReadBack(rt0, w, h);

        ReleaseRT(rt0);
        ReleaseRT(rt1);

        return result;
    }

    public static Texture2D Erode(Texture2D source, int radius)
    {
        if (radius <= 0 || Material == null)
        {
            return Blit2D(source);
        }

        Texture2D inverted = Invert(source);
        Texture2D dilated = Dilate(inverted, radius);
        Object.DestroyImmediate(inverted);
        Texture2D result = Invert(dilated);
        Object.DestroyImmediate(dilated);
        return result;
    }

    private static Texture2D Invert(Texture2D source)
    {
        int w = source.width, h = source.height;
        Color[] pixels = source.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(1f - pixels[i].r, 1f - pixels[i].g, 1f - pixels[i].b, pixels[i].a);
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static RenderTexture TempRT(int w, int h)
    {
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        rt.Create();
        return rt;
    }

    private static void ReleaseRT(RenderTexture rt)
    {
        if (rt == null) return;
        if (RenderTexture.active == rt) RenderTexture.active = null;
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    private static Texture2D ReadBack(RenderTexture rt, int w, int h)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        return tex;
    }

    private static Texture2D Blit2D(Texture2D source)
    {
        var tex = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        tex.SetPixels(source.GetPixels());
        tex.Apply();
        return tex;
    }
}