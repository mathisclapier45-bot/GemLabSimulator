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
                var stone = GenerateStone(dealer, diamond, day, out var price);
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
    private static Vector3 DimensionsFor(string shape, float carat)
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

        dealer.Stock.Remove(stone);
        dealer.Prices.Remove(stone);
        GemCodex.Instance?.MarkDiscovered(stone.Definition.name);
        OnStockChanged?.Invoke();
        return true;
    }
}
