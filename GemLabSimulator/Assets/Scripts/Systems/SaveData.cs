using System;
using System.Collections.Generic;

[Serializable]
public class GemInstanceSaveData
{
    public string definitionName;
    public string specimenId;
    public string clientName;
    public bool isDonation;
    public bool isRough;
    public bool isPersonalSpecimen;
    public bool isIdentified;
    public bool hasWindow;
    public string cutShape;
    public string cutStyle;

    public string originName;
    public float originValueMultiplier;
    public string originInclusionOverride;

    public string saturationGrade;
    public string clarityGrade;
    public string clarityDescription;
    public string inclusionDescription;

    public float caratWeight;
    public float lengthMm;
    public float widthMm;
    public float depthMm;

    public string treatmentGrade;
    public string treatmentDescription;
    public bool isSynthetic;
    public string authenticityGrade;
    public string authenticityDescription;
    public string growthStructureDescription;

    public int finalValue;

    // Diamonds from the dealers: grades and certificate (empty for other stones).
    public string colorGrade;
    public bool hasColor;
    public float colorHue;
    public float colorSaturation;
    public float colorBrightness;
    public bool isFancyDiamond;
    public string tradeName;
    public bool hasReport;
    public string reportNumber;
    public int reportDay;
    public string diamondClarity;
    public string certificateNumber;
    public string certificateIssuer;
    public int certificateDay;

    // Rough diamonds of the 3rd floor: crystal form, expected yield, and the grades hidden until the stone is cut (index + 1, 0 = none).
    public string roughForm;
    public float roughYield;
    public int roughColor;
    public int roughClarity;
    public int cutQuality;          // quality of the cut given by a diamond cutter (index + 1, 0 = none)
    public int purchasePrice;       // what the player paid for the stone (0: unknown)
    public int cuttingCost;         // what its cutting cost
    public bool heatStar;           // the furnace made a star appear
    public float diffusionDepth;    // how deep a diffused element went, in millimetres (0: not diffused)
    public float diffusionHue;      // the colour before the diffusion
    public float diffusionSaturation;
    public float diffusionBrightness;
    public bool diffusionStar;
    public string fillerName;       // what fills its fissures or its pores, if anything
    public string coatingName;      // the coating of the pavilion, if any
    public float coatingIridescence;

    public List<string> measuredKeys = new List<string>();
    public List<float> measuredValues = new List<float>();
    public List<string> observedKeys = new List<string>();
}

[Serializable]
public class LapidaryJobSaveData
{
    public GemInstanceSaveData gem;
    public int readyDay;
}

[Serializable]
public class DiamondCutJobSaveData
{
    public int cutterIndex;
    public int readyDay;
    public string summary;
    public List<GemInstanceSaveData> stones = new List<GemInstanceSaveData>();
}

[Serializable]
public class BleachJobSaveData
{
    public int vatIndex;
    public int startDay;
    public int readyDay;
    public int fee;
    public GemInstanceSaveData stone;
}

[Serializable]
public class DyeJobSaveData
{
    public int potIndex;
    public int startDay;
    public int readyDay;
    public int fee;
    public GemInstanceSaveData stone;
}

[Serializable]
public class CoatJobSaveData
{
    public int startDay;
    public int readyDay;
    public int fee;
    public string recipe;           // the targets chosen: "0,1" ...
    public int thickness;
    public GemInstanceSaveData stone;
}

[Serializable]
public class FillJobSaveData
{
    public int startDay;
    public int readyDay;
    public int fee;
    public int product;
    public int procedure;
    public GemInstanceSaveData stone;
}

[Serializable]
public class HeatJobSaveData
{
    public int startDay;
    public int readyDay;
    public int fee;
    public int temperature;
    public int atmosphere;
    public int cooling;
    public int days;
    public GemInstanceSaveData stone;
}

[Serializable]
public class DiffusionJobSaveData
{
    public int startDay;
    public int readyDay;
    public int fee;
    public int dopant;
    public int temperature;
    public int days;
    public GemInstanceSaveData stone;
}

[Serializable]
public class SaveData
{
    public string saveName = "";
    public int money;
    public int currentDay = 1;
    public int birthdayDay;            // birthday of the player, chosen when the game is created (0: none)
    public int birthdayMonth;
    public float timeOfDay = -1f;      // hour of the in-game day when saved (-1: older saves, the day starts at 7h)
    public List<GemInstanceSaveData> inventoryGems = new List<GemInstanceSaveData>();
    public List<GemInstanceSaveData> chestGems = new List<GemInstanceSaveData>();
    public List<GemInstanceSaveData> cabinetGems = new List<GemInstanceSaveData>();
    public List<GemInstanceSaveData> orderCabinetGems = new List<GemInstanceSaveData>();
    public List<string> ownedTools = new List<string>();
    public bool lanternLit;
    public bool bankOpen;               // the account at the bank of the town
    public int bankBalance;
    public int bankMonthKey;
    public int bankMinBalance;
    public int bankLastInterest;
    public float bankLastRate;
    public int rapaportUntilDay;       // last day of the subscription to the Rapaport price list (0: never subscribed)
    public List<string> bagSlots = new List<string>();
    public string mobility = "";
    public List<string> ownedPets = new List<string>();
    public string equippedPet;
    public List<string> cardIds = new List<string>();
    public List<int> cardCounts = new List<int>();
    public int boosterDay;
    public int boostersToday;
    public List<MicroPhotoEntry> microPhotos = new List<MicroPhotoEntry>();
    public List<string> discoveredSpecies = new List<string>();
    public string sceneName;
    public float playerX;
    public float playerY;
    public int reputationPoints;
    public List<LapidaryJobSaveData> lapidaryJobs = new List<LapidaryJobSaveData>();
    public List<DiamondCutJobSaveData> diamondCutJobs = new List<DiamondCutJobSaveData>();
    public List<BleachJobSaveData> bleachJobs = new List<BleachJobSaveData>();
    public bool bleachSamplesGiven;
    public List<DyeJobSaveData> dyeJobs = new List<DyeJobSaveData>();
    public bool dyeSamplesGiven;
    public List<CoatJobSaveData> coatJobs = new List<CoatJobSaveData>();
    public bool coatSamplesGiven;
    public List<FillJobSaveData> fillJobs = new List<FillJobSaveData>();
    public bool fillSamplesGiven;
    public List<HeatJobSaveData> heatJobs = new List<HeatJobSaveData>();
    public bool heatSamplesGiven;
    public List<DiffusionJobSaveData> diffusionJobs = new List<DiffusionJobSaveData>();
    public bool diffusionSamplesGiven;
}
