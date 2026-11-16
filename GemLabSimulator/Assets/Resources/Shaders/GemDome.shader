// A polished dome (cabochon). A clear stone is seen through: the light goes in through the dome, crosses the stone down to the flat base, is mirrored there and comes back out, so the
// colour deepens where the stone is thick, and the inclusions of the volume (see GemInclusions.cginc) are met on the way, in front and behind each other. A milky or opaque stone is
// mostly its own colour, lit. The optical effects of a cabochon are worked out from the light and not painted: the star of an asterism (four or six rays, a bright heart, a halo)
// and the line of a cat's eye; both sit where the light is mirrored, so they move across the dome when the stone is tilted. The silk also shows as zones of denser silk with the
// pseudo-hexagonal outline of the rhombohedron, and as fine streaks along the needles; those turn with the stone. World mirrored along z, as in GemRealistic (the viewer is on
// the -z side, the dome faces -z and the flat base is at _BaseZ).
Shader "GemLab/RealDome"
{
    Properties { _Body ("Body colour", Color) = (1, 0.2, 0.2, 1) }
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
            float _Milky;          // 0 = clear, 1 = very milky (dense silk)
            float _Exposure;
            float _StarRays;       // 0, 4 or 6
            float _StarStrength;
            float _Silk;           // 0..1: density of the silk (a soft sheen, a sharper star, zones and streaks)
            float _StarWidth;      // width of the rays of the star
            float _CatsEye;        // 0 or 1
            float _EyeAxis;        // angle of the fibres in the base plane, radians
            float _Ior;
            float _Seed;
            float _Pattern;
            float _PatStrength;
            float4 _Accent;
            float _Metal;
            float4 _MetalTint;
            float _BaseZ;          // where the flat base of the stone is, in the space of the stone
            float _Scatter;        // how much the body scatters the light (haze and silk), per unit of the stone
            float _QZone;          // growth zoning
            float4 _QAxis;         // w = how many zones across the stone

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

            float Lamp (float3 d, float3 axis, float inner, float outer) { return smoothstep(inner, outer, dot(d, normalize(axis))); }

            // The studio, softer than for a faceted stone: a dome shows big windows, not pinpoint sparks.
            float3 Env (float3 d)
            {
                float3 c = lerp(float3(0.07, 0.08, 0.11), float3(0.42, 0.45, 0.54), saturate(d.y * 0.5 + 0.55)) * 0.9;
                c += float3(1.0, 0.98, 0.94) * 2.2 * Lamp(d, float3(-0.50, 0.65, -0.60), 0.86, 0.96);
                c += float3(0.90, 0.95, 1.0) * 1.0 * Lamp(d, float3(0.70, -0.22, -0.70), 0.88, 0.96);
                c += float3(1.0, 1.0, 1.0) * 0.8 * Lamp(d, float3(0.0, 1.0, -0.15), 0.90, 0.97);
                return c;
            }

            float3 ToWorld (float3 d) { return normalize(mul((float3x3)unity_ObjectToWorld, d)); }
            float3 ToObject (float3 d) { return normalize(mul((float3x3)unity_WorldToObject, d)); }
            float Noise3 (float3 x) { return PNoise(x); }

            // The inclusions (shared with the faceted stones).
            #include "GemInclusions.cginc"

            // One ray of the star or the eye: a thin line through the point where the light is mirrored, along the direction `axis` (in the plane of the base of the stone),
            // seen in the space of the normals; it fades toward the edge of the dome.
            float Line (float2 n, float2 centre, float2 axis, float width)
            {
                float2 d = n - centre;
                float across = abs(d.x * axis.y - d.y * axis.x);
                float along = abs(dot(d, axis));
                float tail = saturate(1.0 - along / 1.25);
                return exp(-(across * across) / (width * width)) * tail * tail;
            }

            // The zones of denser silk: the rhombohedral growth of corundum gives pseudo-hexagonal outlines (straight edges in three directions, 60 degrees apart), seen as
            // flat bands of more and of less silk, one inside the other. 0..1.
            float HexBand (float2 p)
            {
                float2 a0 = float2(cos(_EyeAxis), sin(_EyeAxis));
                float2 a1 = float2(cos(_EyeAxis + 1.0472), sin(_EyeAxis + 1.0472));
                float2 a2 = float2(cos(_EyeAxis + 2.0944), sin(_EyeAxis + 2.0944));
                float hd = max(abs(dot(p, a0)), max(abs(dot(p, a1)), abs(dot(p, a2))));
                float freq = (_QAxis.w > 0.5 ? _QAxis.w : 3.0) * 0.9;
                float band = frac(hd * freq + PNoise(float3(p * 2.2, _Seed)) * 0.18 + _Seed);
                return smoothstep(0.30, 0.38, band) - smoothstep(0.70, 0.78, band);
            }

            // The fine streaks that the bundles of needles draw along their three directions.
            float Streaks (float2 p)
            {
                float s = 0.0;
                [unroll]
                for (int k = 0; k < 3; k++)
                {
                    float a = _EyeAxis + 1.5708 + k * 1.0472;
                    float2 along = float2(cos(a), sin(a));
                    float across = dot(p, float2(-along.y, along.x));
                    float stripe = sin(across * 46.0 + PNoise(float3(p * 3.0, k * 3.1 + _Seed)) * 5.0) * 0.5 + 0.5;
                    s += smoothstep(0.86, 1.0, stripe) * PNoise(float3(p * 5.0 + k * 7.0, _Seed + 4.0));
                }
                return s * 0.33;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.objNormal);
                float3 view = ToObject(float3(0.0, 0.0, 1.0));              // from the camera into the scene
                float facing = -dot(view, n);
                if (facing <= 0.0) discard;
                float3 nw = ToWorld(n);

                float cosi = saturate(facing);
                float f0 = pow((_Ior - 1.0) / (_Ior + 1.0), 2.0);
                float fresnel = f0 + (1.0 - f0) * pow(1.0 - cosi, 5.0);

                // The body: lit by the studio with a wrap, deeper in the middle, lighter and more saturated toward the rim where the light goes through less stone.
                float3 key = normalize(float3(-0.50, 0.65, -0.60));
                float diffuse = saturate(dot(nw, key) * 0.5 + 0.5);
                float3 body = max(_Body.rgb, 0.02);
                float3 extra;
                body = max(PatternAlbedo((int)(_Pattern + 0.5), i.objPos, body, _Accent.rgb, _PatStrength, _Seed, n, view, extra), 0.02);
                float hexA = HexBand(i.objPos.xy);
                float3 deep = body * body;
                float3 lit = lerp(deep, body, 0.35 + 0.65 * diffuse);
                lit = lit * (0.55 + 0.95 * diffuse);
                // Light scattered inside: a glow of the body colour, stronger when the stone is milky (the silk).
                lit += body * (0.10 + 0.38 * _Milky) * (0.4 + 0.6 * cosi);
                lit = lerp(lit, lit + lerp(float3(0.18, 0.17, 0.17), body * 0.5, 0.6) * _Milky, 0.5);
                lit *= 1.0 + _QZone * 0.35 * (hexA - 0.5);

                // The light that goes in: through the dome down to the flat base, mirrored there, back up and out. The colour deepens with the thickness; the inclusions are met
                // on both legs.
                float3 dIn = refract(view, n, 1.0 / _Ior);
                if (dot(dIn, dIn) < 0.01) dIn = view;
                dIn = normalize(dIn);
                float tPath = clamp((_BaseZ - i.objPos.z) / max(dIn.z, 0.25), 0.03, 3.0);
                float3 pBase = i.objPos + dIn * tPath;
                float3 dUp = float3(dIn.x, dIn.y, -dIn.z);
                float baseR = 0.45 + 0.45 * pow(saturate(1.0 - dIn.z), 2.0);
                float3 dOut = refract(dUp, -n, _Ior);
                dOut = dot(dOut, dOut) > 0.01 ? normalize(dOut) : reflect(dUp, -n);
                float3 seen = Env(ToWorld(dOut));
                float3 sigma = -log(max(body, 0.02)) * 0.30;
                float zoneAvg = max(0.15, 1.0 + _QZone * 1.4 * (hexA - 0.5));
                float3 scatterTint = lerp(body, float3(1.0, 1.0, 1.0), 0.12);
                float3 tcol = float3(0.0, 0.0, 0.0);
                [unroll]
                for (int ch = 0; ch < 3; ch++)
                {
                    float w = exp(-Chan(sigma, ch) * tPath * zoneAvg);
                    float acc = 0.0;
                    if (_IncCount > 0) acc += Inclusions(i.objPos, dIn, tPath, ch, w);
                    if (_Scatter > 0.01)
                    {
                        float tr = exp(-_Scatter * tPath);
                        acc += w * (1.0 - tr) * Chan(scatterTint, ch) * (0.22 + 0.32 * EnvChan(dIn, ch));
                        w *= tr;
                    }
                    w *= baseR;
                    w *= exp(-Chan(sigma, ch) * tPath * zoneAvg);
                    if (_IncCount > 0) acc += Inclusions(pBase, dUp, tPath, ch, w);
                    if (_Scatter > 0.01)
                    {
                        float tr2 = exp(-_Scatter * tPath);
                        acc += w * (1.0 - tr2) * Chan(scatterTint, ch) * (0.22 + 0.32 * EnvChan(dUp, ch));
                        w *= tr2;
                    }
                    acc += w * Chan(seen, ch);
                    tcol[ch] = acc;
                }

                // A clear stone is mostly what the light does inside it; a milky or an opaque one is mostly its own colour, lit.
                float mixOpaque = saturate(_Milky * (_Scatter > 0.05 ? 0.6 : 1.0));
                float3 inner = lerp(tcol * 1.9 + lit * 0.30, lit, mixOpaque);

                // The studio mirrored in the surface.
                float3 mirrored = Env(ToWorld(reflect(view, n)));
                float3 col = inner * (1.0 - fresnel) + mirrored * fresnel + extra;

                // The highlight: where the normal points half-way between the viewer and the key light.
                float3 halfVec = normalize(key - float3(0.0, 0.0, 1.0));
                float spec = pow(saturate(dot(nw, halfVec)), 120.0);
                col += float3(1.0, 0.98, 0.95) * spec * 1.4;

                // The star and the eye. The point where the light is mirrored, in object space, is where the normal equals the half vector.
                float3 halfObj = ToObject(halfVec);
                float2 centre = halfObj.xy;
                float2 nn = n.xy;
                float2 dc = nn - centre;
                float rayWidth = _StarWidth > 0.001 ? _StarWidth : 0.055;
                if (_StarRays > 3.5)
                {
                    float starLight = 0.0;
                    float lines = _StarRays * 0.5;                          // 2 lines for 4 rays, 3 for 6
                    float stepAngle = 3.14159265 / lines;
                    [loop]
                    for (int k = 0; k < 3; k++)
                    {
                        if (k >= (int)(lines + 0.5)) break;
                        float a = _EyeAxis + k * stepAngle;
                        starLight += Line(nn, centre, float2(cos(a), sin(a)), rayWidth);
                    }
                    // A bright, sharp heart where the rays cross, and a wide soft halo of the colour of the stone around it.
                    float core = exp(-dot(dc, dc) / 0.0035) * 2.2;
                    float halo = exp(-dot(dc, dc) / 0.035) * 0.45;
                    starLight = starLight * 0.85 + core;
                    col += lerp(float3(1.0, 0.96, 0.94), body * 1.8 + 0.25, 0.45) * starLight * _StarStrength * (0.55 + 0.45 * cosi);
                    col += body * 1.6 * halo * _StarStrength;
                }
                if (_CatsEye > 0.5)
                {
                    float a = _EyeAxis + 1.5707963;                         // the line is across the fibres
                    float eye = Line(nn, centre, float2(cos(a), sin(a)), 0.045);
                    col += lerp(float3(1.0, 0.97, 0.88), body * 1.6 + 0.3, 0.25) * eye * (0.85 + 0.5 * _Silk);
                }
                if (_Silk > 0.02)
                {
                    // The silk gives the dome a soft sheen around the point where the light is mirrored, denser in some of the hexagonal zones, and fine streaks along the needles.
                    float soft = exp(-dot(dc, dc) / 0.07);
                    col += float3(1.0, 0.97, 0.93) * _Silk * 0.34 * soft * (0.55 + 0.9 * hexA) * (0.45 + 0.55 * cosi);
                    col += lerp(float3(1.0, 0.92, 0.90), body * 1.8 + 0.2, 0.5) * _Silk * 0.12 * Streaks(i.objPos.xy) * (0.5 + 0.5 * cosi);
                }

                // A touch of darkening at the very rim, where the dome turns away.
                col *= 0.72 + 0.28 * sqrt(cosi);

                // A polished metal (pyrite, hematite): a chrome ball. The studio is mirrored in the dome, tinted, whiter at grazing angles.
                if (_Metal > 0.5)
                {
                    float3 tint = _MetalTint.rgb;
                    float3 f = tint + (1.0 - tint) * pow(1.0 - cosi, 5.0);
                    col = f * EnvMetal(ToWorld(reflect(view, n))) * 1.2 + tint * 0.02;
                }
                col = 1.0 - exp(-col * _Exposure);
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}

// 2026-11-16: cabochons seen through the stone (GemInclusions.cginc shared with GemRealistic).
