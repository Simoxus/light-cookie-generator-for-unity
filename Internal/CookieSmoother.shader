Shader "Hidden/CookieSmoother"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_TexelSize;
        int       _Radius;
        float     _KawaseOffset;

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f    { float4 pos : SV_POSITION;  float2 uv : TEXCOORD0; };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv  = v.uv;
            return o;
        }
        ENDCG

        // dilation (horizontal)
        Pass
        {
            Name "DilateH"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half best = 1.0h;
                for (int x = -_Radius; x <= _Radius; x++)
                    best = min(best, tex2D(_MainTex, i.uv + float2(x * _MainTex_TexelSize.x, 0)).r);
                return half4(best, best, best, 1);
            }
            ENDCG
        }

        // Dilation (vertical)
        Pass
        {
            Name "DilateV"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half best = 1.0h;
                for (int y = -_Radius; y <= _Radius; y++)
                    best = min(best, tex2D(_MainTex, i.uv + float2(0, y * _MainTex_TexelSize.y)).r);
                return half4(best, best, best, 1);
            }
            ENDCG
        }

        // Gaussian (horizontal)
        Pass
        {
            Name "GaussianH"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half sum = 0, total = 0;
                float sigma = max(1.0, _Radius * 0.5);
                for (int x = -_Radius; x <= _Radius; x++)
                {
                    half w = (half)exp(-0.5 * (x * x) / (sigma * sigma));
                    sum   += tex2D(_MainTex, i.uv + float2(x * _MainTex_TexelSize.x, 0)).r * w;
                    total += w;
                }
                half r = sum / total;
                return half4(r, r, r, 1);
            }
            ENDCG
        }

        // Gaussian (vertical)
        Pass
        {
            Name "GaussianV"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                half sum = 0, total = 0;
                float sigma = max(1.0, _Radius * 0.5);
                for (int y = -_Radius; y <= _Radius; y++)
                {
                    half w = (half)exp(-0.5 * (y * y) / (sigma * sigma));
                    sum   += tex2D(_MainTex, i.uv + float2(0, y * _MainTex_TexelSize.y)).r * w;
                    total += w;
                }
                half r = sum / total;
                return half4(r, r, r, 1);
            }
            ENDCG
        }

        // Kawase
        Pass
        {
            Name "Kawase"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float2 o = (_KawaseOffset + 0.5) * _MainTex_TexelSize.xy;
                half r =
                    tex2D(_MainTex, i.uv + float2( o.x,  o.y)).r +
                    tex2D(_MainTex, i.uv + float2(-o.x,  o.y)).r +
                    tex2D(_MainTex, i.uv + float2( o.x, -o.y)).r +
                    tex2D(_MainTex, i.uv + float2(-o.x, -o.y)).r;
                return half4(r * 0.25, r * 0.25, r * 0.25, 1);
            }
            ENDCG
        }

        // Spiral
        Pass
        {
            Name "Spiral"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 delta  = i.uv - center;
                float  dist   = length(delta);
                float  angle  = atan2(delta.y, delta.x);

                float  twist  = (_KawaseOffset + 1.0) * 0.3;
                int    steps  = max(4, _Radius * 2);
                half   sum    = 0;

                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / (steps - 1) - 0.5;
                    float a = angle + t * twist * (1.0 + dist * 4.0);
                    float2 sampleUV = center + dist * float2(cos(a), sin(a));
                    sum += tex2D(_MainTex, sampleUV).r;
                }

                half r = sum / steps;
                return half4(r, r, r, 1);
            }
            ENDCG
        }
    }
}