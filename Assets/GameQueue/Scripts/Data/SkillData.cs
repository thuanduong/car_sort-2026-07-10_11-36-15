using UnityEngine;

[System.Serializable]
public class SkillData
{
    public string SkillId;
    public Sprite SkillIcon;
    public string SkillName;
}

[System.Serializable]
public class SkillUsageData
{
    public string SkillId;
    [System.NonSerialized] public Sprite SkillIcon;
    public int UsageCount;
}