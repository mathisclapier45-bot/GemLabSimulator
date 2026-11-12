using UnityEngine;
using UnityEngine.UI;

public class DayNightOverlay : MonoBehaviour
{
    [SerializeField] private Image overlayImage;

    // A full 24h in-game day takes 20 real minutes (it was 14, the length of a day in Stardew Valley).
    private const float CycleDurationSeconds = 1200f;      // one game day, in real seconds (20 minutes; it was 14)
    // The player wakes up at 7h, after sleeping and when a game is loaded; a new game starts at 7h too.
    private const float DayPhaseStart = 7f / 24f;

    private static float dayResetTime;
    private static bool dayResetSet;

    // The clock can be stopped (the prologue: the player lands at nightfall and the walk through the town does not wear the night out).
    private static bool frozen;
    private static float frozenPhase;
    private static float frozenSince;

    private struct Keyframe
    {
        public float Time;
        public Color Color;
    }

    private static readonly Keyframe[] Keyframes =
    {
        new Keyframe { Time = 0.00f, Color = new Color(0.12f, 0.14f, 0.32f, 0.45f) },
        new Keyframe { Time = 0.12f, Color = new Color(0.85f, 0.65f, 0.45f, 0.15f) },
        new Keyframe { Time = 0.30f, Color = new Color(0f, 0f, 0f, 0f) },
        new Keyframe { Time = 0.60f, Color = new Color(0f, 0f, 0f, 0f) },
        new Keyframe { Time = 0.68f, Color = new Color(0.85f, 0.45f, 0.25f, 0.30f) },
        new Keyframe { Time = 0.80f, Color = new Color(0.45f, 0.25f, 0.35f, 0.42f) },
        new Keyframe { Time = 0.90f, Color = new Color(0.10f, 0.12f, 0.30f, 0.55f) },
        new Keyframe { Time = 1.00f, Color = new Color(0.12f, 0.14f, 0.32f, 0.45f) },
    };

    // Called when the player sleeps, so Town always resumes in daylight regardless of the visual cycle's position.
    public static void ResetToDay()
    {
        frozen = false;
        dayResetTime = Time.time - DayPhaseStart * CycleDurationSeconds;
        dayResetSet = true;
    }

    // Sets the time of the day, from 0 to 24.
    public static void SetHour(float hour)
    {
        frozen = false;
        dayResetTime = Time.time - Mathf.Repeat(hour, 24f) / 24f * CycleDurationSeconds;
        dayResetSet = true;
    }

    // Available scene-wide (not just where the overlay itself exists) so any UI can show the current time of day.
    // Stops the clock at the time it is, and starts it again from there.
    public static void Freeze()
    {
        if (frozen) return;
        frozenPhase = GetCurrentPhase();
        frozenSince = Time.time;
        frozen = true;
    }

    public static void Unfreeze()
    {
        if (!frozen) return;
        dayResetTime += Time.time - frozenSince;
        frozen = false;
    }

    public static float GetCurrentPhase()
    {
        if (!dayResetSet) ResetToDay();
        if (frozen) return frozenPhase;
        return Mathf.Repeat((Time.time - dayResetTime) / CycleDurationSeconds, 1f);
    }

    public static string GetClockLabel()
    {
        var totalMinutes = Mathf.FloorToInt(GetCurrentPhase() * 24f * 60f);
        var hours = (totalMinutes / 60) % 24;
        var minutes = totalMinutes % 60;
        return $"{hours:D2}h{minutes:D2}";
    }

    private void Update()
    {
        if (overlayImage == null) return;
        // The lighting effect on the camera replaces the translucent veil.
        overlayImage.color = DayNightLighting.Active ? new Color(0f, 0f, 0f, 0f) : Evaluate(GetCurrentPhase());
    }

    private static Color Evaluate(float phase)
    {
        for (var i = 0; i < Keyframes.Length - 1; i++)
        {
            var a = Keyframes[i];
            var b = Keyframes[i + 1];
            if (phase < a.Time || phase > b.Time) continue;

            var t = Mathf.InverseLerp(a.Time, b.Time, phase);
            return Color.Lerp(a.Color, b.Color, t);
        }
        return Keyframes[Keyframes.Length - 1].Color;
    }
}
