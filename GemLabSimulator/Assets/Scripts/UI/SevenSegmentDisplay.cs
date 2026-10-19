using UnityEngine;
using UnityEngine.UI;

// Row of 7-segment LCD digits drawn with uGUI images, with a decimal dot that can sit after any digit.
public class SevenSegmentDisplay
{
    // Segment order: a (top), b (top right), c (bottom right), d (bottom), e (bottom left), f (top left), g (middle).
    private static readonly bool[][] DigitSegments =
    {
        new[] { true, true, true, true, true, true, false },
        new[] { false, true, true, false, false, false, false },
        new[] { true, true, false, true, true, false, true },
        new[] { true, true, true, true, false, false, true },
        new[] { false, true, true, false, false, true, true },
        new[] { true, false, true, true, false, true, true },
        new[] { true, false, true, true, true, true, true },
        new[] { true, true, true, false, false, false, false },
        new[] { true, true, true, true, true, true, true },
        new[] { true, true, true, true, false, true, true },
    };

    private readonly Image[][] segments;
    private readonly Image dot;
    private readonly Color ink;
    private readonly Color ghost;
    private readonly float originX;
    private readonly float originY;
    private readonly float digitWidth;
    private readonly float gap;
    private readonly float digitHeight;
    private readonly float dotSize;

    public int DigitCount { get; }

    public SevenSegmentDisplay(Transform parent, Vector2 topLeft, int digitCount, float digitWidth, float digitHeight,
        float gap, float thickness, Color ink, Color ghost)
    {
        DigitCount = digitCount;
        originX = topLeft.x;
        originY = topLeft.y;
        this.digitWidth = digitWidth;
        this.digitHeight = digitHeight;
        this.gap = gap;
        this.ink = ink;
        this.ghost = ghost;
        dotSize = Mathf.Max(4f, thickness + 1f);

        segments = new Image[digitCount][];
        for (var i = 0; i < digitCount; i++)
            segments[i] = BuildDigit(parent, new Vector2(originX + i * (digitWidth + gap), originY), thickness);

        dot = RuntimeUIKit.MakeBox("Dot", parent, Vector2.zero, new Vector2(dotSize, dotSize), ink, false);
        dot.raycastTarget = false;
    }

    // Shows `value` (clamped to the available digits) with the dot after digit `dotAfter` (-1 = no dot).
    // Leading zeros before the dot are left blank, except the digit just before it.
    public void ShowNumber(int value, int dotAfter)
    {
        var max = (int)Mathf.Pow(10, DigitCount) - 1;
        var text = Mathf.Clamp(value, 0, max).ToString("D" + DigitCount);

        for (var i = 0; i < DigitCount; i++)
        {
            var blank = dotAfter >= 0 && i < dotAfter && AllZeros(text, i);
            SetDigit(i, blank ? -1 : text[i] - '0');
        }

        if (dotAfter < 0 || dotAfter >= DigitCount - 1)
        {
            dot.color = Color.clear;
            return;
        }

        var digitEnd = originX + dotAfter * (digitWidth + gap) + digitWidth;
        RuntimeUIKit.SetTopLeft(dot.rectTransform, new Vector2(digitEnd + (gap - dotSize) / 2f, originY - digitHeight + dotSize + 2f),
            new Vector2(dotSize, dotSize));
        dot.color = ink;
    }

    public void ShowBlank()
    {
        for (var i = 0; i < DigitCount; i++) SetDigit(i, -1);
        dot.color = Color.clear;
    }

    private static bool AllZeros(string text, int upTo)
    {
        for (var i = 0; i <= upTo; i++)
        {
            if (text[i] != '0') return false;
        }
        return true;
    }

    private void SetDigit(int index, int value)
    {
        for (var s = 0; s < 7; s++)
        {
            var lit = value >= 0 && DigitSegments[value][s];
            segments[index][s].color = lit ? ink : ghost;
        }
    }

    private Image[] BuildDigit(Transform parent, Vector2 topLeft, float t)
    {
        var w = digitWidth;
        var h = digitHeight;
        var halfH = (h - 3f * t) / 2f;

        var result = new Image[7];
        result[0] = MakeSegment(parent, topLeft + new Vector2(t, 0f), new Vector2(w - 2f * t, t));
        result[1] = MakeSegment(parent, topLeft + new Vector2(w - t, -t), new Vector2(t, halfH));
        result[2] = MakeSegment(parent, topLeft + new Vector2(w - t, -(2f * t + halfH)), new Vector2(t, halfH));
        result[3] = MakeSegment(parent, topLeft + new Vector2(t, -(h - t)), new Vector2(w - 2f * t, t));
        result[4] = MakeSegment(parent, topLeft + new Vector2(0f, -(2f * t + halfH)), new Vector2(t, halfH));
        result[5] = MakeSegment(parent, topLeft + new Vector2(0f, -t), new Vector2(t, halfH));
        result[6] = MakeSegment(parent, topLeft + new Vector2(t, -(t + halfH)), new Vector2(w - 2f * t, t));
        foreach (var segment in result) segment.color = ghost;
        return result;
    }

    private static Image MakeSegment(Transform parent, Vector2 topLeft, Vector2 size)
    {
        var image = RuntimeUIKit.MakeBox("Segment", parent, topLeft, size, Color.clear, false);
        image.raycastTarget = false;
        return image;
    }
}
