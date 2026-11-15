// A faceted gem seen through ray tracing: the light that enters at the front is refracted into the stone, bounces against the facets (total internal reflection when the angle is
// too shallow), and leaves where it can, toward a small studio of lights. The three colours are traced with slightly different refractive indices (dispersion), which is where the
// fire comes from. The stone is a convex polyhedron given as a list of planes (_Planes: xyz = outward normal, w = distance from the middle, in the object space of the mesh).
// The world is mirrored along z (the camera looks along +z and the viewer's side is -z); see RealGemRenderer.
Shader "GemLab/RealGem"
{
    Properties
    {
        _Body ("Body colour", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"
            #include "GemPatterns.cginc"

            float4 _Body;
            float _Ior;            // refractive index of the green light
            float _Dispersion;     // red is Ior - Dispersion, blue is Ior + Dispersion
            float _Density;        // how deep the colour of the stone is
            float _Milky;          // 0 = clear glass, 1 = a lit matte body (translucent or opaque stones)
            float _Exposure;
            float _Rough;          // 0 = polished, 1 = a dull, frosted surface (a rough crystal)
            float _Stria;          // strength of the striations of a crystal
            float _StriaMode;      // 1 = fine lines across the long axis (corundum, quartz), 2 = lines along it (tourmaline, topaz, beryl)
            float _Zoning;         // bands of colour along the long axis
            float _Speck;          // dark specks (mica, rutile) in and on the crystal
            float _Seed;
            float _Pattern;        // GemPattern number (see GemPatterns.cginc), 0 = none
            float _PatStrength;
            float4 _Accent;
            float _Metal;          // 1 = a polished metal (pyrite, hematite)
            float4 _MetalTint;
            float4 _Planes[256];
            int _PlaneCount;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 objPos : TEXCOORD0; float3 objNormal : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.objPos = v.vertex.xyz;
                o.objNormal = v.normal;
                return o;
            }

            // The studio, in world directions (z < 0 is toward the viewer): a dim room, a big soft window at the upper left, a smaller one at the lower right, a light overhead,
            // and a handful of small bright lamps that make the sparkles.
            static const float3 Spark0 = float3(-0.30, 0.55, -0.78);
            static const float3 Spark1 = float3(0.42, 0.30, -0.86);
            static const float3 Spark2 = float3(-0.62, -0.12, -0.78);
            static const float3 Spark3 = float3(0.10, -0.55, -0.83);
            static const float3 Spark4 = float3(0.70, 0.62, -0.35);
            static const float3 Spark5 = float3(-0.80, 0.30, -0.52);
            static const float3 Spark6 = float3(0.05, 0.92, -0.38);
            static const float3 Spark7 = float3(0.60, -0.55, -0.58);

            float Lamp (float3 d, float3 axis, float inner, float outer)
            {
                return smoothstep(inner, outer, dot(d, normalize(axis)));
            }

            float3 Env (float3 d)
            {
                float3 c = lerp(float3(0.07, 0.08, 0.11), float3(0.42, 0.45, 0.54), saturate(d.y * 0.5 + 0.55)) * 0.95;
                // A wide soft dome above and in front of the stone: it lights the facets seen face on, so the middle of the stone is not black.
                c += float3(0.95, 0.97, 1.0) * 0.55 * (1.0 - smoothstep(-0.9, -0.2, d.z));
                c += float3(1.0, 0.98, 0.94) * 2.4 * Lamp(d, float3(-0.50, 0.65, -0.60), 0.80, 0.93);
                c += float3(0.90, 0.95, 1.0) * 1.3 * Lamp(d, float3(0.70, -0.22, -0.70), 0.84, 0.94);
                c += float3(1.0, 1.0, 1.0) * 1.0 * Lamp(d, float3(0.0, 1.0, -0.15), 0.88, 0.96);
                float s = Lamp(d, Spark0, 0.985, 0.998) + Lamp(d, Spark1, 0.985, 0.998) + Lamp(d, Spark2, 0.986, 0.998) + Lamp(d, Spark3, 0.986, 0.998)
                        + Lamp(d, Spark4, 0.987, 0.998) + Lamp(d, Spark5, 0.987, 0.998) + Lamp(d, Spark6, 0.988, 0.998) + Lamp(d, Spark7, 0.988, 0.998);
                c += float3(1.0, 1.0, 1.0) * 8.0 * s;
                return c;
            }

            float Hash31 (float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise3 (float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash31(i), Hash31(i + float3(1, 0, 0)), f.x), lerp(Hash31(i + float3(0, 1, 0)), Hash31(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash31(i + float3(0, 0, 1)), Hash31(i + float3(1, 0, 1)), f.x), lerp(Hash31(i + float3(0, 1, 1)), Hash31(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            // The surface of a rough crystal: tilted a little everywhere by a fine noise (the frosting) and by the fine lines that cross the long axis (the striations).
            float3 Frost (float3 p, float3 n)
            {
                float3 q = p * 9.0 + _Seed;
                float3 g = float3(Noise3(q), Noise3(q + 17.1), Noise3(q + 43.7)) - 0.5;
                float3 tilt = float3(0.0, 0.0, 0.0);
                if (_StriaMode > 1.5)
                {
                    float l = cos(p.y * 58.0 + p.z * 41.0 + Noise3(p * 3.0 + _Seed) * 3.0) * 0.28 * _Stria;
                    tilt = float3(0.0, l * 0.8, l * 0.6);
                }
                else
                {
                    tilt = float3(cos(p.x * 62.0 + Noise3(p * 3.0 + _Seed) * 3.0) * 0.30 * _Stria, 0.0, 0.0);
                }
                return normalize(n + _Rough * 0.80 * g + tilt);
            }

            float3 ToWorld (float3 d)
            {
                return normalize(mul((float3x3)unity_ObjectToWorld, d));
            }

            // What one colour channel sees through the stone: from a point just inside the front facet, going along `dir`.
            float Trace (float3 p, float3 dir, float ior, float sigma, int channel)
            {
                float acc = 0.0;
                float weight = 1.0;
                [loop]
                for (int bounce = 0; bounce < 8; bounce++)
                {
                    float tMin = 1.0e9;
                    int hit = -1;
                    [loop]
                    for (int i = 0; i < 256; i++)
                    {
                        if (i >= _PlaneCount) break;
                        float4 pl = _Planes[i];
                        float den = dot(pl.xyz, dir);
                        if (den > 1.0e-5)
                        {
                            float t = (pl.w - dot(pl.xyz, p)) / den;
                            if (t > 1.0e-4 && t < tMin) { tMin = t; hit = i; }
                        }
                    }
                    if (hit < 0) break;

                    float3 mid = p + dir * (tMin * 0.5);
                    p += dir * tMin;
                    // Bands of colour along the long axis, and the odd dark speck inside.
                    float zone = max(0.1, 1.0 + _Zoning * (Noise3(float3(mid.x * 3.5, mid.y * 0.9, mid.z * 0.9) + _Seed) * 2.2 - 0.9));
                    weight *= exp(-sigma * tMin * zone);
                    if (_Speck > 0.01 && Noise3(mid * 6.5 + _Seed * 1.3) > 1.0 - 0.06 * _Speck) weight *= 0.3;
                    float3 n = _Planes[hit].xyz;
                    if (_Rough > 0.01) n = normalize(n + _Rough * 0.30 * (float3(Noise3(p * 6.0 + 1.7), Noise3(p * 6.0 + 9.1), Noise3(p * 6.0 + 4.3)) - 0.5));
                    float cosi = dot(dir, n);
                    float s2 = ior * ior * (1.0 - cosi * cosi);
                    if (s2 >= 1.0)
                    {
                        // Total internal reflection: the light stays in.
                        dir = dir - 2.0 * cosi * n;
                        p -= n * 1.0e-3;
                        continue;
                    }

                    float cost = sqrt(1.0 - s2);
                    float rs = (ior * cosi - cost) / (ior * cosi + cost);
                    float rp = (cosi - ior * cost) / (cosi + ior * cost);
                    float reflectance = 0.5 * (rs * rs + rp * rp);
                    float3 outDir = refract(dir, -n, ior);
                    float3 seen = Env(ToWorld(outDir));
                    acc += weight * (1.0 - reflectance) * (channel == 0 ? seen.r : channel == 1 ? seen.g : seen.b);

                    weight *= reflectance;
                    dir = dir - 2.0 * cosi * n;
                    p -= n * 1.0e-3;
                    if (weight < 0.004) break;
                }
                return acc;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.objNormal);
                float3 view = normalize(mul((float3x3)unity_WorldToObject, float3(0.0, 0.0, 1.0)));      // from the camera into the scene
                float facing = -dot(view, n);
                if (facing <= 0.0) discard;                                                             // the far side of the stone
                if (_Rough > 0.01)
                {
                    n = Frost(i.objPos, n);
                    if (dot(view, n) > -0.05) n = normalize(n - view * (dot(view, n) + 0.05));          // never turned away from the viewer
                }

                float cosi = saturate(facing);
                float f0 = pow((_Ior - 1.0) / (_Ior + 1.0), 2.0);
                float fresnel = f0 + (1.0 - f0) * pow(1.0 - cosi, 5.0);

                // The light mirrored off the front facet.
                float3 mirrored = Env(ToWorld(reflect(view, n)));

                // The light that goes in, one colour at a time (the dispersion).
                // The colour of the body at this point (the pattern of the species), which also tints the light that goes through.
                float3 extra;
                float3 albedo = PatternAlbedo((int)(_Pattern + 0.5), i.objPos, _Body.rgb, _Accent.rgb, _PatStrength, _Seed, n, view, extra);
                float3 body = max(albedo, 0.03);
                float3 sigma = -log(body) * _Density;
                float3 start = i.objPos;
                float3 glass;
                glass.r = Trace(start, refract(view, n, 1.0 / (_Ior - _Dispersion)), _Ior - _Dispersion, sigma.r, 0);
                glass.g = Trace(start, refract(view, n, 1.0 / _Ior), _Ior, sigma.g, 1);
                glass.b = Trace(start, refract(view, n, 1.0 / (_Ior + _Dispersion)), _Ior + _Dispersion, sigma.b, 2);
                float3 clear = glass * (1.0 - fresnel) + mirrored * fresnel * (1.0 - 0.85 * _Rough);
                if (_Rough > 0.01)
                {
                    // A frosted surface scatters the light: a veil of the colour of the stone, and dark specks on the surface.
                    clear = lerp(clear, _Body.rgb * (0.45 + 0.55 * saturate(-dot(view, n))) * 1.5, 0.55 * _Rough);
                    if (_Speck > 0.01 && Noise3(start * 12.0 + _Seed * 2.1) > 1.0 - 0.05 * _Speck) clear *= 0.18;
                }

                // A translucent or opaque stone is mostly its own colour, lit: the glass is only a layer on top. The pattern of the species lives in that body colour.
                float3 nw = ToWorld(n);
                float diffuse = 0.28 + 0.72 * saturate(dot(nw, normalize(float3(-0.5, 0.65, -0.6))));
                float3 matte = albedo * diffuse * 1.6 + mirrored * fresnel * 0.6 + extra;
                float3 col = lerp(clear, matte, _Milky);
                if (_Pattern > 0.5) col += extra * (1.0 - _Milky) * 0.6;

                // A polished metal: nothing but the studio mirrored in its facets, tinted, brighter at grazing angles.
                if (_Metal > 0.5)
                {
                    float3 tint = _MetalTint.rgb;
                    float3 f = tint + (1.0 - tint) * pow(1.0 - cosi, 5.0);
                    col = f * EnvMetal(ToWorld(reflect(view, n))) * 1.15 + tint * 0.02;
                }

                col = 1.0 - exp(-col * _Exposure);
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}

// 2026-11-15: patterns up to 22 (snowflake obsidian); this line only forces Unity to recompile the shader.
