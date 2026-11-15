// What a species shows in the body of its stone, besides its colour: bands, veins, flecks, a sheen, the play of colour... and the chrome of the metallic ones. Worked out in 3D
// from the position inside the stone (so the pattern turns with the stone) and from the angle of view (for the effects that move: opal, labradorite, moonstone, pearl).
// The pattern numbers are those of GemPattern in GemLook.cs.
#ifndef GEM_PATTERNS_INCLUDED
#define GEM_PATTERNS_INCLUDED

float PHash (float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float PNoise (float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(PHash(i), PHash(i + float3(1, 0, 0)), f.x), lerp(PHash(i + float3(0, 1, 0)), PHash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(PHash(i + float3(0, 0, 1)), PHash(i + float3(1, 0, 1)), f.x), lerp(PHash(i + float3(0, 1, 1)), PHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

float PFbm (float3 x)
{
    return 0.5 * PNoise(x) + 0.3 * PNoise(x * 2.1 + 7.3) + 0.2 * PNoise(x * 4.3 + 3.1);
}

float3 PHue (float h)
{
    float3 k = abs(frac(float3(h, h + 0.6667, h + 0.3333)) * 6.0 - 3.0) - 1.0;
    return saturate(k);
}

// The colour of the body at point p (about -1 .. 1), seen along `v` (from the camera into the stone) where the surface normal is `n` (facing the camera). `extra` is light added on
// top, for the effects that shine by themselves.
float3 PatternAlbedo (int pat, float3 p, float3 c, float3 acc, float s, float seed, float3 n, float3 v, out float3 extra)
{
    extra = float3(0, 0, 0);
    float3 q = p * 2.2 + seed;
    float cosv = saturate(-dot(n, v));
    float3 r = c;

    if (pat == 1)            // bands that follow the outline, uneven
    {
        float w = PFbm(q * 1.3) * 0.6;
        float t = frac((length(p * float3(1.0, 1.15, 0.85)) + w * 0.35) * 4.4);
        r = t < 0.5 ? c * (1.0 - 0.22 * s) : c + (1.0 - c) * 0.22 * s;
    }
    else if (pat == 2)       // pale patches and grains of gold pyrite
    {
        if (PNoise(q * 1.6) > 0.68) r = lerp(c, float3(0.78, 0.82, 0.92), 0.40 * s);
        float g = PHash(floor(p * 34.0) + seed);
        if (g > 0.955) { r = float3(1.0, 0.84, 0.28); extra = float3(0.5, 0.40, 0.10) * 0.7; }
    }
    else if (pat == 3)       // a network of thin veins
    {
        float a = PNoise(q * 1.5);
        float b = PNoise(q * 3.1 + 9.0);
        float vein = min(abs(a - 0.5), abs(b - 0.5) * 1.4);
        r = lerp(c, acc, (1.0 - smoothstep(0.012, 0.045, vein)) * 0.85 * s);
    }
    else if (pat == 4)       // soft blotches
    {
        float b = PFbm(q * 1.8) - 0.5;
        r = b > 0.0 ? c + (1.0 - c) * b * 0.9 * s : c * (1.0 + b * 0.9 * s);
    }
    else if (pat == 5)       // fibres (tiger's eye)
    {
        float fibre = sin(p.y * 34.0 + PNoise(q) * 4.0) * 0.5 + 0.5;
        r = lerp(c * 0.55, lerp(c, acc, 0.6), fibre);
    }
    else if (pat == 6)       // adularescence: a blue-white sheen that floats when the stone turns
    {
        float flow = PFbm(p * 2.0 + seed + dot(v, n) * 1.7);
        extra = acc * s * (0.20 + 0.8 * flow) * pow(1.0 - cosv, 1.4) * 1.8;
        r = lerp(c, float3(0.86, 0.88, 0.92), 0.35);
    }
    else if (pat == 7)       // opal: patches of spectral colour whose colours change with the angle
    {
        float3 cell = floor(p * 5.0 + seed);
        float hue = frac(PHash(cell) + cosv * 1.3 + p.x * 0.3);
        float mask = smoothstep(0.52, 0.72, PFbm(p * 3.0 + seed));
        extra = PHue(hue) * mask * s * 0.85;
        r = lerp(c, float3(0.9, 0.92, 0.95), 0.45);
    }
    else if (pat == 8)       // labradorite: a flash of blue and gold that appears at one angle
    {
        float f = pow(saturate(dot(-v, normalize(float3(0.30, 0.35, 0.88)))), 12.0);
        float mask = smoothstep(0.30, 0.80, PFbm(p * 2.4 + seed));
        float3 tint = lerp(acc, float3(0.95, 0.78, 0.30), saturate(p.x * 0.8 + 0.4));
        extra = tint * f * mask * s * 1.7;
        r = c * 0.8;
    }
    else if (pat == 9)       // pearl: a pink overtone on one side, a green one on the other, stronger at grazing angles
    {
        float3 overtone = lerp(acc, float3(0.80, 0.95, 0.90), saturate(0.5 + (n.x - n.y) * 0.6));
        extra = overtone * pow(1.0 - cosv, 1.2) * 0.55 * s;
        r = c;
    }
    else if (pat == 12)      // tortoiseshell
    {
        float b = PNoise(q * 1.5 + 2.0);
        if (b > 0.58) r = lerp(c, acc, 0.85 * s);
        else if (b > 0.50) r = lerp(c, acc, 0.35 * s);
    }
    else if (pat == 13)      // amber: a warm glow from inside, a few dark specks
    {
        extra = acc * exp(-dot(p, p) * 0.7) * 0.40 * s;
        if (PHash(floor(p * 30.0) + seed) > 0.985) r = c * 0.5;
    }
    else if (pat == 14)      // fine growth lines, crossing
    {
        float a = sin((p.x + p.y) * 22.0 + PNoise(q) * 3.0);
        float b = sin((p.x - p.y) * 17.0);
        r = a > 0.55 ? c * (1.0 - 0.10 * s) : c;
        if (b > 0.85) r *= 1.0 - 0.06 * s;
    }
    else if (pat == 15)      // vertical streaks
    {
        float streak = sin(p.x * 15.0 + PNoise(float3(p.x * 1.1, p.y * 0.7, p.z) + seed) * 2.2) * 0.5 + 0.5;
        r = lerp(c, acc, saturate((streak - 0.62) * 2.4) * 0.55 * s);
    }
    else if (pat == 16)      // two colours side by side
    {
        float side = saturate((p.x - p.y * 0.5) * 2.5 + 0.5);
        r = lerp(c, acc, side * s);
    }
    else if (pat == 17)      // colour change: one colour from one angle, another from the other
    {
        float side = smoothstep(0.25, 0.75, saturate(0.5 + dot(n, v) * -0.5 + p.x * 0.35));
        r = lerp(c, acc, side * s);
    }
    else if (pat == 18)      // a gradient of colour along the stone
    {
        float side = saturate(p.x * 0.8 + 0.5);
        r = lerp(c, acc, side * s);
    }
    else if (pat == 19)      // pale streaks along the cleavage planes
    {
        float plane = abs(sin((p.x * 0.8 + p.y * 0.6) * 9.0 + PNoise(q) * 2.0));
        if (plane < 0.10) r = lerp(c, float3(1, 1, 1), 0.40 * s);
    }
    else if (pat == 21)      // horsetail: golden fibres of chrysotile fanning out from a point, the mark of a demantoid
    {
        float3 d = p - float3(0.15, -0.25, 0.1);
        float ang = atan2(d.y, d.x) + sin(d.z * 3.0) * 0.3;
        float rad = length(d.xy);
        float fibre = pow(abs(sin(ang * 14.0 + PNoise(q) * 2.0)), 6.0);
        float mask = smoothstep(0.75, 0.10, rad) * smoothstep(0.0, 0.12, rad);
        r = lerp(c, acc, saturate(fibre * mask * 1.3) * s);
    }
    else if (pat == 22)      // snowflake obsidian: white and gray spherulites of cristobalite, with feathery radial edges, in black glass
    {
        float3 g = p * 5.0 + seed;
        float3 ig = floor(g);
        float best = 9.0;
        float3 rel = float3(0, 0, 0);
        [loop]
        for (int dz = -1; dz <= 1; dz++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    float3 cell = ig + float3(dx, dy, dz);
                    float3 jitter = float3(PHash(cell), PHash(cell + 7.3), PHash(cell + 13.1));
                    float3 centre = cell + 0.2 + 0.6 * jitter;
                    float size = 0.24 + 0.18 * PHash(cell + 3.7);
                    float present = PHash(cell + 21.7) < 0.6 ? 1.0 : 0.0;          // only some of the cells hold a snowflake: most of the stone stays black
                    float score = present > 0.5 ? length(g - centre) / size : 9.0;
                    if (score < best) { best = score; rel = g - centre; }
                }
            }
        }
        float ang = atan2(rel.y, rel.x);
        float feather = 0.5 + 0.5 * sin(ang * 9.0 + best * 6.0);
        float edge = best + (feather - 0.5) * 0.4;
        float inside = 1.0 - smoothstep(0.55, 0.95, edge);
        float3 flake = lerp(float3(0.52, 0.54, 0.60), float3(0.90, 0.92, 0.95), feather);
        r = lerp(c, flake, inside * s);
    }
    else if (pat == 20)      // bubbles in glass
    {
        float3 cell = floor(p * 7.0);
        float3 centre = (float3(PHash(cell + seed), PHash(cell + seed + 3.1), PHash(cell + seed + 7.7)) - 0.5) * 0.4 + 0.5;
        float d = length(frac(p * 7.0) - centre);
        if (PHash(cell + 11.0 + seed) > 0.80 && d < 0.14) { r = c + (1.0 - c) * 0.5; extra = float3(0.25, 0.25, 0.25); }
    }
    return r;
}

// The studio of a chrome ball: a bright sky over a sharp horizon, a dark brown floor, a bright line at the horizon, two softboxes and a black strip for contrast.
float PLamp (float3 d, float3 axis, float inner, float outer)
{
    return smoothstep(inner, outer, dot(d, normalize(axis)));
}

float3 EnvMetal (float3 d)
{
    // Panels around the room, alternately bright and dark: on a curved or faceted mirror they make the hard bands of a chrome surface.
    float az = atan2(d.x, -d.z);
    float panel = smoothstep(0.30, 0.55, abs(frac(az * 0.9549 + 0.20) - 0.5) * 2.0);
    float3 sky = lerp(float3(0.42, 0.50, 0.68), float3(1.15, 1.18, 1.25), saturate(d.y * 1.3)) * (0.25 + 1.15 * panel);
    float3 ground = lerp(float3(0.20, 0.14, 0.10), float3(0.01, 0.01, 0.015), saturate(-d.y * 1.6)) * (0.45 + 0.9 * panel);
    float3 c = lerp(ground, sky, smoothstep(-0.025, 0.025, d.y));
    c += float3(1.0, 0.92, 0.75) * 1.4 * exp(-(d.y * d.y) / 0.0010);
    c += float3(1.0, 0.98, 0.95) * 3.2 * PLamp(d, float3(-0.50, 0.65, -0.60), 0.86, 0.96);
    c += float3(0.9, 0.95, 1.0) * 2.0 * PLamp(d, float3(0.70, -0.20, -0.70), 0.90, 0.97);
    c += float3(1.0, 1.0, 1.0) * 1.4 * PLamp(d, float3(0.0, 1.0, -0.10), 0.93, 0.985);
    c *= 1.0 - 0.80 * PLamp(d, float3(0.45, 0.15, -0.88), 0.93, 0.985);
    return pow(max(c, 0.0), 1.18);
}

#endif

// 2026-11-15: patterns up to 22 (snowflake obsidian); this line only forces Unity to recompile the shader.
