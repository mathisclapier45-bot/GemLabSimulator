using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    public const int SlotCount = 3;

    public static int ActiveSlot { get; set; } = 1;
    public static bool PendingLoad { get; set; } = true;

    private static string SavePath(int slot) => Path.Combine(Application.persistentDataPath, $"savegame_{slot}.json");

    public static bool HasSave(int slot)
    {
        return File.Exists(SavePath(slot));
    }

    public static bool HasSave()
    {
        return HasSave(ActiveSlot);
    }

    public static SaveData PeekSlot(int slot)
    {
        if (!HasSave(slot)) return null;
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath(slot)));
    }

    public static void DeleteSlot(int slot)
    {
        if (HasSave(slot)) File.Delete(SavePath(slot));
    }

    public static void CreateNewSave(int slot, string saveName)
    {
        var data = new SaveData
        {
            saveName = saveName,
            money = 0,
            currentDay = 1,
            sceneName = "Home",
        };
        File.WriteAllText(SavePath(slot), JsonUtility.ToJson(data, true));
    }

    public static void Save()
    {
        if (PlayerWallet.Instance == null || PlayerInventory.Instance == null || PlayerChest.Instance == null)
            return;

        var existingName = PeekSlot(ActiveSlot)?.saveName;

        var data = new SaveData
        {
            saveName = string.IsNullOrEmpty(existingName) ? $"Partie {ActiveSlot}" : existingName,
            money = PlayerWallet.Instance.Money,
            currentDay = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1,
            sceneName = SceneManager.GetActiveScene().name,
            reputationPoints = ReputationState.Instance != null ? ReputationState.Instance.Points : 0,
        };

        if (PlayerController.Instance != null)
        {
            var pos = PlayerController.Instance.transform.position;
            data.playerX = pos.x;
            data.playerY = pos.y;
        }

        foreach (var gem in PlayerInventory.Instance.Gems)
            data.inventoryGems.Add(gem.ToSaveData());
        foreach (var gem in PlayerChest.Instance.Gems)
            data.chestGems.Add(gem.ToSaveData());

        if (GemCodex.Instance != null)
            data.discoveredSpecies = GemCodex.Instance.GetDiscoveredNames();

        File.WriteAllText(SavePath(ActiveSlot), JsonUtility.ToJson(data, true));
    }

    public static void Load()
    {
        if (!HasSave()) return;

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath(ActiveSlot)));
        if (data == null) return;

        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.SetMoney(data.money);

        if (GameClock.Instance != null)
            GameClock.Instance.SetDay(data.currentDay);

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.LoadGems(RestoreGems(data.inventoryGems));

        if (PlayerChest.Instance != null)
            PlayerChest.Instance.LoadGems(RestoreGems(data.chestGems));

        if (GemCodex.Instance != null)
            GemCodex.Instance.LoadDiscovered(data.discoveredSpecies);

        if (ReputationState.Instance != null)
            ReputationState.Instance.SetPoints(data.reputationPoints);

        if (!string.IsNullOrEmpty(data.sceneName) && SceneManager.GetActiveScene().name != data.sceneName)
            SceneManager.LoadScene(data.sceneName);

        if (PlayerController.Instance != null)
            PlayerController.Instance.transform.position = new Vector3(data.playerX, data.playerY, 0f);
    }

    private static List<GemInstance> RestoreGems(List<GemInstanceSaveData> savedGems)
    {
        var result = new List<GemInstance>();
        if (GemDatabase.Instance == null) return result;

        foreach (var saved in savedGems)
        {
            var definition = GemDatabase.Instance.FindByAssetName(saved.definitionName);
            if (definition != null) result.Add(GemInstance.FromSaveData(definition, saved));
        }
        return result;
    }
}
