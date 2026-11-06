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

    public static void CreateNewSave(int slot, string saveName, int birthdayDay = 0, int birthdayMonth = 0)
    {
        var data = new SaveData
        {
            saveName = saveName,
            birthdayDay = birthdayDay,
            birthdayMonth = birthdayMonth,
            money = 0,
            currentDay = 1,
            sceneName = "Town",
            storyStarted = true,
            storyStage = StoryState.Arrival,
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
            birthdayDay = GameCalendar.BirthdayDay,
            birthdayMonth = GameCalendar.BirthdayMonth,
            rapaportUntilDay = RapaportState.Instance != null ? RapaportState.Instance.PaidUntilDay : 0,
            ownedBooks = BookShelfState.ToSaveData(),
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

        BankState.Instance?.WriteTo(data);

        var ownedTools = PlayerOwnedTools.Instance;
        if (ownedTools != null)
        {
            data.ownedTools.AddRange(ownedTools.Owned);
            foreach (var slot in ownedTools.BagSlots) data.bagSlots.Add(slot ?? "");
            data.lanternLit = ownedTools.LanternLit;
            data.mobility = ownedTools.Mobility;
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

        if (BleachingState.Instance != null)
        {
            data.bleachJobs = BleachingState.Instance.ToSaveData();
            data.bleachSamplesGiven = BleachingState.Instance.SamplesGiven;
        }

        if (DyeingState.Instance != null)
        {
            data.dyeJobs = DyeingState.Instance.ToSaveData();
            data.dyeSamplesGiven = DyeingState.Instance.SamplesGiven;
        }

        if (CoatingState.Instance != null)
        {
            data.coatJobs = CoatingState.Instance.ToSaveData();
            data.coatSamplesGiven = CoatingState.Instance.SamplesGiven;
            data.coatDiamondSamplesGiven = CoatingState.Instance.DiamondSamplesGiven;
        }

        if (FillingState.Instance != null)
        {
            data.fillJobs = FillingState.Instance.ToSaveData();
            data.fillSamplesGiven = FillingState.Instance.SamplesGiven;
            data.fillDiamondSamplesGiven = FillingState.Instance.DiamondSamplesGiven;
        }

        if (HeatingState.Instance != null)
        {
            data.heatJobs = HeatingState.Instance.ToSaveData();
            data.heatSamplesGiven = HeatingState.Instance.SamplesGiven;
        }

        if (DiffusionState.Instance != null)
        {
            data.diffusionJobs = DiffusionState.Instance.ToSaveData();
            data.diffusionSamplesGiven = DiffusionState.Instance.SamplesGiven;
        }

        if (IrradiationState.Instance != null)
        {
            data.irradiationJobs = IrradiationState.Instance.ToSaveData();
            data.irradiationSamplesGiven = IrradiationState.Instance.SamplesGiven;
            data.irradiationDiamondSamplesGiven = IrradiationState.Instance.DiamondSamplesGiven;
        }

        if (LaserState.Instance != null)
        {
            data.laserJobs = LaserState.Instance.ToSaveData();
            data.laserSamplesGiven = LaserState.Instance.SamplesGiven;
        }

        if (HphtState.Instance != null)
        {
            data.hphtJobs = HphtState.Instance.ToSaveData();
            data.hphtSamplesGiven = HphtState.Instance.SamplesGiven;
        }

        if (AuctionState.Instance != null) AuctionState.Instance.WriteSave(data);
        if (StoryState.Instance != null) StoryState.Instance.Save(data);
        if (JewelerState.Instance != null) data.jewelJobs = JewelerState.Instance.ToSaveData();

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

        GameCalendar.SetBirthday(data.birthdayDay, data.birthdayMonth);
        RapaportState.Ensure()?.Load(data.rapaportUntilDay);
        BookShelfState.Ensure()?.Load(data.ownedBooks, data.guideBookOwned);
        StoryState.Ensure()?.Load(data);
        BankState.Ensure()?.LoadFrom(data);
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
            tools.SetMobility(data.mobility);
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

        var bleaching = BleachingState.Ensure();
        if (bleaching != null)
        {
            bleaching.LoadJobs(data.bleachJobs, RestoreGem);
            bleaching.LoadSamplesGiven(data.bleachSamplesGiven);
        }

        var dyeing = DyeingState.Ensure();
        if (dyeing != null)
        {
            dyeing.LoadJobs(data.dyeJobs, RestoreGem);
            dyeing.LoadSamplesGiven(data.dyeSamplesGiven);
        }

        var coating = CoatingState.Ensure();
        if (coating != null)
        {
            coating.LoadJobs(data.coatJobs, RestoreGem);
            coating.LoadSamplesGiven(data.coatSamplesGiven);
            coating.LoadDiamondSamplesGiven(data.coatDiamondSamplesGiven);
        }

        var filling = FillingState.Ensure();
        if (filling != null)
        {
            filling.LoadJobs(data.fillJobs, RestoreGem);
            filling.LoadSamplesGiven(data.fillSamplesGiven);
            filling.LoadDiamondSamplesGiven(data.fillDiamondSamplesGiven);
        }

        var heating = HeatingState.Ensure();
        if (heating != null)
        {
            heating.LoadJobs(data.heatJobs, RestoreGem);
            heating.LoadSamplesGiven(data.heatSamplesGiven);
        }

        var diffusion = DiffusionState.Ensure();
        if (diffusion != null)
        {
            diffusion.LoadJobs(data.diffusionJobs, RestoreGem);
            diffusion.LoadSamplesGiven(data.diffusionSamplesGiven);
        }

        var irradiation = IrradiationState.Ensure();
        if (irradiation != null)
        {
            irradiation.LoadJobs(data.irradiationJobs, RestoreGem);
            irradiation.LoadSamplesGiven(data.irradiationSamplesGiven);
            irradiation.LoadDiamondSamplesGiven(data.irradiationDiamondSamplesGiven);
        }

        var laser = LaserState.Ensure();
        if (laser != null)
        {
            laser.LoadJobs(data.laserJobs, RestoreGem);
            laser.LoadSamplesGiven(data.laserSamplesGiven);
        }

        var hpht = HphtState.Ensure();
        if (hpht != null)
        {
            hpht.LoadJobs(data.hphtJobs, RestoreGem);
            hpht.LoadSamplesGiven(data.hphtSamplesGiven);
        }

        var jeweler = JewelerState.Ensure();
        if (jeweler != null) jeweler.LoadJobs(data.jewelJobs, RestoreGem);

        var auction = AuctionState.Ensure();
        if (auction != null) auction.ReadSave(data, RestoreGem);

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
