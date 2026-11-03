using System;
using System.Collections.Generic;
using UnityEngine;

// One diamond dealer of the 1st floor of the Diamond Quarter tower: a house with its own speciality and its own stock.
public class DiamondDealer
{
    public string Name;
    public string Specialty;
    public string Pitch;
    public int StockSize;
    public float CaratMin;
    public float CaratMax;
    public float CaratSkew;            // > 1 pushes the sizes towards the small end of the range
    public int ColorMin;               // index in DiamondGrading.ColorScale (0 = D)
    public int ColorMax;
    public int ClarityMin;             // index in DiamondGrading.ClarityScale (0 = FL)
    public int ClarityMax;
    public float Markup;               // asking price = market value x markup
    public (string shape, float weight)[] Shapes;

    // Rough dealers of the 3rd floor: they sell uncut crystals of these forms, of which this share of the weight remains once cut.
    public bool Rough;
    public float YieldMin;
    public float YieldMax;
    public (string form, float weight)[] Forms;

    public readonly List<GemInstance> Stock = new List<GemInstance>();
    public readonly Dictionary<GemInstance, int> Prices = new Dictionary<GemInstance, int>();
}

// Stock of the four diamond dealers, renewed every day. Natural colourless diamonds, graded D to Z, each sold with a
// certificate. Lives on the player (added on demand, like the other persistent states).
public class DiamondDealerState : MonoBehaviour
{
    public static DiamondDealerState Instance { get; private set; }

    public const string DiamondAssetName = "Diamant";

    public List<DiamondDealer> Dealers { get; private set; } = new List<DiamondDealer>();

    public event Action OnStockChanged;

    private static int nextCertificateNumber = 482000;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        BuildDealers();
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += OnDayChanged;
        Restock();
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= OnDayChanged;
    }

    // Scenes generated before this state existed do not carry it: add it to the player on demand.
    public static DiamondDealerState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<DiamondDealerState>();
        return Instance;
    }

    private void OnDayChanged(int day)
    {
        Restock();
    }

    private void BuildDealers()
    {
        Dealers = new List<DiamondDealer>
        {
            new DiamondDealer
            {
                Name = "Maison Verhoeven",
                Specialty = "Diamants classiques",
                Pitch = "Le diamant de joaillerie par excellence : des tailles courantes, de belles couleurs et une pureté soignée.",
                StockSize = 5, CaratMin = 0.40f, CaratMax = 2.00f, CaratSkew = 1.6f,
                ColorMin = 0, ColorMax = 8, ClarityMin = 2, ClarityMax = 7, Markup = 1.30f,
                Shapes = new[] { ("Rond", 62f), ("Ovale", 12f), ("Coussin", 10f), ("Poire", 6f), ("Carré", 6f), ("Marquise", 4f) },
            },
            new DiamondDealer
            {
                Name = "Maison Kolenberg",
                Specialty = "Très gros diamants",
                Pitch = "Des pierres exceptionnelles de plusieurs carats, réservées aux clients les plus exigeants. Les prix sont à la hauteur.",
                StockSize = 4, CaratMin = 3.00f, CaratMax = 12.00f, CaratSkew = 1.8f,
                ColorMin = 0, ColorMax = 9, ClarityMin = 1, ClarityMax = 6, Markup = 1.45f,
                Shapes = new[] { ("Rond", 38f), ("Coussin", 20f), ("Rectangle à pans coupés", 16f), ("Ovale", 16f), ("Poire", 10f) },
            },
            new DiamondDealer
            {
                Name = "Atelier Janssens",
                Specialty = "Mêlées",
                Pitch = "Des diamants minuscules, vendus pour le pavage et les petites montures. Toute la gamme de couleurs, à petit prix.",
                StockSize = 8, CaratMin = 0.02f, CaratMax = 0.18f, CaratSkew = 1.0f,
                ColorMin = 3, ColorMax = 20, ClarityMin = 4, ClarityMax = 8, Markup = 1.20f,
                Shapes = new[] { ("Rond", 88f), ("Carré", 12f) },
            },
            new DiamondDealer
            {
                Name = "Comptoir Mertens",
                Specialty = "Qualité économique",
                Pitch = "Des diamants plus jaunes ou plus inclus, à prix serrés : pour qui cherche la taille avant la perfection.",
                StockSize = 6, CaratMin = 0.30f, CaratMax = 3.00f, CaratSkew = 1.5f,
                ColorMin = 7, ColorMax = 22, ClarityMin = 7, ClarityMax = 10, Markup = 1.10f,
                Shapes = new[] { ("Rond", 55f), ("Ovale", 15f), ("Coussin", 12f), ("Poire", 10f), ("Carré", 8f) },
            },
            new DiamondDealer
            {
                Name = "Compagnie Minière du Kasaï",
                Specialty = "Lots de la mine",
                Pitch = "Des cristaux bruts sortis de la mine cette semaine, vendus tels quels : couleurs et puretés très variées.",
                Rough = true, StockSize = 6, CaratMin = 0.40f, CaratMax = 4.00f, CaratSkew = 1.7f,
                ColorMin = 5, ColorMax = 18, ClarityMin = 4, ClarityMax = 9, Markup = 1.12f, YieldMin = 0.36f, YieldMax = 0.46f,
                Forms = new[] { ("Octaèdre", 35f), ("Dodécaèdre", 20f), ("Macle", 15f), ("Galet", 20f), ("Éclat", 10f) },
            },
            new DiamondDealer
            {
                Name = "Maison Sarin & Fils",
                Specialty = "Beaux cristaux",
                Pitch = "Une sélection de cristaux bien formés, aux belles couleurs. Les prix sont ceux de la belle taille qu'on peut en tirer.",
                Rough = true, StockSize = 4, CaratMin = 1.50f, CaratMax = 10.00f, CaratSkew = 1.8f,
                ColorMin = 0, ColorMax = 8, ClarityMin = 1, ClarityMax = 6, Markup = 1.30f, YieldMin = 0.40f, YieldMax = 0.50f,
                Forms = new[] { ("Octaèdre", 60f), ("Dodécaèdre", 25f), ("Galet", 15f) },
            },
            new DiamondDealer
            {
                Name = "Négoce Vanderlinden",
                Specialty = "Macles et fragments",
                Pitch = "Des macles, des éclats et des galets : peu de rendement, mais des prix serrés pour qui sait tailler.",
                Rough = true, StockSize = 7, CaratMin = 0.30f, CaratMax = 2.50f, CaratSkew = 1.3f,
                ColorMin = 6, ColorMax = 22, ClarityMin = 6, ClarityMax = 10, Markup = 0.95f, YieldMin = 0.26f, YieldMax = 0.38f,
                Forms = new[] { ("Macle", 40f), ("Éclat", 40f), ("Galet", 20f) },
            },
        };
    }

    private void Restock()
    {
        var database = GemDatabase.Instance;
        GemDefinition diamond = null;
        if (database != null)
        {
            foreach (var def in database.allGems)
                if (def != null && def.name == DiamondAssetName) diamond = def;
        }
        if (diamond == null) return;

        var day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        foreach (var dealer in Dealers)
        {
            dealer.Stock.Clear();
            dealer.Prices.Clear();
            for (var i = 0; i < dealer.StockSize; i++)
            {
                int price;
                var stone = dealer.Rough ? GenerateRough(dealer, diamond, out price) : GenerateStone(dealer, diamond, day, out price);
                dealer.Stock.Add(stone);
                dealer.Prices[stone] = price;
            }
            // Cheapest first, so each dealer's list reads like a price list.
            dealer.Stock.Sort((a, b) => dealer.Prices[a].CompareTo(dealer.Prices[b]));
        }

        OnStockChanged?.Invoke();
    }

    private static GemInstance GenerateStone(DiamondDealer dealer, GemDefinition diamond, int day, out int price)
    {
        var carat = Mathf.Lerp(dealer.CaratMin, dealer.CaratMax, Mathf.Pow(UnityEngine.Random.value, dealer.CaratSkew));
        carat = carat < 1f ? Mathf.Round(carat * 100f) / 100f : Mathf.Round(carat * 20f) / 20f;

        // Colour and clarity cluster around the middle of the dealer's range.
        var colorIndex = dealer.ColorMin + Mathf.RoundToInt((UnityEngine.Random.value + UnityEngine.Random.value) * 0.5f * (dealer.ColorMax - dealer.ColorMin));
        var clarityIndex = dealer.ClarityMin + Mathf.RoundToInt((UnityEngine.Random.value + UnityEngine.Random.value) * 0.5f * (dealer.ClarityMax - dealer.ClarityMin));

        var shape = PickShape(dealer);
        var style = StyleFor(shape);
        var dimensions = DimensionsFor(shape, carat);
        var value = DiamondGrading.Value(carat, colorIndex, clarityIndex);
        price = Mathf.Max(10, Mathf.RoundToInt(value * dealer.Markup));

        nextCertificateNumber += UnityEngine.Random.Range(1, 40);
        return GemInstance.CreateDiamond(diamond, null, carat, colorIndex, clarityIndex, shape, style,
            dimensions.x, dimensions.y, dimensions.z, value, dealer.Name, day, $"GL-{nextCertificateNumber}");
    }

    // A rough crystal: its weight, its form, and the colour and clarity of the stone it will give. The dealer's price follows the polished
    // stone it should yield, so a careful buyer can read the quality from the price per carat.
    private static GemInstance GenerateRough(DiamondDealer dealer, GemDefinition diamond, out int price)
    {
        var carat = Mathf.Lerp(dealer.CaratMin, dealer.CaratMax, Mathf.Pow(UnityEngine.Random.value, dealer.CaratSkew));
        carat = carat < 1f ? Mathf.Round(carat * 100f) / 100f : Mathf.Round(carat * 20f) / 20f;

        var colorIndex = dealer.ColorMin + Mathf.RoundToInt((UnityEngine.Random.value + UnityEngine.Random.value) * 0.5f * (dealer.ColorMax - dealer.ColorMin));
        var clarityIndex = dealer.ClarityMin + Mathf.RoundToInt((UnityEngine.Random.value + UnityEngine.Random.value) * 0.5f * (dealer.ClarityMax - dealer.ClarityMin));

        var total = 0f;
        foreach (var option in dealer.Forms) total += option.weight;
        var roll = UnityEngine.Random.value * total;
        var form = dealer.Forms[0].form;
        foreach (var option in dealer.Forms)
        {
            roll -= option.weight;
            if (roll <= 0f)
            {
                form = option.form;
                break;
            }
        }

        // Octahedra cut best, splinters and twins lose the most.
        var formYield = form == "Octaèdre" ? 1.0f : form == "Dodécaèdre" ? 0.95f : form == "Galet" ? 0.9f : form == "Macle" ? 0.8f : 0.75f;
        var yield = Mathf.Clamp(Mathf.Lerp(dealer.YieldMin, dealer.YieldMax, UnityEngine.Random.value) * (0.9f + 0.1f * formYield), 0.2f, 0.55f);
        yield = Mathf.Round(yield * 100f) / 100f;

        var polishedValue = DiamondGrading.Value(carat * yield, colorIndex, clarityIndex);
        var roughValue = Mathf.Max(5, Mathf.RoundToInt(polishedValue * 0.72f));
        price = Mathf.Max(5, Mathf.RoundToInt(roughValue * dealer.Markup * UnityEngine.Random.Range(0.92f, 1.08f)));

        var size = RoughSize(form, carat);
        return GemInstance.CreateRoughDiamond(diamond, carat, colorIndex, clarityIndex, form, yield, size.x, size.y, size.z, roughValue, dealer.Name);
    }

    // Length x width x depth of a crystal in mm: a carat of diamond is about 57 mm3, spread according to the form.
    private static Vector3 RoughSize(string form, float carat)
    {
        var diameter = 4.9f * Mathf.Pow(carat, 1f / 3f);
        switch (form)
        {
            case "Dodécaèdre": return new Vector3(1.05f, 0.97f, 0.98f) * diameter;
            case "Macle": return new Vector3(1.25f, 1.15f, 0.70f) * diameter;
            case "Éclat": return new Vector3(1.30f, 1.00f, 0.77f) * diameter;
            case "Galet": return new Vector3(1.08f, 1.00f, 0.93f) * diameter;
            default: return new Vector3(1f, 1f, 1f) * diameter;
        }
    }

    // What the dealer announces about the yield of a rough stone: a range around the real one, off-centre by a stable amount.
    public static void YieldEstimate(GemInstance stone, out int low, out int high)
    {
        var offset = (int)(StoneOptics.StableHash((stone.SpecimenId ?? "") + "|yield") % 7) - 3;
        var center = Mathf.RoundToInt(DiamondCutting.YieldOf(stone) * 100f) + offset;
        low = Mathf.Max(5, center - 5);
        high = center + 5;
    }

    private static string PickShape(DiamondDealer dealer)
    {
        var total = 0f;
        foreach (var option in dealer.Shapes) total += option.weight;
        var roll = UnityEngine.Random.value * total;
        foreach (var option in dealer.Shapes)
        {
            roll -= option.weight;
            if (roll <= 0f) return option.shape;
        }
        return dealer.Shapes[0].shape;
    }

    private static string StyleFor(string shape)
    {
        switch (shape)
        {
            case "Carré": return "Princesse";
            case "Rectangle à pans coupés": return "À degrés";
            default: return "Brillant";
        }
    }

    // Length x width x depth in mm: a 1 ct round brilliant measures about 6.5 mm; other shapes stretch around that.
    public static Vector3 DimensionsFor(string shape, float carat)
    {
        var diameter = 6.5f * Mathf.Pow(carat, 1f / 3f);
        float length = diameter, width = diameter;
        switch (shape)
        {
            case "Ovale": length = diameter * 1.12f; width = diameter * 0.86f; break;
            case "Poire": length = diameter * 1.22f; width = diameter * 0.80f; break;
            case "Marquise": length = diameter * 1.40f; width = diameter * 0.70f; break;
            case "Coussin": length = diameter * 0.98f; width = diameter * 0.98f; break;
            case "Carré": length = diameter * 0.90f; width = diameter * 0.90f; break;
            case "Rectangle à pans coupés": length = diameter * 1.08f; width = diameter * 0.80f; break;
            case "Rectangle": length = diameter * 1.10f; width = diameter * 0.84f; break;
            case "Baguette": length = diameter * 1.30f; width = diameter * 0.62f; break;
            case "Shield": length = diameter * 1.05f; width = diameter * 0.93f; break;
            case "Triangle": length = diameter * 1.04f; width = diameter * 0.95f; break;
            case "Octogone": length = diameter * 0.96f; width = diameter * 0.96f; break;
            case "Cœur": length = diameter * 1.00f; width = diameter * 0.98f; break;
        }
        return new Vector3(length, width, 0.61f * (length + width) * 0.5f);
    }

    public int GetPrice(DiamondDealer dealer, GemInstance stone)
    {
        return dealer != null && dealer.Prices.TryGetValue(stone, out var price) ? price : stone.FinalValue;
    }

    public bool TryPurchase(DiamondDealer dealer, GemInstance stone, PlayerInventory inventory, PlayerWallet wallet)
    {
        if (dealer == null || stone == null || inventory == null || wallet == null) return false;
        if (!dealer.Stock.Contains(stone)) return false;

        var price = GetPrice(dealer, stone);
        if (!wallet.TrySpend(price)) return false;

        if (!inventory.TryAddGem(stone))
        {
            wallet.AddMoney(price);
            return false;
        }

        stone.RecordPurchase(price);
        dealer.Stock.Remove(stone);
        dealer.Prices.Remove(stone);
        GemCodex.Instance?.MarkDiscovered(stone.Definition.name);
        OnStockChanged?.Invoke();
        return true;
    }
}
