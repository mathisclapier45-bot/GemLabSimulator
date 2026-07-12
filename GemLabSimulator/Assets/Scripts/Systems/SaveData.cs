using System;
using System.Collections.Generic;

[Serializable]
public class GemInstanceSaveData
{
    public string definitionName;
    public bool isIdentified;

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
public class SaveData
{
    public int money;
    public List<GemInstanceSaveData> inventoryGems = new List<GemInstanceSaveData>();
    public List<GemInstanceSaveData> chestGems = new List<GemInstanceSaveData>();
    public string sceneName;
    public float playerX;
    public float playerY;
}
