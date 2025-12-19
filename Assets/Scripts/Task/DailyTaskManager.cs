using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DailyTaskManager : MonoBehaviour
{
    public static DailyTaskManager Instance { get; private set; }
    
    [Header("Daily Task Settings")]
    [SerializeField] private bool enableDailyTasks = true;
    [SerializeField] private float checkInterval = 60f;
    [SerializeField] private DailyTaskDatabase taskDatabase;
    [SerializeField] private int maxDailyTasksPerDay = 3;
    
    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private int debugResetHour = 0;
    
    private static readonly TimeZoneInfo KyivTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
    
    // События
    public System.Action OnDailyTasksReset;
    public System.Action OnNewDayStarted;
    
    // Данные
    private List<TaskData> dailyTasks = new List<TaskData>();
    private DateTime lastResetTime;
    private DateTime nextResetTime;
    private Coroutine timeCheckCoroutine;
    private bool isInitialized = false;
    
    private void Awake()
    {
        InitializeSingleton();
    }
    
    private void Start()
    {
        if (enableDailyTasks)
        {
            InitializeSystem();
        }
    }
    
    #region Initialization
    
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTaskDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSystem()
    {
        LoadLastResetTime();
        CalculateNextResetTime();
        InitializeOrLoadDailyTasks();
        StartTimeChecking();
        
        isInitialized = true;
        Debug.Log($"DailyTaskManager: Инициализирован. Следующий сброс: {nextResetTime:yyyy-MM-dd HH:mm:ss} (Киев)");
    }
    
    private void LoadTaskDatabase()
    {
        if (taskDatabase == null)
        {
            taskDatabase = Resources.Load<DailyTaskDatabase>("DailyTaskDatabase");
            
            if (taskDatabase == null)
            {
                Debug.LogError("DailyTaskDatabase не найдена! Создайте базу данных через Tools/Game/Create Daily Task Database");
                return;
            }
        }
        
        if (!taskDatabase.ValidateDatabase())
        {
            Debug.LogError("DailyTaskDatabase содержит ошибки!");
        }
        
        Debug.Log($"DailyTaskDatabase загружена: {taskDatabase.GetAllTemplates().Count} шаблонов");
    }
    
    #endregion
    
    #region Task Management
    
    private void InitializeOrLoadDailyTasks()
    {
        DateTime currentKyivTime = GetKyivTime();
        
        if (lastResetTime.Date < currentKyivTime.Date)
        {
            Debug.Log("Обнаружен пропущенный сброс ежедневных тасков. Выполняем сброс...");
            ResetDailyTasks();
        }
        else
        {
            LoadExistingDailyTasks();
        }
    }
    
    private void LoadExistingDailyTasks()
    {
        Debug.Log("Загрузка существующих ежедневных тасков...");
        
        string savedTaskIds = PlayerPrefs.GetString("TodayDailyTaskIds", "");
        
        if (string.IsNullOrEmpty(savedTaskIds))
        {
            Debug.Log("Нет сохраненных тасков для сегодня, создаем новые");
            CreateNewDailyTasks();
            return;
        }
        
        var taskIds = ParseTaskIds(savedTaskIds);
        if (taskIds.Count == 0)
        {
            Debug.Log("Не удалось распарсить ID тасков, создаем новые");
            CreateNewDailyTasks();
            return;
        }
        
        RestoreTasksFromIds(taskIds);
        
        if (dailyTasks.Count == 0)
        {
            Debug.Log("Не удалось восстановить таски, создаем новые");
            CreateNewDailyTasks();
            return;
        }
        
        LoadDailyTaskProgress();
        AddDailyTasksToManager();
        
        Debug.Log($"Восстановлено {dailyTasks.Count} ежедневных тасков");
    }
    
    private List<int> ParseTaskIds(string savedTaskIds)
    {
        var taskIds = new List<int>();
        string[] taskIdStrings = savedTaskIds.Split(',');
        
        foreach (string idString in taskIdStrings)
        {
            if (int.TryParse(idString.Trim(), out int id))
            {
                taskIds.Add(id);
            }
        }
        
        return taskIds;
    }
    
    private void RestoreTasksFromIds(List<int> taskIds)
    {
        dailyTasks.Clear();
        
        foreach (int taskId in taskIds)
        {
            TaskData restoredTask = RestoreTaskFromId(taskId);
            if (restoredTask != null)
            {
                dailyTasks.Add(restoredTask);
            }
        }
    }
    
    private TaskData RestoreTaskFromId(int taskId)
    {
        if (taskDatabase == null) return null;
        
        var allTemplates = taskDatabase.GetAllTemplates();
        int templateIndex = taskId - 101; // Базовый ID = 101
        
        if (templateIndex < 0 || templateIndex >= allTemplates.Count)
        {
            Debug.LogWarning($"Не найден шаблон для ID таска {taskId}");
            return null;
        }
        
        var template = allTemplates[templateIndex];
        
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
    
    private void CreateNewDailyTasks()
    {
        if (taskDatabase == null)
        {
            Debug.LogError("TaskDatabase не загружена!");
            return;
        }
        
        var generatedTasks = taskDatabase.GenerateDailyTasks();
        dailyTasks = generatedTasks.Take(maxDailyTasksPerDay).ToList();
        
        SaveTodayTaskIds();
        AddDailyTasksToManager();
        
        Debug.Log($"Создано {dailyTasks.Count} новых ежедневных тасков (лимит: {maxDailyTasksPerDay})");
    }
    
    private void SaveTodayTaskIds()
    {
        var taskIdStrings = dailyTasks.Select(task => task.taskId.ToString()).ToList();
        string taskIdsString = string.Join(",", taskIdStrings);
        
        PlayerPrefs.SetString("TodayDailyTaskIds", taskIdsString);
        PlayerPrefs.SetString("DailyTasksCreatedDate", GetKyivTime().ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
        
        Debug.Log($"Сохранены ID тасков: {taskIdsString}");
    }
    
    #endregion
    
    #region Reset Management
    
    public void ResetDailyTasks()
    {
        Debug.Log("Сброс ежедневных тасков...");
        
        ClearOldDailyTaskData();
        CreateNewDailyTasks();
        ResetDailyPlayerStats();
        UpdateResetTime();
        
        OnDailyTasksReset?.Invoke();
        OnNewDayStarted?.Invoke();
        
        Debug.Log($"Ежедневные таски сброшены! Создано {dailyTasks.Count} тасков. Следующий сброс: {nextResetTime:yyyy-MM-dd HH:mm:ss}");
    }
    
    private void ClearOldDailyTaskData()
    {
        string oldTaskIds = PlayerPrefs.GetString("TodayDailyTaskIds", "");
        
        if (!string.IsNullOrEmpty(oldTaskIds))
        {
            var taskIds = ParseTaskIds(oldTaskIds);
            
            foreach (int id in taskIds)
            {
                PlayerPrefs.DeleteKey($"DailyTask_{id}_Current");
                PlayerPrefs.DeleteKey($"DailyTask_{id}_Completed");
                PlayerPrefs.DeleteKey($"DailyTask_{id}_Claimed");
            }
        }
        
        PlayerPrefs.DeleteKey("TodayDailyTaskIds");
        PlayerPrefs.DeleteKey("DailyTasksCreatedDate");
    }
    
    private void ResetDailyPlayerStats()
    {
        foreach (TaskType taskType in System.Enum.GetValues(typeof(TaskType)))
        {
            PlayerPrefs.SetInt($"Daily_{taskType}", 0);
        }
        PlayerPrefs.Save();
    }
    
    private void UpdateResetTime()
    {
        lastResetTime = GetKyivTime();
        SaveLastResetTime();
        CalculateNextResetTime();
    }
    
    #endregion
    
    #region Time Management
    
    private void StartTimeChecking()
    {
        if (timeCheckCoroutine != null)
        {
            StopCoroutine(timeCheckCoroutine);
        }
        
        timeCheckCoroutine = StartCoroutine(TimeCheckCoroutine());
    }
    
    private IEnumerator TimeCheckCoroutine()
    {
        while (enableDailyTasks)
        {
            yield return new WaitForSeconds(checkInterval);
            
            DateTime currentKyivTime = GetKyivTime();
            
            if (currentKyivTime >= nextResetTime)
            {
                Debug.Log($"Время сброса ежедневных тасков! Текущее время: {currentKyivTime:yyyy-MM-dd HH:mm:ss}");
                ResetDailyTasks();
            }
        }
    }
    
    private DateTime GetKyivTime()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KyivTimeZone);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось получить киевское время: {e.Message}. Используем UTC+2");
            return DateTime.UtcNow.AddHours(2);
        }
    }
    
    private void CalculateNextResetTime()
    {
        DateTime currentKyivTime = GetKyivTime();
        nextResetTime = currentKyivTime.Date.AddDays(1); // Полночь следующего дня
        
        if (debugMode)
        {
            nextResetTime = currentKyivTime.Date.AddHours(debugResetHour);
            if (nextResetTime <= currentKyivTime)
            {
                nextResetTime = nextResetTime.AddDays(1);
            }
        }
    }
    
    private void LoadLastResetTime()
    {
        string lastResetString = PlayerPrefs.GetString("LastDailyTaskReset", "");
        
        if (DateTime.TryParse(lastResetString, out DateTime parsed))
        {
            lastResetTime = parsed;
        }
        else
        {
            lastResetTime = GetKyivTime().Date.AddDays(-1);
        }
    }
    
    private void SaveLastResetTime()
    {
        PlayerPrefs.SetString("LastDailyTaskReset", lastResetTime.ToString());
        PlayerPrefs.Save();
    }
    
    public string GetTimeUntilReset(string language = "ua")
    {
        System.DateTime currentTime = System.DateTime.Now;
        System.DateTime nextReset = GetNextResetTime();
        
        System.TimeSpan timeSpan = nextReset - currentTime;
        
        if (timeSpan.TotalSeconds <= 0)
        {
            return GetLocalizedTimeFormat(0, 0, language);
        }
        
        int hours = (int)timeSpan.TotalHours;
        int minutes = timeSpan.Minutes;
        
        return GetLocalizedTimeFormat(hours, minutes, language);
    }
    
    private string GetLocalizedTimeFormat(int hours, int minutes, string language)
    {
        switch (language.ToLower())
        {
            case "ua":
            case "ukrainian":
                if (hours > 0)
                    return $"{hours:D2}г {minutes:D2}хв";
                else
                    return $"{minutes:D2}хв";
                    
            case "en":
            case "english":
                if (hours > 0)
                    return $"{hours:D2}h {minutes:D2}m";
                else
                    return $"{minutes:D2}m";
                    
            case "ru":
            case "russian":
                if (hours > 0)
                    return $"{hours:D2}ч {minutes:D2}м";
                else
                    return $"{minutes:D2}м";
                    
            default:
                if (hours > 0)
                    return $"{hours:D2}h {minutes:D2}m";
                else
                    return $"{minutes:D2}m";
        }
    }
    
    #endregion

    #region Task Progress Management

    public void OnDailyLoginDetected()
    {
        Debug.Log("DailyTaskManager: Получен сигнал о ежедневном входе");

        var loginTask = dailyTasks.FirstOrDefault(task =>
            task.taskType == TaskType.DailyLogin && !task.isCompleted);

        if (loginTask != null)
        {
            loginTask.currentValue = 1;

            if (loginTask.currentValue >= loginTask.targetValue)
            {
                loginTask.isCompleted = true;
                Debug.Log($"✅ Ежедневный таск входа '{loginTask.taskTitle}' выполнен!");
                SaveDailyTaskProgress(loginTask);

                TaskManager.Instance?.OnDailyLogin();
            }
        }
    }

    private void SaveDailyTaskProgress(TaskData task)
    {
        PlayerPrefs.SetInt($"DailyTask_{task.taskId}_Current", task.currentValue);
        PlayerPrefs.SetInt($"DailyTask_{task.taskId}_Completed", task.isCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"DailyTask_{task.taskId}_Claimed", task.isRewardClaimed ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"Сохранен прогресс ежедневного таска {task.taskId}: {task.currentValue}/{task.targetValue}");
    }
    
    private void LoadDailyTaskProgress()
    {
        foreach (var task in dailyTasks)
        {
            task.currentValue = PlayerPrefs.GetInt($"DailyTask_{task.taskId}_Current", 0);
            task.isCompleted = PlayerPrefs.GetInt($"DailyTask_{task.taskId}_Completed", 0) == 1;
            task.isRewardClaimed = PlayerPrefs.GetInt($"DailyTask_{task.taskId}_Claimed", 0) == 1;
        }
        
        Debug.Log("Загружен прогресс ежедневных тасков");
    }
    
    #endregion
    
    #region Task Manager Integration
    
    private void AddDailyTasksToManager()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.RemoveDailyTasks();
            
            Debug.Log($"Добавляем {dailyTasks.Count} ежедневных тасков в TaskManager");
            
            foreach (var dailyTask in dailyTasks)
            {
                TaskManager.Instance.AddDailyTask(dailyTask);
                Debug.Log($"Добавлен ежедневный таск: {dailyTask.taskTitle} ({dailyTask.currentValue}/{dailyTask.targetValue})");
            }
            
            TaskManager.Instance.RefreshActiveTasksAfterDailyTasksAdded();
            Debug.Log("Ежедневные таски добавлены в TaskManager и UI обновлен");
        }
        else
        {
            Debug.LogWarning("TaskManager не найден! Ежедневные таски не могут быть добавлены.");
        }
    }
    
    #endregion
    
    #region Database Management
    
    public void ReloadTaskDatabase()
    {
        LoadTaskDatabase();
        
        if (taskDatabase != null && isInitialized)
        {
            Debug.Log("База данных ежедневных тасков перезагружена");
        }
    }
    
    public void SetTaskDatabase(DailyTaskDatabase database)
    {
        taskDatabase = database;
        ReloadTaskDatabase();
    }
    
    #endregion
    
    #region Public Getters
    
    public DateTime GetNextResetTime() => nextResetTime;
    public DateTime GetLastResetTime() => lastResetTime;
    public List<TaskData> GetDailyTasks() => new List<TaskData>(dailyTasks);
    public DailyTaskDatabase GetTaskDatabase() => taskDatabase;
    
    #endregion
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceResetDailyTasks()
    {
        Debug.Log("[DEBUG] Принудительный сброс ежедневных тасков");
        ResetDailyTasks();
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestNextReset(int secondsFromNow = 10)
    {
        DateTime currentKyivTime = GetKyivTime();
        nextResetTime = currentKyivTime.AddSeconds(secondsFromNow);
        Debug.Log($"[TEST] Следующий сброс через {secondsFromNow} секунд: {nextResetTime:HH:mm:ss}");
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintSavedData()
    {
        string taskIds = PlayerPrefs.GetString("TodayDailyTaskIds", "НЕТ");
        string createdDate = PlayerPrefs.GetString("DailyTasksCreatedDate", "НЕТ");
        string lastReset = PlayerPrefs.GetString("LastDailyTaskReset", "НЕТ");
        
        Debug.Log($"[DEBUG] Saved Task IDs: {taskIds}");
        Debug.Log($"[DEBUG] Created Date: {createdDate}");
        Debug.Log($"[DEBUG] Last Reset: {lastReset}");
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnDestroy()
    {
        if (timeCheckCoroutine != null)
        {
            StopCoroutine(timeCheckCoroutine);
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && enableDailyTasks && isInitialized)
        {
            InitializeOrLoadDailyTasks();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && enableDailyTasks && isInitialized)
        {
            InitializeOrLoadDailyTasks();
        }
    }
    
    #endregion
}