using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "DailyTaskDatabase", menuName = "Game/Daily Task Database")]
public class DailyTaskDatabase : ScriptableObject
{
    [Header("Daily Task Configuration")]
    [SerializeField] private List<DailyTaskTemplate> taskTemplates = new List<DailyTaskTemplate>();
    
    [Header("Settings")]
    [SerializeField] private int maxDailyTasks = 5;
    [SerializeField] private bool randomizeDailyTasks = false;
    [SerializeField] private int baseTaskId = 101; // Стартовый ID для ежедневных тасков
    
    [Header("Seasonal Events")]
    [SerializeField] private List<SeasonalTaskEvent> seasonalEvents = new List<SeasonalTaskEvent>();
    
    /// <summary>
    /// Получить список ежедневных тасков для сегодня
    /// </summary>
    public List<TaskData> GenerateDailyTasks()
    {
        List<TaskData> dailyTasks = new List<TaskData>();
        
        // Проверяем сезонные события
        var activeSeasonalTasks = GetActiveSeasonalTasks();
        
        // Получаем базовые ежедневные таски
        var availableTemplates = new List<DailyTaskTemplate>(taskTemplates);
        
        // Добавляем сезонные таски с приоритетом
        foreach (var seasonalTask in activeSeasonalTasks)
        {
            availableTemplates.Insert(0, seasonalTask.taskTemplate);
        }
        
        // Фильтруем только включенные таски
        availableTemplates = availableTemplates.Where(t => t.isEnabled).ToList();
        
        // Выбираем таски
        List<DailyTaskTemplate> selectedTemplates;
        
        if (randomizeDailyTasks)
        {
            selectedTemplates = GetRandomizedTasks(availableTemplates);
        }
        else
        {
            // Берем первые таски по приоритету
            selectedTemplates = availableTemplates
                .OrderByDescending(t => t.priority)
                .Take(maxDailyTasks)
                .ToList();
        }
        
        // ВАЖНО: окончательно ограничиваем количество
        selectedTemplates = selectedTemplates.Take(maxDailyTasks).ToList();
        
        // Создаем TaskData из шаблонов
        for (int i = 0; i < selectedTemplates.Count; i++)
        {
            var template = selectedTemplates[i];
            var taskData = CreateTaskDataFromTemplate(template, baseTaskId + i);
            dailyTasks.Add(taskData);
        }
        
        Debug.Log($"DailyTaskDatabase: Сгенерировано {dailyTasks.Count} из {availableTemplates.Count} доступных шаблонов");
        
        return dailyTasks;
    }
    
    private List<DailyTaskTemplate> GetRandomizedTasks(List<DailyTaskTemplate> availableTemplates)
    {
        // Используем сид на основе текущей даты для консистентности
        System.DateTime today = System.DateTime.Now.Date;
        int seed = today.Year * 10000 + today.Month * 100 + today.Day;
        System.Random random = new System.Random(seed);
        
        // Сначала сортируем по приоритету, потом перемешиваем внутри приоритетных групп
        var prioritizedTasks = availableTemplates
            .GroupBy(t => t.priority)
            .OrderByDescending(g => g.Key)
            .SelectMany(g => g.OrderBy(x => random.Next()))
            .Take(maxDailyTasks)
            .ToList();
        
        Debug.Log($"Randomized selection: выбрано {prioritizedTasks.Count} тасков с сидом {seed}");
        
        return prioritizedTasks;
    }
    
    private List<SeasonalTaskEvent> GetActiveSeasonalTasks()
    {
        System.DateTime today = System.DateTime.Now;
        return seasonalEvents.Where(e => e.IsActive(today)).ToList();
    }
    
    private TaskData CreateTaskDataFromTemplate(DailyTaskTemplate template, int taskId)
    {
        return new TaskData
        {
            taskId = taskId,
            taskTitle = template.taskTitle,
            taskDescription = template.taskDescription,
            taskType = template.taskType,
            targetValue = template.targetValue,
            currentValue = 0,
            isCompleted = false,
            isRewardClaimed = false,
            reward = new TaskReward
            {
                gems = template.rewardGems,
                experience = template.rewardExperience,
                coins = template.rewardCoins
            }
        };
    }
    
    /// <summary>
    /// Добавить новый шаблон таска
    /// </summary>
    public void AddTaskTemplate(DailyTaskTemplate template)
    {
        if (!taskTemplates.Contains(template))
        {
            taskTemplates.Add(template);
        }
    }
    
    /// <summary>
    /// Удалить шаблон таска
    /// </summary>
    public void RemoveTaskTemplate(DailyTaskTemplate template)
    {
        taskTemplates.Remove(template);
    }
    
    /// <summary>
    /// Получить все доступные шаблоны
    /// </summary>
    public List<DailyTaskTemplate> GetAllTemplates()
    {
        return new List<DailyTaskTemplate>(taskTemplates);
    }
    
    /// <summary>
    /// Найти шаблон по типу таска
    /// </summary>
    public DailyTaskTemplate FindTemplateByType(TaskType taskType)
    {
        return taskTemplates.FirstOrDefault(t => t.taskType == taskType);
    }
    
    /// <summary>
    /// Валидация базы данных
    /// </summary>
    public bool ValidateDatabase()
    {
        if (taskTemplates.Count == 0)
        {
            Debug.LogError("DailyTaskDatabase: Нет ни одного шаблона таска!");
            return false;
        }
        
        // Проверяем на дублирующиеся типы тасков
        var duplicateTypes = taskTemplates.GroupBy(t => t.taskType)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => g.Key);
        
        foreach (var duplicateType in duplicateTypes)
        {
            Debug.LogWarning($"DailyTaskDatabase: Найдены дублирующиеся таски типа {duplicateType}");
        }
        
        return true;
    }
    
    #if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Game/Create Daily Task Database")]
    public static void CreateDefaultDatabase()
    {
        var database = CreateInstance<DailyTaskDatabase>();
        
        // Создаем базовые шаблоны
        database.taskTemplates.Add(CreateTemplate(
            "Щоденний вхід", "Увійди в гру сьогодні", 
            TaskType.DailyLogin, 1, 25, 25, 0));
            
        database.taskTemplates.Add(CreateTemplate(
            "Розумник дня", "Дай 15 правильних відповідей", 
            TaskType.CorrectAnswers, 15, 50, 50, 0));
            
        database.taskTemplates.Add(CreateTemplate(
            "Активний гравець", "Зіграй 5 ігор", 
            TaskType.PlayGames, 5, 75, 75, 0));
            
        database.taskTemplates.Add(CreateTemplate(
            "Швидкість блискавки", "Дай 5 швидких відповідей (менше 3 сек)", 
            TaskType.AnswerInTime, 5, 100, 100, 0));
            
        database.taskTemplates.Add(CreateTemplate(
            "Чемпіон дня", "Вигради 3 гри", 
            TaskType.WinGames, 3, 125, 125, 0));
        
        // Сохраняем в Resources
        string path = "Assets/Resources/DailyTaskDatabase.asset";
        UnityEditor.AssetDatabase.CreateAsset(database, path);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log($"Daily Task Database создана: {path}");
    }
    
    private static DailyTaskTemplate CreateTemplate(string title, string description, 
        TaskType type, int target, int gems, int exp, int coins)
    {
        return new DailyTaskTemplate
        {
            taskTitle = title,
            taskDescription = description,
            taskType = type,
            targetValue = target,
            rewardGems = gems,
            rewardExperience = exp,
            rewardCoins = coins
        };
    }
    #endif
}

[System.Serializable]
public class DailyTaskTemplate
{
    [Header("Task Info")]
    public string taskTitle;
    [TextArea(2, 4)]
    public string taskDescription;
    public TaskType taskType;
    public int targetValue;
    
    [Header("Rewards")]
    public int rewardGems;
    public int rewardExperience;
    public int rewardCoins;
    
    [Header("Additional Settings")]
    public bool isEnabled = true;
    public int priority = 0; // Чем выше, тем приоритетнее
    
    [Header("Seasonal")]
    public bool isSeasonalTask = false;
    public string seasonalEventName = "";
}

[System.Serializable]
public class SeasonalTaskEvent
{
    [Header("Event Info")]
    public string eventName;
    public string eventDescription;
    
    [Header("Date Range")]
    public int startMonth = 1;
    public int startDay = 1;
    public int endMonth = 12;
    public int endDay = 31;
    
    [Header("Task Template")]
    public DailyTaskTemplate taskTemplate;
    
    [Header("Settings")]
    public bool isEnabled = true;
    public bool isYearly = true; // Повторяется каждый год
    
    /// <summary>
    /// Проверяет, активно ли событие в указанную дату
    /// </summary>
    public bool IsActive(System.DateTime date)
    {
        if (!isEnabled) return false;
        
        int month = date.Month;
        int day = date.Day;
        
        // Простая проверка диапазона дат
        if (startMonth <= endMonth)
        {
            // Событие в пределах одного года
            return (month > startMonth || (month == startMonth && day >= startDay)) &&
                   (month < endMonth || (month == endMonth && day <= endDay));
        }
        else
        {
            // Событие переходит на следующий год (например, Новый год)
            return (month > startMonth || (month == startMonth && day >= startDay)) ||
                   (month < endMonth || (month == endMonth && day <= endDay));
        }
    }
}