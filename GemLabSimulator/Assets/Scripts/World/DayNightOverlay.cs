using UnityEngine;
using UnityEngine.UI;

public class DayNightOverlay : MonoBehaviour
{
    [SerializeField] private Image overlayImage;

    // A full 24h in-game day takes 14 real minutes, matching Stardew Valley's day length.
    private const float CycleDurationSeconds = 840f;
    private const float DayPhaseStart = 8f / 24f;

    private static float dayResetTime = -1f;

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
        dayResetTime = Time.time - DayPhaseStart * CycleDurationSeconds;
    }

    // Available scene-wide (not just where the overlay itself exists) so any UI can show the current time of day.
    public static float GetCurrentPhase()
    {
        var referenceTime = dayResetTime >= 0f ? dayResetTime : 0f;
        return Mathf.Repeat((Time.time - referenceTime) / CycleDurationSeconds, 1f);
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
        overlayImage.color = Evaluate(GetCurrentPhase());
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
