using System;
using System.Collections.Generic;
using UnityEngine;

// The lessons of Professor Thouvenot, written in JSON files (Assets/Resources/Lessons/<id>.json): one file per instrument, plus the lessons on the gemstone and the
// crystal systems. The board at the entrance of the laboratory lists them and replays them. To change a text, edit the JSON: no code to touch.
[Serializable]
public class LessonStepData
{
    public string title;
    public string text;          // rich text allowed (<b>...</b>); "\n" for a new line
    public string figure;        // the drawing of the board (see LessonFigures), or empty
    public int focus;            // what the drawing shows (which criterion, which crystal system...)
    public string image;         // optional: a picture in Resources/Lessons/Images (without extension); it replaces the drawing when it exists
}

[Serializable]
public class LessonToolData
{
    public string name;
    public string measures;
    public string where;
    public string[] records;     // what the instrument gives to the grimoire
    public string[] tips;
}

[Serializable]
public class LessonData
{
    public string id;
    public string kind;          // "tool" or "concept"
    public string title;
    public string unlock;        // the ExaminationTool.MeasurementType that opens the lesson, or empty for a lesson that is always open
    public string animation;     // optional: the name of a specific animated lesson (RefractionLessonUI)
    public LessonToolData tool;
    public LessonStepData[] steps;
}

public static class LessonLibrary
{
    // The order of the board.
    public static readonly string[] Order =
    {
        "gemmes", "durete", "tenacite", "stabilite", "loupe", "balance_carats", "balance_hydrostatique", "refractometre", "systemes_cristallins", "polariscope",
    };

    private static readonly Dictionary<string, LessonData> cache = new Dictionary<string, LessonData>();

    public static LessonData Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (cache.TryGetValue(id, out var known)) return known;

        LessonData data = null;
        var asset = Resources.Load<TextAsset>("Lessons/" + id);
        if (asset != null)
        {
            try { data = JsonUtility.FromJson<LessonData>(asset.text); }
            catch (Exception) { data = null; }
        }
        if (data == null) Debug.LogWarning($"Leçon introuvable ou illisible : Lessons/{id}.json");
        cache[id] = data;
        return data;
    }

    // A lesson opens when the instrument it teaches is open (Thouvenot has presented it). An old save has everything.
    public static bool Unlocked(LessonData lesson)
    {
        if (lesson == null) return false;
        var story = StoryState.Instance;
        if (story == null || story.Legacy || string.IsNullOrEmpty(lesson.unlock)) return true;
        if (Enum.TryParse(lesson.unlock, out ExaminationTool.MeasurementType type)) return story.ToolUnlocked(type);
        return true;
    }
}
