using System;
using UnityEngine;
public static class GameGlobal
{
    public static int CompletedLevel {get; set;}
    public static int SelectedLevel {get; set;}
    public static int MaxLevel {get; set;}

    public static void ReloadGameValue()
    {
        CompletedLevel = PlayerPrefs.GetInt("GAME_COMPLETE_LEVEL", 1);
        //HARD SET
        MaxLevel = 50;
    }

    public static bool CompleteLevel(int value)
    {
        if (value < CompletedLevel) return false;
        CompletedLevel = value + 1;
        PlayerPrefs.SetInt("GAME_COMPLETE_LEVEL", CompletedLevel);
        PlayerPrefs.Save();
        return true;
    }

    public static void NextLevel()
    {
        if (SelectedLevel < MaxLevel) 
            SelectedLevel += 1;
    }

}