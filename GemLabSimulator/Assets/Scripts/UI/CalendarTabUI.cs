using System;
using UnityEngine;
using UnityEngine.UI;

// The calendar tab of the bag: one page for each month, with its days in a grid (the holidays, the events and the birthday show on them),
// the birthstones of the month and what the selected day holds. Built by the bag, inside its panel.
public class CalendarTabUI
{
    private const float GridX = 24f;
    private const float CellW = 60f;
    private const float CellH = 56f;
    private const float Gap = 4f;
    private const float SideX = 526f;
    private const float SideW = 250f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color CellColor = new Color(0.17f, 0.15f, 0.14f, 1f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.26f, 0.22f, 1f);

    private static readonly string[] WeekdayLetters = { "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim" };

    private readonly Transform root;
    private RectTransform gridRoot;
    private RectTransform sideRoot;
    private Text monthLabel;

    private int year;
    private int month;
    private DateTime selected;
    private int pickDay = 15;
    private int pickMonth = 6;

    public GameObject Root { get; }

    public CalendarTabUI(Transform parent)
    {
        Root = new GameObject("CalendarContent", typeof(RectTransform));
        Root.transform.SetParent(parent, false);
        var rect = Root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        root = Root.transform;

        // Month navigation.
        RuntimeUIKit.MakeButton("Previous", root, "<", 16, ButtonColor, new Vector2(GridX, -56f), new Vector2(36f, 30f)).onClick.AddListener(() => Step(-1));
        monthLabel = RuntimeUIKit.MakeText("Month", root, "", 20, TextAnchor.MiddleCenter, Gold, new Vector2(GridX + 40f, -56f), new Vector2(300f, 30f));
        monthLabel.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeButton("Next", root, ">", 16, ButtonColor, new Vector2(GridX + 344f, -56f), new Vector2(36f, 30f)).onClick.AddListener(() => Step(1));
        RuntimeUIKit.MakeButton("Today", root, "Aujourd'hui", 11, ButtonColor, new Vector2(GridX + 386f, -56f), new Vector2(58f, 30f)).onClick.AddListener(Show);

        for (var i = 0; i < 7; i++)
            RuntimeUIKit.MakeText("Weekday" + i, root, WeekdayLetters[i], 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.66f, 0.58f),
                new Vector2(GridX + i * (CellW + Gap), -94f), new Vector2(CellW, 18f));

        gridRoot = NewLayer("Grid");
        sideRoot = NewLayer("Side");
    }

    private RectTransform NewLayer(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // Opens the page of the current month with today selected.
    public void Show()
    {
        var today = GameCalendar.Today;
        year = today.Year;
        month = today.Month;
        selected = today.Date;
        if (GameCalendar.HasBirthday)
        {
            pickDay = GameCalendar.BirthdayDay;
            pickMonth = GameCalendar.BirthdayMonth;
        }
        Rebuild();
    }

    private void Step(int delta)
    {
        month += delta;
        if (month < 1) { month = 12; year--; }
        if (month > 12) { month = 1; year++; }
        if (year < GameCalendar.Start.Year) { year = GameCalendar.Start.Year; month = 1; }
        // The selected day follows to the new page when it is not on it.
        if (selected.Year != year || selected.Month != month) selected = new DateTime(year, month, 1);
        Rebuild();
    }

    private static void Clear(RectTransform layer)
    {
        for (var i = layer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(layer.GetChild(i).gameObject);
    }

    private void Rebuild()
    {
        monthLabel.text = $"{Capital(GameCalendar.MonthNames[month - 1])} {year}";
        Clear(gridRoot);
        Clear(sideRoot);
        BuildGrid();
        BuildSide();
    }

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

    // ---- The month -----------------------------------------------------------------------------------------------

    private void BuildGrid()
    {
        var first = new DateTime(year, month, 1);
        var offset = ((int)first.DayOfWeek + 6) % 7;
        var days = DateTime.DaysInMonth(year, month);
        var today = GameCalendar.Today.Date;

        for (var d = 1; d <= days; d++)
        {
            var date = new DateTime(year, month, d);
            var slot = offset + d - 1;
            var x = GridX + (slot % 7) * (CellW + Gap);
            var y = -118f - (slot / 7) * (CellH + Gap);
            var events = GameCalendar.EventsOn(date);

            var isToday = date == today;
            var isSelected = date == selected.Date;
            if (isToday || isSelected)
                RuntimeUIKit.MakeBox("Border", gridRoot, new Vector2(x - 2f, y + 2f), new Vector2(CellW + 4f, CellH + 4f),
                    isToday ? new Color(0.95f, 0.78f, 0.30f, 1f) : new Color(0.85f, 0.85f, 0.90f, 1f)).raycastTarget = false;

            var color = CellColor;
            foreach (var e in events)
            {
                if (e.IsHoliday) color = new Color(0.30f, 0.14f, 0.13f, 1f);
                if (e.Name == "Votre anniversaire") color = new Color(0.40f, 0.31f, 0.10f, 1f);
            }
            var past = date < today;
            if (past) color = Color.Lerp(color, new Color(0.10f, 0.09f, 0.09f, 1f), 0.45f);

            var captured = date;
            var cell = RuntimeUIKit.MakeButton("Day" + d, gridRoot, "", 10, color, new Vector2(x, y), new Vector2(CellW, CellH));
            cell.onClick.AddListener(() => Select(captured));

            var number = RuntimeUIKit.MakeText("Number", cell.transform, d.ToString(), 15, TextAnchor.UpperLeft,
                isToday ? new Color(1f, 0.9f, 0.5f) : (past ? new Color(0.6f, 0.58f, 0.54f) : Color.white), new Vector2(5f, -3f), new Vector2(30f, 20f));
            number.fontStyle = FontStyle.Bold;

            // The first notable event of the day, in a few letters; a dot for each event.
            foreach (var e in events)
            {
                if (!e.Notable) continue;
                var label = e.Name.Length > 11 ? e.Name.Substring(0, 10) + "." : e.Name;
                RuntimeUIKit.MakeText("Event", cell.transform, label, 9, TextAnchor.UpperLeft, e.Color, new Vector2(4f, -22f), new Vector2(CellW, 14f));
                break;
            }
            for (var i = 0; i < events.Count && i < 5; i++)
                RuntimeUIKit.MakeBox("Dot", cell.transform, new Vector2(5f + i * 10f, -CellH + 11f), new Vector2(7f, 7f), events[i].Color, false).raycastTarget = false;
        }
    }

    private void Select(DateTime date)
    {
        selected = date;
        Rebuild();
    }

    // ---- The side: stones of the month, the selected day -----------------------------------------------------------

    private void BuildSide()
    {
        var header = RuntimeUIKit.MakeText("StoneHeader", sideRoot, $"Pierre{(GameCalendar.StonesOf(month).Length > 1 ? "s" : "")} du mois : {GameCalendar.MonthNames[month - 1]}",
            14, TextAnchor.UpperLeft, Gold, new Vector2(SideX, -58f), new Vector2(SideW, 20f));
        header.fontStyle = FontStyle.Bold;

        var stones = GameCalendar.StonesOf(month);
        for (var i = 0; i < stones.Length; i++)
        {
            var y = -84f - i * 58f;
            var icon = new GameObject("StoneIcon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(sideRoot, false);
            RuntimeUIKit.SetTopLeft(icon.GetComponent<RectTransform>(), new Vector2(SideX, y), new Vector2(40f, 40f));
            var image = icon.GetComponent<Image>();
            image.sprite = GameCalendar.IconOf(stones[i]);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var name = RuntimeUIKit.MakeText("StoneName", sideRoot, stones[i].Name, 14, TextAnchor.UpperLeft, Color.white, new Vector2(SideX + 48f, y - 1f), new Vector2(SideW - 48f, 18f));
            name.fontStyle = FontStyle.Bold;
            Wrapped(RuntimeUIKit.MakeText("StoneText", sideRoot, stones[i].Description, 11, TextAnchor.UpperLeft, new Color(0.78f, 0.75f, 0.68f),
                new Vector2(SideX + 48f, y - 18f), new Vector2(SideW - 48f, 32f)));
        }

        // The selected day.
        RuntimeUIKit.MakeBox("Separator", sideRoot, new Vector2(SideX, -262f), new Vector2(SideW, 1f), new Color(0.45f, 0.38f, 0.28f, 1f), false).raycastTarget = false;
        var dateText = RuntimeUIKit.MakeText("SelectedDate", sideRoot, Capital(GameCalendar.Long(selected)), 13, TextAnchor.UpperLeft, Gold, new Vector2(SideX, -270f), new Vector2(SideW, 36f));
        dateText.fontStyle = FontStyle.Bold;
        Wrapped(dateText);

        var events = GameCalendar.EventsOn(selected);
        var cursor = -310f;
        var limit = GameCalendar.HasBirthday ? -500f : -462f;
        if (events.Count == 0)
            RuntimeUIKit.MakeText("None", sideRoot, "Rien de particulier ce jour-là.", 12, TextAnchor.UpperLeft, new Color(0.62f, 0.58f, 0.52f), new Vector2(SideX, cursor), new Vector2(SideW, 18f));
        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];
            var lines = 1 + Mathf.CeilToInt(e.Description.Length / 40f);
            var height = 15f + lines * 13f - 13f + 6f;
            if (cursor - height < limit)
            {
                RuntimeUIKit.MakeText("More", sideRoot, $"+ {events.Count - i} autre(s)", 11, TextAnchor.UpperLeft, new Color(0.62f, 0.58f, 0.52f), new Vector2(SideX, cursor), new Vector2(SideW, 16f));
                break;
            }
            RuntimeUIKit.MakeBox("Mark", sideRoot, new Vector2(SideX, cursor - 3f), new Vector2(8f, 8f), e.Color, false).raycastTarget = false;
            var title = RuntimeUIKit.MakeText("EventName", sideRoot, e.Name, 13, TextAnchor.UpperLeft, e.Color, new Vector2(SideX + 14f, cursor), new Vector2(SideW - 14f, 18f));
            title.fontStyle = FontStyle.Bold;
            Wrapped(RuntimeUIKit.MakeText("EventText", sideRoot, e.Description, 11, TextAnchor.UpperLeft, new Color(0.82f, 0.78f, 0.70f),
                new Vector2(SideX + 14f, cursor - 16f), new Vector2(SideW - 14f, lines * 13f)));
            cursor -= height + 12f;
        }

        if (!GameCalendar.HasBirthday) BuildBirthdayPicker();
    }

    private static void Wrapped(Text text)
    {
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // A game created before the calendar has no birthday yet: it can be chosen here.
    private void BuildBirthdayPicker()
    {
        const float y = -470f;
        RuntimeUIKit.MakeText("PickLabel", sideRoot, "Votre anniversaire :", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.80f, 0.70f), new Vector2(SideX, y), new Vector2(SideW, 16f));
        var row = y - 22f;
        RuntimeUIKit.MakeButton("DayPrev", sideRoot, "<", 13, ButtonColor, new Vector2(SideX, row), new Vector2(24f, 24f)).onClick.AddListener(() => PickStep(false, -1));
        RuntimeUIKit.MakeText("DayValue", sideRoot, pickDay.ToString(), 14, TextAnchor.MiddleCenter, Color.white, new Vector2(SideX + 26f, row), new Vector2(28f, 24f));
        RuntimeUIKit.MakeButton("DayNext", sideRoot, ">", 13, ButtonColor, new Vector2(SideX + 56f, row), new Vector2(24f, 24f)).onClick.AddListener(() => PickStep(false, 1));
        RuntimeUIKit.MakeButton("MonthPrev", sideRoot, "<", 13, ButtonColor, new Vector2(SideX + 88f, row), new Vector2(24f, 24f)).onClick.AddListener(() => PickStep(true, -1));
        RuntimeUIKit.MakeText("MonthValue", sideRoot, GameCalendar.MonthNames[pickMonth - 1], 13, TextAnchor.MiddleCenter, Color.white, new Vector2(SideX + 114f, row), new Vector2(60f, 24f));
        RuntimeUIKit.MakeButton("MonthNext", sideRoot, ">", 13, ButtonColor, new Vector2(SideX + 176f, row), new Vector2(24f, 24f)).onClick.AddListener(() => PickStep(true, 1));
        RuntimeUIKit.MakeButton("Confirm", sideRoot, "OK", 13, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(SideX + 206f, row), new Vector2(44f, 24f)).onClick.AddListener(() =>
        {
            GameCalendar.SetBirthday(pickDay, pickMonth);
            Rebuild();
        });
    }

    private void PickStep(bool monthStep, int delta)
    {
        if (monthStep) pickMonth = (pickMonth - 1 + delta + 12) % 12 + 1;
        else
        {
            var daysInMonth = DateTime.DaysInMonth(2024, pickMonth);
            pickDay = (pickDay - 1 + delta + daysInMonth) % daysInMonth + 1;
        }
        pickDay = Mathf.Clamp(pickDay, 1, DateTime.DaysInMonth(2024, pickMonth));
        Rebuild();
    }
}
