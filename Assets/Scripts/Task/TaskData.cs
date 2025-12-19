using UnityEngine;

[System.Serializable]
public class TaskData
{
    public int taskId;
    public string taskTitle;
    public string taskDescription;
    public TaskType taskType;
    public int targetValue;
    public int currentValue;
    public bool isCompleted;
    public bool isRewardClaimed;
    public TaskReward reward;
}

[System.Serializable]
public class TaskReward
{
    public int gems = 0;        // Изменили с coins на gems
    public int experience = 0;
    public int coins = 0;       // Оставили и монеты тоже, на случай если понадобятся
}

public enum TaskType
{
    CorrectAnswers,
    PlayGames,
    WinGames,
    AnswerInTime,
    ConsecutiveCorrect,
    DailyLogin
}