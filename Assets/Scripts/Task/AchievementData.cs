using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AchievementData
{
    [Header("Achievement Info")]
    public int achievementId = 1001;
    public string achievementTitle = "Новое достижение";
    [TextArea(2, 4)]
    public string achievementDescription = "Описание достижения";

    [Header("Requirements")]
    public TaskType taskType = TaskType.CorrectAnswers;
    public int targetValue = 10;

    [Header("Rewards")]
    public int gemsReward = 50;
    public int coinsReward = 0;
    public int experienceReward = 100;

    [Header("Visual Settings")]
    public Sprite achievementIcon;
    public Color achievementColor = Color.yellow;
    [TextArea(1, 2)]
    public string flavorText = "";

    [Header("Rarity")]
    public AchievementRarity rarity = AchievementRarity.Common;

    [Header("Visibility")]
    public bool isHidden = false;

    [Header("Prerequisites")]
    public List<int> requiredAchievementIds = new();

    // Конвертация в TaskData (если нужно)
    public TaskData ToTaskData()
    {
        return new TaskData
        {
            taskId = achievementId,
            taskTitle = achievementTitle,
            taskDescription = achievementDescription,
            taskType = taskType,
            targetValue = targetValue,
            currentValue = 0,
            isCompleted = false,
            isRewardClaimed = false,
            reward = new TaskReward
            {
                gems = gemsReward,
                coins = coinsReward,
                experience = experienceReward
            }
        };
    }
}
