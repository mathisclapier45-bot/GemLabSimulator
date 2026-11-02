// Full-screen lighting of the outdoor scenes: the picture is multiplied by the colour of the hour (sunrise, noon, sunset, night), then
// the lamps, the windows and the lantern of the player add warm pools of light on top of it.
Shader "GemLab/DayNightLighting"
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

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Ambient;          // rgb: colour multiplied with the picture
            float4 _Grade;            // x: saturation, y: vignette strength, z: aspect ratio (width / height), w: number of lights
            float4 _Lights[32];       // xy: position in the picture (0..1), z: radius in screen heights, w: intensity
            float4 _LightColors[32];

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 rgb = col.rgb;

                float luma = dot(rgb, float3(0.299, 0.587, 0.114));
                rgb = lerp(float3(luma, luma, luma), rgb, _Grade.x);

                float3 light = _Ambient.rgb;
                int count = (int)_Grade.w;
                for (int k = 0; k < 32; k++)
                {
                    if (k >= count) break;
                    float2 d = (i.uv - _Lights[k].xy) * float2(_Grade.z, 1.0);
                    float f = saturate(1.0 - length(d) / _Lights[k].z);
                    f = f * f;
                    light += _LightColors[k].rgb * (f * _Lights[k].w);
                }

                float2 c = i.uv - 0.5;
                float vignette = saturate(dot(c, c) * 2.2);
                light *= 1.0 - vignette * _Grade.y;

                rgb *= light;
                return fixed4(rgb, col.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
