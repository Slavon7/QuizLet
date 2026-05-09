using UnityEngine;

// Типы наград
public enum RewardType { Crystals, XP, Sticker, Item }

[System.Serializable]
public class LeagueData
{
    public string leagueName;
    public Sprite leagueIcon;
}

[System.Serializable]
public class RewardData
{
    public string title;
    public int cupsRequired;
    public RewardType type;
    public Sprite icon;
    public int amount;
    public bool isClaimed;
    public bool isLeagueMilestone; // Маркер лиги вместо синей точки
}