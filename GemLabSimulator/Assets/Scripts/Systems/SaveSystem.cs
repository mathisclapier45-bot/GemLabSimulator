using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void Save()
    {
        if (PlayerWallet.Instance == null || PlayerInventory.Instance == null || PlayerChest.Instance == null)
            return;

        var data = new SaveData
        {
            money = PlayerWallet.Instance.Money,
            sceneName = SceneManager.GetActiveScene().name,
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

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public static void Load()
    {
        if (!HasSave()) return;

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data == null) return;

        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.SetMoney(data.money);

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.LoadGems(RestoreGems(data.inventoryGems));

        if (PlayerChest.Instance != null)
            PlayerChest.Instance.LoadGems(RestoreGems(data.chestGems));

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
