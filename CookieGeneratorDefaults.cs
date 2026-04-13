using UnityEngine;

public static class CookieGeneratorDefaults
{
    // Editor
    public const bool SHOW_STUPID_STUFF = true; // don't do it :(

    // Naming
    public const bool AUTOMATIC_NAMING = true;
    public const bool USE_PARENT_NAME = true;
    public const int PARENT_LEVEL = 3;
    public const bool PUT_COOKIES_IN_PARENT_FOLDER = true;
    public const string NAME_SUFFIX = "";

    // Render
    public const float SHADOW_OPACITY = 1f;
    public const float COOKIE_BRIGHTNESS = 0f;

    // Blur
    public const CookieTextureProcessor.SmoothingMethod BLUR_METHOD = CookieTextureProcessor.SmoothingMethod.Gaussian;
    public const int BLUR_RADIUS = 3;
    public const int BLUR_ITERATIONS = 1;

    // Camera
    public static readonly Vector3 ROTATION_OFFSET = Vector3.zero;
    public const float SPOT_NEAR_CLIP = 0.1f;
    public const float SPOT_FAR_CLIP = 10f;
    public const float ORTHOGRAPHIC_SIZE = 10f;

    // Output
    public const string SAVE_PATH = "Assets/_Project/Art/Cookies";
    public const string BASE_NAME = "cookiesyum";
    public const int RESOLUTION = 512;

    // This layer is used as a render layer during baking
    public const int OCCLUDER_LAYER = 31;
}