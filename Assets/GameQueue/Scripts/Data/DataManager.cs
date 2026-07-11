using System;
using System.Collections.Generic;
using Singleton;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    private const string SAVE_KEY = "PlayerSaveData_Offline";
    private PlayerSaveData _saveData = new PlayerSaveData();

    private Dictionary<string, int> _skillDict = new Dictionary<string, int>();

    void Start()
    {
        LoadData();
    }

    public void AddSkillUsage(string skillId, int amount)
    {
        if (_skillDict.ContainsKey(skillId))
            _skillDict[skillId] += amount;
        else
            _skillDict.Add(skillId, amount);

        SaveData();
    }

    public bool TryUseSkill(string skillId)
    {
        if (_skillDict.ContainsKey(skillId) && _skillDict[skillId] > 0)
        {
            _skillDict[skillId]--;
            Debug.Log($"Đã dùng {skillId}. Còn lại: {_skillDict[skillId]} lần.");
            SaveData();
            return true;
        }
        
        return false;
    }

    public int GetSkillUsageLeft(string skillId)
    {
        return _skillDict.ContainsKey(skillId) ? _skillDict[skillId] : 0;
    }

    public void SaveData()
    {
        _saveData.Skills.Clear();
        foreach (var kvp in _skillDict)
        {
            _saveData.Skills.Add(new SkillUsageData { SkillId = kvp.Key, UsageCount = kvp.Value });
        }

        string json = JsonUtility.ToJson(_saveData);

        string encodedString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        PlayerPrefs.SetString(SAVE_KEY, encodedString);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            try
            {
                string encodedString = PlayerPrefs.GetString(SAVE_KEY);
                byte[] decodedBytes = Convert.FromBase64String(encodedString);
                string json = System.Text.Encoding.UTF8.GetString(decodedBytes);

                _saveData = JsonUtility.FromJson<PlayerSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Lỗi đọc file save (có thể bị can thiệp sai): " + e.Message);
                _saveData = new PlayerSaveData(); 
            }
        }
        else
        {
            _saveData = new PlayerSaveData();
        }

        _skillDict.Clear();
        foreach (var skill in _saveData.Skills)
        {
            if (!_skillDict.ContainsKey(skill.SkillId))
            {
                _skillDict.Add(skill.SkillId, skill.UsageCount);
            }
        }
        
        Debug.Log("Tải game thành công!");
    }
    
}
