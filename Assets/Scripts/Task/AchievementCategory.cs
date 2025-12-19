
using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class AchievementCategory
{
    public string categoryName = "Новая категория";
    public string categoryDescription = "";
    public Sprite categoryIcon;
    public Color categoryColor = Color.white;
    public List<AchievementData> categoryAchievements = new List<AchievementData>();
}