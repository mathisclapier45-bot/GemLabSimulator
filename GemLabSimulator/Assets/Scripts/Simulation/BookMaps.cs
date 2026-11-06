using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// A map for a page of the Atlas: the outline of the country (or of the region) with its neighbours, a grid of latitudes and longitudes, and the marks of the deposit.
public class BookMapMarker
{
    public Vector2 Pixel;            // from the top left corner of the map
    public string Label;
}

public class BookMap
{
    public Sprite Sprite;
    public string Caption;
    public readonly List<BookMapMarker> Markers = new List<BookMapMarker>();
}

// The maps of the deposits. The outlines come from Natural Earth (public domain, 1:50 million), simplified into Assets/Resources/BookMaps/countries.txt, and are
// drawn in a texture when a page is opened. The marks are the best known gem regions of each country (approximate positions, from gemology), or the place the
// name of the deposit gives (Mogok, Muzo, the Urals...).
public static class BookMaps
{
    public const int Width = 344;
    public const int Height = 176;

    private class Country
    {
        public string Name;
        public string Continent;
        public readonly List<Vector2[]> Rings = new List<Vector2[]>();       // (longitude, latitude)
    }

    private struct Mark
    {
        public float Lat, Lon;
        public string Label;
    }

    private class Place
    {
        public string Key;
        public Mark[] Marks;
    }

    // How a map is framed: the countries drawn in the colour of the deposit, the part of them that counts, and the box of the map.
    private class View
    {
        public string[] Main;                 // names of countries; or null
        public string Continent;              // all the countries of a continent are drawn as the main ones
        public Vector4? Clip;                 // the rings of the main countries that do not touch this box (minLon, minLat, maxLon, maxLat) are left out
        public Vector4? Box;                  // the box of the map, when it is not the one of the main countries
        public string Title;                  // the caption, when it is not the name of the country
    }

    private static readonly Dictionary<string, Country> Countries = new Dictionary<string, Country>();
    private static readonly Dictionary<string, BookMap> Cache = new Dictionary<string, BookMap>();
    private static bool loaded;

    private static Mark M(float lat, float lon, string label) => new Mark { Lat = lat, Lon = lon, Label = label };

    private static readonly Dictionary<string, View> Views = new Dictionary<string, View>
    {
        { "United States of America", new View { Main = new[] { "United States of America" }, Clip = new Vector4(-126f, 23f, -65f, 50f), Title = "États-Unis" } },
        { "Russia", new View { Main = new[] { "Russia" }, Clip = new Vector4(19f, 41f, 180f, 82f), Title = "Russie" } },
        { "Norway", new View { Main = new[] { "Norway" }, Clip = new Vector4(4f, 57f, 32f, 72f), Title = "Norvège" } },
        { "Chile", new View { Main = new[] { "Chile" }, Clip = new Vector4(-76f, -56f, -66f, -17f), Title = "Chili" } },
        { "Spain", new View { Main = new[] { "Spain" }, Clip = new Vector4(-10f, 35f, 5f, 44.5f), Title = "Espagne" } },
        { "Mexico", new View { Main = new[] { "Mexico" }, Clip = new Vector4(-118f, 14f, -86f, 33f), Title = "Mexique" } },
        { "Japan", new View { Main = new[] { "Japan" }, Clip = new Vector4(128f, 30f, 146f, 46f), Title = "Japon" } },
        { "New Zealand", new View { Main = new[] { "New Zealand" }, Clip = new Vector4(165f, -48f, 179f, -34f), Title = "Nouvelle-Zélande" } },
        { "United Kingdom", new View { Main = new[] { "United Kingdom" }, Clip = new Vector4(-11f, 49.8f, 2.5f, 61f), Title = "Royaume-Uni" } },
        { "Australia", new View { Main = new[] { "Australia" }, Clip = new Vector4(112f, -45f, 155f, -9f), Title = "Australie" } },
        { "Czechia", new View { Main = new[] { "Czechia" }, Title = "République tchèque" } },
        { "Myanmar", new View { Main = new[] { "Myanmar" }, Title = "Myanmar" } },
        { "@Africa", new View { Continent = "Africa", Title = "Afrique" } },
        { "@SouthAfrica", new View { Main = new[] { "South Africa", "Namibia", "Botswana", "Zimbabwe", "Mozambique", "Zambia", "Angola", "Lesotho", "Eswatini", "Malawi" }, Box = new Vector4(10f, -36f, 42f, -8f), Title = "Afrique australe" } },
        { "@Arctic", new View { Main = new[] { "Canada", "United States of America" }, Box = new Vector4(-170f, 50f, -55f, 76f), Title = "Alaska et nord du Canada" } },
        { "@Baltic", new View { Main = new[] { "Poland", "Lithuania", "Latvia", "Estonia", "Russia", "Sweden", "Finland", "Denmark", "Germany" }, Clip = new Vector4(4f, 49f, 36f, 68f), Box = new Vector4(4f, 49f, 36f, 67f), Title = "Mer Baltique" } },
        { "@Kashmir", new View { Main = new[] { "India", "Pakistan" }, Box = new Vector4(66f, 23f, 86f, 41f), Title = "Cachemire" } },
        { "@Med", new View { Main = new[] { "Italy", "Spain", "France", "Greece", "Tunisia", "Algeria", "Morocco", "Libya", "Egypt", "Turkey" }, Clip = new Vector4(-10f, 29f, 38f, 47f), Box = new Vector4(-10f, 29f, 38f, 47f), Title = "Mer Méditerranée" } },
        { "@World", new View { Box = new Vector4(-180f, -58f, 180f, 82f), Title = "Le monde" } },
    };

    // The deposits of the game, with where they are on the map.
    private static readonly Dictionary<string, Place> Places = BuildPlaces();

    private static Dictionary<string, Place> BuildPlaces()
    {
        var d = new Dictionary<string, Place>();
        void P(string origin, string key, params Mark[] marks) => d[origin] = new Place { Key = key, Marks = marks };

        P("Afghanistan", "Afghanistan", M(36.1f, 70.8f, "Badakhshan"));
        P("Afghanistan (Nuristan)", "Afghanistan", M(35.3f, 70.9f, "Nuristan"));
        P("Afghanistan (Panjshir)", "Afghanistan", M(35.4f, 69.7f, "Panjshir"));
        P("Afrique (défenses d'éléphant)", "@Africa", M(0f, 24f, "Afrique centrale"));
        P("Afrique (zones humides)", "@Africa", M(-19.3f, 22.9f, "Zones humides"));
        P("Afrique australe", "@SouthAfrica", M(-27f, 24f, "Afrique australe"));
        P("Afrique du Sud", "South Africa", M(-28.7f, 24.8f, "Kimberley"));
        P("Angleterre", "United Kingdom", M(54.5f, -0.6f, "Whitby"));
        P("Arctique (Alaska, Canada)", "@Arctic", M(65f, -150f, "Alaska"), M(66f, -105f, "Nord du Canada"));
        P("Arctique (Groenland)", "Greenland", M(67f, -50f, "Groenland"));
        P("Argentine", "Argentina", M(-27.4f, -66.4f, "Capillitas"));
        P("Australie", "Australia", M(-29f, 134.75f, "Coober Pedy"));
        P("Australie (mers du Sud)", "Australia", M(-17.96f, 122.24f, "Broome"));
        P("Autre gisement", "@World");
        P("Autriche", "Austria", M(47.2f, 12.4f, "Habachtal"));
        P("Baltique", "@Baltic", M(54.9f, 19.9f, "Côte de la Baltique"));
        P("Birmanie", "Myanmar", M(22.92f, 96.5f, "Mogok"));
        P("Birmanie (Mogok)", "Myanmar", M(22.92f, 96.5f, "Mogok"));
        P("Bolivie", "Bolivia", M(-17f, -58f, "Anahí"));
        P("Botswana", "Botswana", M(-21.3f, 25.4f, "Orapa"));
        P("Brésil", "Brazil", M(-18.6f, -43.9f, "Minas Gerais"));
        P("Brésil (Paraíba)", "Brazil", M(-6.9f, -36.5f, "Paraíba"));
        P("Cachemire (Kashmir)", "@Kashmir", M(33.5f, 76.3f, "Padar"));
        P("Cambodge (Ratanakiri)", "Cambodia", M(13.9f, 107f, "Ratanakiri"));
        P("Canada", "Canada", M(64.7f, -110.6f, "Territoires du Nord-Ouest"));
        P("Caraïbes / Océan Indien", "@World", M(18.5f, -74f, "Caraïbes"), M(-5f, 70f, "Océan Indien"));
        P("Chili", "Chile", M(-30.6f, -71.2f, "Ovalle"));
        P("Chine", "China", M(37.1f, 79.9f, "Hotan"));
        P("Chine (eau douce)", "China", M(29.7f, 120.2f, "Zhuji"));
        P("Colombie (Muzo/Chivor)", "Colombia", M(5.53f, -74.1f, "Muzo"), M(4.88f, -73.37f, "Chivor"));
        P("Divers gisements mondiaux", "@World");
        P("Espagne", "Spain", M(43.4f, -5.8f, "Asturies"));
        P("Finlande", "Finland", M(60.7f, 28f, "Ylämaa"));
        P("Guatemala", "Guatemala", M(15f, -90f, "Vallée du Motagua"));
        P("Inde", "India", M(17.4f, 78.4f, "Golconda"));
        P("Iran", "Iran", M(36.3f, 58.8f, "Nishapur"));
        P("Islande", "Iceland", M(65f, -14f, "Helgustaðir"));
        P("Japon (Akoya)", "Japan", M(34.3f, 136.8f, "Baie d'Ago"));
        P("Japon (momo)", "Japan", M(33.5f, 133.6f, "Kôchi"));
        P("Kenya", "Kenya", M(-3.4f, 38.4f, "Taita"));
        P("Madagascar", "Madagascar", M(-22.7f, 45.2f, "Ilakaka"));
        P("Mexique", "Mexico", M(20.7f, -100.4f, "Querétaro"));
        P("Mozambique", "Mozambique", M(-13.1f, 38.9f, "Montepuez"));
        P("Myanmar", "Myanmar", M(22.92f, 96.5f, "Mogok"));
        P("Myanmar (birmite)", "Myanmar", M(26.2f, 96.6f, "Vallée de Hukawng"));
        P("Méditerranée", "@Med", M(38f, 11f, "Mer Méditerranée"));
        P("Namibie", "Namibia", M(-21.9f, 15.85f, "Karibib"));
        P("Nigeria", "Nigeria", M(9.9f, 8.9f, "Plateau de Jos"));
        P("Norvège", "Norway", M(59.05f, 10.05f, "Larvik"));
        P("Nouvelle-Zélande", "New Zealand", M(-42.7f, 171f, "Hokitika"));
        P("Népal", "Nepal", M(28.1f, 85.1f, "Ganesh Himal"));
        P("Pakistan", "Pakistan", M(35.3f, 75.6f, "Skardu"));
        P("Pakistan (vallée de Kashmir/Sopat)", "Pakistan", M(34.6f, 73.9f, "Vallée de Kashmir"));
        P("Polynésie française", "Fr. Polynesia", M(-14.4f, -145.9f, "Manihi"));
        P("Pérou", "Peru", M(-15.4f, -74.6f, "Acari"));
        P("Russie", "Russia", M(57f, 61.5f, "Monts Oural"));
        P("Russie (Baïkal)", "Russia", M(53.5f, 108f, "Lac Baïkal"));
        P("Russie (Monts Oural)", "Russia", M(57f, 61.5f, "Monts Oural"));
        P("Russie (Oural)", "Russia", M(57f, 61.5f, "Oural"));
        P("Russie (Sibérie)", "Russia", M(62.5f, 113.9f, "Iakoutie"));
        P("République Tchèque (Bohême)", "Czechia", M(50.5f, 13.9f, "Bohême"));
        P("République dominicaine", "Dominican Rep.", M(19.5f, -70.7f, "Cordillère septentrionale"));
        P("Sibérie (pergélisol)", "Russia", M(68f, 135f, "Iakoutie"));
        P("Sri Lanka", "Sri Lanka", M(6.68f, 80.4f, "Ratnapura"));
        P("Sri Lanka (Ceylan)", "Sri Lanka", M(6.68f, 80.4f, "Ratnapura"));
        P("Suisse", "Switzerland", M(46.6f, 8.3f, "Alpes"));
        P("Suède", "Sweden", M(62f, 15f, "Suède"));
        P("Tanzanie", "Tanzania", M(-3.6f, 37.1f, "Merelani"));
        P("Taïwan", "Taiwan", M(23.6f, 121.4f, "Hualien"));
        P("Thaïlande", "Thailand", M(12.6f, 102.1f, "Chanthaburi"));
        P("Thaïlande (Chanthaburi)", "Thailand", M(12.6f, 102.1f, "Chanthaburi"));
        P("Ukraine", "Ukraine", M(50.6f, 28f, "Volhynie"));
        P("Uruguay", "Uruguay", M(-30.4f, -56.5f, "Artigas"));
        P("Vietnam", "Vietnam", M(22.1f, 104.7f, "Luc Yen"));
        P("Zambie", "Zambia", M(-13.3f, 28f, "Kagem"));
        P("Écosse", "United Kingdom", M(57.1f, -3.7f, "Cairngorms"));
        P("États-Unis", "United States of America", M(46.8f, -110.5f, "Montana"));
        P("États-Unis (Arizona)", "United States of America", M(33.3f, -110.5f, "San Carlos"));
        P("États-Unis (Californie)", "United States of America", M(36.4f, -120.8f, "San Benito"));
        P("États-Unis (Dakota du Sud)", "United States of America", M(44f, -103.5f, "Black Hills"));
        P("États-Unis (Idaho)", "United States of America", M(47f, -116.4f, "Emerald Creek"));
        P("États-Unis (Maine)", "United States of America", M(44.3f, -70.5f, "Mont Mica"));
        P("États-Unis (Utah, Wah Wah Mountains)", "United States of America", M(38.5f, -113.4f, "Wah Wah"));
        P("Éthiopie", "Ethiopia", M(11.9f, 39.6f, "Wollo"));
        return d;
    }

    // Whether a deposit has a map (an industrial process has none).
    public static bool Has(string origin) => origin != null && Places.ContainsKey(origin);

    // The map of a deposit, or null.
    public static BookMap For(string origin)
    {
        if (origin == null || !Places.TryGetValue(origin, out var place)) return null;
        var cacheKey = origin;
        if (Cache.TryGetValue(cacheKey, out var cached) && cached != null && cached.Sprite != null) return cached;

        Load();
        var map = Render(place, origin);
        Cache[cacheKey] = map;
        return map;
    }

    // ---- Data ----------------------------------------------------------------------------------------------------------

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        var asset = Resources.Load<TextAsset>("BookMaps/countries");
        if (asset == null) return;

        Country current = null;
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("C|", StringComparison.Ordinal))
            {
                var parts = line.Split('|');
                current = new Country { Name = parts[1], Continent = parts.Length > 2 ? parts[2] : "" };
                Countries[current.Name] = current;
            }
            else if (line.StartsWith("R|", StringComparison.Ordinal) && current != null)
            {
                var numbers = line.Substring(2).Split(' ');
                var count = numbers.Length / 2;
                var ring = new Vector2[count];
                int x = 0, y = 0;
                for (var i = 0; i < count; i++)
                {
                    x += int.Parse(numbers[i * 2], CultureInfo.InvariantCulture);
                    y += int.Parse(numbers[i * 2 + 1], CultureInfo.InvariantCulture);
                    ring[i] = new Vector2(x / 100f, y / 100f);
                }
                current.Rings.Add(ring);
            }
        }
    }

    private static bool Touches(Vector2[] ring, Vector4 box)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in ring)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        return maxX >= box.x && minX <= box.z && maxY >= box.y && minY <= box.w;
    }

    // ---- Drawing -------------------------------------------------------------------------------------------------------

    private static readonly Color32 Sea = new Color32(188, 208, 204, 255);
    private static readonly Color32 Grid = new Color32(170, 192, 188, 255);
    private static readonly Color32 NeighbourFill = new Color32(232, 222, 188, 255);
    private static readonly Color32 NeighbourLine = new Color32(158, 136, 98, 255);
    private static readonly Color32 MainFill = new Color32(222, 184, 104, 255);
    private static readonly Color32 MainLine = new Color32(102, 66, 30, 255);

    private static BookMap Render(Place place, string origin)
    {
        Views.TryGetValue(place.Key, out var view);
        if (view == null) view = new View { Main = new[] { place.Key }, Title = place.Key };

        // The main countries, and the rings of them that count.
        var mains = new List<Country>();
        if (view.Continent != null)
        {
            foreach (var country in Countries.Values) if (country.Continent == view.Continent) mains.Add(country);
        }
        else if (view.Main != null)
        {
            foreach (var name in view.Main) if (Countries.TryGetValue(name, out var found)) mains.Add(found);
        }

        var mainRings = new List<Vector2[]>();
        foreach (var country in mains)
            foreach (var ring in country.Rings)
                if (!view.Clip.HasValue || Touches(ring, view.Clip.Value)) mainRings.Add(ring);

        // The box of the map: the one asked for, or the one of the main rings, with a margin.
        float minLon, minLat, maxLon, maxLat;
        if (view.Box.HasValue)
        {
            minLon = view.Box.Value.x; minLat = view.Box.Value.y; maxLon = view.Box.Value.z; maxLat = view.Box.Value.w;
        }
        else
        {
            minLon = minLat = float.MaxValue;
            maxLon = maxLat = float.MinValue;
            foreach (var ring in mainRings)
                foreach (var p in ring)
                {
                    minLon = Mathf.Min(minLon, p.x); maxLon = Mathf.Max(maxLon, p.x);
                    minLat = Mathf.Min(minLat, p.y); maxLat = Mathf.Max(maxLat, p.y);
                }
            if (minLon > maxLon) { minLon = -180f; maxLon = 180f; minLat = -58f; maxLat = 82f; }
            var padLon = Mathf.Max(0.8f, (maxLon - minLon) * 0.12f);
            var padLat = Mathf.Max(0.8f, (maxLat - minLat) * 0.12f);
            minLon -= padLon; maxLon += padLon; minLat -= padLat; maxLat += padLat;
        }

        // The deposits count in the box too (a mark must be on the map).
        foreach (var mark in place.Marks)
        {
            minLon = Mathf.Min(minLon, mark.Lon - 0.5f); maxLon = Mathf.Max(maxLon, mark.Lon + 0.5f);
            minLat = Mathf.Min(minLat, mark.Lat - 0.5f); maxLat = Mathf.Max(maxLat, mark.Lat + 0.5f);
        }

        // Equirectangular projection, fitted to the frame (a degree of longitude is shorter away from the equator).
        var midLon = (minLon + maxLon) / 2f;
        var midLat = (minLat + maxLat) / 2f;
        var cos = Mathf.Max(0.2f, Mathf.Cos(midLat * Mathf.Deg2Rad));
        var scale = Mathf.Min(Width / ((maxLon - minLon) * cos), Height / (maxLat - minLat));
        Vector2 Project(Vector2 lonLat) => new Vector2(Width / 2f + (lonLat.x - midLon) * cos * scale, Height / 2f - (lonLat.y - midLat) * scale);

        var viewLon = Width / (cos * scale) / 2f;
        var viewLat = Height / scale / 2f;
        var visible = new Vector4(midLon - viewLon, midLat - viewLat, midLon + viewLon, midLat + viewLat);

        var pixels = new Color32[Width * Height];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = Sea;

        DrawGrid(pixels, visible, Project);

        // The neighbours first, then the main countries on top.
        var mainSet = new HashSet<Country>(mains);
        foreach (var country in Countries.Values)
        {
            if (mainSet.Contains(country)) continue;
            var rings = new List<Vector2[]>();
            foreach (var ring in country.Rings) if (Touches(ring, visible)) rings.Add(Projected(ring, Project));
            if (rings.Count == 0) continue;
            Fill(pixels, rings, NeighbourFill);
            foreach (var ring in rings) Stroke(pixels, ring, NeighbourLine, 1);
        }

        var main = new List<Vector2[]>();
        foreach (var ring in mainRings) main.Add(Projected(ring, Project));
        Fill(pixels, main, MainFill);
        foreach (var ring in main) Stroke(pixels, ring, MainLine, 2);

        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var flipped = new Color32[pixels.Length];
        for (var y = 0; y < Height; y++)
            Array.Copy(pixels, y * Width, flipped, (Height - 1 - y) * Width, Width);
        texture.SetPixels32(flipped);
        texture.Apply();

        var map = new BookMap
        {
            Sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100f),
            Caption = view.Title ?? BookFlags.CountryOf(origin),
        };
        foreach (var mark in place.Marks)
            map.Markers.Add(new BookMapMarker { Pixel = Project(new Vector2(mark.Lon, mark.Lat)), Label = mark.Label });
        return map;
    }

    private static Vector2[] Projected(Vector2[] ring, Func<Vector2, Vector2> project)
    {
        var result = new Vector2[ring.Length];
        for (var i = 0; i < ring.Length; i++) result[i] = project(ring[i]);
        return result;
    }

    // A grid of meridians and parallels, every 5, 10 or 20 degrees depending on the size of the map.
    private static void DrawGrid(Color32[] pixels, Vector4 visible, Func<Vector2, Vector2> project)
    {
        var span = Mathf.Max(visible.z - visible.x, (visible.w - visible.y) * 1.5f);
        var step = span > 140f ? 30f : span > 60f ? 10f : span > 25f ? 5f : 2f;
        for (var lon = Mathf.Ceil(visible.x / step) * step; lon <= visible.z; lon += step)
        {
            var x = Mathf.RoundToInt(project(new Vector2(lon, 0f)).x);
            if (x >= 0 && x < Width) for (var y = 0; y < Height; y++) pixels[y * Width + x] = Grid;
        }
        for (var lat = Mathf.Ceil(visible.y / step) * step; lat <= visible.w; lat += step)
        {
            var y = Mathf.RoundToInt(project(new Vector2(0f, lat)).y);
            if (y >= 0 && y < Height) for (var x = 0; x < Width; x++) pixels[y * Width + x] = Grid;
        }
    }

    // Fills the rings with an even-odd rule, so that a hole (Lesotho in South Africa) stays a hole.
    private static void Fill(Color32[] pixels, List<Vector2[]> rings, Color32 color)
    {
        var crossings = new List<float>();
        for (var y = 0; y < Height; y++)
        {
            var yc = y + 0.5f;
            crossings.Clear();
            foreach (var ring in rings)
            {
                for (var i = 0; i < ring.Length; i++)
                {
                    var a = ring[i];
                    var b = ring[(i + 1) % ring.Length];
                    if ((a.y <= yc && b.y > yc) || (b.y <= yc && a.y > yc))
                        crossings.Add(a.x + (yc - a.y) * (b.x - a.x) / (b.y - a.y));
                }
            }
            if (crossings.Count < 2) continue;
            crossings.Sort();
            for (var k = 0; k + 1 < crossings.Count; k += 2)
            {
                var x0 = Mathf.Max(0, Mathf.CeilToInt(crossings[k] - 0.5f));
                var x1 = Mathf.Min(Width - 1, Mathf.FloorToInt(crossings[k + 1] - 0.5f));
                for (var x = x0; x <= x1; x++) pixels[y * Width + x] = color;
            }
        }
    }

    private static void Stroke(Color32[] pixels, Vector2[] ring, Color32 color, int thickness)
    {
        for (var i = 0; i < ring.Length; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Length];
            // A segment far away from the map is skipped.
            if ((a.x < -50f && b.x < -50f) || (a.x > Width + 50f && b.x > Width + 50f) || (a.y < -50f && b.y < -50f) || (a.y > Height + 50f && b.y > Height + 50f)) continue;
            Line(pixels, Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.y), color, thickness);
        }
    }

    private static void Line(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color, int thickness)
    {
        var dx = Mathf.Abs(x1 - x0);
        var dy = -Mathf.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        for (var guard = 0; guard < 2000; guard++)
        {
            Plot(pixels, x0, y0, color, thickness);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * error;
            if (e2 >= dy) { error += dy; x0 += sx; }
            if (e2 <= dx) { error += dx; y0 += sy; }
        }
    }

    private static void Plot(Color32[] pixels, int x, int y, Color32 color, int thickness)
    {
        for (var oy = 0; oy < thickness; oy++)
            for (var ox = 0; ox < thickness; ox++)
            {
                var px = x + ox;
                var py = y + oy;
                if (px >= 0 && px < Width && py >= 0 && py < Height) pixels[py * Width + px] = color;
            }
    }
}
