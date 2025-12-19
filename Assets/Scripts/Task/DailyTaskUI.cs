using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyTaskUI : MonoBehaviour
{
    [Header("Daily Task Timer")]
    [SerializeField] private TMP_Text resetTimerText;
    [SerializeField] private Button forceResetButton; // Только для отладки
    
    [Header("Daily Progress Overview")]
    [SerializeField] private TMP_Text dailyProgressText;
    [SerializeField] private Slider dailyProgressSlider;
    
    [Header("Daily Rewards Summary")]
    [SerializeField] private TMP_Text dailyRewardsText;
    [SerializeField] private GameObject dailyTaskPanel;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color urgentTimerColor = Color.red;
    [SerializeField] private float urgentTimeThreshold = 3600f; // 1 час в секундах
    
    [Header("Localization")]
    [SerializeField] private string progressTextFormat = "Виконано: {0}/{1}";
    [SerializeField] private string availableRewardsFormat = "Доступно нагород: {0}\n💎 {1} гемів\n⭐ {2} досвіду";
    [SerializeField] private string totalRewardsFormat = "Всього можна отримати:\n💎 {0} гемів\n⭐ {1} досвіду";
    
    private DailyTaskManager dailyTaskManager;
    private TaskManager taskManager;
    
    private void Start()
    {
        // Получаем ссылки на менеджеры
        dailyTaskManager = DailyTaskManager.Instance;
        taskManager = TaskManager.Instance;
        
        if (dailyTaskManager == null)
        {
            Debug.LogWarning("DailyTaskManager не найден!");
            return;
        }
        
        // Подписываемся на события
        if (dailyTaskManager != null)
        {
            dailyTaskManager.OnDailyTasksReset += HandleDailyTasksReset;
            dailyTaskManager.OnNewDayStarted += HandleNewDayStarted;
        }
        
        if (taskManager != null)
        {
            taskManager.OnTaskProgressUpdated += HandleTaskProgressUpdate;
            taskManager.OnTaskCompleted += HandleTaskCompleted;
            taskManager.OnTaskRewardClaimed += HandleRewardClaimed;
        }
        
        // Настраиваем кнопку принудительного сброса (только в редакторе)
        if (forceResetButton != null)
        {
            forceResetButton.onClick.AddListener(ForceResetDaily);
            
            #if !UNITY_EDITOR
            forceResetButton.gameObject.SetActive(false);
            #endif
        }
        
        // Устанавливаем начальные значения
        UpdateUI();
        
        // Запускаем обновление таймера
        InvokeRepeating(nameof(UpdateTimer), 0f, 1f);
    }
    
    private void UpdateTimer()
    {
        if (dailyTaskManager == null || resetTimerText == null) return;
        
        string timeUntilReset = dailyTaskManager.GetTimeUntilReset();
        
        // Объединяем текст и время в одном объекте
        resetTimerText.text = $"{timeUntilReset}";
        
        // Проверяем, нужно ли изменить цвет таймера
        UpdateTimerColor();
    }
    
    private void UpdateTimerColor()
    {
        if (resetTimerText == null || dailyTaskManager == null) return;
        
        System.DateTime nextReset = dailyTaskManager.GetNextResetTime();
        System.DateTime currentTime = System.DateTime.Now;
        
        double timeLeft = (nextReset - currentTime).TotalSeconds;
        
        if (timeLeft <= urgentTimeThreshold)
        {
            resetTimerText.color = urgentTimerColor;
            
            // Можем также сделать мигание при критическом времени
            if (timeLeft <= 300f) // Менее 5 минут
            {
                StartBlinkingEffect();
            }
        }
        else
        {
            resetTimerText.color = normalTimerColor;
            StopBlinkingEffect();
        }
    }
    
    private bool isBlinking = false;
    
    private void StartBlinkingEffect()
    {
        if (!isBlinking)
        {
            isBlinking = true;
            InvokeRepeating(nameof(BlinkTimer), 0f, 0.5f);
        }
    }
    
    private void StopBlinkingEffect()
    {
        if (isBlinking)
        {
            isBlinking = false;
            CancelInvoke(nameof(BlinkTimer));
            
            if (resetTimerText != null)
            {
                resetTimerText.color = urgentTimerColor;
            }
        }
    }
    
    private void BlinkTimer()
    {
        if (resetTimerText != null)
        {
            resetTimerText.color = resetTimerText.color == urgentTimerColor ? 
                Color.clear : urgentTimerColor;
        }
    }
    
    private void UpdateUI()
    {
        UpdateDailyProgress();
        UpdateDailyRewards();
    }
    
    private void UpdateDailyProgress()
    {
        if (taskManager == null || dailyProgressText == null) return;
        
        var dailyTasks = taskManager.GetDailyTasks();
        if (dailyTasks.Count == 0) return;
        
        int completedTasks = 0;
        int totalTasks = dailyTasks.Count;
        
        foreach (var task in dailyTasks)
        {
            if (task.isCompleted)
                completedTasks++;
        }
        
        // Обновляем текст прогресса на украинском
        dailyProgressText.text = string.Format(progressTextFormat, completedTasks, totalTasks);
        
        // Обновляем слайдер прогресса
        if (dailyProgressSlider != null)
        {
            dailyProgressSlider.maxValue = totalTasks;
            dailyProgressSlider.value = completedTasks;
        }
    }
    
    private void UpdateDailyRewards()
    {
        if (taskManager == null || dailyRewardsText == null) return;
        
        var dailyTasks = taskManager.GetDailyTasks();
        if (dailyTasks.Count == 0) return;
        
        int totalGems = 0;
        int totalExp = 0;
        int availableRewards = 0;
        
        foreach (var task in dailyTasks)
        {
            if (task.isCompleted && !task.isRewardClaimed)
            {
                totalGems += task.reward.gems;
                totalExp += task.reward.experience;
                availableRewards++;
            }
        }
        
        if (availableRewards > 0)
        {
            dailyRewardsText.text = string.Format(availableRewardsFormat, 
                availableRewards, totalGems, totalExp);
            dailyRewardsText.color = Color.green;
        }
        else
        {
            // Показываем общие награды за все ежедневные таски
            int totalPossibleGems = 0;
            int totalPossibleExp = 0;
            
            foreach (var task in dailyTasks)
            {
                totalPossibleGems += task.reward.gems;
                totalPossibleExp += task.reward.experience;
            }
            
            dailyRewardsText.text = string.Format(totalRewardsFormat, 
                totalPossibleGems, totalPossibleExp);
            dailyRewardsText.color = normalTimerColor;
        }
    }
    
    // Обработчики событий
    private void HandleDailyTasksReset()
    {
        Debug.Log("DailyTaskUI: Ежедневные таски сброшены");
        UpdateUI();
        
        // Можно добавить визуальный эффект или уведомление
        ShowResetNotification();
    }
    
    private void HandleNewDayStarted()
    {
        Debug.Log("DailyTaskUI: Начался новый день");
        UpdateUI();
    }
    
    private void HandleTaskProgressUpdate(TaskData task)
    {
        // Обновляем UI только если это ежедневный таск
        if (IsDaily(task))
        {
            UpdateUI();
        }
    }
    
    private void HandleTaskCompleted(TaskData task)
    {
        if (IsDaily(task))
        {
            UpdateUI();
            ShowTaskCompletedNotification(task);
        }
    }
    
    private void HandleRewardClaimed(TaskData task)
    {
        if (IsDaily(task))
        {
            UpdateUI();
        }
    }
    
    private bool IsDaily(TaskData task)
    {
        return task.taskId >= 101; // Ежедневные таски имеют ID >= 101
    }
    
    private void ShowResetNotification()
    {
        // Можно добавить анимацию или временное уведомление
        Debug.Log("🔄 Щоденні завдання оновлені!");
        
        // Здесь может быть код для показа UI уведомления
        // Например, через NotificationManager или простой текст
    }
    
    private void ShowTaskCompletedNotification(TaskData task)
    {
        Debug.Log($"✅ Щоденне завдання виконано: {task.taskTitle}");
        
        // Здесь может быть код для показа уведомления о выполнении
    }
    
    // Метод для принудительного сброса (только для отладки)
    private void ForceResetDaily()
    {
        #if UNITY_EDITOR
        if (dailyTaskManager != null)
        {
            dailyTaskManager.ForceResetDailyTasks();
            Debug.Log("[DEBUG] Принудительный сброс ежедневных тасков");
        }
        #endif
    }
    
    // Публичные методы для внешнего управления
    public void ShowDailyTaskPanel()
    {
        if (dailyTaskPanel != null)
        {
            dailyTaskPanel.SetActive(true);
        }
    }
    
    public void HideDailyTaskPanel()
    {
        if (dailyTaskPanel != null)
        {
            dailyTaskPanel.SetActive(false);
        }
    }
    
    public void ToggleDailyTaskPanel()
    {
        if (dailyTaskPanel != null)
        {
            dailyTaskPanel.SetActive(!dailyTaskPanel.activeSelf);
        }
    }
    
    // Метод для получения информации о времени до сброса
    public string GetTimeUntilReset()
    {
        return dailyTaskManager?.GetTimeUntilReset() ?? "00:00:00";
    }
    
    // Публичные методы для смены языка
    public void SetLanguage(string language)
    {
        switch (language.ToLower())
        {
            case "ua":
            case "ukrainian":
                SetUkrainianTexts();
                break;
            case "en":
            case "english":
                SetEnglishTexts();
                break;
            case "ru":
            case "russian":
                SetRussianTexts();
                break;
            default:
                SetUkrainianTexts(); // По умолчанию украинский
                break;
        }
        
        // Обновляем UI с новыми текстами
        UpdateUI();
        UpdateTimer();
    }
    
    private void SetUkrainianTexts()
    {
        progressTextFormat = "Виконано: {0}/{1}";
        availableRewardsFormat = "Доступно нагород: {0}\n💎 {1} гемів\n⭐ {2} досвіду";
        totalRewardsFormat = "Всього можна отримати:\n💎 {0} гемів\n⭐ {1} досвіду";
    }
    
    private void SetEnglishTexts()
    {
        progressTextFormat = "Completed: {0}/{1}";
        availableRewardsFormat = "Available rewards: {0}\n💎 {1} gems\n⭐ {2} experience";
        totalRewardsFormat = "Total possible rewards:\n💎 {0} gems\n⭐ {1} experience";
    }
    
    private void SetRussianTexts()
    {
        progressTextFormat = "Выполнено: {0}/{1}";
        availableRewardsFormat = "Доступно наград: {0}\n💎 {1} гемов\n⭐ {2} опыта";
        totalRewardsFormat = "Всего можно получить:\n💎 {0} гемов\n⭐ {1} опыта";
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (dailyTaskManager != null)
        {
            dailyTaskManager.OnDailyTasksReset -= HandleDailyTasksReset;
            dailyTaskManager.OnNewDayStarted -= HandleNewDayStarted;
        }
        
        if (taskManager != null)
        {
            taskManager.OnTaskProgressUpdated -= HandleTaskProgressUpdate;
            taskManager.OnTaskCompleted -= HandleTaskCompleted;
            taskManager.OnTaskRewardClaimed -= HandleRewardClaimed;
        }
        
        // Останавливаем обновление таймера и эффекты
        CancelInvoke(nameof(UpdateTimer));
        StopBlinkingEffect();
    }
}