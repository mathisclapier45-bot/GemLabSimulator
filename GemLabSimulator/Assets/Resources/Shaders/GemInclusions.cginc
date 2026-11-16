// The inclusions of a stone as little bodies in its volume, shared by the shaders GemLab/RealGem (faceted stones) and GemLab/RealDome (cabochons).
// The shader that includes this file must define, before the include: Env (the studio, a world direction in, a colour out), ToWorld (a direction of the stone to the world),
// Noise3 (a value noise in 0..1) and the colour _Body. See StoneInclusions.cs for what each vector of a body holds.
#ifndef GEM_INCLUSIONS_INCLUDED
#define GEM_INCLUSIONS_INCLUDED

// The inclusions: little bodies in the volume of the stone (see StoneInclusions.cs for what each vector holds).
#define INC_MAX 144
float4 _IncA[INC_MAX];
float4 _IncB[INC_MAX];
float4 _IncC[INC_MAX];
float4 _IncD[INC_MAX];
int _IncCount;
float _IncPixel;       // the size of one pixel of the picture, in the space of the stone
float _IncGain;        // overall brightness of the inclusions

float Chan (float3 c, int channel)
{
    return channel == 0 ? c.r : (channel == 1 ? c.g : c.b);
}

// The light of the studio that comes along a direction of the stone, for one colour (the brightest lamps are held back).
float EnvChan (float3 dirObj, int channel)
{
    return Chan(min(Env(ToWorld(dirObj)), 3.0), channel);
}

// tanh(1.2 x): close to the error function, for the integral of a soft cloud along a ray.
float ErfA (float x)
{
    float e = exp(-2.4 * clamp(x, -4.0, 4.0));
    return (1.0 - e) / (1.0 + e);
}

// What the inclusions do to the light on one straight stretch of its way (from p along u, for tMax): they hide some of it (`weight` goes down) and they show
// their own light (the value returned, to be added to the light seen). The bodies are in the space of the stone, so they are seen through the facets.
float Inclusions (float3 p, float3 u, float tMax, int channel, inout float weight)
{
    float add = 0.0;
    float3 keyObj = normalize(mul((float3x3)unity_WorldToObject, float3(-0.5, 0.65, -0.6)));
    [loop]
    for (int k = 0; k < INC_MAX; k++)
    {
        if (k >= _IncCount) break;
        float4 A = _IncA[k];
        float4 B = _IncB[k];
        float4 C = _IncC[k];
        float4 D = _IncD[k];
        int type = (int)(A.w + 0.5);

        // A body that the stretch does not come near is left out.
        float3 oc = A.xyz - p;
        float tc = dot(oc, u);
        float ext = B.w * (type == 0 ? 3.0 : (type == 1 ? max(D.z, 1.0) : 1.0)) + (type == 3 ? D.x : 0.0) + 0.02;
        if (tc < -ext || tc > tMax + ext) continue;
        if (dot(oc, oc) - tc * tc > ext * ext) continue;

        if (type == 0)
        {
            // A cloud: a soft blob, its density goes down like a bell around the middle; the integral along the ray is worked out in closed form.
            float s = B.w;
            float d2 = dot(oc, oc) - tc * tc;
            float hi = (tMax - tc) / s;
            float lo = (0.0 - tc) / s;
            float tau = C.w * _IncGain * s * 0.886 * exp(-d2 / (s * s)) * 0.5 * (ErfA(hi) - ErfA(lo));
            float cover = 1.0 - exp(-tau);
            float lit = 0.35 + 0.30 * EnvChan(u, channel);
            add += weight * cover * Chan(C.rgb, channel) * lit;
            weight *= 1.0 - cover * 0.45;
        }
        else if (type == 1)
        {
            // A ball or an ellipsoid (a crystal, a bubble, a cavity): opaque or clear inside, and it mirrors the studio on its edge.
            float3 ax = B.xyz;
            float e = max(D.z, 1.0);
            float r = B.w;
            float3 ocS = oc - ax * (dot(oc, ax) * (1.0 - 1.0 / e));
            float3 uS = u - ax * (dot(u, ax) * (1.0 - 1.0 / e));
            float qa = dot(uS, uS);
            float qb = dot(ocS, uS);
            float qc = dot(ocS, ocS) - r * r;
            float disc = qb * qb - qa * qc;
            if (disc <= 0.0) continue;
            float sq = sqrt(disc);
            float t0 = (qb - sq) / qa;
            float t1 = (qb + sq) / qa;
            if (t1 <= 0.0 || t0 >= tMax) continue;
            float tin = max(t0, 0.0);
            float tout = min(t1, tMax);
            float3 pe = (p + u * tin) - A.xyz;
            float3 nn = normalize(pe - ax * (dot(pe, ax) * (1.0 - 1.0 / (e * e))));
            float op = 1.0 - exp(-C.w * (tout - tin) / r);
            float diff = 0.30 + 0.70 * saturate(dot(nn, keyObj));
            float cosv = abs(dot(u, nn));
            float fres = D.x * (0.05 + 0.95 * pow(saturate(1.0 - cosv), D.y));
            add += weight * _IncGain * (op * Chan(C.rgb, channel) * diff + fres * EnvChan(reflect(u, nn), channel));
            weight *= (1.0 - op) * (1.0 - fres * 0.5);
        }
        else if (type == 3)
        {
            // A needle: a thin rod. It shines when the studio is mirrored along its length.
            float3 v = B.xyz;
            float hl = B.w;
            float rad = max(D.x, _IncPixel * 0.45);
            float fine = saturate(D.x / rad * 1.5);
            float3 w0 = p - A.xyz;
            float bb = dot(u, v);
            float dd = dot(u, w0);
            float ee = dot(v, w0);
            float den = 1.0 - bb * bb;
            float dist;
            float len;
            if (den < 0.015)
            {
                // The needle runs along the ray: seen end on, it is a point.
                float bs = bb >= 0.0 ? max(bb, 0.5) : min(bb, -0.5);
                float ta = (-hl - ee) / bs;
                float tb = (hl - ee) / bs;
                float tlo = max(min(ta, tb), 0.0);
                float thi = min(max(ta, tb), tMax);
                if (thi <= tlo) continue;
                dist = length(w0 - v * ee);
                len = thi - tlo;
            }
            else
            {
                float sa = clamp((ee - bb * dd) / den, -hl, hl);
                float3 q = A.xyz + v * sa;
                float tt = dot(q - p, u);
                if (tt < 0.0 || tt > tMax) continue;
                dist = length(q - (p + u * tt));
                len = min(2.0 * rad / sqrt(den), 4.0 * rad);
            }
            if (dist >= rad) continue;
            float prof = sqrt(saturate(1.0 - dist * dist / (rad * rad)));
            float op = (1.0 - exp(-C.w * len / rad * prof)) * fine;
            float3 sd = 2.0 * v * bb - u;
            float shine = 0.20 + 0.45 * EnvChan(sd, channel);
            // The silk is lit by the studio but seen through the coloured crystal: its light takes a part of the colour of the stone (D.z) and is not white.
            float3 needleColor = lerp(C.rgb, saturate(_Body.rgb * 1.5 + 0.1), D.z);
            add += weight * op * Chan(needleColor, channel) * shine * _IncGain;
            weight *= 1.0 - op * D.y;
        }
        else if (type == 4)
        {
            // A thin sheet (a feather, a healed fissure, a flake of mica): it mirrors the studio and flashes when it faces a lamp.
            float3 nrm = B.xyz;
            float nu = dot(u, nrm);
            if (abs(nu) < 0.02) continue;
            float tt = dot(A.xyz - p, nrm) / nu;
            if (tt <= 0.0 || tt >= tMax) continue;
            float3 h = p + u * tt - A.xyz;
            if (D.w > 4.5)
            {
                // A crack that is not flat: the sheet waves, so that its web of cracks meanders through the stone.
                float wave = (Noise3(h * 5.0 + A.xyz * 9.0) - 0.5) * 0.5 * B.w;
                float nus = nu >= 0.0 ? max(nu, 0.25) : min(nu, -0.25);
                tt += wave / nus;
                if (tt <= 0.0 || tt >= tMax) continue;
                h = p + u * tt - A.xyz;
            }
            if (D.w > 2.5 && D.w < 3.5)
            {
                // A jagged cavity sheet (a three-phase inclusion): flat, long along the c axis (z), pointed at both ends, with a saw-tooth edge. It is clear inside, with a dark
                // edge and a bright rim.
                float3 cdir = abs(nrm.z) > 0.95 ? float3(1.0, 0.0, 0.0) : normalize(float3(0.0, 0.0, 1.0) - nrm * nrm.z);
                float3 sdir = cross(nrm, cdir);
                float along = dot(h, cdir);
                float across = dot(h, sdir);
                float tn = along / B.w;
                if (abs(tn) >= 1.0) continue;
                float saw = Noise3(float3(along * 38.0, across * 4.0, A.x * 20.0));
                float halfW = B.w * C.w * (1.0 - abs(tn)) * (0.55 + 0.9 * saw);
                float ac = abs(across);
                if (ac >= halfW) continue;
                float edgeBand = smoothstep(halfW * 0.55, halfW * 0.9, ac);
                float3 facing3 = nu > 0.0 ? -nrm : nrm;
                float3 mirror3 = reflect(u, facing3);
                add += weight * (edgeBand * 0.30 + (1.0 - edgeBand) * 0.10) * EnvChan(mirror3, channel) * _IncGain;
                weight *= 1.0 - edgeBand * 0.6;
                continue;
            }
            float rr;
            if (C.w > 0.01)
            {
                // An elongated sheet (a fracture, a feather): longer along one direction of its plane than across.
                float3 e1 = normalize(cross(nrm, float3(0.31, 0.87, 0.38)));
                float3 e2 = cross(nrm, e1);
                rr = length(float2(dot(h, e1), dot(h, e2) / C.w));
            }
            else rr = length(h);
            float edge = Noise3(h * 14.0 + A.xyz * 37.0);
            float limit = B.w * (0.55 + 0.75 * edge);
            float mask = 1.0 - smoothstep(limit * 0.8, limit, rr);
            if (D.w > 0.5 && D.w < 1.5) mask *= smoothstep(0.50, 0.60, Noise3(h * 55.0 + A.xyz * 11.0));
            if (D.w > 4.5)
            {
                // A web of meandering cracks that cross and join (like the trichites of a tourmaline): the thin lines where a noise crosses its middle value, at two scales.
                float n1 = Noise3(h * 9.0 + A.xyz * 7.0);
                float n2 = Noise3(h * 19.0 + A.xyz * 3.0);
                float ridge = max(1.0 - abs(2.0 * n1 - 1.0), 1.0 - abs(2.0 * n2 - 1.0));
                mask *= smoothstep(0.88, 0.97, ridge);
            }
            if (mask <= 0.001) continue;
            float cosn = abs(nu);
            float baseR = D.w > 4.5 ? 0.45 : 0.10;
            float fres = D.y * (baseR + (1.0 - baseR) * pow(saturate(1.0 - cosn), 2.0)) * mask;
            float3 col = C.rgb;
            if (D.z > 0.5) col = lerp(col, 0.5 + 0.5 * cos(6.2832 * (cosn * 2.5 + float3(0.0, 0.33, 0.67))), 0.7);
            float3 facing = nu > 0.0 ? -nrm : nrm;
            add += weight * fres * Chan(col, channel) * EnvChan(reflect(u, facing), channel) * _IncGain;
            weight *= (1.0 - fres * 0.6) * (1.0 - D.x * mask);
        }
        else if (type == 5)
        {
            // An octahedron (a crystal of spinel, diamond...): the ray crosses its eight faces.
            float rr = B.w;
            float3 po = p - A.xyz;
            float tEnter = 0.0;
            float tExit = tMax;
            float3 nEnter = float3(0.0, 0.0, -1.0);
            bool miss = false;
            [unroll]
            for (int f = 0; f < 8; f++)
            {
                float fs = (float)f;
                float3 nf;
                float off;
                if (D.z > 0.5)
                {
                    // A cube: six faces (the last two places of the loop stay empty).
                    float axisIndex = floor(fs * 0.5);
                    float sgn = frac(fs * 0.5) * 4.0 - 1.0;
                    nf = float3(axisIndex < 0.5 ? sgn : 0.0, (axisIndex > 0.5 && axisIndex < 1.5) ? sgn : 0.0, (axisIndex > 1.5 && axisIndex < 2.5) ? sgn : 0.0);
                    off = rr;
                }
                else
                {
                    nf = float3(frac(fs * 0.5) * 4.0 - 1.0, frac(floor(fs * 0.5) * 0.5) * 4.0 - 1.0, floor(fs * 0.25) * 2.0 - 1.0) * 0.57735;
                    off = rr * 0.57735;
                }
                float nd = dot(nf, u);
                float no = dot(nf, po) - off;
                if (abs(nd) < 1.0e-5)
                {
                    if (no > 0.0) miss = true;
                }
                else
                {
                    float tq = -no / nd;
                    if (nd < 0.0) { if (tq > tEnter) { tEnter = tq; nEnter = nf; } }
                    else tExit = min(tExit, tq);
                }
            }
            if (miss || tExit <= tEnter) continue;
            float op = 1.0 - exp(-C.w * (tExit - tEnter) / rr);
            float diff = 0.30 + 0.70 * saturate(dot(nEnter, keyObj));
            float cosv = abs(dot(u, nEnter));
            float fres = D.x * (0.05 + 0.95 * pow(saturate(1.0 - cosv), D.y));
            add += weight * _IncGain * (op * Chan(C.rgb, channel) * diff + fres * EnvChan(reflect(u, nEnter), channel));
            weight *= (1.0 - op) * (1.0 - fres * 0.5);
        }
        else if (type == 6)
        {
            // A crystal with flat faces (a cube of pyrite, a prism, a rod): a box in its own frame, long along B.xyz. Its faces mirror the studio and its edges stay sharp.
            float3 ax = B.xyz;
            float3 refv = abs(ax.y) < 0.9 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
            float3 bx = normalize(cross(ax, refv));
            float3 cx = cross(ax, bx);
            float ca = cos(D.w);
            float sa = sin(D.w);
            float3 b2 = bx * ca + cx * sa;
            float3 c2 = -bx * sa + cx * ca;
            float across = B.w / max(D.z, 1.0);
            float3 po = p - A.xyz;
            float3 lc = float3(dot(po, ax), dot(po, b2), dot(po, c2));
            float3 ld = float3(dot(u, ax), dot(u, b2), dot(u, c2));
            float3 hs = float3(B.w, across, across);
            float tEnter = 0.0;
            float tExit = tMax;
            float3 nEnter = float3(0.0, 0.0, -1.0);
            bool miss = false;
            [unroll]
            for (int s = 0; s < 3; s++)
            {
                float o = s == 0 ? lc.x : (s == 1 ? lc.y : lc.z);
                float dd = s == 0 ? ld.x : (s == 1 ? ld.y : ld.z);
                float hh = s == 0 ? hs.x : (s == 1 ? hs.y : hs.z);
                float3 faceAxis = s == 0 ? ax : (s == 1 ? b2 : c2);
                if (abs(dd) < 1.0e-5)
                {
                    if (abs(o) > hh) miss = true;
                }
                else
                {
                    float t1 = (-hh - o) / dd;
                    float t2 = (hh - o) / dd;
                    float tn = min(t1, t2);
                    float tf = max(t1, t2);
                    if (tn > tEnter) { tEnter = tn; nEnter = faceAxis * (dd > 0.0 ? -1.0 : 1.0); }
                    tExit = min(tExit, tf);
                }
            }
            if (miss || tExit <= tEnter) continue;
            float op = 1.0 - exp(-C.w * (tExit - tEnter) / across);
            float diff = 0.30 + 0.70 * saturate(dot(nEnter, keyObj));
            float cosv = abs(dot(u, nEnter));
            float fres = D.x * (0.05 + 0.95 * pow(saturate(1.0 - cosv), D.y));
            add += weight * _IncGain * (op * Chan(C.rgb, channel) * diff + fres * EnvChan(reflect(u, nEnter), channel));
            weight *= (1.0 - op) * (1.0 - fres * 0.5);
        }
    }
    // The light of the inclusions is seen through the coloured crystal: it takes a part of the colour of the stone, instead of staying white.
    float3 incTint = lerp(float3(1.0, 1.0, 1.0), saturate(_Body.rgb * 1.5 + 0.1), 0.55);
    return add * Chan(incTint, channel);
}


#endif
