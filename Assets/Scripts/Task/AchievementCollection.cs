using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Achievement Collection", menuName = "Task System/Achievement Collection")]
public class AchievementCollection : ScriptableObject
{
    [Header("All Game Achievements")]
    public List<AchievementData> achievements = new List<AchievementData>();

    [Header("Categories")]
    public List<AchievementCategory> categories = new List<AchievementCategory>();

    // Получить все достижения определенного типа
    public List<AchievementData> GetAchievementsByType(TaskType taskType)
    {
        return achievements.Where(a => a.taskType == taskType).ToList();
    }

    // Получить достижения по редкости
    public List<AchievementData> GetAchievementsByRarity(AchievementRarity rarity)
    {
        return achievements.Where(a => a.rarity == rarity).ToList();
    }

    // Получить достижение по ID
    public AchievementData GetAchievementById(int id)
    {
        return achievements.FirstOrDefault(a => a.achievementId == id);
    }

    // Получить все видимые достижения
    public List<AchievementData> GetVisibleAchievements()
    {
        return achievements.Where(a => !a.isHidden).ToList();
    }

    private void OnValidate()
    {
        HashSet<int> usedIds = new HashSet<int>();
        foreach (var achievement in achievements)
        {
            if (achievement != null)
            {
                if (usedIds.Contains(achievement.achievementId))
                {
                    Debug.LogError($"Дублирующийся Achievement ID: {achievement.achievementId} — {achievement.achievementTitle}");
                }
                else
                {
                    usedIds.Add(achievement.achievementId);
                }

                // Простейшая внутренняя валидация
                if (achievement.targetValue <= 0) achievement.targetValue = 1;
                if (achievement.gemsReward < 0) achievement.gemsReward = 0;
                if (achievement.coinsReward < 0) achievement.coinsReward = 0;
                if (achievement.experienceReward < 0) achievement.experienceReward = 0;

                if (achievement.achievementId < 1001 || achievement.achievementId > 9999)
                {
                    Debug.LogWarning($"Achievement ID должен быть в диапазоне 1001-9999. ID: {achievement.achievementId}, Title: {achievement.achievementTitle}");
                }
            }
        }
    }
}
