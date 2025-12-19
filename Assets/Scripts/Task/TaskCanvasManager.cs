using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Linq;

public class TaskCanvasManager : MonoBehaviour
{
    [Header("Display Mode")]
    [SerializeField] private TaskDisplayMode displayMode = TaskDisplayMode.AlwaysVisible;
    
    [Header("UI References")]
    [SerializeField] private GameObject taskCanvas;
    [SerializeField] private TMP_Text playerGemsText;
    [SerializeField] private TMP_Text playerExperienceText;
    
    [Header("Task Switching")]
    [SerializeField] private Button dailyTasksButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private GameObject dailyTasksPanel;
    [SerializeField] private GameObject achievementsPanel;
    
    [Header("Daily Tasks UI")]
    [SerializeField] private Transform dailyTaskContainer;
    [SerializeField] private GameObject dailyTaskUIPrefab;
    
    [Header("Achievement Tasks UI")]
    [SerializeField] private Transform achievementContainer;
    [SerializeField] private GameObject achievementUIPrefab;

    [Header("Popup Mode Settings (только если displayMode = PopupMode)")]
    [SerializeField] private Button openTasksButton;
    [SerializeField] private Button closeTasksButton;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Notification Settings")]
    [SerializeField] private GameObject notificationIcon;
    [SerializeField] private TMP_Text notificationCountText;

    [Header("Button Sprites")]
    [SerializeField] private Sprite activeButtonSprite;
    [SerializeField] private Sprite inactiveButtonSprite;

    [Header("Audio Settings")]
    [Tooltip("Название UI звука при получении награды за квест")]
    [SerializeField] private string taskRewardClaimSound = "task_reward_claim";
    [Tooltip("Название UI звука при разблокировке достижения")]
    [SerializeField] private string achievementUnlockSound = "achievement_unlock";
    [Tooltip("Название UI звука при переключении вкладок")]
    [SerializeField] private string tabSwitchSound = "ui_switch";
    [Tooltip("Название UI звука при открытии канваса")]
    [SerializeField] private string canvasOpenSound = "ui_open";
    [Tooltip("Название UI звука при закрытии канваса")]
    [SerializeField] private string canvasCloseSound = "ui_close";
    [Tooltip("Воспроизводить звуки в системе тасков")]
    [SerializeField] private bool enableTaskSounds = true;

    private bool isTaskCanvasOpen = false;
    private int completedTasksCount = 0;
    private TaskViewMode currentViewMode = TaskViewMode.DailyTasks;

    #region Audio Helper Methods

    /// <summary>
    /// Воспроизводит UI звук если включены звуки тасков
    /// </summary>
    private void PlayTaskSound(string soundName)
    {
        if (!enableTaskSounds || string.IsNullOrEmpty(soundName)) return;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(soundName);
        }
        else
        {
            Debug.LogWarning("AudioManager недоступен для воспроизведения звука: " + soundName);
        }
    }

    /// <summary>
    /// Воспроизводит звук получения награды за квест
    /// </summary>
    private void PlayTaskRewardSound()
    {
        PlayTaskSound(taskRewardClaimSound);
    }

    /// <summary>
    /// Воспроизводит звук разблокировки достижения
    /// </summary>
    private void PlayAchievementUnlockSound()
    {
        PlayTaskSound(achievementUnlockSound);
    }

    /// <summary>
    /// Воспроизводит звук переключения вкладок
    /// </summary>
    private void PlayTabSwitchSound()
    {
        PlayTaskSound(tabSwitchSound);
    }

    /// <summary>
    /// Воспроизводит звук открытия канваса
    /// </summary>
    private void PlayCanvasOpenSound()
    {
        PlayTaskSound(canvasOpenSound);
    }

    /// <summary>
    /// Воспроизводит звук закрытия канваса
    /// </summary>
    private void PlayCanvasCloseSound()
    {
        PlayTaskSound(canvasCloseSound);
    }

    #endregion

    private void Start()
    {
        InitializeUI();
        SubscribeToTaskEvents();
        UpdatePlayerStats();
        CountCompletedTasks();
        UpdateNotificationIcon();
    }

    private void InitializeUI()
    {
        // Настраиваем UI в зависимости от выбранного режима
        switch (displayMode)
        {
            case TaskDisplayMode.AlwaysVisible:
                SetupAlwaysVisibleMode();
                break;
            case TaskDisplayMode.PopupMode:
                SetupPopupMode();
                break;
            case TaskDisplayMode.DisabledInScene:
                SetupDisabledMode();
                break;
        }

        // Устанавливаем ссылки в TaskManager если система активна
        if (displayMode != TaskDisplayMode.DisabledInScene && TaskManager.Instance != null)
        {
            // Устанавливаем ссылки для daily tasks
            if (dailyTaskUIPrefab != null && dailyTaskContainer != null)
            {
                TaskManager.Instance.SetUIReferences(dailyTaskContainer, dailyTaskUIPrefab);
                Debug.Log($"TaskCanvasManager: UI ссылки для daily tasks переданы в TaskManager (режим: {displayMode})");
            }
            
            // Устанавливаем ссылки для achievements
            if (achievementUIPrefab != null && achievementContainer != null)
            {
                TaskManager.Instance.SetAchievementUIReferences(achievementContainer, achievementUIPrefab);
                Debug.Log($"TaskCanvasManager: UI ссылки для achievements переданы в TaskManager (режим: {displayMode})");
            }
        }

        // Инициализируем кнопки переключения
        InitializeSwitchButtons();
        
        // Устанавливаем начальный режим просмотра
        SwitchToView(currentViewMode);
    }

    private void SetupAlwaysVisibleMode()
    {
        // Канвас всегда видим
        if (taskCanvas != null)
        {
            taskCanvas.SetActive(true);
            isTaskCanvasOpen = true;
        }

        // Скрываем кнопки открытия/закрытия если они есть
        if (openTasksButton != null)
            openTasksButton.gameObject.SetActive(false);
        if (closeTasksButton != null)
            closeTasksButton.gameObject.SetActive(false);

        Debug.Log("TaskCanvasManager: Настроен режим 'Всегда видимый'");
    }

    private void SetupPopupMode()
    {
        // Инициализируем кнопки
        if (openTasksButton != null)
        {
            openTasksButton.onClick.AddListener(OpenTaskCanvas);
            openTasksButton.gameObject.SetActive(true);
        }

        if (closeTasksButton != null)
        {
            closeTasksButton.onClick.AddListener(CloseTaskCanvas);
        }

        // Скрываем канвас по умолчанию
        if (taskCanvas != null)
        {
            taskCanvas.SetActive(false);
            isTaskCanvasOpen = false;
        }

        Debug.Log("TaskCanvasManager: Настроен режим 'Всплывающий'");
    }

    private void SetupDisabledMode()
    {
        // Полностью отключаем систему в этой сцене
        if (taskCanvas != null)
            taskCanvas.SetActive(false);

        if (openTasksButton != null)
            openTasksButton.gameObject.SetActive(false);

        if (notificationIcon != null)
            notificationIcon.SetActive(false);

        Debug.Log("TaskCanvasManager: Система тасков отключена в этой сцене");
    }

    private void InitializeSwitchButtons()
    {
        // Настраиваем кнопки переключения только если система не отключена
        if (displayMode != TaskDisplayMode.DisabledInScene)
        {
            if (dailyTasksButton != null)
            {
                dailyTasksButton.onClick.AddListener(() => SwitchToView(TaskViewMode.DailyTasks));
            }

            if (achievementsButton != null)
            {
                achievementsButton.onClick.AddListener(() => SwitchToView(TaskViewMode.Achievements));
            }

            Debug.Log("TaskCanvasManager: Кнопки переключения настроены");
        }
    }

    public void SwitchToView(TaskViewMode viewMode)
    {
        currentViewMode = viewMode;

        // Анимированное переключение панелей с DoTween
        if (dailyTasksPanel != null)
        {
            if (viewMode == TaskViewMode.DailyTasks)
            {
                dailyTasksPanel.SetActive(true);
                dailyTasksPanel.transform.localScale = Vector3.zero;
                dailyTasksPanel.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutCubic);
            }
            else
            {
                dailyTasksPanel.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => dailyTasksPanel.SetActive(false));
            }
        }
            
        if (achievementsPanel != null)
        {
            if (viewMode == TaskViewMode.Achievements)
            {
                achievementsPanel.SetActive(true);
                achievementsPanel.transform.localScale = Vector3.zero;
                achievementsPanel.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutCubic);
            }
            else
            {
                achievementsPanel.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => achievementsPanel.SetActive(false));
            }
        }

        // Обновляем спрайты кнопок — ЭТО ВАЖНО!
        UpdateButtonSprites();

        // Воспроизводим звук переключения
        PlayTabSwitchSound();

        Debug.Log($"TaskCanvasManager: Переключено на {(viewMode == TaskViewMode.DailyTasks ? "Daily Tasks" : "Achievements")}");
    }

    private void SubscribeToTaskEvents()
    {
        // Подписываемся на события только если система не отключена
        if (displayMode != TaskDisplayMode.DisabledInScene && TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskCompleted += OnTaskCompleted;
            TaskManager.Instance.OnTaskRewardClaimed += OnTaskRewardClaimed;
            TaskManager.Instance.OnTaskProgressUpdated += OnTaskProgressUpdated;
            TaskManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }
    }

    private void OnTaskCompleted(TaskData task)
    {
        completedTasksCount++;
        UpdateNotificationIcon();
        ShowTaskCompletedNotification(task);
    }

    private void OnTaskRewardClaimed(TaskData task)
    {
        completedTasksCount = Mathf.Max(0, completedTasksCount - 1);
        UpdateNotificationIcon();
        UpdatePlayerStats();
        ShowRewardEffect(task.reward);
    }

    private void OnTaskProgressUpdated(TaskData task)
    {
        // UI автоматически обновится через TaskManager
    }

    private void OnAchievementUnlocked(TaskData achievement)
    {
        completedTasksCount++;
        UpdateNotificationIcon();
        ShowAchievementUnlockedNotification(achievement);
    }

    private void UpdateNotificationIcon()
    {
        if (notificationIcon != null)
        {
            bool shouldShow = completedTasksCount > 0;
            notificationIcon.SetActive(shouldShow);
            
            // 🔍 ОТЛАДКА
            Debug.Log($"🔔 Notification Update:");
            Debug.Log($"   - Completed Tasks: {completedTasksCount}");
            Debug.Log($"   - Should Show: {shouldShow}");
            Debug.Log($"   - Icon Object: {notificationIcon.name}");
            Debug.Log($"   - Is Active After Set: {notificationIcon.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("⚠️ notificationIcon НЕ НАЗНАЧЕН в TaskCanvasManager!");
        }

        if (notificationCountText != null)
        {
            notificationCountText.text = completedTasksCount.ToString();
        }
    }

    private void UpdatePlayerStats()
    {
        if (CurrencyManager.Instance != null)
        {
            if (playerGemsText != null)
            {
                playerGemsText.text = CurrencyManager.Instance.Gems.ToString();
            }

            if (playerExperienceText != null)
            {
                int experience = PlayerPrefs.GetInt("PlayerExperience", 0);
                playerExperienceText.text = experience.ToString();
            }
        }
        else
        {
            Debug.LogWarning("CurrencyManager.Instance не найден!");
        }
    }

    // Методы для popup режима
    public void OpenTaskCanvas()
    {
        if (displayMode != TaskDisplayMode.PopupMode || isTaskCanvasOpen || taskCanvas == null) 
            return;

        isTaskCanvasOpen = true;
        taskCanvas.SetActive(true);

        // Анимация появления с DoTween
        taskCanvas.transform.localScale = Vector3.zero;
        taskCanvas.transform.DOScale(Vector3.one, animationDuration)
            .SetEase(Ease.OutBack);

        // Воспроизводим звук открытия
        PlayCanvasOpenSound();
    }

    public void CloseTaskCanvas()
    {
        if (displayMode != TaskDisplayMode.PopupMode || !isTaskCanvasOpen || taskCanvas == null) 
            return;

        // Анимация исчезновения с DoTween
        taskCanvas.transform.DOScale(Vector3.zero, animationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                taskCanvas.SetActive(false);
                isTaskCanvasOpen = false;
            });

        // Воспроизводим звук закрытия
        PlayCanvasCloseSound();
    }

    private void ShowTaskCompletedNotification(TaskData task)
    {
        Debug.Log($"Таск '{task.taskTitle}' выполнен!");
        
        // В popup режиме показываем уведомление о том, что нужно открыть канвас
        if (displayMode == TaskDisplayMode.PopupMode)
        {
            Debug.Log("Нажмите на кнопку тасков, чтобы получить награду.");
        }
    }

    private void ShowAchievementUnlockedNotification(TaskData achievement)
    {
        Debug.Log($"🏆 Достижение разблокировано: '{achievement.taskTitle}'!");
        
        // Воспроизводим звук разблокировки достижения
        PlayAchievementUnlockSound();
        
        // В popup режиме показываем уведомление о том, что нужно открыть канвас
        if (displayMode == TaskDisplayMode.PopupMode)
        {
            Debug.Log("Нажмите на кнопку тасков, чтобы получить награду за достижение.");
        }
    }

    private void ShowRewardEffect(TaskReward reward)
    {
        Debug.Log($"Получена награда: {reward.gems} гемов, {reward.experience} опыта!");

        // ВОСПРОИЗВОДИМ ЗВУК ПОЛУЧЕНИЯ НАГРАДЫ ЗА КВЕСТ
        PlayTaskRewardSound();

        if (CurrencyManager.Instance != null && reward.gems > 0)
        {
            // CurrencyManager.Instance.AddGems(reward.gems, "Награда за задачу");
        }

        // Анимация увеличения опыта (если ты её используешь)
        if (playerExperienceText != null && reward.experience > 0)
        {
            int startExp = PlayerPrefs.GetInt("PlayerExperience", 0);
            int endExp = startExp + reward.experience;

            PlayerPrefs.SetInt("PlayerExperience", endExp);
            PlayerPrefs.Save();

            DOTween.To(() => startExp, x =>
            {
                playerExperienceText.text = x.ToString();
            }, endExp, 1f).SetEase(Ease.OutQuart);

            playerExperienceText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 0.3f);
        }

        // Анимация увеличения гемов (если есть награда)
        if (playerGemsText != null && reward.gems > 0)
        {
            playerGemsText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 3, 0.2f);
        }
    }
    
    private void UpdateButtonSprites()
    {
        if (dailyTasksButton != null)
        {
            Image image = dailyTasksButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = currentViewMode == TaskViewMode.DailyTasks ? activeButtonSprite : inactiveButtonSprite;
            }
        }

        if (achievementsButton != null)
        {
            Image image = achievementsButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = currentViewMode == TaskViewMode.Achievements ? activeButtonSprite : inactiveButtonSprite;
            }
        }
    }

    private void CountCompletedTasks()
    {
        completedTasksCount = 0;

        if (displayMode != TaskDisplayMode.DisabledInScene && TaskManager.Instance != null)
        {
            // Считаем завершенные daily tasks и achievements отдельно
            var activeTasks = TaskManager.Instance.GetActiveTasks();
            var activeAchievements = TaskManager.Instance.GetActiveAchievements();

            int completedDailyTasks = activeTasks.Count(task => task.isCompleted && !task.isRewardClaimed);
            int completedAchievements = activeAchievements.Count(task => task.isCompleted && !task.isRewardClaimed);

            completedTasksCount = completedDailyTasks + completedAchievements;

            Debug.Log($"Завершенные таски: Daily Tasks - {completedDailyTasks}, Achievements - {completedAchievements}");
        }

        UpdateNotificationIcon();
    }
    
    // Методы для получения ссылок на UI элементы (используются TaskManager'ом)
    public Transform GetDailyTaskContainer()
    {
        return dailyTaskContainer;
    }
    
    public GameObject GetDailyTaskUIPrefab()
    {
        return dailyTaskUIPrefab;
    }

    public Transform GetAchievementContainer()
    {
        return achievementContainer;
    }
    
    public GameObject GetAchievementUIPrefab()
    {
        return achievementUIPrefab;
    }

    // Для обратной совместимости - старые методы
    public Transform GetTaskContainer()
    {
        return dailyTaskContainer; // Возвращаем daily task container по умолчанию
    }
    
    public GameObject GetTaskUIPrefab()
    {
        return dailyTaskUIPrefab; // Возвращаем daily task prefab по умолчанию
    }

    // Публичные методы для управления
    public TaskViewMode GetCurrentViewMode()
    {
        return currentViewMode;
    }

    public void SetCurrentViewMode(TaskViewMode viewMode)
    {
        SwitchToView(viewMode);
    }

    public bool IsDailyTasksActive()
    {
        return currentViewMode == TaskViewMode.DailyTasks;
    }

    public bool IsAchievementsActive()
    {
        return currentViewMode == TaskViewMode.Achievements;
    }

    // Публичный метод для изменения режима отображения во время выполнения
    public void SetDisplayMode(TaskDisplayMode newMode)
    {
        if (displayMode != newMode)
        {
            displayMode = newMode;
            InitializeUI();
        }
    }

    // Публичные методы для настройки звуков (если нужно изменить во время выполнения)
    public void SetTaskSoundsEnabled(bool enabled)
    {
        enableTaskSounds = enabled;
    }

    public void SetTaskRewardSound(string soundName)
    {
        taskRewardClaimSound = soundName;
    }

    public void SetAchievementUnlockSound(string soundName)
    {
        achievementUnlockSound = soundName;
    }

    private void OnDestroy()
    {
        // Останавливаем все DoTween анимации для этого объекта
        transform.DOKill();
        
        // Отписываемся от событий
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
            TaskManager.Instance.OnTaskRewardClaimed -= OnTaskRewardClaimed;
            TaskManager.Instance.OnTaskProgressUpdated -= OnTaskProgressUpdated;
            TaskManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }

        // Отписываемся от кнопок
        if (openTasksButton != null)
            openTasksButton.onClick.RemoveListener(OpenTaskCanvas);

        if (closeTasksButton != null)
            closeTasksButton.onClick.RemoveListener(CloseTaskCanvas);

        // Отписываемся от кнопок переключения
        if (dailyTasksButton != null)
            dailyTasksButton.onClick.RemoveAllListeners();

        if (achievementsButton != null)
            achievementsButton.onClick.RemoveAllListeners();
    }
}

// Enum для режимов просмотра тасков
public enum TaskViewMode
{
    DailyTasks,
    Achievements
}