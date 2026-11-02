using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sound effects of the game: clicks on the buttons, doors, tools, chests, the lift, footsteps, coins, gems... All of them are synthesized
// in code (no audio files), the first time they are played. Sfx.Play(Sfx.Id.X) from anywhere.
public static class Sfx
{
    public enum Id
    {
        Click, Chat, ToolOpen, ChestOpen, Ding, Stairs, Sleep, Paper, Keys, Printer, DoorOpen, DoorLocked,
        Coin, Spend, Pickup, GemClink, Success, Error, Flame, StepGrass, StepWood, StepStone,
        Shutter, ComputerBoot, ComputerClick, ComputerOff,
        PageTurn, PageFlurry, BookOpen, BookClose, CardSwish, PackTear, CardFlip, RareChime, SuperChime, UltraSparkle, SecretShimmer, NewCard,
    }

    // Overall volume of the effects, from 0 to 1.
    public static float Volume = 0.45f;

    private static SfxPlayer player;

    public static void Play(Id id, float volume = 1f, float pitch = 1f)
    {
        if (!Application.isPlaying) return;
        if (player == null)
        {
            var go = new GameObject("Sfx");
            UnityEngine.Object.DontDestroyOnLoad(go);
            player = go.AddComponent<SfxPlayer>();
        }
        player.Play(id, volume * Volume, pitch);
    }

    // A footstep, according to the ground of the scene: the earth of the town, the stone of the tunnel, the floor of the buildings.
    public static void Step()
    {
        var scene = SceneManager.GetActiveScene().name;
        var id = scene == "Town" ? Id.StepGrass : scene == "Tunnel" ? Id.StepStone : Id.StepWood;
        Play(id, 0.35f, UnityEngine.Random.Range(0.9f, 1.1f));
    }
}

// Plays the effects (a few at a time) and makes a click when a button of the interface is pressed.
public class SfxPlayer : MonoBehaviour
{
    private const int Rate = 44100;
    private const int Voices = 8;

    private readonly Dictionary<Sfx.Id, AudioClip> clips = new Dictionary<Sfx.Id, AudioClip>();
    private readonly Dictionary<Sfx.Id, float> lastPlayed = new Dictionary<Sfx.Id, float>();
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private AudioSource[] voices;
    private int next;

    private void Awake()
    {
        voices = new AudioSource[Voices];
        for (var i = 0; i < Voices; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
        }
    }

    // The buttons of the interface click when they are pressed.
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0) || EventSystem.current == null) return;
        var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        hits.Clear();
        EventSystem.current.RaycastAll(data, hits);
        foreach (var hit in hits)
        {
            var button = hit.gameObject.GetComponentInParent<Button>();
            if (button == null) continue;
            if (button.interactable) Play(Sfx.Id.Click, 0.6f, 1f);
            return;
        }
    }

    public void Play(Sfx.Id id, float volume, float pitch)
    {
        // The same effect never plays twice in a few hundredths of a second (a handful of gems added at once).
        var now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out var last) && now - last < 0.05f) return;
        lastPlayed[id] = now;

        if (!clips.TryGetValue(id, out var clip))
        {
            clip = Build(id);
            clips[id] = clip;
        }
        var source = voices[next];
        next = (next + 1) % Voices;
        source.pitch = pitch;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ---- Synthesis -----------------------------------------------------------------------------------------------

    private static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);
    private static float Decay(float t, float rate) => t < 0f ? 0f : Mathf.Exp(-t * rate);

    private AudioClip Make(string name, float seconds, Func<float, float> sample)
    {
        var count = Mathf.RoundToInt(Rate * seconds);
        var data = new float[count];
        for (var i = 0; i < count; i++)
        {
            var t = i / (float)Rate;
            // A short fade at both ends so nothing clicks.
            var edge = Mathf.Min(1f, i / 64f) * Mathf.Min(1f, (count - i) / 256f);
            data[i] = Mathf.Clamp(sample(t), -1f, 1f) * edge;
        }
        var clip = AudioClip.Create(name, count, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Noise through a one-pole filter: a small alpha is dull, an alpha close to 1 is bright.
    private static Func<float> Noise(int seed, float alpha)
    {
        var rnd = new System.Random(seed);
        var state = 0f;
        return () =>
        {
            state += ((float)rnd.NextDouble() * 2f - 1f - state) * alpha;
            return state;
        };
    }

    private static Func<float> Hiss(int seed)
    {
        var rnd = new System.Random(seed);
        return () => (float)rnd.NextDouble() * 2f - 1f;
    }

    // The sounds are kept low and soft: mostly below 1 kHz, short, with no sharp attack.
    private AudioClip Build(Sfx.Id id)
    {
        var name = id.ToString();
        switch (id)
        {
            case Sfx.Id.Click:
                return Make(name, 0.06f, t => (Sine(1200f, t) * 0.35f + Sine(700f, t) * 0.25f) * Decay(t, 120f));

            case Sfx.Id.Chat:
                return Make(name, 0.1f, t => t < 0.04f ? Sine(330f, t) * 0.22f : Sine(440f, t) * 0.22f * Decay(t - 0.04f, 25f));

            case Sfx.Id.ToolOpen:
            {
                var noise = Noise(3, 0.3f);
                return Make(name, 0.2f, t => Sine(120f, t) * 0.55f * Decay(t, 28f) + noise() * 0.2f * Decay(t, 150f)
                    + (t > 0.07f ? Sine(600f, t) * 0.16f * Decay(t - 0.07f, 80f) : 0f));
            }

            case Sfx.Id.ChestOpen:
            {
                var noise = Noise(5, 0.12f);
                return Make(name, 0.5f, t =>
                {
                    var creak = t < 0.34f ? Sine(160f + 100f * (t / 0.34f) + Mathf.Sin(t * 40f) * 10f, t) * 0.22f * Mathf.Sin(Mathf.PI * t / 0.34f) : 0f;
                    var thunk = t > 0.34f ? Sine(100f, t) * 0.5f * Decay(t - 0.34f, 26f) : 0f;
                    return creak + noise() * 0.12f * (t < 0.34f ? 1f : 0f) + thunk;
                });
            }

            case Sfx.Id.Ding:
                return Make(name, 1.1f, t => (Sine(784f, t) * 0.35f + Sine(1568f, t) * 0.08f) * Decay(t, 3.5f));

            case Sfx.Id.Stairs:
            {
                var noise = Noise(7, 0.25f);
                return Make(name, 0.5f, t =>
                {
                    var sum = 0f;
                    for (var i = 0; i < 4; i++)
                    {
                        var local = t - i * 0.11f;
                        if (local >= 0f) sum += (Sine(120f, local) * 0.4f + noise() * 0.22f) * Decay(local, 48f);
                    }
                    return sum;
                });
            }

            case Sfx.Id.Sleep:
                return Make(name, 1.5f, t =>
                {
                    var notes = new[] { 523f, 440f, 349f };
                    var sum = 0f;
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var local = t - i * 0.3f;
                        if (local >= 0f) sum += (Sine(notes[i], local) * 0.3f + Sine(notes[i] * 2f, local) * 0.05f) * Decay(local, 3.2f);
                    }
                    return sum;
                });

            case Sfx.Id.Paper:
            {
                var noise = Noise(11, 0.3f);
                return Make(name, 0.18f, t => noise() * 0.5f * Mathf.Sin(Mathf.PI * t / 0.18f));
            }

            case Sfx.Id.Keys:
            {
                var noise = Noise(13, 0.4f);
                return Make(name, 0.22f, t =>
                {
                    var sum = 0f;
                    foreach (var at in new[] { 0f, 0.06f, 0.13f })
                        if (t >= at) sum += (noise() * 0.3f + Sine(800f, t - at) * 0.12f) * Decay(t - at, 150f);
                    return sum;
                });
            }

            // The printer: the motor, then the paper ratchets through the rollers.
            case Sfx.Id.Printer:
            {
                var motor = Noise(17, 0.12f);
                var feed = Noise(18, 0.3f);
                return Make(name, 1.8f, t =>
                {
                    var envelope = Mathf.Min(1f, t / 0.15f) * Mathf.Min(1f, (1.8f - t) / 0.2f);
                    var hum = motor() * (0.35f + 0.25f * Sine(26f, t)) + Sine(95f, t) * 0.1f;
                    var tick = feed() * 0.3f * Decay(Mathf.Repeat(t, 0.075f), 90f);
                    return (hum * 0.7f + tick) * envelope * 0.8f;
                });
            }

            // A wooden door: the latch, the creak of the hinges, a soft bump as it swings against its stop.
            case Sfx.Id.DoorOpen:
            {
                var latch = Noise(19, 0.45f);
                var wood = Noise(20, 0.12f);
                return Make(name, 0.7f, t =>
                {
                    var click = latch() * 0.4f * Decay(t, 160f) + Sine(700f, t) * 0.18f * Decay(t, 110f);
                    var creak = 0f;
                    if (t > 0.05f && t < 0.48f)
                    {
                        var u = (t - 0.05f) / 0.43f;
                        var f = 330f - 100f * u + Mathf.Sin(t * 50f) * 14f;
                        creak = (Sine(f, t) * 0.2f + Sine(f * 2f, t) * 0.08f) * Mathf.Sin(Mathf.PI * u) + wood() * 0.1f * Mathf.Sin(Mathf.PI * u);
                    }
                    var bump = t > 0.5f ? (Sine(140f, t) * 0.4f + Sine(280f, t) * 0.12f) * Decay(t - 0.5f, 22f) : 0f;
                    return click + creak + bump;
                });
            }

            case Sfx.Id.DoorLocked:
            {
                var noise = Noise(23, 0.25f);
                return Make(name, 0.35f, t =>
                {
                    var sum = 0f;
                    foreach (var at in new[] { 0f, 0.12f })
                        if (t >= at) sum += (Sine(150f, t - at) * 0.4f + Sine(300f, t - at) * 0.12f + noise() * 0.25f) * Decay(t - at, 36f);
                    if (t > 0.2f) sum += noise() * 0.15f * Decay(t - 0.2f, 40f);
                    return sum;
                });
            }

            case Sfx.Id.Coin:
                return Make(name, 0.45f, t => Sine(1046f, t) * 0.28f * Decay(t, 14f) + (t > 0.07f ? Sine(1397f, t) * 0.28f * Decay(t - 0.07f, 10f) : 0f));

            case Sfx.Id.Spend:
                return Make(name, 0.4f, t => Sine(880f, t) * 0.28f * Decay(t, 14f) + (t > 0.07f ? Sine(659f, t) * 0.28f * Decay(t - 0.07f, 10f) : 0f));

            case Sfx.Id.Pickup:
                return Make(name, 0.12f, t => Sine(320f * (1f + t / 0.12f), t) * 0.3f * Decay(t, 22f));

            case Sfx.Id.GemClink:
                return Make(name, 0.4f, t => (Sine(1760f, t) * 0.22f + Sine(2349f, t) * 0.1f) * Decay(t, 11f));

            case Sfx.Id.Success:
                return Make(name, 0.6f, t =>
                {
                    var notes = new[] { 659f, 784f, 988f, 1319f };
                    var sum = 0f;
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var local = t - i * 0.09f;
                        if (local >= 0f) sum += Sine(notes[i], local) * 0.2f * Decay(local, 7f);
                    }
                    return sum;
                });

            case Sfx.Id.Error:
                return Make(name, 0.3f, t =>
                {
                    var a = (Sine(130f, t) * 0.25f + Sine(390f, t) * 0.06f) * Decay(t, 11f);
                    var b = t > 0.1f ? (Sine(100f, t) * 0.25f + Sine(300f, t) * 0.06f) * Decay(t - 0.1f, 11f) : 0f;
                    return a + b;
                });

            case Sfx.Id.Flame:
            {
                var noise = Noise(29, 0.18f);
                return Make(name, 0.35f, t => noise() * 0.35f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.3f)));
            }

            case Sfx.Id.StepGrass:
            {
                var noise = Noise(31, 0.15f);
                return Make(name, 0.1f, t => noise() * 0.7f * Decay(t, 45f));
            }

            case Sfx.Id.StepWood:
            {
                var noise = Noise(37, 0.3f);
                return Make(name, 0.1f, t => Sine(140f, t) * 0.4f * Decay(t, 42f) + noise() * 0.22f * Decay(t, 160f));
            }

            // A camera: the shutter opens, the mirror clacks back, and a faint whirr of the motor.
            case Sfx.Id.Shutter:
            {
                var noise = Noise(43, 0.5f);
                var motor = Noise(44, 0.2f);
                return Make(name, 0.4f, t =>
                {
                    var open = (noise() * 0.4f + Sine(900f, t) * 0.18f) * Decay(t, 140f);
                    var close = t > 0.07f ? (noise() * 0.35f + Sine(520f, t) * 0.2f) * Decay(t - 0.07f, 110f) : 0f;
                    var whirr = t > 0.14f && t < 0.38f ? motor() * 0.14f * Mathf.Sin(Mathf.PI * (t - 0.14f) / 0.24f) * (0.6f + 0.4f * Sine(90f, t)) : 0f;
                    return open + close + whirr;
                });
            }

            case Sfx.Id.ComputerBoot:
                return Make(name, 1.0f, t =>
                {
                    var notes = new[] { 392f, 523f, 659f, 784f };
                    var sum = 0f;
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var local = t - i * 0.13f;
                        if (local >= 0f) sum += (Sine(notes[i], local) * 0.2f + Sine(notes[i] * 2f, local) * 0.04f) * Mathf.Min(1f, local / 0.02f) * Decay(local, 4f);
                    }
                    return sum;
                });

            case Sfx.Id.ComputerClick:
            {
                var noise = Noise(47, 0.35f);
                return Make(name, 0.05f, t => (noise() * 0.28f + Sine(700f, t) * 0.14f) * Decay(t, 170f));
            }

            case Sfx.Id.ComputerOff:
                return Make(name, 0.7f, t =>
                {
                    var notes = new[] { 659f, 523f, 392f };
                    var sum = 0f;
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var local = t - i * 0.11f;
                        if (local >= 0f) sum += Sine(notes[i], local) * 0.2f * Decay(local, 5f);
                    }
                    return sum;
                });

            // A page of the binder turning: a soft rustle with a little flutter.
            case Sfx.Id.PageTurn:
            {
                var noise = Noise(53, 0.38f);
                return Make(name, 0.3f, t =>
                {
                    var envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.3f)), 1.5f);
                    return noise() * 0.5f * envelope * (0.7f + 0.3f * Sine(34f, t));
                });
            }

            // Several pages in a row, when the binder jumps to another series.
            case Sfx.Id.PageFlurry:
            {
                var noise = Noise(54, 0.38f);
                return Make(name, 0.65f, t =>
                {
                    var sum = 0f;
                    for (var i = 0; i < 4; i++)
                    {
                        var local = t - i * 0.1f;
                        if (local < 0f || local > 0.28f) continue;
                        sum += Mathf.Pow(Mathf.Sin(Mathf.PI * local / 0.28f), 1.5f) * (0.5f - i * 0.08f);
                    }
                    return noise() * 0.45f * sum;
                });
            }

            case Sfx.Id.BookOpen:
            {
                var noise = Noise(55, 0.3f);
                return Make(name, 0.7f, t =>
                {
                    var creak = t < 0.25f ? Sine(140f + 50f * (t / 0.25f), t) * 0.13f * Mathf.Sin(Mathf.PI * t / 0.25f) : 0f;
                    var rustle = t > 0.15f ? noise() * 0.35f * Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - 0.15f) / 0.45f)) : 0f;
                    var thump = t > 0.12f ? Sine(95f, t) * 0.3f * Decay(t - 0.12f, 18f) : 0f;
                    return creak + rustle + thump;
                });
            }

            case Sfx.Id.BookClose:
            {
                var noise = Noise(56, 0.25f);
                return Make(name, 0.4f, t => Sine(90f, t) * 0.5f * Decay(t, 16f) + noise() * 0.3f * Decay(t, 60f));
            }

            case Sfx.Id.CardSwish:
            {
                var noise = Noise(57, 0.45f);
                return Make(name, 0.14f, t => noise() * 0.4f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.14f)));
            }

            // A booster being torn open: the foil rips, crackles, then gives way.
            case Sfx.Id.PackTear:
            {
                var rip = Noise(58, 0.5f);
                var crackle = Hiss(59);
                return Make(name, 0.7f, t =>
                {
                    var ripping = t < 0.45f ? rip() * 0.45f * Mathf.Sin(Mathf.PI * t / 0.45f) * (0.6f + 0.4f * Sine(70f, t)) : 0f;
                    var crinkle = crackle() > 0.86f ? 0.3f * Decay(Mathf.Repeat(t, 0.02f), 150f) : 0f;
                    var pop = t > 0.45f ? Sine(220f, t) * 0.3f * Decay(t - 0.45f, 26f) : 0f;
                    return ripping + crinkle * (t < 0.5f ? 1f : 0f) + pop;
                });
            }

            case Sfx.Id.CardFlip:
            {
                var noise = Noise(60, 0.45f);
                return Make(name, 0.16f, t => noise() * 0.35f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.12f)) + (t > 0.06f ? Sine(500f, t) * 0.1f * Decay(t - 0.06f, 70f) : 0f));
            }

            case Sfx.Id.RareChime:
                return Make(name, 0.7f, t => (Sine(784f, t) * 0.22f * Decay(t, 5f)) + (t > 0.1f ? Sine(1047f, t) * 0.22f * Decay(t - 0.1f, 5f) : 0f));

            case Sfx.Id.SuperChime:
                return Make(name, 0.9f, t =>
                {
                    var notes = new[] { 784f, 988f, 1319f };
                    var sum = 0f;
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var local = t - i * 0.1f;
                        if (local >= 0f) sum += (Sine(notes[i], local) * 0.2f + Sine(notes[i] * 2f, local) * 0.04f) * Decay(local, 4.5f);
                    }
                    return sum;
                });

            case Sfx.Id.UltraSparkle:
                return Make(name, 1.2f, t =>
                {
                    var notes = new[] { 659f, 784f, 988f, 1319f, 1568f, 1976f };
                    var sum = 0f;
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var local = t - i * 0.075f;
                        if (local >= 0f) sum += Sine(notes[i], local) * (0.15f + 0.05f * Sine(14f, local)) * Decay(local, 3.5f);
                    }
                    return sum;
                });

            // The secret cards: a soft chord that swells and shimmers.
            case Sfx.Id.SecretShimmer:
                return Make(name, 1.6f, t =>
                {
                    var swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.6f));
                    var vibrato = 1f + 0.004f * Sine(6f, t);
                    var chord = Sine(523f * vibrato, t) + Sine(659f * vibrato, t) + Sine(784f * vibrato, t) + Sine(1047f * vibrato, t) * 0.5f;
                    return chord * 0.08f * swell * (0.8f + 0.2f * Sine(9f, t));
                });

            case Sfx.Id.NewCard:
                return Make(name, 0.25f, t => Sine(880f, t) * 0.18f * Decay(t, 11f));

            default:
            {
                var noise = Noise(41, 0.4f);
                return Make(name, 0.09f, t => Sine(190f, t) * 0.35f * Decay(t, 55f) + noise() * 0.25f * Decay(t, 180f));
            }
        }
    }
}
