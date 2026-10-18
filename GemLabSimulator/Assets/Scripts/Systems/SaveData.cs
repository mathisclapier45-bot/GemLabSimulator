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
public class SaveData
{
    public string saveName = "";
    public int money;
    public int currentDay = 1;
    public List<GemInstanceSaveData> inventoryGems = new List<GemInstanceSaveData>();
    public List<GemInstanceSaveData> chestGems = new List<GemInstanceSaveData>();
    public List<string> discoveredSpecies = new List<string>();
    public string sceneName;
    public float playerX;
    public float playerY;
    public int reputationPoints;
    public List<LapidaryJobSaveData> lapidaryJobs = new List<LapidaryJobSaveData>();
}
