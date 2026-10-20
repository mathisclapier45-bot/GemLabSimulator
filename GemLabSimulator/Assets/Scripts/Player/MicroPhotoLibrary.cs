using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class MicroPhotoEntry
{
    public string fileName;
    public string gemName;
    public string gemId;
    public float magnification;
    public string lighting;
    public int day;
}

// Photos taken with the microscope camera. The images are PNG files in the game's data folder; the list below is what
// the lab computer will show (its interface comes later) and is saved with the game.
public class MicroPhotoLibrary : MonoBehaviour
{
    public static MicroPhotoLibrary Instance { get; private set; }

    private readonly List<MicroPhotoEntry> photos = new List<MicroPhotoEntry>();

    public IReadOnlyList<MicroPhotoEntry> Photos => photos;
    public event Action OnPhotosChanged;

    public static string Folder => Path.Combine(Application.persistentDataPath, "MicroPhotos");

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static MicroPhotoLibrary Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<MicroPhotoLibrary>();
        return Instance;
    }

    public void AddPhoto(Texture2D image, MicroPhotoEntry entry)
    {
        Directory.CreateDirectory(Folder);
        entry.fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}_{photos.Count + 1}.png";
        File.WriteAllBytes(Path.Combine(Folder, entry.fileName), image.EncodeToPNG());
        photos.Add(entry);
        OnPhotosChanged?.Invoke();
    }

    public Texture2D LoadImage(MicroPhotoEntry entry)
    {
        var path = Path.Combine(Folder, entry.fileName);
        if (!File.Exists(path)) return null;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    public void LoadEntries(IEnumerable<MicroPhotoEntry> entries)
    {
        photos.Clear();
        if (entries != null) photos.AddRange(entries);
        OnPhotosChanged?.Invoke();
    }
}
