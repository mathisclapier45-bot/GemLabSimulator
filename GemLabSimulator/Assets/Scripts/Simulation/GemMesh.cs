using System.Collections.Generic;
using UnityEngine;

// Real facet geometry of a cut stone (crown, girdle, pavilion), used to render gems in 3D. The mesh is
// normalized: x, y and z all run from -1 to 1, ready to be scaled by the half sizes of a stone.
public class GemMesh
{
    public Vector3[] Vertices;
    public int[][] Triangles;       // each: 3 vertex indices
    public int[] FacetIds;          // one id per triangle; triangles of the same flat facet share it
    public int FacetCount;          // number of facets of the cut (the thin girdle band is not counted); 0 for a dome or rough stone

    private struct Ring
    {
        public float Rho;
        public float Z;
        public float Alternate;

        public Ring(float rho, float z, float alternate = 0f)
        {
            Rho = rho;
            Z = z;
            Alternate = alternate;
        }
    }

    private static readonly Dictionary<string, GemMesh> Cache = new Dictionary<string, GemMesh>();

    // Mesh of a cut stone. Faceted cuts are built facet by facet (see BuildFaceted); the cabochon is a smooth dome.
    // `openCulet`: a tiny flat facet at the tip of a brilliant-type pavilion (rare); ignored by the other cuts.
    public static GemMesh Get(string shape, string style, bool openCulet = false)
    {
        if (style == "Cabochon")
        {
            var domeKey = $"{shape}|{style}";
            if (Cache.TryGetValue(domeKey, out var dome)) return dome;
            dome = Build(shape, Profile(style), 72);
            Cache[domeKey] = dome;
            return dome;
        }

        var culet = openCulet && HasCulet(style);
        var key = $"faceted|{shape}|{style}|{culet}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var mesh = BuildFaceted(shape, style, culet);
        Cache[key] = mesh;
        return mesh;
    }

    // Cuts with a brilliant-type pavilion end in a point (culet): rarely, the tip is cut flat (open culet, one more facet).
    public static bool HasCulet(string style)
    {
        switch (style)
        {
            case "Brillant":
            case "Mixte":
            case "Princesse":
            case "Radiant":
            case "Ciseaux":
                return true;
            default:
                return false;
        }
    }

    // Width and height of the outline of a shape, scaled so the larger one is 1: lets an icon keep the proportions of its shape.
    public static Vector2 Aspect(string shape)
    {
        float maxX = 0.01f, maxY = 0.01f;
        for (var i = 0; i < 180; i++)
        {
            var theta = i * 2f * Mathf.PI / 180f;
            var radius = GemPixelArt.BoundaryRadius(shape, theta);
            maxX = Mathf.Max(maxX, Mathf.Abs(Mathf.Cos(theta) * radius));
            maxY = Mathf.Max(maxY, Mathf.Abs(Mathf.Sin(theta) * radius));
        }
        var largest = Mathf.Max(maxX, maxY);
        return new Vector2(maxX / largest, maxY / largest);
    }

    private static readonly Dictionary<string, float> AspectRatios = new Dictionary<string, float>();

    // Width / length of the outline of a shape (1 for a round or square one, about 0.73 for an oval).
    public static float AspectRatio(string shape)
    {
        var key = shape ?? "";
        if (AspectRatios.TryGetValue(key, out var cached)) return cached;

        var aspect = Aspect(shape);
        var ratio = Mathf.Min(aspect.x, aspect.y) / Mathf.Max(aspect.x, aspect.y);
        AspectRatios[key] = ratio;
        return ratio;
    }

    // ---- Faceted cuts ------------------------------------------------------------------------------------------

    private struct CutPlan
    {
        public int CrownSteps;          // 0 = brilliant crown (table, stars, bezels, upper girdle facets); > 0 = that many step rows
        public int PavilionSteps;       // 0 = brilliant pavilion (mains, lower girdle facets); > 0 = that many step rows plus a keel
        public bool SplitMains;         // radiant: each pavilion main is split in two
        public float StarRadius;        // where the points of the star facets reach (0.80 normally, long stars for the scissor cut)
        public float TableRadius;
        public float CrownHeight;
        public float PavilionDepth;
    }

    private static CutPlan PlanFor(string shape, string style)
    {
        // Tolkowsky proportions (in units of the girdle radius): table 53 %, crown 16.2 %, pavilion 43.1 % of the diameter.
        var plan = new CutPlan { StarRadius = 0.80f, TableRadius = 0.53f, CrownHeight = 0.324f, PavilionDepth = 0.862f };
        switch (style)
        {
            case "Mixte":
                plan.CrownSteps = 2;
                plan.TableRadius = 0.56f;
                plan.CrownHeight = 0.30f;
                break;
            case "À degrés":
                plan.CrownSteps = 3;
                plan.PavilionSteps = 3;
                plan.TableRadius = 0.58f;
                plan.CrownHeight = 0.26f;
                plan.PavilionDepth = 0.72f;
                break;
            case "Ascher":
                plan.CrownSteps = 4;
                plan.PavilionSteps = 3;
                plan.TableRadius = 0.50f;
                plan.CrownHeight = 0.40f;
                plan.PavilionDepth = 0.90f;
                break;
            case "Princesse":
                plan.TableRadius = 0.62f;
                plan.CrownHeight = 0.28f;
                plan.PavilionDepth = 0.95f;
                break;
            case "Radiant":
                plan.SplitMains = true;
                plan.TableRadius = 0.60f;
                plan.CrownHeight = 0.30f;
                plan.PavilionDepth = 0.90f;
                break;
            case "Brillant" when shape == "Coussin":
                // Cushion brilliant: a larger, squarer table and a shallower pavilion than the round.
                plan.TableRadius = 0.58f;
                plan.CrownHeight = 0.30f;
                plan.PavilionDepth = 0.80f;
                break;
            case "Ciseaux":
                // Star-burst ("split brilliant"): long star facets reach almost to the girdle around a small table.
                plan.StarRadius = 0.93f;
                plan.TableRadius = 0.44f;
                plan.CrownHeight = 0.34f;
                break;
        }

        // The baguette is a very simple step cut: one crown row, two pavilion rows and the keel (14 facets).
        if (shape == "Baguette" && style == "À degrés")
        {
            plan.CrownSteps = 1;
            plan.PavilionSteps = 2;
            plan.TableRadius = 0.62f;
            plan.CrownHeight = 0.22f;
            plan.PavilionDepth = 0.62f;
        }
        return plan;
    }

    // Angles (degrees, ascending) of the main vertices of the girdle: the corners of the outline, or evenly spaced points.
    private static float[] MainAngles(string shape, string style)
    {
        // Cushion brilliant: the main facets sit in pairs around the four corners (45 +/- 16 degrees), which gives a square table
        // with cut corners, large bezels at the corners and along the sides, like the crown of a cushion cut.
        if (shape == "Coussin" && style == "Brillant")
            return new[] { 29f, 61f, 119f, 151f, 209f, 241f, 299f, 331f };

        // Brilliant-type cuts spread their main facets evenly around the stone; only the step cuts follow the corners.
        var stepCut = style == "À degrés" || style == "Ascher";
        if (!stepCut) return shape == "Triangle" ? Evenly(6) : Evenly(8);

        // Step cuts follow the corners: sharp-cornered shapes (square, rectangle, baguette) have 4 mitred corners, the
        // cut-cornered ones (emerald, octagon) 8 vertices, the triangle 3.
        switch (shape)
        {
            case "Carré": return CornerAngles(0.84f, 0.84f);
            case "Rectangle": return CornerAngles(0.98f, 0.66f);
            case "Baguette": return CornerAngles(1.00f, 0.40f);
            case "Rectangle à pans coupés": return ChamferAngles(0.98f, 0.70f, 0.32f);
            case "Octogone": return ChamferAngles(0.88f, 0.88f, 0.40f);
            case "Triangle": return Evenly(3);
            default: return Evenly(8);
        }
    }

    private static float[] Evenly(int count)
    {
        var angles = new float[count];
        for (var i = 0; i < count; i++) angles[i] = 360f * i / count;
        return angles;
    }

    // The 8 vertices of a rectangle with its four corners cut (half sizes hx, hy; cut of size c).
    private static float[] ChamferAngles(float hx, float hy, float c)
    {
        var a1 = Mathf.Atan2(hy - c, hx) * Mathf.Rad2Deg;
        var a2 = Mathf.Atan2(hy, hx - c) * Mathf.Rad2Deg;
        return new[] { a1, a2, 180f - a2, 180f - a1, 180f + a1, 180f + a2, 360f - a2, 360f - a1 };
    }

    private static float[] CornerAngles(float hx, float hy)
    {
        var a = Mathf.Atan2(hy, hx) * Mathf.Rad2Deg;
        return new[] { a, 180f - a, 180f + a, 360f - a };
    }

    private static readonly float[][] CrownStepFractions =
    {
        null,
        new[] { 0f, 1f },
        new[] { 0f, 0.40f, 1f },
        new[] { 0f, 0.22f, 0.55f, 1f },
        new[] { 0f, 0.14f, 0.32f, 0.62f, 1f },
    };

    private static readonly float[][] PavilionStepFractions =
    {
        null,
        new[] { 0f, 1f },
        new[] { 0f, 0.55f, 1f },
        new[] { 0f, 0.30f, 0.62f, 1f },
    };

    // The princess cut: a flat table framed by four bezels; underneath, the pavilion mains run from the four corners to the
    // culet and, on each side, three chevron facets meet at a point: the "X" and four-pointed star of the diagrams.
    // Facets: crown 1 + 12, pavilion 12 chevrons + 4 corner kites (+1 with an open culet).
    private static GemMesh BuildPrincess(string shape, bool openCulet)
    {
        var aspect = Aspect(shape);
        var corner = Mathf.Atan2(aspect.y, aspect.x) * Mathf.Rad2Deg;
        var angles = new[] { corner, 180f - corner, 180f + corner, 360f - corner };
        const int n = 4;
        const int thirds = 3;
        const float girdleHalf = 0.02f;
        const float tableRadius = 0.64f;
        const float crownHeight = 0.28f;
        const float pavilionDepth = 0.95f;
        const float chevronRadius = 0.52f;

        var vertices = new List<Vector3>();
        var triangles = new List<int[]>();
        var facetIds = new List<int>();
        var nextFacet = 1;
        var counted = 0;

        float Outline(float degrees) => Mathf.Max(0.05f, GemPixelArt.BoundaryRadius(shape, degrees * Mathf.Deg2Rad));
        int AddAt(Vector3 position)
        {
            vertices.Add(position);
            return vertices.Count - 1;
        }

        void Facet(bool count, params int[] polygon)
        {
            var id = nextFacet++;
            if (count) counted++;
            for (var i = 1; i + 1 < polygon.Length; i++)
                AddTriangle(vertices, triangles, facetIds, polygon[0], polygon[i], polygon[i + 1], id);
        }

        // The girdle: the four corners and, between two corners, two more points on the straight side (a ring of 12).
        var corners = new Vector2[n];
        for (var k = 0; k < n; k++)
        {
            var radians = angles[k] * Mathf.Deg2Rad;
            var radius = Outline(angles[k]);
            corners[k] = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        var ringSize = n * thirds;
        var girdleXY = new Vector2[ringSize];
        for (var k = 0; k < n; k++)
        {
            var a = corners[k];
            var b = corners[(k + 1) % n];
            for (var j = 0; j < thirds; j++) girdleXY[k * thirds + j] = Vector2.Lerp(a, b, j / (float)thirds);
        }

        var tableZ = girdleHalf + crownHeight;
        var culetZ = -girdleHalf - pavilionDepth;
        var upper = new int[ringSize];
        var lower = new int[ringSize];
        var table = new int[ringSize];
        for (var i = 0; i < ringSize; i++)
        {
            upper[i] = AddAt(new Vector3(girdleXY[i].x, girdleXY[i].y, girdleHalf));
            lower[i] = AddAt(new Vector3(girdleXY[i].x, girdleXY[i].y, -girdleHalf));
            table[i] = AddAt(new Vector3(girdleXY[i].x * tableRadius, girdleXY[i].y * tableRadius, tableZ));
        }

        // Crown: the table and a ring of 12 bezel trapezoids (four sides of three), as in the diagram.
        Facet(true, table);
        for (var i = 0; i < ringSize; i++)
        {
            var next = (i + 1) % ringSize;
            Facet(true, table[i], table[next], upper[next], upper[i]);
            Facet(false, upper[i], upper[next], lower[next], lower[i]);                  // girdle band
        }

        // Pavilion: on each side a chevron point P inside the stone; three triangles from P to the side, and at each corner
        // a kite from the corner to the culet between the two neighbouring chevron points.
        var pavilionPoint = new int[n];
        var pointXY = new Vector2[n];
        for (var k = 0; k < n; k++)
        {
            var mid = (corners[k] + corners[(k + 1) % n]) * 0.5f;
            pointXY[k] = mid * chevronRadius;
            // Keeps each corner kite flat: its chevron points lie on a line that meets the corner-to-culet line.
            var z = culetZ + 0.5f * chevronRadius * (-girdleHalf - culetZ);
            pavilionPoint[k] = AddAt(new Vector3(pointXY[k].x, pointXY[k].y, z));
        }

        // Pointed culet, or (rarely) a tiny flat one: four points on the spokes that run from the tip to the chevron points,
        // which keeps every corner kite flat.
        var tipPosition = new Vector3(0f, 0f, culetZ);
        var openCuletPoints = new int[n];
        var tip = -1;
        if (openCulet)
        {
            for (var k = 0; k < n; k++)
                openCuletPoints[k] = AddAt(tipPosition + (vertices[pavilionPoint[k]] - tipPosition) * 0.12f);
        }
        else
        {
            tip = AddAt(tipPosition);
        }

        for (var k = 0; k < n; k++)
        {
            for (var j = 0; j < thirds; j++)
            {
                var a = k * thirds + j;
                var b = (a + 1) % ringSize;
                Facet(true, lower[a], lower[b], pavilionPoint[k]);                          // chevron facet
            }
        }

        for (var k = 0; k < n; k++)
        {
            // Corner k sits between side k - 1 and side k.
            var previousSide = (k + n - 1) % n;
            if (openCulet)
                Facet(true, lower[k * thirds], pavilionPoint[k], openCuletPoints[k], openCuletPoints[previousSide], pavilionPoint[previousSide]);
            else
                Facet(true, lower[k * thirds], pavilionPoint[k], tip, pavilionPoint[previousSide]);          // corner kite
        }
        if (openCulet) Facet(true, openCuletPoints);

        var mesh = Normalize(vertices, triangles, facetIds);
        mesh.FacetCount = counted;
        return mesh;
    }

    // Builds a faceted cut from its real facet layout:
    //  - brilliant crown: table + n stars + n bezels + 2n upper girdle facets; brilliant pavilion: n mains + 2n lower girdle
    //    facets (+ the culet facet when open). With n = 8 this is the standard 57 (58) facet brilliant.
    //  - step cuts: rows of n trapezoids from the table to the girdle and from the girdle down to the keel.
    // The thin girdle band is not counted among the facets.
    private static GemMesh BuildFaceted(string shape, string style, bool openCulet)
    {
        if (style == "Princesse") return BuildPrincess(shape, openCulet);

        var plan = PlanFor(shape, style);
        var angles = MainAngles(shape, style);
        var n = angles.Length;
        var vertices = new List<Vector3>();
        var triangles = new List<int[]>();
        var facetIds = new List<int>();
        var nextFacet = 1;
        var counted = 0;
        const float girdleHalf = 0.02f;

        // An oval is a round brilliant stretched along its length: same facet layout, stretched when the mesh is scaled to the
        // stone's dimensions (so the facets stay evenly spread instead of bunching up at the ends).
        float Outline(float degrees) => shape == "Ovale" ? 1f : Mathf.Max(0.05f, GemPixelArt.BoundaryRadius(shape, degrees * Mathf.Deg2Rad));
        float Next(int k) => k + 1 < n ? angles[k + 1] : angles[0] + 360f;
        float MidAngle(int k) => (angles[k] + Next(k)) * 0.5f;
        float HalfSector(int k) => (Next(k) - angles[k]) * 0.5f * Mathf.Deg2Rad;

        int Add(float radius, float degrees, float z)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var r = radius * Outline(degrees);
            vertices.Add(new Vector3(Mathf.Cos(radians) * r, Mathf.Sin(radians) * r, z));
            return vertices.Count - 1;
        }

        int AddAt(Vector3 position)
        {
            vertices.Add(position);
            return vertices.Count - 1;
        }

        // A flat facet from a convex polygon (fan), all of its triangles sharing one id.
        void Facet(bool count, params int[] polygon)
        {
            var id = nextFacet++;
            if (count) counted++;
            for (var i = 1; i + 1 < polygon.Length; i++)
                AddTriangle(vertices, triangles, facetIds, polygon[0], polygon[i], polygon[i + 1], id);
        }

        var tableZ = girdleHalf + plan.CrownHeight;
        var crownSlope = plan.CrownHeight / (1f - plan.TableRadius);
        var culetZ = -girdleHalf - plan.PavilionDepth;
        var brilliantCrown = plan.CrownSteps == 0;
        var brilliantPavilion = plan.PavilionSteps == 0;
        var hasMids = brilliantCrown || brilliantPavilion;

        // Girdle (top): main points, plus mid points when a brilliant crown or pavilion needs them.
        var girdle = new int[n];
        var girdleMid = new int[n];
        for (var k = 0; k < n; k++) girdle[k] = Add(1f, angles[k], girdleHalf);
        if (hasMids)
        {
            for (var k = 0; k < n; k++)
            {
                if (brilliantCrown && brilliantPavilion)
                {
                    girdleMid[k] = Add(1f, MidAngle(k), girdleHalf);                       // on the outline: smooth girdle
                }
                else
                {
                    var a = vertices[girdle[k]];
                    var b = vertices[girdle[(k + 1) % n]];
                    girdleMid[k] = AddAt(new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, girdleHalf));   // on the straight edge
                }
            }
        }

        // ---- Crown ----
        if (brilliantCrown)
        {
            var starRadius = plan.StarRadius;
            var table = new int[n];
            var star = new int[n];
            for (var k = 0; k < n; k++)
            {
                table[k] = Add(plan.TableRadius, angles[k], tableZ);
                // The star points sit on the planes of the bezels so each bezel is flat.
                var z = tableZ - crownSlope * (starRadius * Mathf.Cos(HalfSector(k)) - plan.TableRadius);
                star[k] = Add(starRadius, MidAngle(k), z);
            }

            Facet(true, table);
            for (var k = 0; k < n; k++)
            {
                var next = (k + 1) % n;
                var previous = (k + n - 1) % n;
                Facet(true, table[k], table[next], star[k]);                                   // star
                Facet(true, table[k], star[previous], girdle[k], star[k]);                     // bezel
                Facet(true, star[k], girdle[k], girdleMid[k]);                                 // upper girdle facets, in pairs
                Facet(true, star[k], girdleMid[k], girdle[next]);
            }
        }
        else
        {
            var rows = plan.CrownSteps;
            var fractions = CrownStepFractions[rows];
            var ring = new int[rows + 1][];
            ring[rows] = girdle;
            for (var j = 0; j < rows; j++)
            {
                ring[j] = new int[n];
                var radius = plan.TableRadius + (1f - plan.TableRadius) * j / rows;
                for (var k = 0; k < n; k++) ring[j][k] = Add(radius, angles[k], Mathf.Lerp(tableZ, girdleHalf, fractions[j]));
            }

            Facet(true, ring[0]);
            for (var j = 0; j < rows; j++)
                for (var k = 0; k < n; k++)
                    Facet(true, ring[j][k], ring[j][(k + 1) % n], ring[j + 1][(k + 1) % n], ring[j + 1][k]);
        }

        // ---- Girdle band (not counted) ----
        var lowerGirdle = new int[n];
        var lowerMid = new int[n];
        for (var k = 0; k < n; k++)
        {
            var top = vertices[girdle[k]];
            lowerGirdle[k] = AddAt(new Vector3(top.x, top.y, -girdleHalf));
            if (hasMids)
            {
                var topMid = vertices[girdleMid[k]];
                lowerMid[k] = AddAt(new Vector3(topMid.x, topMid.y, -girdleHalf));
            }
        }
        for (var k = 0; k < n; k++)
        {
            var next = (k + 1) % n;
            if (hasMids)
            {
                Facet(false, girdle[k], girdleMid[k], lowerMid[k], lowerGirdle[k]);
                Facet(false, girdleMid[k], girdle[next], lowerGirdle[next], lowerMid[k]);
            }
            else
            {
                Facet(false, girdle[k], girdle[next], lowerGirdle[next], lowerGirdle[k]);
            }
        }

        // ---- Pavilion ----
        if (brilliantPavilion)
        {
            const float sideRadius = 0.68f;
            const float openCuletRadius = 0.07f;
            var side = new int[n];
            for (var k = 0; k < n; k++)
            {
                var z = culetZ + plan.PavilionDepth * (sideRadius * Mathf.Cos(HalfSector(k)));
                side[k] = Add(sideRadius, MidAngle(k), z);
            }

            var useOpenCulet = openCulet && !plan.SplitMains;
            int tip = -1;
            var culet = new int[n];
            if (useOpenCulet)
            {
                for (var k = 0; k < n; k++)
                {
                    var z = culetZ + plan.PavilionDepth * (openCuletRadius * Mathf.Cos(HalfSector(k)));
                    culet[k] = Add(openCuletRadius, MidAngle(k), z);
                }
            }
            else
            {
                tip = AddAt(new Vector3(0f, 0f, culetZ));
            }

            var halfway = new int[n];
            if (plan.SplitMains)
            {
                for (var k = 0; k < n; k++)
                    halfway[k] = AddAt((vertices[tip] + vertices[side[k]]) * 0.5f);
            }

            for (var k = 0; k < n; k++)
            {
                var previous = (k + n - 1) % n;
                if (useOpenCulet)
                    Facet(true, culet[previous], side[previous], lowerGirdle[k], side[k], culet[k]);        // main
                else if (plan.SplitMains)
                {
                    Facet(true, side[previous], lowerGirdle[k], side[k], halfway[k], halfway[previous]);    // upper half of the main
                    Facet(true, tip, halfway[previous], halfway[k]);                                        // lower half
                }
                else
                    Facet(true, tip, side[previous], lowerGirdle[k], side[k]);                              // main
            }
            if (useOpenCulet) Facet(true, culet);

            for (var k = 0; k < n; k++)
            {
                var next = (k + 1) % n;
                Facet(true, side[k], lowerGirdle[k], lowerMid[k]);                                          // lower girdle facets, in pairs
                Facet(true, side[k], lowerMid[k], lowerGirdle[next]);
            }
        }
        else
        {
            var rows = plan.PavilionSteps;
            const float keelRadius = 0.07f;
            var fractions = PavilionStepFractions[rows];
            var ring = new int[rows + 1][];
            ring[0] = lowerGirdle;
            for (var j = 1; j <= rows; j++)
            {
                ring[j] = new int[n];
                var radius = 1f - (1f - keelRadius) * j / rows;
                var z = Mathf.Lerp(-girdleHalf, culetZ, fractions[j]);
                for (var k = 0; k < n; k++) ring[j][k] = Add(radius, angles[k], z);
            }

            for (var j = 0; j < rows; j++)
                for (var k = 0; k < n; k++)
                    Facet(true, ring[j][k], ring[j][(k + 1) % n], ring[j + 1][(k + 1) % n], ring[j + 1][k]);
            Facet(true, ring[rows]);                                                                        // the keel
        }

        var mesh = Normalize(vertices, triangles, facetIds);
        mesh.FacetCount = counted;
        return mesh;
    }

    // The mesh of a crystal habit (octahedron, cube, prism...), for a rough stone that has one; null for an unknown habit.
    public static GemMesh GetHabit(string habitId)
    {
        if (string.IsNullOrEmpty(habitId)) return null;
        var key = $"habit|{habitId}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var mesh = HabitMesh.Build(habitId);
        Cache[key] = mesh;
        return mesh;
    }

    // Lumpy, angular rock for rough stones.
    public static GemMesh GetRough(int seed)
    {
        var key = $"rough|{seed}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var rng = new System.Random(seed);
        var a = (float)rng.NextDouble() * 6.28f;
        var b = (float)rng.NextDouble() * 6.28f;
        var c = (float)rng.NextDouble() * 6.28f;
        var rings = new[]
        {
            new Ring(0f, 0.42f), new Ring(0.50f, 0.36f), new Ring(0.85f, 0.14f), new Ring(1.0f, 0.0f),
            new Ring(0.86f, -0.26f), new Ring(0.50f, -0.40f), new Ring(0f, -0.46f),
        };

        const int segments = 14;
        var mesh = BuildRings(rings, segments, theta =>
        {
            return 0.78f + 0.14f * Mathf.Sin(2f * theta + a) + 0.08f * Mathf.Sin(3f * theta + b) + 0.06f * Mathf.Sin(5f * theta + c);
        }, rng, 0.16f, 0.09f);

        Cache[key] = mesh;
        return mesh;
    }

    private static Ring[] Profile(string style)
    {
        switch (style)
        {
            case "Cabochon":
                return new[]
                {
                    new Ring(0f, 0.64f), new Ring(0.20f, 0.62f), new Ring(0.40f, 0.56f), new Ring(0.58f, 0.47f), new Ring(0.72f, 0.37f),
                    new Ring(0.84f, 0.26f), new Ring(0.93f, 0.15f), new Ring(1.0f, 0.04f), new Ring(1.0f, -0.02f),
                    new Ring(0.96f, -0.10f), new Ring(0f, -0.10f),
                };
            case "À degrés":
            case "Émeraude":
            case "Émeraude (à degrés)":
                return new[]
                {
                    new Ring(0f, 0.30f), new Ring(0.42f, 0.30f), new Ring(0.58f, 0.24f), new Ring(0.74f, 0.17f), new Ring(0.88f, 0.10f),
                    new Ring(1.0f, 0.04f), new Ring(1.0f, -0.03f), new Ring(0.82f, -0.14f), new Ring(0.64f, -0.27f),
                    new Ring(0.46f, -0.40f), new Ring(0.28f, -0.50f), new Ring(0f, -0.55f),
                };
            case "Ascher":
                return new[]
                {
                    new Ring(0f, 0.34f), new Ring(0.30f, 0.34f), new Ring(0.43f, 0.29f), new Ring(0.55f, 0.24f), new Ring(0.67f, 0.19f),
                    new Ring(0.78f, 0.13f), new Ring(0.89f, 0.07f), new Ring(1.0f, 0.03f), new Ring(1.0f, -0.03f),
                    new Ring(0.84f, -0.14f), new Ring(0.68f, -0.26f), new Ring(0.52f, -0.38f), new Ring(0.36f, -0.48f), new Ring(0f, -0.54f),
                };
            case "Baguette":
                return new[]
                {
                    new Ring(0f, 0.26f), new Ring(0.45f, 0.26f), new Ring(0.65f, 0.17f), new Ring(0.85f, 0.08f), new Ring(1.0f, 0.03f),
                    new Ring(1.0f, -0.03f), new Ring(0.80f, -0.20f), new Ring(0.55f, -0.36f), new Ring(0f, -0.46f),
                };
            case "Mixte":
                return new[]
                {
                    new Ring(0f, 0.32f), new Ring(0.40f, 0.32f), new Ring(0.64f, 0.25f, 0.06f), new Ring(0.78f, 0.18f), new Ring(0.90f, 0.11f),
                    new Ring(1.0f, 0.04f), new Ring(1.0f, -0.03f), new Ring(0.82f, -0.14f), new Ring(0.62f, -0.28f),
                    new Ring(0.40f, -0.42f), new Ring(0f, -0.52f),
                };
            case "Radiant":
                return new[]
                {
                    new Ring(0f, 0.32f), new Ring(0.40f, 0.32f), new Ring(0.70f, 0.22f, 0.06f), new Ring(0.86f, 0.12f), new Ring(1.0f, 0.04f),
                    new Ring(1.0f, -0.03f), new Ring(0.80f, -0.22f), new Ring(0.50f, -0.44f, 0.05f), new Ring(0f, -0.58f),
                };
            case "Princesse":
                return new[]
                {
                    new Ring(0f, 0.30f), new Ring(0.34f, 0.30f), new Ring(0.52f, 0.23f), new Ring(0.70f, 0.16f), new Ring(0.86f, 0.09f),
                    new Ring(1.0f, 0.03f), new Ring(1.0f, -0.03f), new Ring(0.75f, -0.22f), new Ring(0.40f, -0.44f), new Ring(0f, -0.58f),
                };
            case "Ciseaux":
                return new[]
                {
                    new Ring(0f, 0.32f), new Ring(0.30f, 0.32f), new Ring(0.55f, 0.24f, 0.09f), new Ring(0.80f, 0.14f, 0.06f), new Ring(1.0f, 0.04f),
                    new Ring(1.0f, -0.03f), new Ring(0.78f, -0.22f, 0.05f), new Ring(0.45f, -0.44f), new Ring(0f, -0.58f),
                };
            default:        // Brilliant
                return new[]
                {
                    new Ring(0f, 0.34f), new Ring(0.44f, 0.34f), new Ring(0.62f, 0.27f, 0.07f), new Ring(0.88f, 0.12f, 0.03f),
                    new Ring(1.0f, 0.05f), new Ring(1.0f, -0.03f), new Ring(0.78f, -0.22f, 0.04f), new Ring(0.45f, -0.45f, 0.05f), new Ring(0f, -0.60f),
                };
        }
    }

    private static GemMesh Build(string shape, Ring[] rings, int segments)
    {
        return BuildRings(rings, segments, theta => Mathf.Max(0.05f, GemPixelArt.BoundaryRadius(shape, theta)), null, 0f, 0f);
    }

    // Revolves the profile around the outline: ring i sits at `rho * outlineRadius(theta)` and height `z`.
    private static GemMesh BuildRings(Ring[] rings, int segments, System.Func<float, float> outline,
        System.Random jitter, float radiusJitter, float zJitter)
    {
        var vertices = new List<Vector3>();
        var ringStart = new int[rings.Length];
        var ringSize = new int[rings.Length];

        for (var r = 0; r < rings.Length; r++)
        {
            ringStart[r] = vertices.Count;
            if (rings[r].Rho <= 0f)
            {
                ringSize[r] = 1;
                vertices.Add(new Vector3(0f, 0f, rings[r].Z));
                continue;
            }

            ringSize[r] = segments;
            for (var k = 0; k < segments; k++)
            {
                var theta = k * 2f * Mathf.PI / segments;
                var rho = rings[r].Rho + (k % 2 == 0 ? rings[r].Alternate : -rings[r].Alternate);
                var z = rings[r].Z;
                if (jitter != null)
                {
                    rho *= 1f + ((float)jitter.NextDouble() * 2f - 1f) * radiusJitter;
                    z += ((float)jitter.NextDouble() * 2f - 1f) * zJitter;
                }

                var radius = outline(theta) * rho;
                vertices.Add(new Vector3(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius, z));
            }
        }

        var triangles = new List<int[]>();
        var facetIds = new List<int>();
        var nextFacet = 1;

        for (var r = 0; r + 1 < rings.Length; r++)
        {
            var flat = Mathf.Approximately(rings[r].Z, rings[r + 1].Z) && (ringSize[r] == 1 || ringSize[r + 1] == 1);
            var sharedId = nextFacet++;

            if (ringSize[r] == 1 || ringSize[r + 1] == 1)
            {
                // Fan between a single center vertex and a ring.
                var centerRing = ringSize[r] == 1 ? r : r + 1;
                var otherRing = centerRing == r ? r + 1 : r;
                for (var k = 0; k < segments; k++)
                {
                    var next = (k + 1) % segments;
                    AddTriangle(vertices, triangles, facetIds, ringStart[centerRing], ringStart[otherRing] + k, ringStart[otherRing] + next,
                        flat ? sharedId : nextFacet++);
                }
                continue;
            }

            for (var k = 0; k < segments; k++)
            {
                var next = (k + 1) % segments;
                var a = ringStart[r] + k;
                var b = ringStart[r] + next;
                var c = ringStart[r + 1] + next;
                var d = ringStart[r + 1] + k;
                var id = nextFacet++;
                AddTriangle(vertices, triangles, facetIds, a, d, c, id);
                AddTriangle(vertices, triangles, facetIds, a, c, b, id);
            }
        }

        return Normalize(vertices, triangles, facetIds);
    }

    // Adds a triangle wound so that its normal points away from the middle of the stone.
    private static void AddTriangle(List<Vector3> vertices, List<int[]> triangles, List<int> facetIds, int i0, int i1, int i2, int id)
    {
        var p0 = vertices[i0];
        var p1 = vertices[i1];
        var p2 = vertices[i2];
        var normal = Vector3.Cross(p1 - p0, p2 - p0);
        var centroid = (p0 + p1 + p2) / 3f;
        if (Vector3.Dot(normal, centroid) < 0f)
            triangles.Add(new[] { i0, i2, i1 });
        else
            triangles.Add(new[] { i0, i1, i2 });
        facetIds.Add(id);
    }

    // Scales x and y so the outline's bounding box spans -1..1, and z so the stone's height does too.
    private static GemMesh Normalize(List<Vector3> vertices, List<int[]> triangles, List<int> facetIds)
    {
        float maxX = 0.01f, maxY = 0.01f, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var v in vertices)
        {
            maxX = Mathf.Max(maxX, Mathf.Abs(v.x));
            maxY = Mathf.Max(maxY, Mathf.Abs(v.y));
            minZ = Mathf.Min(minZ, v.z);
            maxZ = Mathf.Max(maxZ, v.z);
        }

        var middleZ = (minZ + maxZ) * 0.5f;
        var halfZ = Mathf.Max(0.01f, (maxZ - minZ) * 0.5f);
        var result = new Vector3[vertices.Count];
        for (var i = 0; i < vertices.Count; i++)
            result[i] = new Vector3(vertices[i].x / maxX, vertices[i].y / maxY, (vertices[i].z - middleZ) / halfZ);

        return new GemMesh { Vertices = result, Triangles = triangles.ToArray(), FacetIds = facetIds.ToArray() };
    }
}
