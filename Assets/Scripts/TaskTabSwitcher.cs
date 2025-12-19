using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

[System.Serializable]
public class TaskTabButton
{
    [Header("Button References")]
    public Button button;
    public TMP_Text buttonText;
    public Image buttonBackground;
    public GameObject notificationDot;
    public TMP_Text notificationCount;
    
    [Header("Visual Settings")]
    public Color activeTextColor = Color.white;
    public Color inactiveTextColor = Color.gray;
    public Color activeBackgroundColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color inactiveBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    public void SetActive(bool isActive, bool animate = true)
    {
        if (animate)
        {
            // Анимированное изменение цветов с DoTween
            if (buttonText != null)
            {
                Color targetTextColor = isActive ? activeTextColor : inactiveTextColor;
                buttonText.DOColor(targetTextColor, 0.3f);
            }
                
            if (buttonBackground != null)
            {
                Color targetBgColor = isActive ? activeBackgroundColor : inactiveBackgroundColor;
                buttonBackground.DOColor(targetBgColor, 0.3f);
            }
            
            // Добавляем эффект "пульса" для активной кнопки
            if (isActive && button != null)
            {
                button.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f);
            }
        }
        else
        {
            // Мгновенное изменение цветов
            if (buttonText != null)
                buttonText.color = isActive ? activeTextColor : inactiveTextColor;
                
            if (buttonBackground != null)
                buttonBackground.color = isActive ? activeBackgroundColor : inactiveBackgroundColor;
        }
    }
    
    public void SetNotificationCount(int count, bool animate = true)
    {
        bool shouldShow = count > 0;
        
        if (notificationDot != null)
        {
            if (animate)
            {
                if (shouldShow && !notificationDot.activeSelf)
                {
                    notificationDot.SetActive(true);
                    notificationDot.transform.localScale = Vector3.zero;
                    notificationDot.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
                else if (!shouldShow && notificationDot.activeSelf)
                {
                    notificationDot.transform.DOScale(Vector3.zero, 0.2f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => notificationDot.SetActive(false));
                }
            }
            else
            {
                notificationDot.SetActive(shouldShow);
            }
        }
            
        if (notificationCount != null)
        {
            notificationCount.text = count.ToString();
            
            if (animate)
            {
                if (shouldShow && !notificationCount.gameObject.activeSelf)
                {
                    notificationCount.gameObject.SetActive(true);
                    notificationCount.transform.localScale = Vector3.zero;
                    notificationCount.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
                else if (!shouldShow && notificationCount.gameObject.activeSelf)
                {
                    notificationCount.transform.DOScale(Vector3.zero, 0.2f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => notificationCount.gameObject.SetActive(false));
                }
                else if (shouldShow && notificationCount.gameObject.activeSelf)
                {
                    // Анимация изменения числа
                    notificationCount.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 5, 0.5f);
                }
            }
            else
            {
                notificationCount.gameObject.SetActive(shouldShow);
            }
        }
    }
}

public class TaskTabSwitcher : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private TaskTabButton dailyTaskTab;
    [SerializeField] private TaskTabButton achievementTab;

    [Header("Content Panels")]
    [SerializeField] private GameObject dailyTaskPanel;
    [SerializeField] private GameObject achievementPanel;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Tab Indicator")]
    [SerializeField] private RectTransform tabIndicator;
    [SerializeField] private bool moveIndicator = true;

    private TaskViewMode currentMode = TaskViewMode.DailyTasks;
    private bool isAnimating = false;

    // События
    public System.Action<TaskViewMode> OnTabSwitched;

    private void Start()
    {
        InitializeTabs();
        SetActiveTab(currentMode, false);
        UpdateNotificationCounts();
    }

    private void InitializeTabs()
    {
        // Настраиваем кнопки
        if (dailyTaskTab.button != null)
        {
            dailyTaskTab.button.onClick.AddListener(() => SwitchTab(TaskViewMode.DailyTasks));
        }

        if (achievementTab.button != null)
        {
            achievementTab.button.onClick.AddListener(() => SwitchTab(TaskViewMode.Achievements));
        }

        // Подписываемся на события TaskManager'а
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskCompleted += OnTaskStatusChanged;
            TaskManager.Instance.OnTaskRewardClaimed += OnTaskStatusChanged;
            TaskManager.Instance.OnAchievementUnlocked += OnAchievementStatusChanged;
        }
    }

    public void SwitchTab(TaskViewMode mode)
    {
        if (isAnimating || currentMode == mode) return;

        isAnimating = true;
        TaskViewMode previousMode = currentMode;
        currentMode = mode;

        SetActiveTab(mode, useAnimation);

        OnTabSwitched?.Invoke(mode);

        // Воспроизводим звук переключения
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ui_tab_switch");
        }

        // Если анимация отключена, сразу снимаем флаг
        if (!useAnimation)
        {
            isAnimating = false;
        }
    }

    private void SetActiveTab(TaskViewMode mode, bool animate = true)
    {
        // Обновляем состояние кнопок с анимацией
        dailyTaskTab.SetActive(mode == TaskViewMode.DailyTasks, animate);
        achievementTab.SetActive(mode == TaskViewMode.Achievements, animate);

        // Переключаем панели с анимацией
        if (animate && useAnimation)
        {
            AnimatePanelSwitch(mode);
        }
        else
        {
            // Мгновенное переключение
            if (dailyTaskPanel != null)
                dailyTaskPanel.SetActive(mode == TaskViewMode.DailyTasks);

            if (achievementPanel != null)
                achievementPanel.SetActive(mode == TaskViewMode.Achievements);
                
            // Снимаем флаг анимации для мгновенного переключения
            isAnimating = false;
        }

        // Перемещаем индикатор
        if (moveIndicator && tabIndicator != null)
        {
            Vector3 targetPosition = GetTabIndicatorPosition(mode);

            if (animate && useAnimation)
            {
                tabIndicator.DOAnchorPos(targetPosition, animationDuration).SetEase(Ease.OutQuart);
            }
            else
            {
                tabIndicator.anchoredPosition = targetPosition;
            }
        }
    }

    private void AnimatePanelSwitch(TaskViewMode mode)
    {
        GameObject fromPanel = currentMode == TaskViewMode.DailyTasks ? dailyTaskPanel : achievementPanel;
        GameObject toPanel = mode == TaskViewMode.DailyTasks ? dailyTaskPanel : achievementPanel;

        // Последовательность анимации
        Sequence switchSequence = DOTween.Sequence();

        // Скрываем старую панель
        if (fromPanel != null && fromPanel != toPanel)
        {
            switchSequence.Append(fromPanel.transform.DOScale(Vector3.zero, animationDuration * 0.4f)
                .SetEase(Ease.InBack)
                .OnComplete(() => fromPanel.SetActive(false)));
        }

        // Показываем новую панель
        if (toPanel != null && toPanel != fromPanel)
        {
            switchSequence.AppendCallback(() =>
            {
                toPanel.SetActive(true);
                toPanel.transform.localScale = Vector3.zero;
            });

            switchSequence.Append(toPanel.transform.DOScale(Vector3.one, animationDuration * 0.6f)
                .SetEase(Ease.OutBack));
        }

        switchSequence.OnComplete(() => isAnimating = false);
    }

    private Vector3 GetTabIndicatorPosition(TaskViewMode mode)
    {
        RectTransform targetButton = mode == TaskViewMode.DailyTasks ?
            dailyTaskTab.button?.GetComponent<RectTransform>() :
            achievementTab.button?.GetComponent<RectTransform>();

        if (targetButton != null)
        {
            return targetButton.anchoredPosition;
        }

        return tabIndicator.anchoredPosition;
    }

    private void UpdateNotificationCounts()
    {
        if (TaskManager.Instance == null) return;
        
        // Считаем завершенные daily tasks
        var activeTasks = TaskManager.Instance.GetActiveTasks();
        int completedDailyTasks = 0;
        foreach (var task in activeTasks)
        {
            if (task.isCompleted && !task.isRewardClaimed)
                completedDailyTasks++;
        }
        
        // Считаем завершенные achievements
        var activeAchievements = TaskManager.Instance.GetActiveAchievements();
        int completedAchievements = 0;
        foreach (var achievement in activeAchievements)
        {
            if (achievement.isCompleted && !achievement.isRewardClaimed)
                completedAchievements++;
        }
        
        // Обновляем уведомления
        dailyTaskTab.SetNotificationCount(completedDailyTasks, false);
        achievementTab.SetNotificationCount(completedAchievements, false);
    }
    
    // Обработчики событий
    private void OnTaskStatusChanged(TaskData task)
    {
        UpdateNotificationCounts();
    }
    
    private void OnAchievementStatusChanged(TaskData achievement)
    {
        UpdateNotificationCounts();
    }
    
    // Публичные методы
    public TaskViewMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public void SetMode(TaskViewMode mode)
    {
        SwitchTab(mode);
    }
    
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    // Методы для внешнего управления уведомлениями
    public void SetDailyTaskNotificationCount(int count)
    {
        dailyTaskTab.SetNotificationCount(count, true);
    }
    
    public void SetAchievementNotificationCount(int count)
    {
        achievementTab.SetNotificationCount(count, true);
    }
    
    // Добавляем эффект встряски для привлечения внимания
    public void ShakeNotification(TaskViewMode mode)
    {
        TaskTabButton targetTab = mode == TaskViewMode.DailyTasks ? dailyTaskTab : achievementTab;
        
        if (targetTab.notificationDot != null)
        {
            targetTab.notificationDot.transform.DOShakePosition(0.5f, 10f, 20, 90, false, true);
        }
    }
    
    // Метод для анимации появления новых уведомлений
    public void AnimateNewNotification(TaskViewMode mode, int newCount)
    {
        TaskTabButton targetTab = mode == TaskViewMode.DailyTasks ? dailyTaskTab : achievementTab;
        
        // Обновляем счетчик с анимацией
        targetTab.SetNotificationCount(newCount, true);
        
        // Добавляем дополнительный эффект "внимания"
        if (targetTab.button != null)
        {
            targetTab.button.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 8, 0.6f);
        }
        
        // Эффект свечения (если есть outline или shadow)
        if (targetTab.buttonBackground != null)
        {
            var originalColor = targetTab.buttonBackground.color;
            targetTab.buttonBackground.DOColor(Color.yellow, 0.1f)
                .OnComplete(() => targetTab.buttonBackground.DOColor(originalColor, 0.3f));
        }
    }
    
    private void OnDestroy()
    {
        // Останавливаем все DoTween анимации для этого объекта
        transform.DOKill();
        
        // Отписываемся от событий
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskCompleted -= OnTaskStatusChanged;
            TaskManager.Instance.OnTaskRewardClaimed -= OnTaskStatusChanged;
            TaskManager.Instance.OnAchievementUnlocked -= OnAchievementStatusChanged;
        }
    }
}