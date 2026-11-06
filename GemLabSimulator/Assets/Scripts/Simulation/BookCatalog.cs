using System;
using System.Collections.Generic;
using UnityEngine;

// What a book is, for the shelf, the library desk and the reader: its title, its cover, its price.
public class BookInfo
{
    public string Id;
    public string Title;
    public string[] CoverTitle;        // the two lines of the cover
    public string Subtitle;            // under the title on the cover
    public string Blurb;               // what the library says about it
    public int Price;
    public Color Leather;
    public Color LeatherDark;
    public Color Foil;                 // the gold of the lines
    public Color Emblem;               // the colour of the lozenge of the cover
}

// A small picture in a strip of a page, with an optional caption under it.
public class BookIcon
{
    public Sprite Sprite;
    public string Label;
}

// A row of small pictures at the top of a page: the instruments that reveal a treatment, the stones of a deposit.
public class BookStrip
{
    public string Title;                         // a line above the pictures, or null
    public bool Labeled;                         // each picture has its caption under it
    public readonly List<BookIcon> Icons = new List<BookIcon>();
}

// The pictures of the instruments of the laboratory, for the books (Assets/Resources/BookIcons).
public static class BookIcons
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    // The instruments, with the name the books give them and the file of their picture.
    public static readonly string[][] Instruments =
    {
        new[] { "Loupe", "Loupe" },
        new[] { "Microscope", "Microscope" },
        new[] { "Lampe UV", "LampeUV" },
        new[] { "Filtre Chelsea", "FiltreChelsea" },
        new[] { "Dichroscope", "Dichroscope" },
        new[] { "Spectroscope", "Spectroscope" },
        new[] { "UV-Vis-NIR", "UVVis" },
        new[] { "Infrarouge", "FTIR" },
        new[] { "Raman", "Raman" },
        new[] { "EDXRF", "EDXRF" },
        new[] { "MEB", "MEB" },
        new[] { "LA-ICP-MS", "LAICPMS" },
        new[] { "Photoluminescence", "Photoluminescence" },
        new[] { "DiamondView", "DiamondView" },
    };

    // The picture of an instrument from its name in the books (null if there is none).
    public static Sprite Instrument(string name)
    {
        foreach (var pair in Instruments)
            if (pair[0] == name) return Load(pair[1]);
        return null;
    }

    private static Sprite Load(string file)
    {
        if (Cache.TryGetValue(file, out var cached) && cached != null) return cached;
        var sprite = Resources.Load<Sprite>("BookIcons/" + file);
        if (sprite == null)
        {
            var texture = Resources.Load<Texture2D>("BookIcons/" + file);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
            }
        }
        Cache[file] = sprite;
        return sprite;
    }
}

// One entry of a book: an index line and a spread (the left page and the right page). The texts are built when the spread is first shown.
public class BookEntry
{
    public string Title;
    public string Subtitle;
    public string LeftHead = "";
    public string RightHead = "";
    public Sprite Icon;                          // the picture of a species, of an instrument, of the best stone of a deposit, if any
    public Sprite IndexIcon;                     // the picture in the index, when it is not the one of the entry
    public bool IndexIconIsFlag;                 // the picture in the index is a flag (drawn wide, with a frame; a neutral one if there is none)
    public BookStrip LeftStrip;                  // pictures at the top of the left page, if any
    public BookStrip RightStrip;                 // pictures at the top of the right page, if any
    public Func<BookMap> MapBuilder;             // a map at the top of the right page, if any (made when the page is shown)
    public List<SpectrumLine> Spectrum;          // the absorption spectrum strip of a species, if any
    public Func<string> LeftBuilder;
    public Func<string> RightBuilder;

    private string left;
    private string right;

    private BookMap map;
    private bool mapBuilt;

    public BookMap Map
    {
        get
        {
            if (!mapBuilt)
            {
                mapBuilt = true;
                map = MapBuilder != null ? MapBuilder() : null;
            }
            return map;
        }
    }

    public string Left => left ?? (left = LeftBuilder != null ? LeftBuilder() : "");
    public string Right => right ?? (right = RightBuilder != null ? RightBuilder() : "");
}

// The text of a book, made when it is opened.
public class BookContent
{
    public string IndexTitle = "Index";
    public string IntroHeading = "Comment lire ce livre";
    public string IntroText = "";
    public string TitleNote = "";                // under the title, on the title page
    public readonly List<BookEntry> Entries = new List<BookEntry>();
}

// The books of the library. The player buys them at the library; each is read from the books tab of the bag.
public static class BookCatalog
{
    public const string Guide = "guide";
    public const string Treatments = "traitements";
    public const string Atlas = "atlas";
    public const string Market = "marche";

    public static readonly BookInfo[] Infos =
    {
        new BookInfo
        {
            Id = Guide, Title = "Le Guide du gemmologue", CoverTitle = new[] { "GUIDE", "du gemmologue" }, Subtitle = "Toutes les pages du grimoire",
            Price = 1800,
            Blurb = "Une page pour chaque espèce du grimoire, découverte ou non : ses propriétés, les traitements qu'on peut lui faire et ce que montrent les instruments du laboratoire.",
            Leather = new Color(0.13f, 0.30f, 0.26f), LeatherDark = new Color(0.07f, 0.17f, 0.15f), Foil = new Color(0.86f, 0.70f, 0.30f), Emblem = new Color(0.62f, 0.86f, 0.98f),
        },
        new BookInfo
        {
            Id = Treatments, Title = "Manuel des traitements", CoverTitle = new[] { "MANUEL", "des traitements" }, Subtitle = "Les faire, les reconnaître",
            Price = 1200,
            Blurb = "Les neuf traitements du laboratoire et les quatorze instruments qui les trahissent, avec leur image : ce que chacun fait à la pierre, ce qu'il vaut à la revente, et ce que chaque instrument y voit.",
            Leather = new Color(0.40f, 0.13f, 0.15f), LeatherDark = new Color(0.24f, 0.07f, 0.09f), Foil = new Color(0.90f, 0.74f, 0.36f), Emblem = new Color(0.60f, 0.88f, 0.60f),
        },
        new BookInfo
        {
            Id = Atlas, Title = "Atlas des gisements", CoverTitle = new[] { "ATLAS", "des gisements" }, Subtitle = "D'où viennent les pierres",
            Price = 900,
            Blurb = "Chaque origine, avec le logo des pierres qu'on y trouve, leur rareté, ce que le gisement ajoute à leur valeur et les inclusions qui le trahissent.",
            Leather = new Color(0.12f, 0.20f, 0.38f), LeatherDark = new Color(0.07f, 0.12f, 0.25f), Foil = new Color(0.88f, 0.80f, 0.55f), Emblem = new Color(0.98f, 0.82f, 0.38f),
        },
        new BookInfo
        {
            Id = Market, Title = "Précis du marché", CoverTitle = new[] { "PRÉCIS", "du marché" }, Subtitle = "Vendre sans se faire prendre",
            Price = 700,
            Blurb = "Les règles de la revente : marchés, divulgation des traitements, enchères du samedi, bijoutier, banque et Rapaport, avec leurs chiffres.",
            Leather = new Color(0.36f, 0.24f, 0.12f), LeatherDark = new Color(0.22f, 0.14f, 0.06f), Foil = new Color(0.92f, 0.76f, 0.38f), Emblem = new Color(0.92f, 0.62f, 0.36f),
        },
    };

    public static BookInfo Find(string id)
    {
        foreach (var info in Infos) if (info.Id == id) return info;
        return null;
    }

    // The content of a book (made when it is opened: the guide holds a page for every species).
    public static BookContent Build(string id)
    {
        switch (id)
        {
            case Guide: return BookContents.BuildGuide();
            case Treatments: return BookContents.BuildTreatments();
            case Atlas: return BookContents.BuildAtlas();
            case Market: return BookContents.BuildMarket();
        }
        return new BookContent();
    }
}
