using UnityEngine;

// The inclusions of a stone as little bodies inside its volume, for the ray-tracing shader GemLab/RealGem: clouds, crystals, needles, cavities, bubbles, feathers.
// The positions are in the space of the studio mesh (the stone fits in a ball of radius 1, mirrored along z), so they turn with the stone and the light that crosses the stone
// meets them where they are: they are refracted and multiplied by the facets, they hide each other and they move with the parallax when the stone turns.
//
// Each body is four vectors (A, B, C, D), see the shader:
//   type 0 cloud     A = centre, B.w = size of the blob, C = colour, strength (optical density)
//   type 1 ball      A = centre, B.xyz = long axis, B.w = radius, C = colour, opacity, D = (edge reflection, its sharpness, elongation along the axis)
//   type 3 needle    A = centre, B.xyz = axis, B.w = half length, C = colour, brightness, D = (radius, how much it blocks, how much it takes the colour of the stone)
//   type 4 disc      A = centre, B.xyz = normal, B.w = radius, C = colour, D = (how much it blocks, reflection, thin-film colours 0/1, 1 = fingerprint of droplets)
//   type 5 octahedron A = centre, B.w = radius, C = colour, opacity, D = (edge reflection, its sharpness)
public static class StoneInclusions
{
    public const int Max = 144;                 // must match INC_MAX in the shader
    public const float Gain = 1f;              // the overall brightness of the inclusions: tuned by eye

    public class Set
    {
        public readonly Vector4[] A = new Vector4[Max];
        public readonly Vector4[] B = new Vector4[Max];
        public readonly Vector4[] C = new Vector4[Max];
        public readonly Vector4[] D = new Vector4[Max];
        public int Count;
    }

    private const float Cloud = 0f, Ball = 1f, Needle = 3f, Disc = 4f, Octa = 5f, Box = 6f;

    // `planes` are the facet planes of the stone in the space of the mesh (xyz = outward normal, w = distance from the middle); `ratios` its half sizes (the largest is 1).
    public static Set Build(StoneQuality quality, Vector4[] planes, int planeCount, Vector3 ratios)
    {
        bool Inside(Vector3 candidate)
        {
            for (var i = 0; i < planeCount; i++)
            {
                var plane = planes[i];
                if (candidate.x * plane.x + candidate.y * plane.y + candidate.z * plane.z > plane.w - 0.10f) return false;
            }
            return true;
        }
        return planeCount < 4 ? new Set() : BuildIn(quality, Inside, ratios);
    }

    // The height of the dome of a cabochon over its outline (1 at the middle, down to the girdle at the edge), after the profile of GemMesh, in the units of the mesh (the top is 1, the
    // flat base is -1).
    private static float DomeHeight(float radius)
    {
        float[] r = { 0f, 0.20f, 0.40f, 0.58f, 0.72f, 0.84f, 0.93f, 1.0f };
        float[] h = { 1.0f, 0.946f, 0.784f, 0.54f, 0.27f, -0.03f, -0.32f, -0.62f };
        radius = Mathf.Clamp01(radius);
        for (var i = 1; i < r.Length; i++)
            if (radius <= r[i]) return Mathf.Lerp(h[i - 1], h[i], Mathf.InverseLerp(r[i - 1], r[i], radius));
        return h[h.Length - 1];
    }

    // The inclusions of a cabochon: inside its dome (the top of the dome is toward -z in the space of the studio, the flat base toward +z).
    public static Set BuildDome(StoneQuality quality, Vector3 ratios)
    {
        bool Inside(Vector3 c)
        {
            var radius = Mathf.Sqrt((c.x / ratios.x) * (c.x / ratios.x) + (c.y / ratios.y) * (c.y / ratios.y));
            if (radius > 0.88f) return false;
            var height = -c.z / ratios.z;
            return height < DomeHeight(radius) - 0.16f && height > -0.80f;
        }
        return BuildIn(quality, Inside, ratios);
    }

    private static Set BuildIn(StoneQuality quality, System.Func<Vector3, bool> inside, Vector3 ratios)
    {
        var set = new Set();
        var rng = new System.Random(quality.Seed ^ 0x51D7);
        float R() => (float)rng.NextDouble();
        float Range(float a, float b) => a + (b - a) * R();

        var style = InclusionStyles.For(quality.StyleKey);
        int total;
        switch (quality.Tier)
        {
            case 3: total = Mathf.RoundToInt(34f + 16f * quality.Haze); break;
            case 2: total = Mathf.RoundToInt(12f + 12f * quality.Haze); break;
            case 1: total = style.PureFloor > 0 ? style.PureFloor + quality.Seed % 3 : ((quality.Seed % 100) < 35 ? 1 + quality.Seed % 3 : 0); break;
            default: total = 0; break;
        }
        total = Mathf.RoundToInt(total * style.Count);
        if (total <= 0) return set;

        bool TryInside(out Vector3 point)
        {
            for (var attempt = 0; attempt < 40; attempt++)
            {
                var candidate = new Vector3(Range(-1f, 1f) * ratios.x, Range(-1f, 1f) * ratios.y, Range(-1f, 1f) * ratios.z) * 0.95f;
                if (inside(candidate)) { point = candidate; return true; }
            }
            point = Vector3.zero;
            return false;
        }

        Vector3 RandomDirection()
        {
            var v = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
            return v.sqrMagnitude < 0.01f ? Vector3.right : v.normalized;
        }

        void Add(float type, Vector3 centre, Vector3 axis, float size, Color color, float strength, Vector4 extra)
        {
            if (set.Count >= Max) return;
            var i = set.Count++;
            set.A[i] = new Vector4(centre.x, centre.y, centre.z, type);
            set.B[i] = new Vector4(axis.x, axis.y, axis.z, size);
            set.C[i] = new Vector4(color.r, color.g, color.b, strength);
            set.D[i] = extra;
        }

        // The clouds that the haze of the stone adds: wide, soft and pale.
        if (quality.Tier >= 2)
        {
            var hazeClouds = Mathf.RoundToInt(quality.Haze * 5f);
            for (var k = 0; k < hazeClouds; k++)
                if (TryInside(out var at)) Add(Cloud, at, Vector3.up, Range(0.15f, 0.30f), style.CloudColor, Range(0.5f, 1.1f), Vector4.zero);
        }

        // The needles of the species that are not really needles (an emerald) turn into fissures and feathers.
        var vNeedles = quality.VNeedles * style.Needles;
        var vFeathers = quality.VFeathers + quality.VNeedles * (1f - style.Needles);

        // Fractures: groups of irregular sheets that cross each other; some are a web of meandering cracks that join and part, like the trichites of a tourmaline.
        void AddFractureCluster(Vector3 clusterCentre)
        {
            var sheets = style.FractureSheets > 1 ? Mathf.RoundToInt(Range(2f, style.FractureSheets)) : 1;
            for (var sheet = 0; sheet < sheets; sheet++)
            {
                var sheetAt = sheet == 0 ? clusterCentre : clusterCentre + RandomDirection() * Range(0.04f, 0.14f);
                if (!inside(sheetAt)) continue;
                var web = style.FractureWeb > 0f && R() < style.FractureWeb;
                Add(Disc, sheetAt, RandomDirection(), Range(style.FeatherSize.x, style.FeatherSize.y), web ? new Color(0.92f, 0.98f, 0.94f) : Color.white,
                    Range(style.FeatherRatio.x, style.FeatherRatio.y), new Vector4(0.10f, web ? 0.95f : 0.85f, R() < style.FeatherIridescence ? 1f : 0f, web ? 5f : 0f));
            }
        }

        // The moss of the garden: patches of fine curly fibres, short and thin, in every direction.
        for (var m = 0; m < style.Moss && quality.Tier >= 2; m++)
        {
            if (!TryInside(out var mossCentre)) continue;
            var fibres = 8 + Mathf.FloorToInt(R() * 4f);
            for (var f = 0; f < fibres; f++)
            {
                var fibreAt = mossCentre + RandomDirection() * Range(0f, 0.14f);
                if (!inside(fibreAt)) continue;
                Add(Needle, fibreAt, RandomDirection(), Range(0.03f, 0.07f), style.FibreColor, style.FibreBrightness * 0.8f, new Vector4(Range(0.002f, 0.004f), 0.08f, 0.35f, 0f));
            }
        }

        // Growth lines: a bundle of very fine parallel lines along the c axis.
        if (style.Striae > 0 && quality.Tier >= 2 && TryInside(out var striaeCentre))
        {
            var lineRow = Vector3.Cross(Vector3.forward, new Vector3(Range(-1f, 1f), Range(-1f, 1f), 0f));
            if (lineRow.sqrMagnitude < 0.01f) lineRow = Vector3.right;
            lineRow.Normalize();
            for (var l = 0; l < style.Striae; l++)
            {
                var lineAt = striaeCentre + lineRow * (l * Range(0.015f, 0.04f)) + Vector3.forward * Range(-0.1f, 0.1f);
                if (!inside(lineAt)) continue;
                Add(Needle, lineAt, Vector3.forward, Range(0.25f, 0.60f), style.TubeColor, style.TubeBrightness * 0.5f, new Vector4(0.0025f, 0.03f, 0.3f, 0f));
            }
        }

        for (var k = 0; k < total && set.Count < Max; k++)
        {
            if (!TryInside(out var centre)) continue;
            var pick = R();
            // The inclusions of a very pure stone are fractures (even the purest emerald has some).
            if (quality.Tier <= 1 && R() < style.PureFractures)
            {
                AddFractureCluster(centre);
                continue;
            }

            if ((pick -= vNeedles) < 0f)
            {
                var kind = R();
                var silk = kind < quality.SilkShare;
                var tube = !silk && kind < quality.SilkShare + quality.TubeShare;
                var copies = silk ? (R() < style.SilkGroup ? 4 : 1) : 1;
                Vector3 axis;
                if (silk)
                {
                    // Rutile silk lies in the basal plane, in three directions 60 degrees apart.
                    var angle = (Mathf.Floor(R() * 3f) * 60f + Range(-4f, 4f)) * Mathf.Deg2Rad;
                    axis = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), Range(-0.05f, 0.05f)).normalized;
                }
                else if (tube) axis = new Vector3(Range(-0.04f, 0.04f), Range(-0.04f, 0.04f), 1f).normalized;
                else axis = RandomDirection();

                var side = Vector3.Cross(axis, Vector3.forward);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();
                for (var c = 0; c < copies; c++)
                {
                    var at = centre + side * (c * Range(0.012f, 0.030f)) + axis * Range(-0.05f, 0.05f);
                    if (silk) Add(Needle, at, axis, Range(style.SilkLength.x, style.SilkLength.y), style.SilkColor, style.SilkBrightness, new Vector4(Range(style.SilkRadius.x, style.SilkRadius.y), 0.12f, style.SilkTint, 0f));
                    else if (tube) Add(Needle, at, axis, Range(style.TubeLength.x, style.TubeLength.y), style.TubeColor, style.TubeBrightness, new Vector4(Range(style.TubeRadius.x, style.TubeRadius.y), 0.04f, 0f, 0f));
                    else Add(Needle, at, axis, Range(style.FibreLength.x, style.FibreLength.y), style.FibreColor, style.FibreBrightness, new Vector4(Range(style.FibreRadius.x, style.FibreRadius.y), 0.10f, 0f, 0f));
                }
            }
            else if ((pick -= quality.VCrystals) < 0f)
            {
                var kind = R();
                if (kind < quality.DarkShare)
                {
                    // Dark grains (pyrite, chromite) come in clusters.
                    for (var g = 0; g < Mathf.Max(1, style.DarkCluster); g++)
                    {
                        var grainAt = centre + (g == 0 ? Vector3.zero : RandomDirection() * Range(0.03f, 0.16f));
                        if (!inside(grainAt)) continue;
                        if (style.AngularCrystals) Add(Box, grainAt, RandomDirection(), Range(0.012f, 0.030f), style.DarkColor, 6f, new Vector4(0.60f, 3f, Range(1f, 1.3f), R() * 6.28f));
                        else Add(Ball, grainAt, RandomDirection(), Range(0.012f, 0.034f), style.DarkColor, 6f, new Vector4(0.55f, 3f, Range(1f, 1.4f), 0f));
                    }
                }
                else if (kind < quality.DarkShare + quality.MicaShare)
                {
                    var normal = (Vector3.forward + new Vector3(Range(-0.3f, 0.3f), Range(-0.3f, 0.3f), 0f)).normalized;
                    Add(Disc, centre, normal, Range(style.MicaSize.x, style.MicaSize.y), style.MicaColor, 0f, new Vector4(0.55f, 0.9f, 0f, 2f));
                }
                else if (kind < quality.DarkShare + quality.MicaShare + quality.OctaShare)
                    Add(Octa, centre, Vector3.up, Range(0.04f, 0.09f), new Color(0.80f, 0.82f, 0.86f), 1.4f, new Vector4(0.45f, 3f, 0f, 0f));
                else
                {
                    var grain = Range(style.GrainSize.x, style.GrainSize.y);
                    if (style.AngularCrystals) Add(Box, centre, RandomDirection(), grain * 1.1f, style.GrainColor, 1.4f, new Vector4(0.30f, 3.5f, Range(1f, 2.8f), R() * 6.28f));
                    else Add(Ball, centre, RandomDirection(), grain, style.GrainColor, 1.4f, new Vector4(0.30f, 4f, Range(1f, 2f), 0f));
                    // A crystal that strains its host (a zircon in a ruby) is surrounded by a thin sheet: the halo.
                    if (R() < style.Halo)
                        Add(Disc, centre, RandomDirection(), grain * Range(3f, 4.5f), new Color(0.97f, 0.95f, 0.95f), 0f, new Vector4(0.02f, 0.40f, 1f, 0f));
                }
            }
            else if ((pick -= quality.VCavities) < 0f)
            {
                var cavityKind = R();
                if (!style.CavityAlign)
                {
                    // A cavity filled with liquid, and often a gas bubble in it.
                    var axis = RandomDirection();
                    var radius = Range(style.CavitySize.x, style.CavitySize.y);
                    var elongation = Range(style.CavityElongation.x, style.CavityElongation.y);
                    Add(Ball, centre, axis, radius, new Color(0.88f, 0.94f, 1f), 0.30f, new Vector4(0.85f, 2.6f, elongation, 0f));
                    if (R() < style.CavityBubble)
                        Add(Ball, centre + axis * (elongation * radius * Range(0.1f, 0.5f)), axis, radius * 0.38f, new Color(0.95f, 0.97f, 1f), 0.10f, new Vector4(0.95f, 2.5f, 1f, 0f));
                    // A third phase: a small crystal (a cube of halite) at the other end of the cavity.
                    if (R() < style.CavityCrystal)
                        Add(Octa, centre - axis * (elongation * radius * Range(0.1f, 0.5f)), Vector3.up, radius * 0.45f, new Color(0.92f, 0.93f, 0.92f), 1.2f, new Vector4(0.40f, 3f, 0f, 0f));
                }
                else if (cavityKind < quality.Cav3Share + quality.CavJagShare)
                {
                    // A jagged cavity sheet, long along the c axis: three-phase (liquid, a gas bubble and a cube of halite) or two-phase. The sheets are big, so some are left out.
                    // They lie inclined to the c axis, so that they show when the stone is looked at through its table.
                    if (R() < 0.6f)
                    {
                        var angle = R() * Mathf.PI * 2f;
                        var tilt = Range(0.35f, 0.70f) * (R() < 0.5f ? -1f : 1f);
                        var flat = Mathf.Sqrt(1f - tilt * tilt);
                        var sheetNormal = new Vector3(Mathf.Cos(angle) * flat, Mathf.Sin(angle) * flat, tilt);
                        var length = Range(style.SheetLength.x, style.SheetLength.y);
                        Add(Disc, centre, sheetNormal, length, new Color(0.90f, 0.96f, 0.92f), Range(style.SheetWidth.x, style.SheetWidth.y), new Vector4(0f, 0f, 0f, 3f));
                        var along = Vector3.Cross(sheetNormal, Vector3.Cross(Vector3.forward, sheetNormal)).normalized;      // the c axis, in the plane of the sheet
                        if (R() < style.CavityBubble)
                            Add(Ball, centre + along * (length * Range(-0.35f, 0.35f)), Vector3.up, length * Range(0.07f, 0.12f), new Color(0.95f, 0.97f, 1f), 0.08f, new Vector4(0.60f, 2.4f, 1f, 0f));
                        if (cavityKind < quality.Cav3Share && R() < style.CavityCrystal)
                            Add(Octa, centre + along * (length * Range(-0.35f, 0.35f)), Vector3.up, length * Range(0.07f, 0.11f), new Color(0.92f, 0.93f, 0.92f), 1.2f, new Vector4(0.40f, 3f, 1f, 0f));
                    }
                }
                else
                {
                    // Rods of two-phase inclusions, along the c axis, side by side in a row, each with a bright outline and often a bubble.
                    var rowDirection = Vector3.Cross(Vector3.forward, new Vector3(Range(-1f, 1f), Range(-1f, 1f), 0f));
                    if (rowDirection.sqrMagnitude < 0.01f) rowDirection = Vector3.right;
                    rowDirection.Normalize();
                    var rods = 1 + Mathf.FloorToInt(R() * 4f);
                    for (var rod = 0; rod < rods; rod++)
                    {
                        var rodAt = centre + rowDirection * (rod * Range(0.04f, 0.10f)) + Vector3.forward * Range(-0.08f, 0.08f);
                        if (!inside(rodAt)) continue;
                        var rodRadius = Range(style.CavitySize.x, style.CavitySize.y);
                        var rodLength = Range(style.CavityElongation.x, style.CavityElongation.y);
                        var rodAxis = (Vector3.forward + new Vector3(Range(-0.05f, 0.05f), Range(-0.05f, 0.05f), 0f)).normalized;
                        // A rod with flat faces (a negative crystal): a long box, clear, whose edges catch the light.
                        Add(Box, rodAt, rodAxis, rodRadius * rodLength, new Color(0.88f, 0.94f, 1f), 0.20f, new Vector4(0.18f, 2.4f, rodLength, R() * 6.28f));
                        if (R() < style.CavityBubble)
                            Add(Ball, rodAt + rodAxis * (rodLength * rodRadius * Range(-0.5f, 0.5f)), rodAxis, rodRadius * 0.55f, new Color(0.95f, 0.97f, 1f), 0.08f, new Vector4(0.60f, 2.4f, 1f, 0f));
                    }
                }
            }
            else if ((pick -= quality.VHealing) < 0f)
            {
                // A healed fissure: a sheet of droplets.
                Add(Disc, centre, RandomDirection(), Range(style.FingerprintSize.x, style.FingerprintSize.y), new Color(0.95f, 0.95f, 0.97f), 0f, new Vector4(0.06f, 0.85f, R() < 0.4f ? 1f : 0f, 1f));
            }
            else if ((pick -= quality.VClouds) < 0f)
            {
                Add(Cloud, centre, Vector3.up, Range(style.CloudSize.x, style.CloudSize.y), style.CloudColor, Range(0.5f, 1.4f), Vector4.zero);
            }
            else if ((pick -= vFeathers) < 0f)
            {
                // A feather or a crack: a thin sheet that flashes when it catches the light (in an emerald, a group of sheets that cross each other).
                AddFractureCluster(centre);
            }
            else
            {
                Add(Ball, centre, Vector3.up, Range(0.012f, 0.030f), new Color(0.95f, 0.97f, 1f), 0.12f, new Vector4(0.9f, 2.5f, 1f, 0f));
            }
        }

        // Dust: tiny bright particles gathered in a few sheets across the stone, the pinpoints that sparkle and that the eye sees first in a Burmese ruby.
        var dust = quality.Tier >= 2 ? Mathf.RoundToInt(total * 0.5f * style.Dust) : (quality.Tier == 1 && quality.Seed % 100 < 20 ? 3 : 0);
        if (dust > 0 && TryInside(out var firstSheet))
        {
            var sheets = 2 + quality.Seed % 2;
            var sheetCentres = new Vector3[sheets];
            var sheetNormals = new Vector3[sheets];
            for (var sh = 0; sh < sheets; sh++)
            {
                sheetCentres[sh] = sh == 0 ? firstSheet : (TryInside(out var other) ? other : firstSheet);
                sheetNormals[sh] = (Vector3.forward + new Vector3(Range(-0.35f, 0.35f), Range(-0.35f, 0.35f), 0f)).normalized;
            }
            for (var d = 0; d < dust && set.Count < Max; d++)
            {
                var pick = Mathf.Min(sheets - 1, Mathf.FloorToInt(R() * sheets));
                var inPlane = Vector3.Cross(sheetNormals[pick], RandomDirection());
                if (inPlane.sqrMagnitude < 0.01f) continue;
                var at = sheetCentres[pick] + inPlane.normalized * Range(0.05f, 0.45f) + sheetNormals[pick] * Range(-0.03f, 0.03f);
                if (!inside(at)) continue;
                Add(Ball, at, Vector3.up, Range(0.010f, 0.018f), style.DustColor, 0.9f, new Vector4(0.95f, 2f, 1f, 0f));
            }
        }
        return set;
    }
}
