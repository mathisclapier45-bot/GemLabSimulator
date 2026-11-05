using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// An event of the calendar: a holiday, a season, a phase of the moon, an event of the gem trade, or something personal (the birthday).
public class CalendarEvent
{
    public string Name;
    public string Description;
    public Color Color;
    public bool Notable;          // announced on the morning of the day (not the phases of the moon and the like)
    public bool IsHoliday;        // a public holiday: the day is tinted red in the calendar
}

// The birthstone of a month: its name, a few words about it, and the gem of the game it is drawn from (empty when the game has none).
public class BirthStone
{
    public string Name;
    public string Description;
    public string AssetName;
    public Color Body;
    public Color Facet;
}

// The calendar of the game: the adventure begins on 3 September 2025, and each day of the game (the counter of GameClock) is one day of
// the calendar, with its months, its years, its holidays, its moon and its birthstones. The birthday of the player is chosen when the game
// is created.
public static class GameCalendar
{
    public static readonly DateTime Start = new DateTime(2025, 9, 3);

    public static readonly string[] MonthNames =
    {
        "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre",
    };

    private static readonly string[] MonthShort = { "janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc." };
    private static readonly string[] WeekdayNames = { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };
    private static readonly string[] WeekdayShort = { "dim.", "lun.", "mar.", "mer.", "jeu.", "ven.", "sam." };

    // The birthday of the player (0 when none has been chosen).
    public static int BirthdayDay { get; private set; }
    public static int BirthdayMonth { get; private set; }

    public static bool HasBirthday => BirthdayDay > 0 && BirthdayMonth > 0;

    public static void SetBirthday(int day, int month)
    {
        month = Mathf.Clamp(month, 0, 12);
        BirthdayMonth = month;
        BirthdayDay = month == 0 ? 0 : Mathf.Clamp(day, 1, DateTime.DaysInMonth(2024, month));
    }

    // ---- Dates ---------------------------------------------------------------------------------------------------

    public static DateTime DateOf(int gameDay) => Start.AddDays(Mathf.Max(1, gameDay) - 1);

    public static DateTime Today => DateOf(GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1);

    // The counter of the game for a date of the calendar (1 on the first day).
    public static int DayOf(DateTime date) => (date.Date - Start.Date).Days + 1;

    public static string Long(DateTime date) => $"{WeekdayNames[(int)date.DayOfWeek]} {date.Day}{(date.Day == 1 ? "er" : "")} {MonthNames[date.Month - 1]} {date.Year}";

    public static string Short(DateTime date) => $"{date.Day} {MonthShort[date.Month - 1]} {date.Year}";

    // "mer. 3 sept. 2025"
    public static string WithWeekday(DateTime date) => $"{WeekdayShort[(int)date.DayOfWeek]} {date.Day} {MonthShort[date.Month - 1]} {date.Year}";

    public static string Short(int gameDay) => Short(DateOf(gameDay));

    // "mer. 3": the weekday and the day of the month, for the clock of the screen.
    public static string DayLabel(DateTime date) => $"{WeekdayShort[(int)date.DayOfWeek]} {date.Day}";

    // ---- Moon ----------------------------------------------------------------------------------------------------

    private static readonly DateTime NewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
    private const double SynodicMonth = 29.530588853;

    // 0 at the new moon, 0.5 at the full moon, back to 1.
    public static float MoonPhase(DateTime date)
    {
        var noon = DateTime.SpecifyKind(date.Date.AddHours(12), DateTimeKind.Utc);
        var age = (noon - NewMoon).TotalDays % SynodicMonth;
        if (age < 0) age += SynodicMonth;
        return (float)(age / SynodicMonth);
    }

    private static double MoonAge(DateTime date) => MoonPhase(date) * SynodicMonth;

    // ---- Easter --------------------------------------------------------------------------------------------------

    public static DateTime Easter(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(year, month, day);
    }

    // ---- Events --------------------------------------------------------------------------------------------------

    private static readonly Color Holiday = new Color(0.93f, 0.40f, 0.34f);
    private static readonly Color Season = new Color(0.45f, 0.82f, 0.50f);
    private static readonly Color Trade = new Color(0.40f, 0.68f, 1.00f);
    private static readonly Color Personal = new Color(1.00f, 0.84f, 0.30f);
    private static readonly Color Fun = new Color(0.78f, 0.52f, 0.95f);
    private static readonly Color Moon = new Color(0.80f, 0.85f, 1.00f);

    private static void Add(List<CalendarEvent> list, string name, string description, Color color, bool notable = true)
    {
        list.Add(new CalendarEvent { Name = name, Description = description, Color = color, Notable = notable, IsHoliday = color.Equals(Holiday) });
    }

    // The n-th (1 to 4) given weekday of the month of the date, or the last one when n is 0.
    private static bool IsWeekday(DateTime date, DayOfWeek weekday, int n)
    {
        if (date.DayOfWeek != weekday) return false;
        return n == 0 ? date.AddDays(7).Month != date.Month : (date.Day - 1) / 7 + 1 == n;
    }

    public static List<CalendarEvent> EventsOn(DateTime date)
    {
        var list = new List<CalendarEvent>();
        var day = date.Day;
        var month = date.Month;

        if (date.Date == Start.Date)
            Add(list, "Début de l'aventure", "Vous prenez possession de la maison et du bureau de gemmologie.", Personal);

        // The birthday of the player; one born on 29 February celebrates on the 28th in the other years.
        if (HasBirthday)
        {
            var leapDay = BirthdayMonth == 2 && BirthdayDay == 29 && !DateTime.IsLeapYear(date.Year);
            if (month == BirthdayMonth && (day == BirthdayDay || (leapDay && day == 28)))
            {
                var years = date.Year - Start.Year;
                Add(list, "Votre anniversaire", "Bon anniversaire ! Une pierre vous attend peut-être au bout de la journée." +
                    (years > 0 ? $" Un an de plus dans la gemmologie : {years} an{(years > 1 ? "s" : "")} d'aventure." : ""), Personal);
            }
        }

        // Fixed holidays.
        if (month == 1 && day == 1) Add(list, "Jour de l'an", "Le premier jour de l'année : bonnes résolutions et nouveaux projets.", Holiday);
        if (month == 2 && day == 14) Add(list, "Saint-Valentin", "La fête des amoureux : les bijoux se vendent bien.", Fun);
        if (month == 4 && day == 1) Add(list, "Poisson d'avril", "Méfiez-vous des fausses pierres...", Fun);
        if (month == 6 && day == 21) Add(list, "Fête de la musique", "Les rues de la ville résonnent de concerts.", Fun);
        if (month == 10 && day == 31) Add(list, "Halloween", "Citrouilles, déguisements et frissons : la nuit appartient aux fantômes.", Fun);
        if (month == 12 && day == 24) Add(list, "Réveillon de Noël", "Les vitrines brillent : veillée avant Noël.", Holiday);
        if (month == 12 && day == 25) Add(list, "Noël", "Le jour des cadeaux, et des plus belles pierres sous le sapin.", Holiday);

        // Easter and the holidays that follow it.
        var easter = Easter(date.Year);
        var sinceEaster = (date.Date - easter).Days;
        if (sinceEaster == 0) Add(list, "Pâques", "Chasse aux œufs et chocolats.", Holiday);
        if (sinceEaster == 1) Add(list, "Lundi de Pâques", "Jour férié.", Holiday);

        // Mother's Day and Father's Day.
        if (month == 5 && IsWeekday(date, DayOfWeek.Sunday, 0)) Add(list, "Fête des mères", "Un bijou pour maman ?", Fun);
        if (month == 6 && IsWeekday(date, DayOfWeek.Sunday, 3)) Add(list, "Fête des pères", "Une montre pour papa ?", Fun);

        // The seasons.
        if (month == 3 && day == 20) Add(list, "Équinoxe de printemps", "Le jour et la nuit sont égaux : le printemps commence.", Season, false);
        if (month == 6 && day == 21) Add(list, "Solstice d'été", "Le jour le plus long de l'année.", Season, false);
        if (month == 9 && day == 22) Add(list, "Équinoxe d'automne", "Le jour et la nuit sont égaux : l'automne commence.", Season, false);
        if (month == 12 && day == 21) Add(list, "Solstice d'hiver", "La nuit la plus longue de l'année.", Season, false);

        // The trade of the gems.
        if (month == 2 && IsWeekday(date, DayOfWeek.Saturday, 1)) Add(list, "Salon des pierres précieuses", "Les marchands de gemmes exposent leurs plus belles pièces.", Trade);
        if (month == 3 && IsWeekday(date, DayOfWeek.Monday, 3)) Add(list, "Assemblée de la Bourse du diamant", "Les diamantaires fixent le climat des prix de l'année.", Trade);
        if (month == 6 && day == 15) Add(list, "Journée des cartes à collectionner", "Les collectionneurs échangent leurs doubles sur la place.", Fun);
        if (month == 10 && IsWeekday(date, DayOfWeek.Saturday, 1)) Add(list, "Foire aux gemmes", "Brut, taillé, synthétique : tout se vend et tout se discute.", Trade);
        if (month == 11 && IsWeekday(date, DayOfWeek.Friday, 0)) Add(list, "Gala du diamant", "La soirée des grands noms de la joaillerie.", Trade);
        if (month == 12 && day == 10) Add(list, "Ouverture du marché de Noël", "Des lumières, des chalets et des bijoux jusqu'au 24.", Fun);

        // The price list of the diamond trade comes out every Thursday.
        if (date.DayOfWeek == DayOfWeek.Thursday)
            Add(list, "Parution du Rapaport", "Le Rapaport, la liste de prix des diamants, paraît chaque jeudi : elle est dans le sac pour les abonnés.", Trade, false);

        // The weekly sale of the bank, every Saturday, with its theme (the day before, a reminder).
        if (AuctionCatalog.IsAuctionDay(date) && date.Date >= Start.Date)
        {
            var theme = AuctionCatalog.ThemeFor(date);
            Add(list, "Enchères", $"Vente aux enchères de la banque, de 9h à 20h. Thème : {theme.Name}. {theme.Description}", Trade);
        }
        if (date.DayOfWeek == DayOfWeek.Sunday && date.Date >= Start.Date)
        {
            var theme = AuctionCatalog.ThemeFor(date.AddDays(1));
            Add(list, "Bulletin des ventes", $"Le catalogue de la vente de samedi est paru (onglet Enchères du sac). Thème : {theme.Name}.", Trade);
        }
        if (date.DayOfWeek == DayOfWeek.Friday && date.Date >= Start.Date)
        {
            var theme = AuctionCatalog.ThemeFor(date.AddDays(1));
            Add(list, "Veille de vente", $"Demain, vente aux enchères à la banque. Thème : {theme.Name}. Déposez vos pierres avant : on ne prend plus rien le jour de la vente.", Trade, false);
        }

        // The bank pays its interest on the first of the month.
        if (day == 1) Add(list, "Versement des intérêts", "La banque verse les intérêts du mois écoulé sur les comptes ouverts.", Trade, false);

        // The first of the month brings its birthstones.
        if (day == 1)
        {
            var stones = StonesOf(month);
            var names = new List<string>();
            foreach (var stone in stones) names.Add(stone.Name);
            Add(list, stones.Length > 1 ? "Pierres du mois" : "Pierre du mois", string.Join(", ", names) + ".", Personal, false);
        }

        // The moon.
        var age = MoonAge(date);
        if (age < 0.5 || age > SynodicMonth - 0.5) Add(list, "Nouvelle lune", "La nuit est noire : les étoiles brillent davantage.", Moon, false);
        else if (Math.Abs(age - SynodicMonth / 4.0) < 0.5) Add(list, "Premier quartier", "La moitié de la lune est éclairée.", Moon, false);
        else if (Math.Abs(age - SynodicMonth / 2.0) < 0.5) Add(list, "Pleine lune", "La lune éclaire la ville toute la nuit.", Moon, false);
        else if (Math.Abs(age - SynodicMonth * 0.75) < 0.5) Add(list, "Dernier quartier", "La moitié de la lune est éclairée.", Moon, false);

        return list;
    }

    // ---- Birthstones ---------------------------------------------------------------------------------------------

    private static BirthStone Stone(string name, string description, string asset, float r, float g, float b)
    {
        var body = new Color(r, g, b);
        return new BirthStone { Name = name, Description = description, AssetName = asset, Body = body, Facet = Color.Lerp(body, Color.white, 0.55f) };
    }

    private static readonly BirthStone[][] Stones =
    {
        new[] { Stone("Grenat", "Rouge profond, pierre de la fidélité et de la constance.", "Grenat_Almandin", 0.55f, 0.08f, 0.14f) },
        new[] { Stone("Améthyste", "Le quartz violet, pierre de la sagesse et du calme.", "Amethyste", 0.55f, 0.30f, 0.75f) },
        new[] { Stone("Aigue-marine", "Le béryl bleu comme la mer, pierre de la clarté.", "", 0.55f, 0.82f, 0.92f) },
        new[] { Stone("Diamant", "Le plus dur des minéraux, symbole de l'éclat et de la pureté.", "Diamant", 0.85f, 0.92f, 0.98f) },
        new[] { Stone("Émeraude", "Le béryl vert, pierre du renouveau et de l'espérance.", "Emeraude", 0.10f, 0.60f, 0.30f) },
        new[]
        {
            Stone("Perle", "Née dans un coquillage, la gemme de la douceur.", "", 0.93f, 0.91f, 0.88f),
            Stone("Alexandrite", "Verte le jour, rouge le soir : la pierre qui change de couleur.", "Chrysoberyl_Alexandrite", 0.18f, 0.55f, 0.45f),
            Stone("Pierre de lune", "Le feldspath au reflet laiteux, pierre des rêves.", "Pierre_De_Lune", 0.78f, 0.84f, 0.95f),
        },
        new[] { Stone("Rubis", "Le corindon rouge, pierre de la passion et du courage.", "Corindon_Rubis", 0.80f, 0.06f, 0.14f) },
        new[]
        {
            Stone("Péridot", "L'olivine vert clair, pierre de la joie et de la lumière.", "Peridot", 0.55f, 0.75f, 0.15f),
            Stone("Spinelle", "Longtemps confondu avec le rubis : toutes les couleurs.", "Spinelle", 0.85f, 0.20f, 0.30f),
        },
        new[] { Stone("Saphir", "Le corindon bleu, pierre de la loyauté et de la sagesse.", "Saphir_Bleu", 0.12f, 0.25f, 0.70f) },
        new[]
        {
            Stone("Opale", "Des éclats de toutes les couleurs dans une pierre : le jeu de couleurs.", "Opale_Noble", 0.75f, 0.85f, 0.90f),
            Stone("Tourmaline rose", "La tourmaline couleur de bonbon, pierre de la tendresse.", "Tourmaline_Rose", 0.90f, 0.40f, 0.60f),
        },
        new[]
        {
            Stone("Topaze", "Le silicate d'aluminium aux teintes de miel et d'or.", "Topaze", 0.95f, 0.65f, 0.15f),
            Stone("Citrine", "Le quartz jaune doré, pierre de la chaleur et du bonheur.", "Citrine", 0.95f, 0.75f, 0.20f),
        },
        new[]
        {
            Stone("Turquoise", "Bleu-vert de ciel et d'eau, pierre-talisman des voyageurs.", "Turquoise", 0.20f, 0.75f, 0.72f),
            Stone("Tanzanite", "Découverte au pied du Kilimandjaro : bleu et violet.", "Tanzanite", 0.30f, 0.30f, 0.78f),
            Stone("Zircon", "Brillant et dispersif, il rivalise avec le diamant.", "Zircon", 0.45f, 0.70f, 0.90f),
        },
    };

    public static BirthStone[] StonesOf(int month) => Stones[Mathf.Clamp(month, 1, 12) - 1];

    // The icon of a birthstone: the colours of the gem in the database when the game has it, those of the entry otherwise.
    public static Sprite IconOf(BirthStone stone)
    {
        var body = stone.Body;
        var facet = stone.Facet;
        var database = GemDatabase.Instance;
        if (database != null && !string.IsNullOrEmpty(stone.AssetName))
        {
            var definition = database.FindByAssetName(stone.AssetName);
            if (definition != null && definition.iconColor.a > 0f)
            {
                body = definition.iconColor;
                facet = definition.iconFacetColor;
            }
        }
        return GemIconFactory.GetSolid("Rond", "Brillant", body, facet);
    }
}

// Announces on screen what the day holds: a holiday, the birthday... once at the start of the day (after a night in bed or when a game is
// loaded). Added when a game scene loads.
public class CalendarAnnouncer : MonoBehaviour
{
    private static CalendarAnnouncer instance;

    private int lastDay = -1;
    private float readyAt;
    private bool pending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("CalendarAnnouncer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CalendarAnnouncer>();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu" || GameClock.Instance == null) return;

        var day = GameClock.Instance.CurrentDay;
        if (day != lastDay)
        {
            lastDay = day;
            pending = true;
            readyAt = Time.unscaledTime + 2.5f;
        }
        if (!pending || Time.unscaledTime < readyAt || ToolFeedbackUI.Instance == null) return;

        pending = false;
        var date = GameCalendar.DateOf(day);
        var notable = new List<string>();
        foreach (var calendarEvent in GameCalendar.EventsOn(date))
        {
            if (!calendarEvent.Notable) continue;
            notable.Add(calendarEvent.Name == "Enchères" ? $"Enchères à la banque : {AuctionCatalog.ThemeFor(date).Name}" : calendarEvent.Name);
        }
        if (date.DayOfWeek == DayOfWeek.Thursday && RapaportState.HasAccess) notable.Add("Le Rapaport est paru (onglet du sac)");
        if (notable.Count == 0) return;
        ToolFeedbackUI.Instance.ShowMessage($"Aujourd'hui, {GameCalendar.Long(date)} : {string.Join(" · ", notable)}.");
    }
}
