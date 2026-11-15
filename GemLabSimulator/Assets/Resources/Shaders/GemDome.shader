// A polished dome (cabochon) of a translucent stone: its colour lit from inside and out, the studio mirrored in its surface, and the optical effects of a cabochon, which are
// worked out from the light and not painted: the star of an asterism (four or six rays) and the line of a cat's eye. Both sit where the light is mirrored, so they move across
// the dome when the stone is tilted. World mirrored along z, as in GemRealistic (the viewer is on the -z side).
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "GemPatterns.cginc"

            float4 _Body;
            float _Milky;          // 0 = clear, 1 = very milky (dense silk)
            float _Exposure;
            float _StarRays;       // 0, 4 or 6
            float _StarStrength;
            float _CatsEye;        // 0 or 1
            float _EyeAxis;        // angle of the fibres in the base plane, radians
            float _Ior;
            float _Seed;
            float _Pattern;
            float _PatStrength;
            float4 _Accent;
            float _Metal;
            float4 _MetalTint;

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

            // One ray of the star or the eye: a thin line through the point where the light is mirrored, along the direction `axis` (in the plane of the base of the stone),
            // seen in the space of the normals; it fades toward the edge of the dome.
            float Line (float2 n, float2 centre, float2 axis, float width)
            {
                float2 d = n - centre;
                float across = abs(d.x * axis.y - d.y * axis.x);
                float along = abs(dot(d, axis));
                float tail = saturate(1.0 - along / 0.95);
                return exp(-(across * across) / (width * width)) * tail * tail;
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
                float3 deep = body * body;
                float3 lit = lerp(deep, body, 0.35 + 0.65 * diffuse);
                lit = lit * (0.55 + 0.95 * diffuse);
                // Light scattered inside: a glow of the body colour, stronger when the stone is milky (the silk).
                lit += body * (0.10 + 0.38 * _Milky) * (0.4 + 0.6 * cosi);
                lit = lerp(lit, lit + float3(0.18, 0.17, 0.17) * _Milky, 0.5);

                // The studio mirrored in the surface.
                float3 mirrored = Env(ToWorld(reflect(view, n)));
                float3 col = lit * (1.0 - fresnel) + mirrored * fresnel + extra;

                // The highlight: where the normal points half-way between the viewer and the key light.
                float3 halfVec = normalize(key - float3(0.0, 0.0, 1.0));
                float spec = pow(saturate(dot(nw, halfVec)), 120.0);
                col += float3(1.0, 0.98, 0.95) * spec * 1.4;

                // The star and the eye. The point where the light is mirrored, in object space, is where the normal equals the half vector.
                float3 halfObj = ToObject(halfVec);
                float2 centre = halfObj.xy;
                float2 nn = n.xy;
                float starLight = 0.0;
                if (_StarRays > 3.5)
                {
                    float lines = _StarRays * 0.5;                          // 2 lines for 4 rays, 3 for 6
                    float stepAngle = 3.14159265 / lines;
                    [loop]
                    for (int k = 0; k < 3; k++)
                    {
                        if (k >= (int)(lines + 0.5)) break;
                        float a = _EyeAxis + k * stepAngle;
                        starLight += Line(nn, centre, float2(cos(a), sin(a)), 0.055);
                    }
                    float glow = exp(-dot(nn - centre, nn - centre) / 0.012);
                    starLight = starLight * 0.8 + glow * 1.2;
                    col += lerp(float3(1.0, 0.96, 0.92), body * 1.8 + 0.25, 0.30) * starLight * _StarStrength * (0.55 + 0.45 * cosi);
                }
                if (_CatsEye > 0.5)
                {
                    float a = _EyeAxis + 1.5707963;                         // the line is across the fibres
                    float eye = Line(nn, centre, float2(cos(a), sin(a)), 0.045);
                    col += lerp(float3(1.0, 0.97, 0.88), body * 1.6 + 0.3, 0.25) * eye * 1.15;
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

// 2026-11-15: patterns up to 22 (snowflake obsidian); this line only forces Unity to recompile the shader.
