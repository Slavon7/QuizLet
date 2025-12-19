using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }
    
    [Header("Task Settings")]
    [SerializeField] private List<TaskData> allTasks = new List<TaskData>();
    [SerializeField] private List<TaskData> dailyTasks = new List<TaskData>();
    [SerializeField] private List<TaskData> achievementTasks = new List<TaskData>(); // Статические достижения
    [SerializeField] private int maxActiveTasks = 3;
    [SerializeField] private int maxActiveAchievements = 5;

    [Header("UI References - переназначаются автоматически")]
    [System.NonSerialized] public Transform taskContainer; // Для обычных тасков
    [System.NonSerialized] public GameObject taskUIPrefab; // Для обычных тасков
    [System.NonSerialized] public Transform dailyTaskContainer; // Для ежедневных тасков
    [System.NonSerialized] public GameObject dailyTaskUIPrefab; // Для ежедневных тасков
    [System.NonSerialized] public Transform achievementContainer; // Для достижений
    [System.NonSerialized] public GameObject achievementUIPrefab; // Для достижений
    
    [Header("Achievement Settings")]
    [SerializeField] private AchievementCollection achievementCollection; // ScriptableObject со всеми достижениями
    [SerializeField] private bool useScriptableAchievements = true; // Использовать ли ScriptableObject
    
    // События
    public System.Action<TaskData> OnTaskProgressUpdated;
    public System.Action<TaskData> OnTaskCompleted;
    public System.Action<TaskData> OnTaskRewardClaimed;
    public System.Action<TaskData> OnAchievementUnlocked; // Событие для достижений
    
    // Активные таски и UI
    private List<TaskData> activeTasks = new List<TaskData>();
    private List<TaskData> activeAchievements = new List<TaskData>(); // Активные достижения
    private List<TaskUI> taskUIElements = new List<TaskUI>();
    
    private List<TaskUI> dailyTaskUIElements = new List<TaskUI>();
    private List<TaskUI> achievementUIElements = new List<TaskUI>();

    private bool isDailyTaskUIReady = false;
    private bool isAchievementUIReady = false;

    // Статистика игрока
    private Dictionary<TaskType, int> playerStats = new Dictionary<TaskType, int>();
    private Dictionary<TaskType, int> dailyPlayerStats = new Dictionary<TaskType, int>();
    
    // Управление UI
    private bool isUIReady = false;
    
    private void Awake()
    {
        InitializeSingleton();
    }
    
    private void Start()
    {
        InitializeSystem();
    }
    
    #region Initialization
    
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayerStats();
            LoadTaskProgress();
            InitializeDefaultAchievements(); // Инициализация достижений
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSystem()
    {
        InitializeTasks();
        RefreshActiveTasks();
        RefreshActiveAchievements(); // Обновление достижений
        CheckAndInitializeUI();
        EnsureDailyTaskManager();
    }
    
    private void InitializePlayerStats()
    {
        foreach (TaskType taskType in System.Enum.GetValues(typeof(TaskType)))
        {
            playerStats[taskType] = PlayerPrefs.GetInt($"Stat_{taskType}", 0);
            dailyPlayerStats[taskType] = PlayerPrefs.GetInt($"Daily_{taskType}", 0);
        }
    }
    
    private void InitializeTasks()
    {
        if (allTasks.Count == 0)
        {
            CreateDefaultTasks();
        }
        LoadTaskProgress();
    }
    
    // Создание стандартных достижений для всех игроков
    private void InitializeDefaultAchievements()
    {
        if (useScriptableAchievements && achievementCollection != null)
        {
            // Загружаем достижения из ScriptableObject
            LoadAchievementsFromScriptableObject();
        }
        else
        {
            // Используем старый способ создания достижений в коде
            if (achievementTasks.Count == 0)
            {
                CreateDefaultAchievements();
            }
        }
        LoadAchievementProgress();
    }
    
    private void LoadAchievementsFromScriptableObject()
    {
        achievementTasks.Clear();
        
        foreach (var achievementData in achievementCollection.achievements)
        {
            if (achievementData != null)
            {
                var taskData = achievementData.ToTaskData();
                achievementTasks.Add(taskData);
            }
        }
        
        Debug.Log($"Загружено {achievementTasks.Count} достижений из ScriptableObject");
    }
    
    // Создание базовых достижений
    private void CreateDefaultAchievements()
    {
        var defaultAchievements = new List<TaskData>
        {
            new TaskData
            {
                taskId = 1001,
                taskTitle = "Новичок",
                taskDescription = "Ответьте правильно на 10 вопросов",
                taskType = TaskType.CorrectAnswers,
                targetValue = 10,
                currentValue = 0,
                reward = new TaskReward { gems = 50, experience = 100 }
            },
            new TaskData
            {
                taskId = 1002,
                taskTitle = "Знаток",
                taskDescription = "Ответьте правильно на 100 вопросов",
                taskType = TaskType.CorrectAnswers,
                targetValue = 100,
                currentValue = 0,
                reward = new TaskReward { gems = 200, experience = 500 }
            },
            new TaskData
            {
                taskId = 1003,
                taskTitle = "Эксперт",
                taskDescription = "Ответьте правильно на 500 вопросов",
                taskType = TaskType.CorrectAnswers,
                targetValue = 500,
                currentValue = 0,
                reward = new TaskReward { gems = 500, experience = 1000 }
            },
            new TaskData
            {
                taskId = 1004,
                taskTitle = "Игроман",
                taskDescription = "Сыграйте 50 игр",
                taskType = TaskType.PlayGames,
                targetValue = 50,
                currentValue = 0,
                reward = new TaskReward { gems = 150, experience = 300 }
            },
            new TaskData
            {
                taskId = 1005,
                taskTitle = "Чемпион",
                taskDescription = "Выиграйте 25 игр",
                taskType = TaskType.WinGames,
                targetValue = 25,
                currentValue = 0,
                reward = new TaskReward { gems = 300, experience = 600 }
            },
            new TaskData
            {
                taskId = 1006,
                taskTitle = "Молниеносный",
                taskDescription = "Ответьте быстро 20 раз",
                taskType = TaskType.AnswerInTime,
                targetValue = 20,
                currentValue = 0,
                reward = new TaskReward { gems = 100, experience = 200 }
            },
            new TaskData
            {
                taskId = 1007,
                taskTitle = "Серия успехов",
                taskDescription = "Ответьте правильно 15 раз подряд",
                taskType = TaskType.ConsecutiveCorrect,
                targetValue = 15,
                currentValue = 0,
                reward = new TaskReward { gems = 250, experience = 400 }
            },
            new TaskData
            {
                taskId = 1008,
                taskTitle = "Постоянство",
                taskDescription = "Войдите в игру 7 дней подряд",
                taskType = TaskType.DailyLogin,
                targetValue = 7,
                currentValue = 0,
                reward = new TaskReward { gems = 300, experience = 500 }
            },
            new TaskData
            {
                taskId = 1009,
                taskTitle = "Мастер",
                taskDescription = "Ответьте правильно на 1000 вопросов",
                taskType = TaskType.CorrectAnswers,
                targetValue = 1000,
                currentValue = 0,
                reward = new TaskReward { gems = 1000, experience = 2000 }
            },
            new TaskData
            {
                taskId = 1010,
                taskTitle = "Легенда",
                taskDescription = "Выиграйте 100 игр",
                taskType = TaskType.WinGames,
                targetValue = 100,
                currentValue = 0,
                reward = new TaskReward { gems = 2000, experience = 5000 }
            }
        };
        
        achievementTasks.AddRange(defaultAchievements);
        Debug.Log($"Создано {defaultAchievements.Count} базовых достижений");
    }
    
    private void EnsureDailyTaskManager()
    {
        if (DailyTaskManager.Instance == null)
        {
            GameObject dailyTaskManagerObj = new GameObject("DailyTaskManager");
            dailyTaskManagerObj.AddComponent<DailyTaskManager>();
            DontDestroyOnLoad(dailyTaskManagerObj);
        }
    }
    
    #endregion
    
    #region Scene Management
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"TaskManager: Сцена загружена - {scene.name}");
        ResetUIReferences();
        StartCoroutine(DelayedUIInitialization());
    }
    
    private IEnumerator DelayedUIInitialization()
    {
        yield return null;
        CheckAndInitializeUI();
    }
    
    private void ResetUIReferences()
    {
        dailyTaskContainer = null;
        dailyTaskUIPrefab = null;
        achievementContainer = null;
        achievementUIPrefab = null;
        isDailyTaskUIReady = false;
        isAchievementUIReady = false;
        
        ClearUIElements();
    }
    
    private void ClearUIElements()
    {
        // Очищаем daily task UI элементы
        foreach (var taskUI in dailyTaskUIElements)
        {
            if (taskUI != null)
                Destroy(taskUI.gameObject);
        }
        dailyTaskUIElements.Clear();
        
        // Очищаем achievement UI элементы
        foreach (var taskUI in achievementUIElements)
        {
            if (taskUI != null)
                Destroy(taskUI.gameObject);
        }
        achievementUIElements.Clear();
    }
    
    #endregion
    
    #region UI Management
    
    private void CheckAndInitializeUI()
    {
        FindUIReferences();
        if (isDailyTaskUIReady || isAchievementUIReady)
        {
            UpdateTaskUI();
        }
    }
    
    private void FindUIReferences()
    {
        TaskCanvasManager canvasManager = FindFirstObjectByType<TaskCanvasManager>();
        
        if (canvasManager != null)
        {
            // Настраиваем daily tasks UI
            dailyTaskContainer = canvasManager.GetDailyTaskContainer();
            dailyTaskUIPrefab = canvasManager.GetDailyTaskUIPrefab();
            isDailyTaskUIReady = (dailyTaskContainer != null && dailyTaskUIPrefab != null);
            
            // Настраиваем achievements UI
            achievementContainer = canvasManager.GetAchievementContainer();
            achievementUIPrefab = canvasManager.GetAchievementUIPrefab();
            isAchievementUIReady = (achievementContainer != null && achievementUIPrefab != null);
            
            Debug.Log($"TaskManager: Daily Tasks UI готов: {isDailyTaskUIReady}, Achievements UI готов: {isAchievementUIReady}");
        }
        else
        {
            Debug.Log("TaskManager: TaskCanvasManager не найден в текущей сцене");
        }
    }
    
    public void SetUIReferences(Transform container, GameObject prefab)
    {
        dailyTaskContainer = container;
        dailyTaskUIPrefab = prefab;
        isDailyTaskUIReady = (container != null && prefab != null);
        
        if (isDailyTaskUIReady)
        {
            Debug.Log("TaskManager: UI ссылки для daily tasks установлены вручную");
            UpdateTaskUI();
        }
        else
        {
            Debug.Log("TaskManager: UI ссылки для daily tasks сброшены");
        }
    }
    
    // Новый метод для установки ссылок на UI достижений
    public void SetAchievementUIReferences(Transform container, GameObject prefab)
    {
        achievementContainer = container;
        achievementUIPrefab = prefab;
        isAchievementUIReady = (container != null && prefab != null);
        
        if (isAchievementUIReady)
        {
            Debug.Log("TaskManager: UI ссылки для achievements установлены");
            UpdateTaskUI();
        }
        else
        {
            Debug.Log("TaskManager: UI ссылки для achievements сброшены");
        }
    }
    
    private void UpdateTaskUI()
    {
        ClearUIElements();
        CreateUIElements();
        
        int totalElements = dailyTaskUIElements.Count + achievementUIElements.Count;
        Debug.Log($"TaskManager: UI обновлен, создано {dailyTaskUIElements.Count} daily tasks и {achievementUIElements.Count} achievements элементов");
    }
    
    // Обновленное создание UI элементов с учетом достижений
    private void CreateUIElements()
    {
        // Создаем UI для daily tasks
        if (isDailyTaskUIReady)
        {
            foreach (var task in activeTasks)
            {
                CreateDailyTaskUIElement(task);
            }
        }
        
        // Создаем UI для achievements
        if (isAchievementUIReady)
        {
            foreach (var achievement in activeAchievements)
            {
                CreateAchievementUIElement(achievement);
            }
        }
    }
    
    private void CreateAchievementUIElement(TaskData achievement)
    {
        if (achievementContainer == null || achievementUIPrefab == null) return;
        
        GameObject achievementUIObj = Instantiate(achievementUIPrefab, achievementContainer);
        TaskUI achievementUI = achievementUIObj.GetComponent<TaskUI>();
        
        if (achievementUI != null)
        {
            achievementUI.Initialize(achievement);
            achievementUI.OnRewardClaimed += HandleAchievementRewardClaimed;
            achievementUIElements.Add(achievementUI);
        }
    }
    
    private void CreateDailyTaskUIElement(TaskData task)
    {
        if (dailyTaskContainer == null || dailyTaskUIPrefab == null) return;

        GameObject taskUIObj = Instantiate(dailyTaskUIPrefab, dailyTaskContainer);
        TaskUI taskUI = taskUIObj.GetComponent<TaskUI>();

        if (taskUI != null)
        {
            taskUI.Initialize(task);
            taskUI.OnRewardClaimed += HandleRewardClaimed;
            dailyTaskUIElements.Add(taskUI);
        }
    }

    
    private void CreateTaskUIElement(TaskData task)
    {
        GameObject taskUIObj = Instantiate(taskUIPrefab, taskContainer);
        TaskUI taskUI = taskUIObj.GetComponent<TaskUI>();

        if (taskUI != null)
        {
            taskUI.Initialize(task);

            // Подписываемся на разные события в зависимости от типа таска
            if (IsAchievement(task))
            {
                taskUI.OnRewardClaimed += HandleAchievementRewardClaimed;
            }
            else
            {
                taskUI.OnRewardClaimed += HandleRewardClaimed;
            }

            taskUIElements.Add(taskUI);
        }
    }
    
    private void UpdateTaskUIElements()
    {
        // Обновляем daily tasks UI
        for (int i = 0; i < dailyTaskUIElements.Count && i < activeTasks.Count; i++)
        {
            if (dailyTaskUIElements[i] != null)
            {
                dailyTaskUIElements[i].UpdateProgress(activeTasks[i]);
            }
        }
        
        // Обновляем achievements UI
        for (int i = 0; i < achievementUIElements.Count && i < activeAchievements.Count; i++)
        {
            if (achievementUIElements[i] != null)
            {
                achievementUIElements[i].UpdateProgress(activeAchievements[i]);
            }
        }
    }
    
    #endregion
    
    #region Achievement Management
    
    // Обновление активных достижений
    private void RefreshActiveAchievements()
    {
        // Берем незавершенные достижения, отсортированные по сложности
        var availableAchievements = achievementTasks
            .Where(a => !a.isRewardClaimed)
            .OrderBy(a => a.targetValue)
            .Take(maxActiveAchievements)
            .ToList();
        
        activeAchievements.Clear();
        activeAchievements.AddRange(availableAchievements);
        
        Debug.Log($"Активных достижений: {activeAchievements.Count}");
    }
    
    // Загрузка прогресса достижений
    private void LoadAchievementProgress()
    {
        foreach (var achievement in achievementTasks)
        {
            LoadSingleTaskProgress(achievement, "Achievement");
            
            // Обновляем текущий прогресс на основе общей статистики
            if (playerStats.ContainsKey(achievement.taskType))
            {
                achievement.currentValue = playerStats[achievement.taskType];
                
                // Проверяем, не завершилось ли достижение
                if (achievement.currentValue >= achievement.targetValue && !achievement.isCompleted)
                {
                    achievement.isCompleted = true;
                    OnAchievementUnlocked?.Invoke(achievement);
                    Debug.Log($"🏆 Достижение разблокировано: {achievement.taskTitle}!");
                }
            }
        }
    }
    
    // Сохранение прогресса достижений
    private void SaveAchievementProgress(TaskData achievement)
    {
        PlayerPrefs.SetInt($"Achievement_{achievement.taskId}_Current", achievement.currentValue);
        PlayerPrefs.SetInt($"Achievement_{achievement.taskId}_Completed", achievement.isCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"Achievement_{achievement.taskId}_Claimed", achievement.isRewardClaimed ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // Обновление прогресса достижений
    private void UpdateAchievementsProgress(TaskType taskType)
    {
        bool hasNewAchievements = false;
        
        foreach (var achievement in achievementTasks)
        {
            if (achievement.taskType == taskType && !achievement.isCompleted)
            {
                // Проверяем предварительные условия
                var achievementData = GetAchievementScriptableById(achievement.taskId);
                if (achievementData != null && !CanUnlockAchievement(achievementData))
                {
                    continue; // Пропускаем, если не выполнены предварительные условия
                }
                
                achievement.currentValue = playerStats[taskType];
                
                if (achievement.currentValue >= achievement.targetValue)
                {
                    achievement.isCompleted = true;
                    OnAchievementUnlocked?.Invoke(achievement);
                    hasNewAchievements = true;
                    
                    // Особое сообщение для редких достижений
                    if (achievementData != null)
                    {
                        string rarityText = GetRarityText(achievementData.rarity);
                        Debug.Log($"🏆 {rarityText} достижение разблокировано: {achievement.taskTitle}!");
                    }
                    else
                    {
                        Debug.Log($"🏆 Достижение разблокировано: {achievement.taskTitle}!");
                    }
                }
                
                SaveAchievementProgress(achievement);
            }
        }
        
        if (hasNewAchievements)
        {
            RefreshActiveAchievements();
        }
    }
    
    // Получение награды за достижение
    private void HandleAchievementRewardClaimed(TaskData achievement)
    {
        achievement.isRewardClaimed = true;
        SaveAchievementProgress(achievement);
        AddRewardToPlayer(achievement.reward);
        OnTaskRewardClaimed?.Invoke(achievement);
        
        RefreshActiveAchievements();
        
        if (isUIReady)
        {
            UpdateTaskUI();
        }
        
        Debug.Log($"Получена награда за достижение: {achievement.taskTitle}");
    }
    
    // Проверка, является ли таск достижением
    private bool IsAchievement(TaskData task)
    {
        return task.taskId >= 1001 && task.taskId <= 9999; // Диапазон ID для достижений
    }
    
    #endregion
    
    #region Task Management
    
    private void CreateDefaultTasks()
    {
        // Заглушка - таски добавляются через инспектор или другими способами
    }
    
    private void RefreshActiveTasks()
    {
        var availableTasks = allTasks.Where(t => !t.isRewardClaimed).ToList();
        var availableDailyTasks = dailyTasks.Where(t => !t.isRewardClaimed).ToList();
        
        activeTasks.Clear();
        activeTasks.AddRange(availableDailyTasks);
        
        int remainingSlots = maxActiveTasks - activeTasks.Count;
        if (remainingSlots > 0)
        {
            activeTasks.AddRange(availableTasks.Take(remainingSlots));
        }
        
        Debug.Log($"RefreshActiveTasks: Ежедневных: {availableDailyTasks.Count}, Обычных: {Math.Min(remainingSlots, availableTasks.Count)}, Всего: {activeTasks.Count}");
    }
    
    public void UpdateTaskProgress(TaskType taskType, int value = 1)
    {
        UpdatePlayerStats(taskType, value);
        UpdateActiveTasksProgress(taskType);
        UpdateAchievementsProgress(taskType); // Обновление достижений
        
        if (isUIReady)
        {
            UpdateTaskUIElements();
        }
    }
    
    private void UpdatePlayerStats(TaskType taskType, int value)
    {
        if (playerStats.ContainsKey(taskType))
        {
            playerStats[taskType] += value;
            PlayerPrefs.SetInt($"Stat_{taskType}", playerStats[taskType]);
        }
        
        if (dailyPlayerStats.ContainsKey(taskType))
        {
            dailyPlayerStats[taskType] += value;
            PlayerPrefs.SetInt($"Daily_{taskType}", dailyPlayerStats[taskType]);
        }
    }
    
    private void UpdateActiveTasksProgress(TaskType taskType)
    {
        bool hasUpdates = false;
        
        foreach (var task in activeTasks)
        {
            if (task.taskType == taskType && !task.isCompleted)
            {
                task.currentValue = IsDailyTask(task) ? 
                    dailyPlayerStats[taskType] : 
                    playerStats[taskType];
                
                if (task.currentValue >= task.targetValue)
                {
                    task.isCompleted = true;
                    OnTaskCompleted?.Invoke(task);
                    Debug.Log($"Таск '{task.taskTitle}' выполнен!");
                }
                
                OnTaskProgressUpdated?.Invoke(task);
                SaveTaskProgress(task);
                hasUpdates = true;
            }
        }
        
        if (hasUpdates)
        {
            PlayerPrefs.Save();
        }
    }
    
    public void ResetTaskProgress(TaskType taskType)
    {
        foreach (var task in activeTasks)
        {
            if (task.taskType == taskType && !task.isCompleted)
            {
                task.currentValue = 0;
                OnTaskProgressUpdated?.Invoke(task);
                SaveTaskProgress(task);
            }
        }

        if (isUIReady)
        {
            UpdateTaskUIElements();
        }
    }
    
    private bool IsDailyTask(TaskData task)
    {
        return task.taskId >= 101;
    }
    
    #endregion
    
    #region Reward Management
    
    private void HandleRewardClaimed(TaskData task)
    {
        task.isRewardClaimed = true;
        SaveTaskProgress(task);
        AddRewardToPlayer(task.reward);
        OnTaskRewardClaimed?.Invoke(task);
        
        RefreshActiveTasks();
        
        if (isUIReady)
        {
            UpdateTaskUI();
        }
    }
    
    private void AddRewardToPlayer(TaskReward reward)
    {
        if (CurrencyManager.Instance != null)
        {
            AddRewardThroughCurrencyManager(reward);
        }
        else
        {
            Debug.LogWarning("CurrencyManager.Instance не найден! Используем PlayerPrefs");
            AddRewardThroughPlayerPrefs(reward);
        }
        
        AddExperience(reward.experience);
    }
    
    private void AddRewardThroughCurrencyManager(TaskReward reward)
    {
        if (reward.gems > 0)
        {
            CurrencyManager.Instance.AddGems(reward.gems, "Награда за таск");
            Debug.Log($"Получена награда: {reward.gems} гемов через CurrencyManager");
        }
        
        if (reward.coins > 0)
        {
            CurrencyManager.Instance.AddCoins(reward.coins, "Награда за таск");
            Debug.Log($"Получена награда: {reward.coins} монет через CurrencyManager");
        }
    }
    
    private void AddRewardThroughPlayerPrefs(TaskReward reward)
    {
        if (reward.gems > 0)
        {
            int currentGems = PlayerPrefs.GetInt("PlayerGems", 0);
            PlayerPrefs.SetInt("PlayerGems", currentGems + reward.gems);
        }
        
        if (reward.coins > 0)
        {
            int currentCoins = PlayerPrefs.GetInt("PlayerCoins", 0);
            PlayerPrefs.SetInt("PlayerCoins", currentCoins + reward.coins);
        }
    }
    
    private void AddExperience(int experience)
    {
        if (experience > 0)
        {
            int currentExp = PlayerPrefs.GetInt("PlayerExperience", 0);
            PlayerPrefs.SetInt("PlayerExperience", currentExp + experience);
            Debug.Log($"Получен опыт: {experience}");
        }
    }
    
    #endregion
    
    #region Data Persistence
    
    private void SaveTaskProgress(TaskData task)
    {
        string prefix = IsAchievement(task) ? "Achievement" : 
                       IsDailyTask(task) ? "DailyTask" : "Task";
        PlayerPrefs.SetInt($"{prefix}_{task.taskId}_Current", task.currentValue);
        PlayerPrefs.SetInt($"{prefix}_{task.taskId}_Completed", task.isCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"{prefix}_{task.taskId}_Claimed", task.isRewardClaimed ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public void SaveAllDailyTaskProgress()
    {
        foreach (var task in dailyTasks)
        {
            SaveTaskProgress(task);
        }
        Debug.Log("Сохранен прогресс всех ежедневных тасков");
    }
    
    private void LoadTaskProgress()
    {
        foreach (var task in allTasks)
        {
            LoadSingleTaskProgress(task, "Task");
        }

        foreach (var task in dailyTasks)
        {
            LoadSingleTaskProgress(task, "DailyTask");
        }
    }
    
    private void LoadSingleTaskProgress(TaskData task, string prefix)
    {
        task.currentValue = PlayerPrefs.GetInt($"{prefix}_{task.taskId}_Current", 0);
        task.isCompleted = PlayerPrefs.GetInt($"{prefix}_{task.taskId}_Completed", 0) == 1;
        task.isRewardClaimed = PlayerPrefs.GetInt($"{prefix}_{task.taskId}_Claimed", 0) == 1;
    }
    
    #endregion
    
    #region Daily Tasks Management
    
    public void AddDailyTask(TaskData dailyTask)
    {
        if (!dailyTasks.Any(t => t.taskId == dailyTask.taskId))
        {
            dailyTasks.Add(dailyTask);
            Debug.Log($"Добавлен ежедневный таск: {dailyTask.taskTitle} (ID: {dailyTask.taskId})");
        }
        else
        {
            Debug.Log($"Ежедневный таск {dailyTask.taskId} уже существует, пропускаем");
        }
    }
    
    public void RefreshActiveTasksAfterDailyTasksAdded()
    {
        RefreshActiveTasks();
        
        if (isUIReady)
        {
            UpdateTaskUI();
        }
        
        Debug.Log($"Финальное обновление: показано {activeTasks.Count} активных тасков");
    }
    
    public void RemoveDailyTasks()
    {
        Debug.Log($"Удаляем {dailyTasks.Count} ежедневных тасков");
        dailyTasks.Clear();
    }
    
    public void ResetDailyStats()
    {
        foreach (TaskType taskType in System.Enum.GetValues(typeof(TaskType)))
        {
            dailyPlayerStats[taskType] = 0;
            PlayerPrefs.SetInt($"Daily_{taskType}", 0);
        }
        PlayerPrefs.Save();
    }
    
    #endregion
    
    #region Game Integration Methods
    
    public void OnCorrectAnswer(bool isQuick = false)
    {
        UpdateTaskProgress(TaskType.CorrectAnswers);
        
        if (isQuick)
        {
            UpdateTaskProgress(TaskType.AnswerInTime);
        }
    }
    
    public void OnIncorrectAnswer()
    {
        ResetTaskProgress(TaskType.ConsecutiveCorrect);
    }
    
    public void OnGamePlayed()
    {
        UpdateTaskProgress(TaskType.PlayGames);
    }
    
    public void OnGameWon()
    {
        UpdateTaskProgress(TaskType.WinGames);
    }
    
    public void OnDailyLogin()
    {
        UpdateTaskProgress(TaskType.DailyLogin);
    }
    
    public void SetTaskProgress(TaskType taskType, int value)
    {
        foreach (var task in activeTasks)
        {
            if (task.taskType == taskType && !task.isCompleted)
            {
                task.currentValue = value;
                
                if (task.currentValue >= task.targetValue)
                {
                    task.isCompleted = true;
                    OnTaskCompleted?.Invoke(task);
                    Debug.Log($"Таск '{task.taskTitle}' выполнен!");
                }
                
                OnTaskProgressUpdated?.Invoke(task);
                SaveTaskProgress(task);
            }
        }
        
        if (isUIReady)
        {
            UpdateTaskUIElements();
        }
    }
    
    #endregion
    
    #region Public Getters
    
    public int GetPlayerStat(TaskType statType)
    {
        return playerStats.ContainsKey(statType) ? playerStats[statType] : 0;
    }
    
    public int GetDailyPlayerStat(TaskType statType)
    {
        return dailyPlayerStats.ContainsKey(statType) ? dailyPlayerStats[statType] : 0;
    }

    // Обновленное получение количества завершенных тасков включая достижения
    public int GetCompletedTasksCount()
    {
        int completedTasks = activeTasks.Count(task => task.isCompleted && !task.isRewardClaimed);
        int completedAchievements = activeAchievements.Count(task => task.isCompleted && !task.isRewardClaimed);
        return completedTasks + completedAchievements;
    }

    public List<TaskData> GetActiveTasks()
    {
        return new List<TaskData>(activeTasks);
    }
    
    public List<TaskData> GetDailyTasks()
    {
        return new List<TaskData>(dailyTasks);
    }
    
    // Получение списка достижений
    public List<TaskData> GetAchievements()
    {
        return new List<TaskData>(achievementTasks);
    }
    
    public List<TaskData> GetActiveAchievements()
    {
        return new List<TaskData>(activeAchievements);
    }
    
    // Новые методы для работы с ScriptableObject
    public AchievementData GetAchievementScriptableById(int achievementId)
    {
        if (achievementCollection != null)
        {
            return achievementCollection.GetAchievementById(achievementId);
        }
        return null;
    }
    
    public List<AchievementData> GetAchievementsByCategory(string categoryName)
    {
        if (achievementCollection != null)
        {
            var category = achievementCollection.categories.Find(c => c.categoryName == categoryName);
            return category?.categoryAchievements ?? new List<AchievementData>();
        }
        return new List<AchievementData>();
    }
    
    public List<AchievementData> GetAchievementsByRarity(AchievementRarity rarity)
    {
        if (achievementCollection != null)
        {
            return achievementCollection.GetAchievementsByRarity(rarity);
        }
        return new List<AchievementData>();
    }
    
    // Проверка предварительных условий для достижения
    public bool CanUnlockAchievement(AchievementData achievement)
    {
        if (achievement.requiredAchievementIds == null || achievement.requiredAchievementIds.Count == 0)
        {
            return true;
        }

        foreach (int requiredId in achievement.requiredAchievementIds)
        {
            var taskData = achievementTasks.Find(t => t.taskId == requiredId);
            if (taskData == null || !taskData.isCompleted)
            {
                return false;
            }
        }

        return true;
    }
    
    private string GetRarityText(AchievementRarity rarity)
    {
        return rarity switch
        {
            AchievementRarity.Common => "Обычное",
            AchievementRarity.Uncommon => "Необычное",
            AchievementRarity.Rare => "Редкое",
            AchievementRarity.Epic => "Эпическое",
            AchievementRarity.Legendary => "ЛЕГЕНДАРНОЕ",
            _ => ""
        };
    }
    
    // Метод для получения цвета редкости
    public Color GetRarityColor(AchievementRarity rarity)
    {
        return rarity switch
        {
            AchievementRarity.Common => Color.gray,
            AchievementRarity.Uncommon => Color.green,
            AchievementRarity.Rare => Color.blue,
            AchievementRarity.Epic => Color.magenta,
            AchievementRarity.Legendary => new Color(1f, 0.5f, 0f), // Оранжевый
            _ => Color.white
        };
    }
    
    // Получение статистики достижений
    public AchievementStatistics GetAchievementStatistics()
    {
        var stats = new AchievementStatistics();
        
        foreach (var achievement in achievementTasks)
        {
            stats.totalAchievements++;
            
            if (achievement.isCompleted)
                stats.unlockedAchievements++;
                
            if (achievement.isRewardClaimed)
                stats.claimedAchievements++;
        }
        
        return stats;
    }

    // Обновленная статистика тасков
    public TaskStatistics GetTaskStatistics()
    {
        var stats = new TaskStatistics();
        var allTasksList = new List<TaskData>();
        allTasksList.AddRange(allTasks);
        allTasksList.AddRange(dailyTasks);
        allTasksList.AddRange(achievementTasks); // Включаем достижения
        
        foreach (var task in allTasksList)
        {
            stats.totalTasks++;
            
            if (task.isCompleted)
                stats.completedTasks++;
                
            if (task.isRewardClaimed)
                stats.claimedTasks++;
        }
        
        return stats;
    }
    
    #endregion
    
    #region Utility Methods
    
    public void ForceUpdateUI()
    {
        if (isDailyTaskUIReady || isAchievementUIReady)
        {
            UpdateTaskUI();
        }
        else
        {
            Debug.Log("TaskManager: UI не готов для принудительного обновления");
        }
    }
    
    private bool ValidateUI()
    {
        bool dailyUIValid = isDailyTaskUIReady && dailyTaskContainer != null && dailyTaskUIPrefab != null;
        bool achievementUIValid = isAchievementUIReady && achievementContainer != null && achievementUIPrefab != null;
        
        if (!dailyUIValid && !achievementUIValid)
        {
            Debug.Log("TaskManager: Ни один из UI не готов для обновления");
            return false;
        }
        return true;
    }

    public void AddNewTask(TaskData newTask)
    {
        if (IsAchievement(newTask))
        {
            achievementTasks.Add(newTask);
            RefreshActiveAchievements();
        }
        else if (IsDailyTask(newTask))
        {
            dailyTasks.Add(newTask);
            RefreshActiveTasks();
        }
        else
        {
            allTasks.Add(newTask);
            RefreshActiveTasks();
        }

        if (isUIReady)
        {
            UpdateTaskUI();
        }
    }

    public void CheckDailyTasks()
    {
        string lastLoginDate = PlayerPrefs.GetString("LastLoginDate", "");
        string currentDate = System.DateTime.Now.ToString("yyyy-MM-dd");
        
        if (lastLoginDate != currentDate)
        {
            PlayerPrefs.SetString("LastLoginDate", currentDate);
            OnDailyLogin();
            PlayerPrefs.Save();
        }
    }

    public void ValidateDailyTaskCount()
    {
        const int EXPECTED_DAILY_TASKS = 3;
        
        if (dailyTasks.Count != EXPECTED_DAILY_TASKS)
        {
            Debug.LogWarning($"⚠️ Неправильное количество ежедневных тасков! Ожидается: {EXPECTED_DAILY_TASKS}, Фактически: {dailyTasks.Count}");
            
            if (dailyTasks.Count > EXPECTED_DAILY_TASKS)
            {
                Debug.LogWarning("Удаляем лишние ежедневные таски...");
                dailyTasks = dailyTasks.Take(EXPECTED_DAILY_TASKS).ToList();
                RefreshActiveTasksAfterDailyTasksAdded();
            }
        }
        else
        {
            Debug.Log($"✅ Правильное количество ежедневных тасков: {dailyTasks.Count}");
        }
    }
    
    // Методы для работы с достижениями
    public void AddNewAchievement(TaskData achievement)
    {
        if (!achievementTasks.Any(a => a.taskId == achievement.taskId))
        {
            achievementTasks.Add(achievement);
            RefreshActiveAchievements();
            
            if (isUIReady)
            {
                UpdateTaskUI();
            }
            
            Debug.Log($"Добавлено новое достижение: {achievement.taskTitle}");
        }
    }
    
    public TaskData GetAchievementById(int achievementId)
    {
        return achievementTasks.FirstOrDefault(a => a.taskId == achievementId);
    }
    
    public bool IsAchievementUnlocked(int achievementId)
    {
        var achievement = GetAchievementById(achievementId);
        return achievement != null && achievement.isCompleted;
    }
    
    public bool IsAchievementClaimed(int achievementId)
    {
        var achievement = GetAchievementById(achievementId);
        return achievement != null && achievement.isRewardClaimed;
    }
    
    // Сброс всех достижений (для тестирования)
    public void ResetAllAchievements()
    {
        foreach (var achievement in achievementTasks)
        {
            achievement.currentValue = 0;
            achievement.isCompleted = false;
            achievement.isRewardClaimed = false;
            SaveAchievementProgress(achievement);
        }
        
        RefreshActiveAchievements();
        
        if (isUIReady)
        {
            UpdateTaskUI();
        }
        
        Debug.Log("Все достижения сброшены");
    }
    
    #endregion
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugReloadAchievements()
    {
        if (useScriptableAchievements && achievementCollection != null)
        {
            LoadAchievementsFromScriptableObject();
            RefreshActiveAchievements();
            
            if (isUIReady)
            {
                UpdateTaskUI();
            }
            
            Debug.Log("[DEBUG] Достижения перезагружены из ScriptableObject");
        }
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPrintAllTasks()
    {
        Debug.Log("=== DEBUG: Все таски в системе ===");
        
        Debug.Log($"Обычные таски ({allTasks.Count}):");
        foreach (var task in allTasks)
        {
            Debug.Log($"  ID:{task.taskId} - {task.taskTitle} - {task.currentValue}/{task.targetValue} - Завершен:{task.isCompleted} - Получен:{task.isRewardClaimed}");
        }
        
        Debug.Log($"Ежедневные таски ({dailyTasks.Count}):");
        foreach (var task in dailyTasks)
        {
            Debug.Log($"  ID:{task.taskId} - {task.taskTitle} - {task.currentValue}/{task.targetValue} - Завершен:{task.isCompleted} - Получен:{task.isRewardClaimed}");
        }
        
        Debug.Log($"Достижения ({achievementTasks.Count}):");
        foreach (var achievement in achievementTasks)
        {
            Debug.Log($"  ID:{achievement.taskId} - {achievement.taskTitle} - {achievement.currentValue}/{achievement.targetValue} - Завершен:{achievement.isCompleted} - Получен:{achievement.isRewardClaimed}");
        }
        
        Debug.Log($"Активные таски ({activeTasks.Count}):");
        foreach (var task in activeTasks)
        {
            string type = IsDailyTask(task) ? "ЕЖЕДНЕВНЫЙ" : "ОБЫЧНЫЙ";
            Debug.Log($"  [{type}] ID:{task.taskId} - {task.taskTitle} - {task.currentValue}/{task.targetValue}");
        }
        
        Debug.Log($"Активные достижения ({activeAchievements.Count}):");
        foreach (var achievement in activeAchievements)
        {
            Debug.Log($"  [ДОСТИЖЕНИЕ] ID:{achievement.taskId} - {achievement.taskTitle} - {achievement.currentValue}/{achievement.targetValue}");
        }
        
        Debug.Log("=== Конец отладочной информации ===");
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugClearAllDailyProgress()
    {
        Debug.Log("[DEBUG] Очистка всего прогресса ежедневных тасков");
        
        foreach (var task in dailyTasks)
        {
            task.currentValue = 0;
            task.isCompleted = false;
            task.isRewardClaimed = false;
            SaveTaskProgress(task);
        }
        
        ResetDailyStats();
        
        if (isUIReady)
        {
            UpdateTaskUI();
        }
        
        Debug.Log("[DEBUG] Прогресс ежедневных тасков очищен");
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugUnlockAllAchievements()
    {
        Debug.Log("[DEBUG] Разблокировка всех достижений");
        
        foreach (var achievement in achievementTasks)
        {
            if (!achievement.isCompleted)
            {
                achievement.currentValue = achievement.targetValue;
                achievement.isCompleted = true;
                OnAchievementUnlocked?.Invoke(achievement);
                SaveAchievementProgress(achievement);
                Debug.Log($"[DEBUG] Разблокировано: {achievement.taskTitle}");
            }
        }
        
        RefreshActiveAchievements();
        
        if (isUIReady)
        {
            UpdateTaskUI();
        }
        
        Debug.Log("[DEBUG] Все достижения разблокированы");
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugAddTestStats()
    {
        Debug.Log("[DEBUG] Добавление тестовой статистики");
        
        UpdateTaskProgress(TaskType.CorrectAnswers, 15);
        UpdateTaskProgress(TaskType.PlayGames, 5);
        UpdateTaskProgress(TaskType.WinGames, 3);
        UpdateTaskProgress(TaskType.AnswerInTime, 8);
        UpdateTaskProgress(TaskType.DailyLogin, 1);
        
        Debug.Log("[DEBUG] Тестовая статистика добавлена");
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveAllDailyTaskProgress();
            SaveAllAchievementProgress();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveAllDailyTaskProgress();
            SaveAllAchievementProgress();
        }
    }
    
    private void SaveAllAchievementProgress()
    {
        foreach (var achievement in achievementTasks)
        {
            SaveAchievementProgress(achievement);
        }
        Debug.Log("Сохранен прогресс всех достижений");
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    
    #endregion

    #region Data Classes
    
    [System.Serializable]
    public class TaskStatistics
    {
        public int totalTasks;
        public int completedTasks;
        public int claimedTasks;
        public float completionPercentage => totalTasks > 0 ? (float)completedTasks / totalTasks * 100f : 0f;
    }
    
    // Статистика достижений
    [System.Serializable]
    public class AchievementStatistics
    {
        public int totalAchievements;
        public int unlockedAchievements;
        public int claimedAchievements;
        public float unlockPercentage => totalAchievements > 0 ? (float)unlockedAchievements / totalAchievements * 100f : 0f;
        public float claimPercentage => totalAchievements > 0 ? (float)claimedAchievements / totalAchievements * 100f : 0f;
    }
    
    #endregion
}