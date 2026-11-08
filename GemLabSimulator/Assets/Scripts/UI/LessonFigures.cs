using UnityEngine;

// The provisional drawings of the lessons, made of rectangles, lines and circles (the board is 480 x 340, the origin in the middle). Each step of a lesson names a figure and
// a "focus" (what to show in it). When the artist makes a picture for a step, the picture replaces the drawing (see LessonStepData.image).
public static class LessonFigures
{
    private static readonly Color Ink = new Color(0.92f, 0.94f, 0.98f, 1f);
    private static readonly Color Dim = new Color(0.50f, 0.55f, 0.63f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f, 1f);
    private static readonly Color Cyan = new Color(0.45f, 0.85f, 1f, 1f);
    private static readonly Color Stone = new Color(0.66f, 0.36f, 0.86f, 1f);
    private static readonly Color StoneLight = new Color(0.82f, 0.62f, 0.96f, 1f);
    private static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f, 1f);
    private static readonly Color Water = new Color(0.30f, 0.55f, 0.90f, 0.40f);
    private static readonly Color Dark = new Color(0.02f, 0.02f, 0.03f, 1f);

    public static void Draw(string id, LessonCanvas c, int focus, float t)
    {
        switch (id)
        {
            case "criteria": Criteria(c, focus, t); break;
            case "taxonomy": Taxonomy(c, focus, t); break;
            case "hardness": Hardness(c, focus, t); break;
            case "tenacity": Tenacity(c, focus, t); break;
            case "stability": Stability(c, focus, t); break;
            case "loupe": Loupe(c, focus, t); break;
            case "carat": Carat(c, focus, t); break;
            case "hydro": Hydro(c, focus, t); break;
            case "systems": Systems(c, focus, t); break;
            case "polariscope": Polariscope(c, focus, t); break;
            default: c.Label("Schéma à venir", Vector2.zero, 16, Dim); break;
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    // ---- What a gemstone is: five criteria around the stone ----------------------------------------------------------------------

    private static readonly string[] CriteriaNames = { "Beauté", "Rareté", "Durabilité", "Valeur", "Acceptabilité" };

    private static readonly Color[] CriteriaColors =
    {
        new Color(0.90f, 0.45f, 0.70f, 1f),
        new Color(0.95f, 0.75f, 0.25f, 1f),
        new Color(0.40f, 0.70f, 0.95f, 1f),
        new Color(0.45f, 0.85f, 0.50f, 1f),
        new Color(0.75f, 0.55f, 0.95f, 1f),
    };

    private static void Criteria(LessonCanvas c, int focus, float t)
    {
        var pulse = 0.5f + 0.5f * Mathf.Sin(t * 3f);
        for (var i = 0; i < 5; i++)
        {
            var angle = (90f + 72f * i) * Mathf.Deg2Rad;
            var position = new Vector2(Mathf.Cos(angle) * 150f, Mathf.Sin(angle) * 112f);
            var selected = focus == i;
            var shown = focus < 0 || selected;
            var color = shown ? CriteriaColors[i] : WithAlpha(CriteriaColors[i], 0.22f);
            c.Line(Vector2.zero, position, selected ? 4f : 2f, color);
            var size = selected ? new Vector2(134f + 8f * pulse, 44f + 4f * pulse) : new Vector2(122f, 38f);
            c.Rect(position, size, color);
            c.Label(CriteriaNames[i], position, selected ? 16 : 14, shown ? Color.white : Dim, 130f);
        }
        c.Rect(Vector2.zero, new Vector2(58f, 58f), Cyan, 45f);
        c.Rect(Vector2.zero, new Vector2(30f, 30f), new Color(0.85f, 0.97f, 1f, 1f), 45f);
        c.Label("GEMME", new Vector2(0f, -52f), 12, Gold, 100f);
    }

    // ---- The family tree of the gemstones ------------------------------------------------------------------------------------------

    private static readonly Color NaturalColor = new Color(0.30f, 0.62f, 0.38f, 1f);
    private static readonly Color ArtificialColor = new Color(0.30f, 0.48f, 0.82f, 1f);

    private static void TreeNode(LessonCanvas c, string label, Vector2 center, Vector2 size, Color color, bool dim, int fontSize = 14)
    {
        c.Rect(center, size, dim ? WithAlpha(color, 0.18f) : color);
        c.Label(label, center, fontSize, dim ? Dim : Color.white, size.x - 8f);
    }

    // An elbow from the right edge of a parent to the left edge of a child.
    private static void TreeLink(LessonCanvas c, Vector2 parent, float parentHalf, Vector2 child, float childHalf, bool dim)
    {
        var color = dim ? new Color(0.5f, 0.55f, 0.63f, 0.25f) : new Color(0.80f, 0.85f, 0.92f, 0.9f);
        var from = new Vector2(parent.x + parentHalf, parent.y);
        var to = new Vector2(child.x - childHalf, child.y);
        var middle = (from.x + to.x) / 2f;
        c.Line(from, new Vector2(middle, from.y), 2f, color);
        c.Line(new Vector2(middle, from.y), new Vector2(middle, to.y), 2f, color);
        c.Line(new Vector2(middle, to.y), to, 2f, color);
    }

    // focus 0: the whole tree, level by level; 1: the natural gemstones; 2: the ones that are not natural; 3: only the root.
    private static void Taxonomy(LessonCanvas c, int focus, float t)
    {
        var reveal = focus == 0 ? t : 99f;
        var dimNatural = focus == 2;
        var dimArtificial = focus == 1;

        var root = new Vector2(-205f, 0f);
        if (focus == 3)
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(t * 3f);
            c.Rect(new Vector2(0f, 20f), new Vector2(86f, 86f), Cyan, 45f);
            c.Rect(new Vector2(0f, 20f), new Vector2(46f, 46f), new Color(0.85f, 0.97f, 1f, 1f), 45f);
            c.Rect(new Vector2(0f, -70f), new Vector2(150f + 8f * pulse, 40f), Gold);
            c.Label("GEMMES", new Vector2(0f, -70f), 20, new Color(0.2f, 0.15f, 0.05f, 1f), 150f);
            return;
        }

        var nat = new Vector2(-100f, 73f);
        var non = new Vector2(-100f, -78f);
        var cris = new Vector2(20f, 105f);
        var noncris = new Vector2(20f, 42f);
        var mineral = new Vector2(160f, 105f);
        var amorph = new Vector2(160f, 62f);
        var organic = new Vector2(160f, 22f);
        var synth = new Vector2(20f, -30f);
        var nocounter = new Vector2(20f, -78f);
        var assembled = new Vector2(20f, -126f);

        // The links first, so the boxes cover their ends.
        if (reveal > 0.8f)
        {
            TreeLink(c, root, 42f, nat, 52f, dimNatural);
            TreeLink(c, root, 42f, non, 52f, dimArtificial);
        }
        if (reveal > 1.6f)
        {
            TreeLink(c, nat, 52f, cris, 62f, dimNatural);
            TreeLink(c, nat, 52f, noncris, 62f, dimNatural);
            TreeLink(c, non, 52f, synth, 62f, dimArtificial);
            TreeLink(c, non, 52f, nocounter, 62f, dimArtificial);
            TreeLink(c, non, 52f, assembled, 62f, dimArtificial);
        }
        if (reveal > 2.4f)
        {
            TreeLink(c, cris, 62f, mineral, 70f, dimNatural);
            TreeLink(c, noncris, 62f, amorph, 70f, dimNatural);
            TreeLink(c, noncris, 62f, organic, 70f, dimNatural);
        }

        TreeNode(c, "GEMMES", root, new Vector2(84f, 34f), Gold, false, 15);
        if (reveal > 0.8f)
        {
            TreeNode(c, "Naturelles", nat, new Vector2(104f, 34f), NaturalColor, dimNatural);
            TreeNode(c, "Non naturelles", non, new Vector2(104f, 34f), ArtificialColor, dimArtificial);
        }
        if (reveal > 1.6f)
        {
            TreeNode(c, "Cristallines", cris, new Vector2(124f, 32f), NaturalColor, dimNatural);
            TreeNode(c, "Non cristallines", noncris, new Vector2(124f, 32f), NaturalColor, dimNatural);
            TreeNode(c, "Synthétiques", synth, new Vector2(124f, 32f), ArtificialColor, dimArtificial);
            TreeNode(c, "Sans contrepartie dans la nature", nocounter, new Vector2(124f, 46f), ArtificialColor, dimArtificial, 12);
            TreeNode(c, "Gemmes assemblées", assembled, new Vector2(124f, 32f), ArtificialColor, dimArtificial, 13);
        }
        if (reveal > 2.4f)
        {
            TreeNode(c, "Minéral", mineral, new Vector2(140f, 30f), NaturalColor, dimNatural);
            TreeNode(c, "Amorphe", amorph, new Vector2(140f, 30f), NaturalColor, dimNatural);
            TreeNode(c, "Matières organiques", organic, new Vector2(140f, 30f), NaturalColor, dimNatural, 13);
        }

        // Examples, on the right of the artificial ones.
        if (reveal > 3.2f)
        {
            var exampleColor = dimArtificial ? WithAlpha(Dim, 0.4f) : Ink;
            c.Label("rubis de synthèse", new Vector2(160f, -30f), 12, exampleColor, 140f);
            c.Label("YAG, zircone cubique", new Vector2(160f, -78f), 12, exampleColor, 140f);
            c.Label("doublets, triplets", new Vector2(160f, -126f), 12, exampleColor, 140f);
        }
    }

    // ---- Hardness: Mohs, the absolute scale, the scratch, the hardness pencils ---------------------------------------------------

    private static readonly string[] MohsNames = { "Talc", "Gypse", "Calcite", "Fluorite", "Apatite", "Orthose", "Quartz", "Topaze", "Corindon", "Diamant" };

    // The absolute hardness (Knoop, approximate values) of the ten minerals of the scale.
    private static readonly float[] Knoop = { 1f, 32f, 135f, 163f, 430f, 560f, 820f, 1340f, 2100f, 7000f };

    // focus 0: the relative scale of Mohs; 1: the absolute scale; 2: a harder stone scratches a softer one; 3: the hardness pencils.
    private static void Hardness(LessonCanvas c, int focus, float t)
    {
        if (focus == 2) { ScratchRule(c, t); return; }
        if (focus == 3) { Pencils(c, t); return; }
        if (focus == 4) { TestPlaces(c, t); return; }

        var absolute = focus == 1;
        const float baseY = -112f;
        for (var i = 0; i < 10; i++)
        {
            var x = -189f + i * 42f;
            var full = absolute ? Mathf.Max(3f, Knoop[i] / 7000f * 215f) : (i + 1) * 21.5f;
            var grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.2f - i * 0.06f));
            var height = full * grow;
            var color = Color.Lerp(new Color(0.45f, 0.75f, 0.95f, 1f), new Color(0.95f, 0.80f, 0.35f, 1f), i / 9f);
            c.Rect(new Vector2(x, baseY + height / 2f), new Vector2(30f, height), color);
            var label = absolute ? (i >= 6 ? Knoop[i].ToString("F0") : "") : (i + 1).ToString();
            c.Label(label, new Vector2(x, baseY + height + 11f), 12, Ink, 46f);
            c.Label(MohsNames[i], new Vector2(x, baseY - 14f), 10, Ink, 46f);
        }

        c.Label(absolute ? "Dureté absolue (Knoop, valeurs approchées)" : "Échelle de Mohs (relative : de 1 à 10)", new Vector2(0f, 152f), 15, Gold, 470f);
        if (absolute) c.Label("le diamant est environ 3 fois plus dur que le corindon", new Vector2(-30f, 105f), 12, Ink, 300f);
        else c.Label("chaque minéral raye ceux qui le précèdent", new Vector2(-70f, 105f), 12, Ink, 300f);
    }

    // A corundum scratches a quartz, but a quartz cannot scratch a corundum.
    private static void ScratchRule(LessonCanvas c, float t)
    {
        var cycle = (t % 4f) / 4f;
        var travel = Mathf.Clamp01(cycle / 0.6f);
        for (var row = 0; row < 2; row++)
        {
            var y = row == 0 ? 70f : -75f;
            var scratches = row == 0;
            var left = new Vector2(-110f, y);
            c.Rect(new Vector2(0f, y - 22f), new Vector2(260f, 44f), row == 0 ? new Color(0.62f, 0.52f, 0.78f, 1f) : new Color(0.85f, 0.35f, 0.40f, 1f));
            c.Label(row == 0 ? "Quartz (7)" : "Corindon (9)", new Vector2(-170f, y - 22f), 12, Ink, 90f);
            if (scratches && travel > 0f) c.Line(left + new Vector2(0f, -3f), left + new Vector2(220f * travel, -3f), 3f, new Color(1f, 1f, 1f, 0.9f));

            // The tip that is drawn across.
            var tip = left + new Vector2(220f * travel, 0f);
            c.Rect(tip + new Vector2(0f, 14f), new Vector2(14f, 30f), row == 0 ? new Color(0.85f, 0.35f, 0.40f, 1f) : new Color(0.62f, 0.52f, 0.78f, 1f), 0f);
            c.Circle(tip, 5f, Ink);
            c.Label(row == 0 ? "pointe de corindon (9)  →  raye" : "pointe de quartz (7)  →  ne raye pas", new Vector2(40f, y + 34f), 12, row == 0 ? Gold : Dim, 300f);
        }
        c.Label("La pierre la plus dure raye la plus tendre", new Vector2(0f, 152f), 15, Gold, 450f);
    }

    // The hardness test scratches: it is done on a rough stone, or under the base of a cabochon, never on a cut stone.
    private static void TestPlaces(LessonCanvas c, float t)
    {
        var green = new Color(0.40f, 0.90f, 0.50f, 1f);
        var red = new Color(0.95f, 0.35f, 0.30f, 1f);
        var background = new Color(0.05f, 0.06f, 0.09f, 1f);
        var pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);

        // A rough crystal.
        c.Rect(new Vector2(-150f, 20f), new Vector2(70f, 46f), Stone, 18f);
        c.Rect(new Vector2(-138f, 38f), new Vector2(40f, 34f), StoneLight, -14f);
        c.Rect(new Vector2(-165f, 6f), new Vector2(34f, 30f), new Color(0.50f, 0.28f, 0.68f, 1f), 30f);

        // A cabochon: a dome on a flat base, tested under the base.
        c.Circle(new Vector2(0f, 14f), 36f, Stone);
        c.Rect(new Vector2(0f, -22f), new Vector2(90f, 44f), background);
        c.Rect(new Vector2(0f, -5f), new Vector2(76f, 12f), new Color(0.50f, 0.28f, 0.68f, 1f));
        c.Circle(new Vector2(-12f, 30f), 8f, new Color(1f, 1f, 1f, 0.35f));

        // A faceted stone.
        c.Rect(new Vector2(150f, 15f), new Vector2(70f, 70f), Stone, 45f);
        c.Rect(new Vector2(150f, 15f), new Vector2(40f, 40f), StoneLight, 45f);

        // The marks: good for the two first, forbidden for the cut stone.
        for (var i = 0; i < 2; i++)
        {
            var x = -150f + i * 150f;
            c.Line(new Vector2(x - 14f, -72f), new Vector2(x - 4f, -84f), 5f, green);
            c.Line(new Vector2(x - 4f, -84f), new Vector2(x + 16f, -60f), 5f, green);
        }
        c.Circle(new Vector2(150f, -72f), 22f + 3f * pulse, WithAlpha(red, 0.25f));
        c.Line(new Vector2(136f, -58f), new Vector2(164f, -86f), 5f, red);
        c.Line(new Vector2(136f, -86f), new Vector2(164f, -58f), 5f, red);

        c.Label("Brut", new Vector2(-150f, 85f), 15, Gold, 120f);
        c.Label("Cabochon", new Vector2(0f, 85f), 15, Gold, 120f);
        c.Label("Pierre taillée", new Vector2(150f, 85f), 15, Gold, 140f);
        c.Label("sur n'importe quelle face", new Vector2(-150f, -108f), 11, Ink, 130f);
        c.Label("sous la base", new Vector2(0f, -108f), 11, Ink, 130f);
        c.Label("jamais : elle serait rayée", new Vector2(150f, -108f), 11, red, 140f);
        c.Label("Un test destructif", new Vector2(0f, 148f), 16, Gold, 440f);
    }

    // The hardness pencils: points of known hardness are drawn across the stone; the first one that leaves a scratch gives the hardness.
    private static void Pencils(LessonCanvas c, float t)
    {
        const float stoneHardness = 6.5f;
        var pencilHardness = new[] { 5f, 6f, 7f, 8f };
        var cycle = t % 14f;
        c.Rect(new Vector2(0f, -45f), new Vector2(300f, 92f), new Color(0.62f, 0.52f, 0.78f, 1f));
        c.Label("Pierre à tester", new Vector2(-190f, -45f), 12, Ink, 70f);

        string result = "";
        for (var k = 0; k < 4; k++)
        {
            var rowY = -17f - k * 21f;
            var start = k * 3f;
            var progress = Mathf.Clamp01((cycle - start) / 1.6f);
            var scratches = pencilHardness[k] > stoneHardness;
            if (cycle >= start)
            {
                var done = cycle >= start + 1.6f;
                if (scratches && progress > 0f) c.Line(new Vector2(-130f, rowY), new Vector2(-130f + 260f * progress, rowY), 3f, new Color(1f, 1f, 1f, 0.9f));
                if (!done || cycle < start + 3f)
                    result = $"Crayon {pencilHardness[k]:0} : " + (scratches ? "raye la pierre" : "ne raye pas");
            }
            if (cycle >= start && cycle < start + 1.8f)
            {
                // The pencil, tilted, its tip on the stone.
                var tip = new Vector2(-130f + 260f * progress, rowY);
                var direction = new Vector2(Mathf.Cos(35f * Mathf.Deg2Rad), Mathf.Sin(35f * Mathf.Deg2Rad));
                c.Rect(tip + direction * 40f, new Vector2(80f, 10f), new Color(0.90f, 0.72f, 0.30f, 1f), 35f);
                c.Rect(tip + direction * 4f, new Vector2(10f, 6f), Dark, 35f);
            }
        }

        // The set of pencils, the one in use lit.
        for (var k = 0; k < 4; k++)
        {
            var active = cycle >= k * 3f && cycle < k * 3f + 3f;
            var x = -105f + k * 70f;
            c.Rect(new Vector2(x, 112f), new Vector2(54f, 30f), active ? Gold : new Color(0.30f, 0.33f, 0.40f, 1f));
            c.Label(pencilHardness[k].ToString("0"), new Vector2(x, 112f), 16, active ? Dark : Ink, 54f);
        }
        c.Label("Crayons de dureté", new Vector2(-190f, 112f), 11, Dim, 80f);
        c.Label(result, new Vector2(0f, -122f), 15, Gold, 440f);
        if (cycle > 10.5f) c.Label("Le 6 ne raye pas, le 7 raye : la dureté est entre 6 et 7", new Vector2(0f, -148f), 13, Ink, 460f);
    }

    // ---- Tenacity: the shock, the cleavage, the fracture ---------------------------------------------------------------------------

    private static readonly string[] TenacityNames = { "Exceptionnelle", "Très bonne", "Bonne", "Moyenne", "Faible" };
    private static readonly string[] TenacityExamples = { "jade néphrite", "jadéite, corindon", "diamant, quartz", "émeraude, topaze", "opale, fluorite, kunzite" };

    // focus 0: a hard stone that breaks; 1: cleavage; 2: fracture; 3: the scale; 4: a tough stone.
    private static void Tenacity(LessonCanvas c, int focus, float t)
    {
        if (focus == 3)
        {
            for (var i = 0; i < 5; i++)
            {
                var y = 100f - i * 48f;
                var width = 260f - i * 40f;
                c.Label(TenacityNames[i], new Vector2(-165f, y), 13, Ink, 110f);
                c.Rect(new Vector2(-95f + width / 2f, y), new Vector2(width, 30f), Color.Lerp(new Color(0.35f, 0.75f, 0.45f, 1f), new Color(0.85f, 0.35f, 0.35f, 1f), i / 4f));
                c.Label(TenacityExamples[i], new Vector2(-95f + width / 2f, y), 12, Color.white, width - 6f);
            }
            c.Label("Plus la barre est longue, plus la pierre encaisse les chocs", new Vector2(0f, -145f), 13, Gold, 460f);
            return;
        }

        var cycle = t % 4.5f;
        var stonePos = new Vector2(0f, -40f);
        var hit = cycle > 0.7f && cycle < 4f;
        var baseColor = focus == 4 ? new Color(0.30f, 0.62f, 0.42f, 1f) : Stone;

        // The hammer falls, hits and bounces up.
        var hammerY = cycle < 0.7f ? Mathf.Lerp(135f, 9f, cycle / 0.7f) : Mathf.Lerp(9f, 135f, Mathf.Clamp01((cycle - 0.9f) / 0.9f));
        if (focus == 4 && cycle >= 0.7f) hammerY = Mathf.Lerp(9f, 135f, Mathf.Clamp01((cycle - 0.75f) / 0.5f));
        c.Rect(new Vector2(0f, hammerY + 60f), new Vector2(10f, 90f), Wood);
        c.Rect(new Vector2(0f, hammerY), new Vector2(76f, 28f), new Color(0.65f, 0.68f, 0.74f, 1f));

        if (focus == 1)
        {
            // Cleavage: the stone splits along a flat plane, in one piece after the other.
            var shift = hit ? Mathf.Min(10f, (cycle - 0.7f) * 40f) : 0f;
            c.Rect(stonePos + new Vector2(-31f - shift, 0f), new Vector2(58f, 70f), baseColor);
            c.Rect(stonePos + new Vector2(31f + shift, 0f), new Vector2(58f, 70f), baseColor);
            if (!hit)
                for (var i = -2; i <= 2; i++) c.Line(stonePos + new Vector2(i * 22f, -34f), stonePos + new Vector2(i * 22f, 34f), 1.5f, new Color(1f, 1f, 1f, 0.35f));
            c.Label("Clivage : la pierre se fend selon un plan lisse", new Vector2(0f, -122f), 14, Gold, 450f);
            return;
        }

        c.Rect(stonePos, new Vector2(120f, 70f), baseColor);
        if (hit && focus == 0)
        {
            // Cracks that start at the point of impact and spread.
            var top = stonePos + new Vector2(0f, 34f);
            c.Line(top, stonePos + new Vector2(-20f, 5f), 2.5f, Dark);
            c.Line(stonePos + new Vector2(-20f, 5f), stonePos + new Vector2(-34f, -22f), 2.5f, Dark);
            c.Line(top, stonePos + new Vector2(18f, 4f), 2.5f, Dark);
            c.Line(stonePos + new Vector2(18f, 4f), stonePos + new Vector2(30f, -24f), 2.5f, Dark);
            c.Label("Très dure, mais elle casse au choc", new Vector2(0f, -122f), 14, Gold, 450f);
        }
        else if (hit && focus == 2)
        {
            // Fracture: arcs like the inside of a shell, where a chip has come off.
            var corner = stonePos + new Vector2(42f, 12f);
            for (var r = 30; r >= 8; r -= 7)
            {
                c.Circle(corner, r, Dark);
                c.Circle(corner, r - 2f, baseColor);
            }
            c.Label("Cassure en coquille : un éclat irrégulier", new Vector2(0f, -122f), 14, Gold, 450f);
        }
        else if (focus == 4)
        {
            if (hit && cycle < 2.2f) c.Label("Rebond : rien ne se passe", new Vector2(0f, 6f), 12, Ink, 200f);
            c.Label("Une pierre tenace encaisse le choc sans casser", new Vector2(0f, -122f), 14, Gold, 450f);
        }
        else if (focus == 0)
        {
            c.Label("Dureté et ténacité ne sont pas la même chose", new Vector2(0f, -122f), 14, Gold, 450f);
        }
        else if (focus == 2)
        {
            c.Label("Cassure : la pierre se brise de façon irrégulière", new Vector2(0f, -122f), 14, Gold, 450f);
        }
    }

    // ---- Stability: light, heat, chemicals ------------------------------------------------------------------------------------------

    // focus 0: light; 1: heat; 2: chemicals and humidity; 3: the three together.
    private static void Stability(LessonCanvas c, int focus, float t)
    {
        if (focus == 3)
        {
            var titles = new[] { "Lumière", "Chaleur", "Produits chimiques\net humidité" };
            var colors = new[] { new Color(0.95f, 0.80f, 0.30f, 1f), new Color(0.90f, 0.45f, 0.25f, 1f), new Color(0.35f, 0.65f, 0.90f, 1f) };
            for (var i = 0; i < 3; i++)
            {
                var x = (i - 1) * 158f;
                c.Rect(new Vector2(x, 0f), new Vector2(146f, 220f), WithAlpha(colors[i], 0.25f));
                c.Circle(new Vector2(x, 50f), 34f, colors[i]);
                c.Label(titles[i], new Vector2(x, -50f), 15, Ink, 140f);
            }
            c.Label("La stabilité : tenir dans le temps", new Vector2(0f, 140f), 15, Gold, 440f);
            return;
        }

        var stone = new Vector2(0f, -25f);
        var phase = Mathf.Clamp01((t % 6f) / 5f);
        var rich = Stone;
        var pale = new Color(0.78f, 0.74f, 0.82f, 1f);

        if (focus == 0)
        {
            var sun = new Vector2(-170f, 100f);
            c.Circle(sun, 26f, new Color(1f, 0.88f, 0.35f, 1f));
            for (var i = 0; i < 10; i++)
            {
                var angle = i * 36f * Mathf.Deg2Rad;
                c.Line(sun + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 34f, sun + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 48f, 3f, new Color(1f, 0.88f, 0.35f, 1f));
            }
            c.Line(sun + new Vector2(30f, -22f), stone + new Vector2(-30f, 30f), 3f, new Color(1f, 0.95f, 0.6f, 0.5f));
            c.Rect(stone, new Vector2(100f, 100f), Color.Lerp(rich, pale, phase), 45f);
            c.Rect(new Vector2(-150f, -120f), new Vector2(44f, 30f), rich);
            c.Rect(new Vector2(150f, -120f), new Vector2(44f, 30f), Color.Lerp(rich, pale, phase));
            c.Label("avant", new Vector2(-150f, -146f), 12, Ink, 70f);
            c.Label("après", new Vector2(150f, -146f), 12, Ink, 70f);
            c.Label("La lumière fait pâlir certaines couleurs", new Vector2(0f, 148f), 14, Gold, 440f);
        }
        else if (focus == 1)
        {
            c.Rect(stone, new Vector2(100f, 100f), Color.Lerp(rich, new Color(0.45f, 0.30f, 0.45f, 1f), phase), 45f);
            var flicker = 1f + 0.12f * Mathf.Sin(t * 14f);
            c.Circle(new Vector2(0f, -100f), 26f * flicker, new Color(0.95f, 0.45f, 0.15f, 1f));
            c.Circle(new Vector2(0f, -94f), 16f * flicker, new Color(1f, 0.82f, 0.30f, 1f));
            if (phase > 0.6f)
            {
                c.Line(stone + new Vector2(-10f, 40f), stone + new Vector2(6f, 5f), 3f, Dark);
                c.Line(stone + new Vector2(6f, 5f), stone + new Vector2(-4f, -25f), 3f, Dark);
            }
            c.Rect(new Vector2(150f, -20f), new Vector2(16f, 150f), new Color(0.85f, 0.85f, 0.90f, 1f));
            c.Rect(new Vector2(150f, -95f + 75f * phase), new Vector2(10f, 150f * phase), new Color(0.90f, 0.25f, 0.20f, 1f));
            c.Label("La chaleur ternit, fend ou ramollit", new Vector2(0f, 148f), 14, Gold, 440f);
        }
        else
        {
            c.Rect(stone, new Vector2(100f, 100f), rich, 45f);
            var dropY = Mathf.Lerp(140f, 20f, Mathf.Clamp01((t % 3f) / 1.2f));
            if ((t % 3f) < 1.4f) c.Circle(new Vector2(0f, dropY), 10f, new Color(0.35f, 0.75f, 0.95f, 1f));
            var damage = Mathf.Clamp01(((t % 6f) - 1.2f) / 3.5f);
            for (var i = 0; i < 6; i++)
            {
                var offset = new Vector2(Mathf.Sin(i * 2.1f) * 28f, Mathf.Cos(i * 1.7f) * 24f - 25f);
                c.Circle(offset, 2f + 6f * damage, new Color(0.88f, 0.86f, 0.80f, 0.85f));
            }
            c.Label("Acides, parfums, nettoyants, eau : la surface est attaquée", new Vector2(0f, 148f), 14, Gold, 450f);
        }
    }

    // ---- The loupe: a dirty stone, the chamois, the lens, the stone turned --------------------------------------------------------

    private static readonly Vector2[] Smudges =
    {
        new Vector2(-22f, 18f), new Vector2(12f, 26f), new Vector2(26f, -6f), new Vector2(-8f, -14f),
        new Vector2(-30f, -22f), new Vector2(6f, 4f), new Vector2(22f, -28f),
    };

    private static void Loupe(LessonCanvas c, int focus, float t)
    {
        var lens = focus == 2;
        var stonePos = lens ? new Vector2(-125f, 10f) : new Vector2(0f, 10f);
        var scale = lens ? 0.6f : 1f;
        var spin = focus == 3 ? t * 45f : 0f;

        c.Rect(stonePos, new Vector2(110f, 110f) * scale, Stone, 45f + spin);
        c.Rect(stonePos, new Vector2(70f, 70f) * scale, StoneLight, 45f - spin);
        c.Rect(stonePos, new Vector2(26f, 26f) * scale, Stone, 45f + spin * 2f);

        // The whitish traces of the dirt, which the chamois wipes away.
        float dirt = 0f;
        if (focus == 0) dirt = 1f;
        else if (focus == 1) dirt = Mathf.Clamp01(1f - (t % 4f) / 3f);
        for (var i = 0; i < Smudges.Length; i++)
            c.Circle(stonePos + Smudges[i] * scale, (11f + (i % 3) * 3f) * scale, new Color(1f, 1f, 1f, 0.55f * dirt));

        if (focus == 1)
        {
            var wipe = Mathf.Sin(t * 6f) * 34f;
            c.Rect(stonePos + new Vector2(wipe, 8f), new Vector2(72f, 36f), new Color(0.92f, 0.76f, 0.34f, 1f), 8f);
            c.Rect(stonePos + new Vector2(wipe, 8f), new Vector2(72f, 5f), new Color(0.70f, 0.54f, 0.20f, 1f), 8f);
            c.Label("Chamoisine", new Vector2(0f, -100f), 14, Gold);
        }
        else if (focus == 0)
        {
            c.Label("Pierre sale : voile blanchâtre en surface", new Vector2(0f, -100f), 14, Gold, 380f);
        }
        else if (focus == 3)
        {
            c.Label("On tourne la pierre dans tous les sens", new Vector2(0f, -100f), 14, Gold, 380f);
        }

        if (lens)
        {
            var lensCenter = new Vector2(75f, 10f);
            const float radius = 105f;
            c.Circle(lensCenter, radius, new Color(0.75f, 0.90f, 1f, 0.16f));
            for (var i = 0; i < 36; i++)
            {
                var angle = i * 10f * Mathf.Deg2Rad;
                c.Circle(lensCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, 3.5f, Gold);
            }
            c.Line(lensCenter + new Vector2(radius * 0.7f, -radius * 0.7f), lensCenter + new Vector2(radius * 1.25f, -radius * 1.25f), 8f, Wood);

            // The stone magnified, with what the loupe shows: a needle, a crystal, a fracture.
            c.Rect(lensCenter, new Vector2(150f, 150f), Stone, 45f);
            c.Rect(lensCenter, new Vector2(98f, 98f), StoneLight, 45f);
            c.Line(lensCenter + new Vector2(-40f, 18f), lensCenter + new Vector2(18f, 34f), 2f, new Color(0.30f, 0.15f, 0.40f, 1f));
            c.Line(lensCenter + new Vector2(-10f, -30f), lensCenter + new Vector2(30f, -8f), 2f, new Color(1f, 1f, 1f, 0.8f));
            c.Circle(lensCenter + new Vector2(22f, 22f), 4f, new Color(0.20f, 0.10f, 0.30f, 1f));
            c.Circle(lensCenter + new Vector2(-24f, -12f), 3f, Color.white);
            c.Label("Loupe 10×", new Vector2(75f, -118f), 14, Gold, 200f);
        }
    }

    // ---- The carat balance ------------------------------------------------------------------------------------------------------

    private static void Carat(LessonCanvas c, int focus, float t)
    {
        var pivot = new Vector2(0f, 20f);
        var tilt = 12f * Mathf.Exp(-0.6f * (t % 7f)) * Mathf.Cos(t * 5f);
        var direction = new Vector2(Mathf.Cos(tilt * Mathf.Deg2Rad), Mathf.Sin(tilt * Mathf.Deg2Rad));
        var left = pivot - direction * 130f;
        var right = pivot + direction * 130f;

        c.Rect(new Vector2(0f, -95f), new Vector2(170f, 14f), Wood);
        c.Rect(new Vector2(0f, -40f), new Vector2(10f, 110f), Wood);
        c.Line(left, right, 6f, new Color(0.75f, 0.70f, 0.55f, 1f));
        c.Circle(pivot, 8f, Gold);

        foreach (var end in new[] { left, right })
        {
            var pan = end + new Vector2(0f, -62f);
            c.Line(end, pan + new Vector2(-38f, 6f), 1.5f, Dim);
            c.Line(end, pan + new Vector2(38f, 6f), 1.5f, Dim);
            c.Rect(pan, new Vector2(88f, 8f), new Color(0.80f, 0.80f, 0.85f, 1f));
        }

        var leftPan = left + new Vector2(0f, -62f);
        var rightPan = right + new Vector2(0f, -62f);
        if (focus == 0)
        {
            // A carob seed against the weight of one carat.
            c.Circle(leftPan + new Vector2(0f, 12f), 10f, new Color(0.40f, 0.24f, 0.12f, 1f));
            c.Circle(rightPan + new Vector2(0f, 12f), 14f, Stone);
            c.Label("graine de caroube", leftPan + new Vector2(0f, 44f), 12, Gold, 130f);
            c.Label("1 carat", rightPan + new Vector2(0f, 44f), 12, Gold, 130f);
            c.Label("1 ct  =  0,2 g", new Vector2(0f, -135f), 18, Ink, 300f);
        }
        else
        {
            c.Circle(leftPan + new Vector2(0f, 20f), 18f, Stone);
            c.Rect(rightPan + new Vector2(0f, 12f), new Vector2(18f, 12f), Dim);
            c.Rect(new Vector2(0f, -135f), new Vector2(150f, 32f), Dark);
            var showGrams = focus >= 2 && (t % 3f) > 1.5f;
            c.Label(showGrams ? "0,470 g" : "2,35 ct", new Vector2(0f, -135f), 18, new Color(0.40f, 1f, 0.55f, 1f), 150f);
            if (focus >= 2) c.Label("1 g = 5 ct", new Vector2(150f, -135f), 14, Gold, 140f);
        }
    }

    // ---- The hydrostatic balance -------------------------------------------------------------------------------------------------

    private static void Hydro(LessonCanvas c, int focus, float t)
    {
        const float airWeight = 2.00f;
        const float waterWeight = 1.45f;

        // The beaker and its water.
        c.Rect(new Vector2(70f, -55f), new Vector2(136f, 140f), Water);
        c.Line(new Vector2(2f, 20f), new Vector2(2f, -125f), 3f, Ink);
        c.Line(new Vector2(138f, 20f), new Vector2(138f, -125f), 3f, Ink);
        c.Line(new Vector2(2f, -125f), new Vector2(138f, -125f), 3f, Ink);
        c.Line(new Vector2(2f, 15f), new Vector2(138f, 15f), 1.5f, WithAlpha(Cyan, 0.7f));

        // The stone hangs from the balance and goes down into the water.
        var progress = focus == 0 ? 0f : focus == 1 ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t % 6f) / 3.5f)) : 1f;
        var y = Mathf.Lerp(70f, -50f, progress);
        var x = 70f;
        c.Rect(new Vector2(x, 155f), new Vector2(60f, 12f), Wood);
        c.Line(new Vector2(x, 150f), new Vector2(x, y + 18f), 2f, Dim);
        c.Rect(new Vector2(x, y - 20f), new Vector2(44f, 5f), new Color(0.80f, 0.80f, 0.85f, 1f));
        c.Circle(new Vector2(x, y), 16f, Stone);

        var underwater = y < 15f;
        if (underwater)
        {
            c.Line(new Vector2(x + 40f, y), new Vector2(x + 40f, y + 40f), 3f, new Color(0.40f, 1f, 0.55f, 1f));
            c.Label("poussée de l'eau", new Vector2(x + 60f, y + 62f), 12, new Color(0.40f, 1f, 0.55f, 1f), 120f);
        }

        var shownWeight = underwater ? waterWeight : airWeight;
        c.Rect(new Vector2(-135f, 60f), new Vector2(130f, 34f), Dark);
        c.Label((underwater ? "P eau = " : "P air = ") + shownWeight.ToString("F2") + " ct", new Vector2(-135f, 60f), 15, new Color(0.40f, 1f, 0.55f, 1f), 130f);

        if (focus >= 2)
        {
            c.Label("d  =  P air  ÷  (P air − P eau)", new Vector2(-100f, -30f), 15, Ink, 200f);
            c.Label("d  =  2,00 ÷ 0,55  ≈  3,64", new Vector2(-100f, -70f), 15, Gold, 200f);
        }
    }

    // ---- The crystal systems -------------------------------------------------------------------------------------------------------

    private static readonly string[] SystemNames =
    {
        "Système cubique", "Système quadratique", "Système hexagonal", "Système trigonal",
        "Système orthorhombique", "Système monoclinique", "Système triclinique",
    };

    private static readonly string[] SystemCaptions =
    {
        "a = b = c   ·   angles de 90°",
        "a = b ≠ c   ·   angles de 90°",
        "a₁ = a₂ = a₃ à 120°   ·   c perpendiculaire",
        "trois axes égaux, inclinés de la même façon",
        "a ≠ b ≠ c   ·   angles de 90°",
        "a ≠ b ≠ c   ·   un angle différent de 90°",
        "a ≠ b ≠ c   ·   aucun angle de 90°",
    };

    private static readonly Color AxisA = new Color(1f, 0.45f, 0.40f, 1f);
    private static readonly Color AxisB = new Color(0.45f, 0.95f, 0.50f, 1f);
    private static readonly Color AxisC = new Color(0.45f, 0.70f, 1f, 1f);

    // A point of the crystal on the board: turned around the vertical axis, seen from a little above.
    private static Vector2 Project(Vector3 p, float spin, float scale, Vector2 origin)
    {
        var cs = Mathf.Cos(spin);
        var sn = Mathf.Sin(spin);
        var x = p.x * cs - p.y * sn;
        var y = p.x * sn + p.y * cs;
        return origin + new Vector2(x, y * 0.42f + p.z * 0.92f) * scale;
    }

    private static void Systems(LessonCanvas c, int focus, float t)
    {
        var spin = t * 0.6f;
        if (focus == 7) { Habits(c, t); return; }
        if (focus == 8) { Families(c); return; }

        var index = Mathf.Clamp(focus, 0, 6);
        if (focus < 0) index = 0;

        if (index == 2) HexPrism(c, new Vector2(0f, 8f), 0.8f, 0.75f, spin, 78f, true);
        else Parallelepiped(c, index, new Vector2(0f, 8f), spin, 78f);

        c.Label(focus < 0 ? "Les trois axes d'un cristal : a, b et c" : SystemNames[index], new Vector2(0f, 150f), 17, Gold, 440f);
        c.Label(focus < 0 ? "et les angles entre eux : α, β, γ" : SystemCaptions[index], new Vector2(0f, -150f), 14, Ink, 460f);
    }

    private static void Parallelepiped(LessonCanvas c, int system, Vector2 origin, float spin, float scale)
    {
        Vector3 a, b, d;
        switch (system)
        {
            case 1: a = new Vector3(1f, 0f, 0f); b = new Vector3(0f, 1f, 0f); d = new Vector3(0f, 0f, 1.7f); break;
            case 3:
                a = new Vector3(0.85f, 0f, 0.75f);
                b = new Vector3(-0.425f, 0.736f, 0.75f);
                d = new Vector3(-0.425f, -0.736f, 0.75f);
                break;
            case 4: a = new Vector3(1.6f, 0f, 0f); b = new Vector3(0f, 1f, 0f); d = new Vector3(0f, 0f, 1.3f); break;
            case 5: a = new Vector3(1.5f, 0f, 0f); b = new Vector3(0f, 1f, 0f); d = new Vector3(0.5f, 0f, 1.2f); break;
            case 6: a = new Vector3(1.5f, 0.25f, 0.1f); b = new Vector3(0.2f, 1f, 0.15f); d = new Vector3(0.45f, 0.3f, 1.2f); break;
            default: a = new Vector3(1.2f, 0f, 0f); b = new Vector3(0f, 1.2f, 0f); d = new Vector3(0f, 0f, 1.2f); break;
        }

        var middle = (a + b + d) / 2f;
        var edge = new Color(0.85f, 0.92f, 1f, 0.9f);
        for (var i = 0; i < 2; i++)
        {
            for (var j = 0; j < 2; j++)
            {
                for (var k = 0; k < 2; k++)
                {
                    var from = i * a + j * b + k * d - middle;
                    if (i == 0) c.Line(Project(from, spin, scale, origin), Project(from + a, spin, scale, origin), 2f, edge);
                    if (j == 0) c.Line(Project(from, spin, scale, origin), Project(from + b, spin, scale, origin), 2f, edge);
                    if (k == 0) c.Line(Project(from, spin, scale, origin), Project(from + d, spin, scale, origin), 2f, edge);
                }
            }
        }

        // The three axes, from the middle of the crystal and a little beyond its faces.
        var tipA = a * 0.95f;
        var tipB = b * 0.95f;
        var tipC = d * 0.95f;
        var center = Project(Vector3.zero, spin, scale, origin);
        c.Line(center, Project(tipA, spin, scale, origin), 3f, AxisA);
        c.Line(center, Project(tipB, spin, scale, origin), 3f, AxisB);
        c.Line(center, Project(tipC, spin, scale, origin), 3f, AxisC);
        c.Label("a", Project(tipA * 1.12f, spin, scale, origin), 15, AxisA, 30f);
        c.Label("b", Project(tipB * 1.12f, spin, scale, origin), 15, AxisB, 30f);
        c.Label("c", Project(tipC * 1.12f, spin, scale, origin), 15, AxisC, 30f);
    }

    // A hexagonal prism (the six faces of the beryl): three horizontal axes at 120 degrees and the vertical axis.
    private static void HexPrism(LessonCanvas c, Vector2 origin, float radius, float halfHeight, float spin, float scale, bool axes)
    {
        var edge = new Color(0.85f, 0.92f, 1f, 0.9f);
        for (var k = 0; k < 6; k++)
        {
            var a0 = k * 60f * Mathf.Deg2Rad;
            var a1 = (k + 1) * 60f * Mathf.Deg2Rad;
            var p0 = new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
            var p1 = new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);
            var up = new Vector3(0f, 0f, halfHeight);
            c.Line(Project(p0 + up, spin, scale, origin), Project(p1 + up, spin, scale, origin), 2f, edge);
            c.Line(Project(p0 - up, spin, scale, origin), Project(p1 - up, spin, scale, origin), 2f, edge);
            c.Line(Project(p0 + up, spin, scale, origin), Project(p0 - up, spin, scale, origin), 2f, edge);
        }

        if (!axes) return;
        var center = Project(Vector3.zero, spin, scale, origin);
        var colors = new[] { AxisA, AxisB, new Color(1f, 0.75f, 0.35f, 1f) };
        var names = new[] { "a₁", "a₂", "a₃" };
        for (var i = 0; i < 3; i++)
        {
            var angle = i * 120f * Mathf.Deg2Rad;
            var tip = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * 1.2f;
            c.Line(center, Project(tip, spin, scale, origin), 3f, colors[i]);
            c.Label(names[i], Project(tip * 1.15f, spin, scale, origin), 15, colors[i], 40f);
        }
        var top = new Vector3(0f, 0f, halfHeight * 1.35f);
        c.Line(center, Project(top, spin, scale, origin), 3f, AxisC);
        c.Label("c", Project(top * 1.12f, spin, scale, origin), 15, AxisC, 30f);
    }

    // The habit: the crystal grows faster in some directions than in others, which gives the needle, the prism or the plate.
    private static void Habits(LessonCanvas c, float t)
    {
        var grow = Mathf.SmoothStep(0.25f, 1f, Mathf.Clamp01((t % 6f) / 4f));
        var spin = t * 0.6f;
        HexPrism(c, new Vector2(-155f, 5f), 0.28f, 1.35f * grow, spin, 62f, false);
        HexPrism(c, new Vector2(0f, 5f), 0.55f, 0.8f * grow, spin, 62f, false);
        HexPrism(c, new Vector2(155f, 5f), 0.95f, 0.18f * grow, spin, 62f, false);
        c.Label("aiguille", new Vector2(-155f, -115f), 15, Gold, 120f);
        c.Label("prisme", new Vector2(0f, -115f), 15, Gold, 120f);
        c.Label("tablette", new Vector2(155f, -115f), 15, Gold, 120f);
        c.Label("croissance rapide selon c", new Vector2(-155f, -142f), 11, AxisC, 150f);
        c.Label("croissance équilibrée", new Vector2(0f, -142f), 11, Dim, 150f);
        c.Label("croissance rapide selon a", new Vector2(155f, -142f), 11, AxisA, 150f);
        c.Label("La vitesse de croissance dépend de la direction", new Vector2(0f, 148f), 15, Ink, 440f);
    }

    // The three optical families.
    private static void Families(LessonCanvas c)
    {
        var titles = new[] { "Isotrope", "Uniaxe", "Biaxe" };
        var systems = new[] { "cubique", "quadratique\nhexagonal\ntrigonal", "orthorhombique\nmonoclinique\ntriclinique" };
        var indices = new[] { "1 indice", "2 indices\n(nε, nω)", "3 indices\n(nα, nβ, nγ)" };
        var axes = new[] { "aucun axe optique", "1 axe optique", "2 axes optiques" };
        var colors = new[] { new Color(0.45f, 0.85f, 0.55f, 1f), new Color(0.95f, 0.75f, 0.30f, 1f), new Color(0.80f, 0.55f, 0.95f, 1f) };
        for (var i = 0; i < 3; i++)
        {
            var x = (i - 1) * 158f;
            c.Rect(new Vector2(x, 0f), new Vector2(146f, 300f), WithAlpha(colors[i], 0.22f));
            c.Rect(new Vector2(x, 128f), new Vector2(146f, 38f), colors[i]);
            c.Label(titles[i], new Vector2(x, 128f), 17, Color.white, 140f);
            c.Label(systems[i], new Vector2(x, 62f), 13, Ink, 140f);
            c.Label(indices[i], new Vector2(x, -20f), 13, Gold, 140f);
            c.Label(axes[i], new Vector2(x, -90f), 13, Ink, 140f);
        }
    }

    // ---- The polariscope and the conoscope -----------------------------------------------------------------------------------------

    private static void Polariscope(LessonCanvas c, int focus, float t)
    {
        if (focus >= 4)
        {
            Conoscope(c, focus, t);
            return;
        }

        var angle = t * 40f;
        var stonePos = new Vector2(-20f, 0f);

        // Without a stone, or with an isotropic one, the crossed filters give darkness; an anisotropic stone lights up four times per turn; an aggregate stays light.
        float brightness = 0f;
        if (focus == 2) brightness = Mathf.Pow(Mathf.Sin(angle * 2f * Mathf.Deg2Rad), 2f);
        else if (focus == 3) brightness = 0.8f;

        c.Circle(new Vector2(-205f, 0f), 15f, new Color(1f, 0.92f, 0.45f, 1f));
        c.Label("Lumière", new Vector2(-205f, -30f), 12, Gold, 80f);

        c.Rect(new Vector2(-125f, 0f), new Vector2(10f, 150f), new Color(0.55f, 0.75f, 1f, 0.55f));
        for (var i = -3; i <= 3; i++) c.Rect(new Vector2(-125f, i * 20f), new Vector2(3f, 14f), Ink);
        c.Label("Polariseur", new Vector2(-125f, -92f), 12, Gold, 90f);

        c.Rect(new Vector2(85f, 0f), new Vector2(10f, 150f), new Color(0.55f, 0.75f, 1f, 0.55f));
        for (var i = -3; i <= 3; i++) c.Rect(new Vector2(85f, i * 20f), new Vector2(14f, 3f), Ink);
        c.Label("Analyseur", new Vector2(85f, -92f), 12, Gold, 90f);

        var beam = new Color(1f, 0.95f, 0.6f, 0.9f);
        c.Line(new Vector2(-190f, 0f), new Vector2(-125f, 0f), 3f, beam);
        c.Line(new Vector2(-125f, 0f), stonePos, 3f, beam);
        if (focus >= 1)
        {
            c.Rect(stonePos, new Vector2(60f, 60f), Stone, 45f + (focus == 1 || focus == 2 || focus == 3 ? angle : 0f));
            c.Rect(stonePos, new Vector2(34f, 34f), StoneLight, 45f + angle);
        }
        c.Line(stonePos, new Vector2(85f, 0f), 3f, new Color(1f, 0.95f, 0.6f, Mathf.Max(0.9f * (focus == 0 ? 0.9f : 1f), 0.3f)));
        c.Line(new Vector2(85f, 0f), new Vector2(150f, 0f), 3f, new Color(1f, 0.95f, 0.6f, brightness * 0.9f));

        c.Label("Ce que voit l'œil", new Vector2(190f, -52f), 12, Gold, 110f);
        c.Circle(new Vector2(190f, 0f), 38f, new Color(0.55f, 0.60f, 0.62f, 1f));
        c.Circle(new Vector2(190f, 0f), 35f, WithAlpha(Color.Lerp(Dark, new Color(0.95f, 0.95f, 0.85f, 1f), brightness), 1f));

        string note;
        switch (focus)
        {
            case 0: note = "Deux filtres croisés : tout est noir"; break;
            case 1: note = "Pierre isotrope : noire, quel que soit l'angle"; break;
            case 2: note = "Pierre anisotrope : 4 éclairs par tour"; break;
            default: note = "Agrégat : toujours claire"; break;
        }
        c.Label(note, new Vector2(0f, 140f), 16, Ink, 440f);
    }

    private static readonly Color[] RingColors =
    {
        new Color(0.95f, 0.85f, 0.30f, 0.85f), new Color(0.90f, 0.35f, 0.35f, 0.85f),
        new Color(0.35f, 0.50f, 0.95f, 0.85f), new Color(0.40f, 0.85f, 0.50f, 0.85f),
    };

    private static void Conoscope(LessonCanvas c, int focus, float t)
    {
        var shimmer = 0.04f * Mathf.Sin(t * 2f);
        if (focus == 4)
        {
            c.Circle(Vector2.zero, 128f, Dark);
            for (var r = 118; r > 6; r -= 16)
                c.Circle(Vector2.zero, r * (1f + shimmer), RingColors[(r / 16) % RingColors.Length]);
            c.Rect(Vector2.zero, new Vector2(250f, 14f), Dark);
            c.Rect(Vector2.zero, new Vector2(14f, 250f), Dark);
            c.Label("Uniaxe : croix noire et anneaux colorés", new Vector2(0f, 150f), 16, Ink, 460f);
        }
        else
        {
            c.Circle(Vector2.zero, 128f, Dark);
            foreach (var x in new[] { -52f, 52f })
            {
                for (var r = 70; r > 6; r -= 14)
                    c.Circle(new Vector2(x, 0f), r * (1f + shimmer), RingColors[(r / 14) % RingColors.Length]);
                c.Rect(new Vector2(x, 0f), new Vector2(10f, 160f), WithAlpha(Dark, 0.95f), 18f);
            }
            c.Rect(Vector2.zero, new Vector2(250f, 10f), WithAlpha(Dark, 0.9f));
            c.Label("Biaxe : deux « yeux », deux axes optiques", new Vector2(0f, 150f), 16, Ink, 460f);
        }
        c.Label("schéma simplifié", new Vector2(0f, -148f), 11, Dim, 200f);
    }
}
