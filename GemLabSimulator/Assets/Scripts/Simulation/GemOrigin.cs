using System;

[Serializable]
public class GemOrigin
{
    public string name;
    public float rarityWeight;
    public float valueMultiplier;
    [UnityEngine.TextArea] public string inclusionOverride;
}
