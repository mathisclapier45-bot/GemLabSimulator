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
            sceneName = "MaisonJoueur",
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
            timeOfDay = OpeningHours.CurrentHour,
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
        if (PlayerCabinet.Instance != null)
        {
            foreach (var gem in PlayerCabinet.Instance.Gems)
                data.cabinetGems.Add(gem.ToSaveData());
        }

        if (PlayerOrderCabinet.Instance != null)
        {
            foreach (var gem in PlayerOrderCabinet.Instance.Gems)
                data.orderCabinetGems.Add(gem.ToSaveData());
        }

        var ownedTools = PlayerOwnedTools.Instance;
        if (ownedTools != null)
        {
            data.ownedTools.AddRange(ownedTools.Owned);
            foreach (var slot in ownedTools.BagSlots) data.bagSlots.Add(slot ?? "");
            data.lanternLit = ownedTools.LanternLit;
        }
        var pets = PlayerPets.Instance;
        if (pets != null)
        {
            data.ownedPets.AddRange(pets.Owned);
            data.equippedPet = pets.Equipped ?? "";
        }
        PlayerCards.Instance?.WriteTo(data);
        if (MicroPhotoLibrary.Instance != null) data.microPhotos.AddRange(MicroPhotoLibrary.Instance.Photos);

        if (GemCodex.Instance != null)
            data.discoveredSpecies = GemCodex.Instance.GetDiscoveredNames();

        if (LapidaryState.Instance != null)
            data.lapidaryJobs = LapidaryState.Instance.ToSaveData();
        if (DiamondCuttingState.Instance != null)
            data.diamondCutJobs = DiamondCuttingState.Instance.ToSaveData();

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

        if (data.timeOfDay >= 0f) DayNightOverlay.SetHour(data.timeOfDay);
        else DayNightOverlay.ResetToDay();

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.LoadGems(RestoreGems(data.inventoryGems));

        if (PlayerChest.Instance != null)
            PlayerChest.Instance.LoadGems(RestoreGems(data.chestGems));

        if (PlayerCabinet.Instance != null)
            PlayerCabinet.Instance.LoadGems(RestoreGems(data.cabinetGems));

        if (PlayerOrderCabinet.Instance != null)
            PlayerOrderCabinet.Instance.LoadGems(RestoreGems(data.orderCabinetGems));

        var tools = PlayerOwnedTools.Ensure();
        if (tools != null)
        {
            tools.LoadTools(data.ownedTools, data.bagSlots);
            tools.SetLantern(data.lanternLit);
        }

        var pets = PlayerPets.Ensure();
        if (pets != null) pets.LoadPets(data.ownedPets, data.equippedPet);

        var cards = PlayerCards.Ensure();
        if (cards != null) cards.LoadFrom(data);

        var photoLibrary = MicroPhotoLibrary.Ensure();
        if (photoLibrary != null) photoLibrary.LoadEntries(data.microPhotos);

        if (GemCodex.Instance != null)
            GemCodex.Instance.LoadDiscovered(data.discoveredSpecies);

        if (ReputationState.Instance != null)
            ReputationState.Instance.SetPoints(data.reputationPoints);

        if (LapidaryState.Instance != null)
            LapidaryState.Instance.LoadJobs(data.lapidaryJobs, RestoreGem);

        var diamondCutting = DiamondCuttingState.Ensure();
        if (diamondCutting != null) diamondCutting.LoadJobs(data.diamondCutJobs, RestoreGem);

        if (!string.IsNullOrEmpty(data.sceneName) && SceneManager.GetActiveScene().name != data.sceneName)
            SceneManager.LoadScene(data.sceneName);

        if (PlayerController.Instance != null)
            PlayerController.Instance.transform.position = new Vector3(data.playerX, data.playerY, 0f);
    }

    private static GemInstance RestoreGem(GemInstanceSaveData saved)
    {
        if (saved == null || GemDatabase.Instance == null) return null;
        var definition = GemDatabase.Instance.FindByAssetName(saved.definitionName);
        return definition != null ? GemInstance.FromSaveData(definition, saved) : null;
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
