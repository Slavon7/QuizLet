using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AchievementCanvasManager : MonoBehaviour
{
    [Header("Display Mode")]
    [SerializeField] private TaskDisplayMode displayMode = TaskDisplayMode.PopupMode;
    
    [Header("Achievement UI References")]
    [SerializeField] private GameObject achievementCanvas;
    [SerializeField] private Transform achievementContainer;
    [SerializeField] private GameObject achievementUIPrefab;
    
    [Header("Tab System")]
    [SerializeField] private Button achievementTabButton;
    [SerializeField] private Button tasksTabButton;
    [SerializeField] private GameObject tasksPanel; // Ссылка на панель обычных тасков
    
    [Header("Filter Buttons")]
    [SerializeField] private Button showAllButton;
    [SerializeField] private Button showUnlockedButton;
    [SerializeField] private Button showLockedButton;
    [SerializeField] private Button showClaimedButton;
    
    [Header("Statistics UI")]
    [SerializeField] private TMP_Text totalAchievementsText;
    [SerializeField] private TMP_Text unlockedAchievementsText;
    [SerializeField] private TMP_Text progressPercentageText;
    [SerializeField] private Slider overallProgressSlider;
    
    [Header("Popup Mode Settings")]
    [SerializeField] private Button openAchievementButton;
    [SerializeField] private Button closeAchievementButton;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Notification Settings")]
    [SerializeField] private GameObject achievementNotificationIcon;
    [SerializeField] private TMP_Text achievementNotificationText;
    
    [Header("Category System")]
    [SerializeField] private Transform categoryContainer;
    [SerializeField] private GameObject categoryButtonPrefab;
    
    // Private variables
    private List<TaskUI> achievementUIElements = new List<TaskUI>();
    private List<Button> categoryButtons = new List<Button>();
    private AchievementFilter currentFilter = AchievementFilter.All;
    private string currentCategory = "All";
    private bool isAchievementCanvasOpen = false;
    private int newAchievementsCount = 0;
    
    public enum AchievementFilter
    {
        All,
        Unlocked,
        Locked,
        Claimed
    }
    
    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
        UpdateAchievementStatistics();
        CountNewAchievements();
        UpdateNotificationIcon();
        CreateCategoryButtons();
    }
    
    #region Initialization
    
    private void InitializeUI()
    {
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
        
        SetupTabButtons();
        SetupFilterButtons();
        
        // Устанавливаем ссылки в TaskManager для достижений
        if (displayMode != TaskDisplayMode.DisabledInScene && 
            TaskManager.Instance != null && achievementUIPrefab != null && achievementContainer != null)
        {
            // Создаем отдельный метод для установки ссылок на достижения
            TaskManager.Instance.SetAchievementUIReferences(achievementContainer, achievementUIPrefab);
            Debug.Log($"AchievementCanvasManager: UI ссылки для достижений переданы в TaskManager (режим: {displayMode})");
        }
    }
    
    private void SetupAlwaysVisibleMode()
    {
        if (achievementCanvas != null)
        {
            achievementCanvas.SetActive(true);
            isAchievementCanvasOpen = true;
        }
        
        if (openAchievementButton != null)
            openAchievementButton.gameObject.SetActive(false);
        if (closeAchievementButton != null)
            closeAchievementButton.gameObject.SetActive(false);
            
        Debug.Log("AchievementCanvasManager: Настроен режим 'Всегда видимый'");
    }
    
    private void SetupPopupMode()
    {
        if (openAchievementButton != null)
        {
            openAchievementButton.onClick.AddListener(OpenAchievementCanvas);
            openAchievementButton.gameObject.SetActive(true);
        }
        
        if (closeAchievementButton != null)
        {
            closeAchievementButton.onClick.AddListener(CloseAchievementCanvas);
        }
        
        if (achievementCanvas != null)
        {
            achievementCanvas.SetActive(false);
            isAchievementCanvasOpen = false;
        }
        
        Debug.Log("AchievementCanvasManager: Настроен режим 'Всплывающий'");
    }
    
    private void SetupDisabledMode()
    {
        if (achievementCanvas != null)
            achievementCanvas.SetActive(false);
            
        if (openAchievementButton != null)
            openAchievementButton.gameObject.SetActive(false);
            
        if (achievementNotificationIcon != null)
            achievementNotificationIcon.SetActive(false);
            
        Debug.Log("AchievementCanvasManager: Система достижений отключена в этой сцене");
    }
    
    private void SetupTabButtons()
    {
        if (achievementTabButton != null)
            achievementTabButton.onClick.AddListener(ShowAchievementPanel);
            
        if (tasksTabButton != null)
            tasksTabButton.onClick.AddListener(ShowTasksPanel);
    }
    
    private void SetupFilterButtons()
    {
        if (showAllButton != null)
            showAllButton.onClick.AddListener(() => SetFilter(AchievementFilter.All));
            
        if (showUnlockedButton != null)
            showUnlockedButton.onClick.AddListener(() => SetFilter(AchievementFilter.Unlocked));
            
        if (showLockedButton != null)
            showLockedButton.onClick.AddListener(() => SetFilter(AchievementFilter.Locked));
            
        if (showClaimedButton != null)
            showClaimedButton.onClick.AddListener(() => SetFilter(AchievementFilter.Claimed));
    }
    
    #endregion
    
    #region Event Subscription
    
    private void SubscribeToEvents()
    {
        if (displayMode != TaskDisplayMode.DisabledInScene && TaskManager.Instance != null)
        {
            TaskManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
            TaskManager.Instance.OnTaskRewardClaimed += OnAchievementRewardClaimed;
        }
    }
    
    private void OnAchievementUnlocked(TaskData achievement)
    {
        newAchievementsCount++;
        UpdateNotificationIcon();
        ShowAchievementUnlockedNotification(achievement);
        
        if (isAchievementCanvasOpen)
        {
            UpdateAchievementUI();
            UpdateAchievementStatistics();
        }
        
        // Воспроизводим звук и эффекты
        PlayAchievementUnlockedEffects(achievement);
    }
    
    private void OnAchievementRewardClaimed(TaskData task)
    {
        // Проверяем, что это достижение
        if (IsAchievement(task))
        {
            newAchievementsCount = Mathf.Max(0, newAchievementsCount - 1);
            UpdateNotificationIcon();
            UpdateAchievementStatistics();
            
            if (isAchievementCanvasOpen)
            {
                UpdateAchievementUI();
            }
        }
    }
    
    private bool IsAchievement(TaskData task)
    {
        return task.taskId >= 1001 && task.taskId <= 9999;
    }
    
    #endregion
    
    #region Canvas Management
    
    public void OpenAchievementCanvas()
    {
        if (displayMode != TaskDisplayMode.PopupMode || isAchievementCanvasOpen || achievementCanvas == null)
            return;
            
        isAchievementCanvasOpen = true;
        achievementCanvas.SetActive(true);
        
        // Обновляем UI при открытии
        UpdateAchievementUI();
        UpdateAchievementStatistics();
        
        StartCoroutine(AnimateCanvasScale(Vector3.zero, Vector3.one));
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ui_open");
        }
    }
    
    public void CloseAchievementCanvas()
    {
        if (displayMode != TaskDisplayMode.PopupMode || !isAchievementCanvasOpen || achievementCanvas == null)
            return;
            
        StartCoroutine(AnimateCanvasScale(Vector3.one, Vector3.zero, () =>
        {
            achievementCanvas.SetActive(false);
            isAchievementCanvasOpen = false;
        }));
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ui_close");
        }
    }
    
    private System.Collections.IEnumerator AnimateCanvasScale(Vector3 startScale, Vector3 endScale, System.Action onComplete = null)
    {
        Transform canvasTransform = achievementCanvas.transform;
        canvasTransform.localScale = startScale;
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = animationCurve.Evaluate(t);
            
            canvasTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        
        canvasTransform.localScale = endScale;
        onComplete?.Invoke();
    }
    
    #endregion
    
    #region Tab System
    
    public void ShowAchievementPanel()
    {
        if (tasksPanel != null)
        {
            StartCoroutine(SwitchPanels(tasksPanel, achievementCanvas));
        }
        
        UpdateTabButtonStates(true);
        UpdateAchievementUI();
        UpdateAchievementStatistics();
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ui_tab_switch");
        }
    }
    
    public void ShowTasksPanel()
    {
        if (achievementCanvas != null && tasksPanel != null)
        {
            StartCoroutine(SwitchPanels(achievementCanvas, tasksPanel));
        }
        
        UpdateTabButtonStates(false);
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ui_tab_switch");
        }
    }
    
    private System.Collections.IEnumerator SwitchPanels(GameObject hidePanel, GameObject showPanel)
    {
        // Скрываем текущую панель
        if (hidePanel != null)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one;
            
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                t = animationCurve.Evaluate(t);
                
                hidePanel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
            
            hidePanel.SetActive(false);
        }
        
        // Показываем новую панель
        if (showPanel != null)
        {
            showPanel.SetActive(true);
            showPanel.transform.localScale = Vector3.zero;
            
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;
            
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (animationDuration * 0.5f);
                t = animationCurve.Evaluate(t);
                
                showPanel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }
            
            showPanel.transform.localScale = targetScale;
        }
    }
    
    private void UpdateTabButtonStates(bool isAchievementTabActive)
    {
        if (achievementTabButton != null)
        {
            var buttonImage = achievementTabButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isAchievementTabActive ? Color.white : Color.gray;
            }
        }
        
        if (tasksTabButton != null)
        {
            var buttonImage = tasksTabButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = !isAchievementTabActive ? Color.white : Color.gray;
            }
        }
    }
    
    #endregion
    
    #region Achievement UI Management
    
    private void UpdateAchievementUI()
    {
        if (TaskManager.Instance == null || achievementContainer == null || achievementUIPrefab == null)
            return;
            
        ClearAchievementUI();
        
        var achievements = GetFilteredAchievements();
        
        foreach (var achievement in achievements)
        {
            CreateAchievementUIElement(achievement);
        }
        
        Debug.Log($"Показано {achievementUIElements.Count} достижений (фильтр: {currentFilter}, категория: {currentCategory})");
    }
    
    private List<TaskData> GetFilteredAchievements()
    {
        var allAchievements = TaskManager.Instance.GetAchievements();
        
        // Фильтрация по категории
        if (currentCategory != "All")
        {
            var categoryAchievements = TaskManager.Instance.GetAchievementsByCategory(currentCategory);
            var categoryIds = categoryAchievements.Select(a => a.achievementId).ToHashSet();
            allAchievements = allAchievements.Where(a => categoryIds.Contains(a.taskId)).ToList();
        }
        
        // Фильтрация по статусу
        return currentFilter switch
        {
            AchievementFilter.Unlocked => allAchievements.Where(a => a.isCompleted && !a.isRewardClaimed).ToList(),
            AchievementFilter.Locked => allAchievements.Where(a => !a.isCompleted).ToList(),
            AchievementFilter.Claimed => allAchievements.Where(a => a.isRewardClaimed).ToList(),
            _ => allAchievements.ToList()
        };
    }
    
    private void CreateAchievementUIElement(TaskData achievement)
    {
        GameObject achievementUIObj = Instantiate(achievementUIPrefab, achievementContainer);
        TaskUI achievementUI = achievementUIObj.GetComponent<TaskUI>();
        
        if (achievementUI != null)
        {
            achievementUI.Initialize(achievement);
            achievementUI.OnRewardClaimed += HandleAchievementRewardClaimed;
            achievementUIElements.Add(achievementUI);
        }
    }
    
    private void ClearAchievementUI()
    {
        foreach (var achievementUI in achievementUIElements)
        {
            if (achievementUI != null)
                Destroy(achievementUI.gameObject);
        }
        achievementUIElements.Clear();
    }
    
    private void HandleAchievementRewardClaimed(TaskData achievement)
    {
        // Переадресуем в TaskManager
        if (TaskManager.Instance != null)
        {
            // TaskManager обработает это через свою систему
        }
    }
    
    #endregion
    
    #region Filter System
    
    public void SetFilter(AchievementFilter filter)
    {
        if (currentFilter == filter) return;
        
        currentFilter = filter;
        UpdateFilterButtonStates();
        
        if (isAchievementCanvasOpen || displayMode == TaskDisplayMode.AlwaysVisible)
        {
            UpdateAchievementUI();
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ui_filter_click");
        }
    }
    
    public void SetCategory(string categoryName)
    {
        if (currentCategory == categoryName) return;
        
        currentCategory = categoryName;
        UpdateCategoryButtonStates();
        
        if (isAchievementCanvasOpen || displayMode == TaskDisplayMode.AlwaysVisible)
        {
            UpdateAchievementUI();
        }
    }
    
    private void UpdateFilterButtonStates()
    {
        UpdateFilterButton(showAllButton, currentFilter == AchievementFilter.All);
        UpdateFilterButton(showUnlockedButton, currentFilter == AchievementFilter.Unlocked);
        UpdateFilterButton(showLockedButton, currentFilter == AchievementFilter.Locked);
        UpdateFilterButton(showClaimedButton, currentFilter == AchievementFilter.Claimed);
    }
    
    private void UpdateFilterButton(Button button, bool isActive)
    {
        if (button == null) return;
        
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isActive ? Color.yellow : Color.white;
        }
        
        var buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.color = isActive ? Color.black : Color.white;
        }
    }
    
    #endregion
    
    #region Category System
    
    private void CreateCategoryButtons()
    {
        if (TaskManager.Instance == null || categoryContainer == null || categoryButtonPrefab == null)
            return;
            
        ClearCategoryButtons();
        
        // Создаем кнопку "Все"
        CreateCategoryButton("All", "Все категории");
        
        // Создаем кнопки для каждой категории
        if (TaskManager.Instance.GetAchievementsByCategory("") != null)
        {
            // Здесь можно добавить логику для получения списка категорий
            // Пока создаем базовые категории
            CreateCategoryButton("Gameplay", "Игровой процесс");
            CreateCategoryButton("Progress", "Прогресс");
            CreateCategoryButton("Special", "Особые");
        }
    }
    
    private void CreateCategoryButton(string categoryName, string displayName)
    {
        GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryContainer);
        Button button = buttonObj.GetComponent<Button>();
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        
        if (button != null && buttonText != null)
        {
            buttonText.text = displayName;
            button.onClick.AddListener(() => SetCategory(categoryName));
            categoryButtons.Add(button);
        }
    }
    
    private void ClearCategoryButtons()
    {
        foreach (var button in categoryButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        categoryButtons.Clear();
    }
    
    private void UpdateCategoryButtonStates()
    {
        foreach (var button in categoryButtons)
        {
            if (button != null)
            {
                var buttonImage = button.GetComponent<Image>();
                var buttonText = button.GetComponentInChildren<TMP_Text>();
                
                // Определяем активность кнопки (это можно улучшить)
                bool isActive = false; // Здесь нужна логика определения активной категории
                
                if (buttonImage != null)
                    buttonImage.color = isActive ? Color.cyan : Color.white;
                if (buttonText != null)
                    buttonText.color = isActive ? Color.black : Color.white;
            }
        }
    }
    
    #endregion
    
    #region Statistics
    
    private void UpdateAchievementStatistics()
    {
        if (TaskManager.Instance == null) return;
        
        var stats = TaskManager.Instance.GetAchievementStatistics();
        
        if (totalAchievementsText != null)
            totalAchievementsText.text = stats.totalAchievements.ToString();
            
        if (unlockedAchievementsText != null)
            unlockedAchievementsText.text = stats.unlockedAchievements.ToString();
            
        if (progressPercentageText != null)
            progressPercentageText.text = $"{stats.unlockPercentage:F1}%";
            
        if (overallProgressSlider != null)
        {
            overallProgressSlider.maxValue = stats.totalAchievements;
            overallProgressSlider.value = stats.unlockedAchievements;
        }
    }
    
    #endregion
    
    #region Notifications
    
    private void CountNewAchievements()
    {
        newAchievementsCount = 0;
        
        if (displayMode != TaskDisplayMode.DisabledInScene && TaskManager.Instance != null)
        {
            var achievements = TaskManager.Instance.GetAchievements();
            newAchievementsCount = achievements.Count(a => a.isCompleted && !a.isRewardClaimed);
        }
        
        UpdateNotificationIcon();
    }
    
    private void UpdateNotificationIcon()
    {
        if (achievementNotificationIcon != null)
        {
            achievementNotificationIcon.SetActive(newAchievementsCount > 0);
        }
        
        if (achievementNotificationText != null)
        {
            achievementNotificationText.text = newAchievementsCount.ToString();
        }
        
        // Анимация пульсации
        if (newAchievementsCount > 0 && achievementNotificationIcon != null)
        {
            StartCoroutine(PulseNotification());
        }
    }
    
    private System.Collections.IEnumerator PulseNotification()
    {
        if (achievementNotificationIcon == null) yield break;
        
        Vector3 originalScale = achievementNotificationIcon.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        while (achievementNotificationIcon.activeInHierarchy && newAchievementsCount > 0)
        {
            // Увеличиваем
            float elapsed = 0f;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                achievementNotificationIcon.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // Уменьшаем
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                achievementNotificationIcon.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        achievementNotificationIcon.transform.localScale = originalScale;
    }
    
    private void ShowAchievementUnlockedNotification(TaskData achievement)
    {
        Debug.Log($"🏆 Достижение разблокировано: {achievement.taskTitle}!");
        Debug.Log($"📜 {achievement.taskDescription}");
        
        // В popup режиме показываем уведомление
        if (displayMode == TaskDisplayMode.PopupMode)
        {
            Debug.Log("Откройте панель достижений, чтобы получить награду!");
        }
    }
    
    private void PlayAchievementUnlockedEffects(TaskData achievement)
    {
        if (AudioManager.Instance != null)
        {
            // Получаем редкость достижения для разных звуков
            var achievementData = TaskManager.Instance.GetAchievementScriptableById(achievement.taskId);
            string soundName = "achievement_unlocked";
            
            if (achievementData != null)
            {
                soundName = achievementData.rarity switch
                {
                    AchievementRarity.Legendary => "achievement_legendary",
                    AchievementRarity.Epic => "achievement_epic",
                    AchievementRarity.Rare => "achievement_rare",
                    _ => "achievement_unlocked"
                };
            }
            
            AudioManager.Instance.PlaySFX(soundName);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void RefreshUI()
    {
        if (isAchievementCanvasOpen || displayMode == TaskDisplayMode.AlwaysVisible)
        {
            UpdateAchievementUI();
            UpdateAchievementStatistics();
        }
    }
    
    public bool IsShowingAchievements()
    {
        return isAchievementCanvasOpen || displayMode == TaskDisplayMode.AlwaysVisible;
    }
    
    public void ShowAchievementByID(int achievementID)
    {
        OpenAchievementCanvas();
        // Здесь можно добавить логику прокрутки к конкретному достижению
    }
    
    // Методы для получения ссылок (аналогично TaskCanvasManager)
    public Transform GetAchievementContainer()
    {
        return achievementContainer;
    }
    
    public GameObject GetAchievementUIPrefab()
    {
        return achievementUIPrefab;
    }
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
            TaskManager.Instance.OnTaskRewardClaimed -= OnAchievementRewardClaimed;
        }
        
        // Отписываемся от кнопок
        if (achievementTabButton != null)
            achievementTabButton.onClick.RemoveAllListeners();
            
        if (tasksTabButton != null)
            tasksTabButton.onClick.RemoveAllListeners();
            
        if (openAchievementButton != null)
            openAchievementButton.onClick.RemoveAllListeners();
            
        if (closeAchievementButton != null)
            closeAchievementButton.onClick.RemoveAllListeners();
            
        // Очищаем кнопки фильтров и категорий
        ClearCategoryButtons();
    }
    
    #endregion
}