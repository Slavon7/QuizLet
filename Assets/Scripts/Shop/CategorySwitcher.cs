using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
public class CategoryTab
{
    [Header("Category Info")]
    public string categoryName;
    public Button categoryButton;
    
    [Header("Button Sprites")]
    public Sprite activeSprite;   // Спрайт для активной кнопки
    public Sprite inactiveSprite; // Спрайт для неактивной кнопки
    
    [HideInInspector]
    public float scrollPosition; // Позиция для прокрутки (0-1)
}

public class CategorySwitcher : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("Animation Settings")]
    [SerializeField] private float scrollDuration = 0.8f;
    [SerializeField] private Ease scrollEase = Ease.OutCubic;
    [SerializeField] private float buttonAnimationDuration = 0.3f;
    
    [Header("Button Animation")]
    [SerializeField] private bool useColorAnimation = false; // Отключаем анимацию цвета
    
    [Header("Button Colors (if using color animation)")]
    [SerializeField] private Color activeButtonColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color inactiveButtonColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    
    [Header("Categories")]
    [SerializeField] private List<CategoryTab> categories = new List<CategoryTab>();
    
    [Header("Simple Settings")]
    [SerializeField] private float stepSize = 0.25f; // Шаг между категориями (25%)
    
    // Приватные переменные
    private int currentCategoryIndex = 0;
    private bool isInitialized = false;
    private Tween currentScrollTween; // Ссылка на текущую анимацию прокрутки

    void Start()
    {
        SetupButtons();
        // Устанавливаем позиции сразу при старте для отладки
        SetSimplePositions();
        
        // Устанавливаем первую категорию как активную по умолчанию
        SetActiveCategory(0, false);
    }

    /// <summary>
    /// Инициализация при открытии панели магазина
    /// </summary>
    public void OnShopPanelOpened()
    {
        Debug.Log("Инициализация простого CategorySwitcher...");
        
        SetupButtons();
        SetSimplePositions();
        
        // Устанавливаем первую категорию как активную без анимации
        SetActiveCategory(0, false);
        
        // Устанавливаем позицию скролла на первую категорию
        if (scrollRect != null && categories.Count > 0)
        {
            scrollRect.verticalNormalizedPosition = categories[0].scrollPosition;
            Debug.Log($"Установлена позиция скролла: {categories[0].scrollPosition:F2}");
        }
        
        isInitialized = true;
        
        Debug.Log("Инициализация завершена! Первая категория установлена как активная.");
    }

    void SetupButtons()
    {
        for (int i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            if (category.categoryButton != null)
            {
                int categoryIndex = i; // Захватываем индекс в замыкание
                
                category.categoryButton.onClick.RemoveAllListeners();
                category.categoryButton.onClick.AddListener(() => OnCategoryButtonClicked(categoryIndex));
            }
        }
    }

    void SetSimplePositions()
    {
        Debug.Log("=== УСТАНАВЛИВАЕМ ПРОСТЫЕ ПОЗИЦИИ ===");
        Debug.Log($"Количество категорий: {categories.Count}");
        Debug.Log($"Размер шага: {stepSize}");
        
        for (int i = 0; i < categories.Count; i++)
        {
            // Первая категория = 1.0 (верх)
            // Каждая следующая = предыдущая - stepSize
            float newPosition = 1f - (i * stepSize);
            
            // Ограничиваем снизу нулем
            newPosition = Mathf.Max(0f, newPosition);
            
            categories[i].scrollPosition = newPosition;
            
            Debug.Log($"Категория {i}: '{categories[i].categoryName}' -> {newPosition:F2}");
        }
        
        Debug.Log("=== ПОЗИЦИИ УСТАНОВЛЕНЫ ===");
        
        // Дополнительная проверка
        Debug.Log("=== ПРОВЕРКА УСТАНОВЛЕННЫХ ПОЗИЦИЙ ===");
        for (int i = 0; i < categories.Count; i++)
        {
            Debug.Log($"Проверка {i}: {categories[i].categoryName} = {categories[i].scrollPosition:F2}");
        }
    }

    public void OnCategoryButtonClicked(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count) return;
        
        // Прерываем текущую анимацию прокрутки, если она выполняется
        if (currentScrollTween != null && currentScrollTween.IsActive())
        {
            currentScrollTween.Kill();
            currentScrollTween = null;
            Debug.Log("Прервана предыдущая анимация прокрутки");
        }
        
        // Проверяем что позиции установлены
        if (categories[categoryIndex].scrollPosition == 0f && categoryIndex > 0)
        {
            Debug.LogWarning("Позиции не установлены! Устанавливаем сейчас...");
            SetSimplePositions();
        }
        
        Debug.Log($"Переключаемся на категорию: {categories[categoryIndex].categoryName} (позиция {categories[categoryIndex].scrollPosition:F2})");
        
        SetActiveCategory(categoryIndex, true);
        ScrollToCategory(categoryIndex);
    }

    void SetActiveCategory(int categoryIndex, bool animated = true)
    {
        if (currentCategoryIndex == categoryIndex && animated) return;
        
        // Деактивируем предыдущую кнопку
        if (currentCategoryIndex >= 0 && currentCategoryIndex < categories.Count)
        {
            SetButtonActive(currentCategoryIndex, false, animated);
        }
        
        // Активируем новую кнопку
        currentCategoryIndex = categoryIndex;
        SetButtonActive(currentCategoryIndex, true, animated);
    }

    void SetButtonActive(int categoryIndex, bool isActive, bool animated)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count) return;
        
        var category = categories[categoryIndex];
        var button = category.categoryButton;
        if (button == null) return;
        
        // Меняем спрайт кнопки
        if (button.image != null)
        {
            Sprite targetSprite = isActive ? category.activeSprite : category.inactiveSprite;
            
            if (targetSprite != null)
            {
                button.image.sprite = targetSprite;
                Debug.Log($"Установлен спрайт для '{category.categoryName}': {(isActive ? "активный" : "неактивный")}");
            }
            else
            {
                Debug.LogWarning($"У категории '{category.categoryName}' не назначен {(isActive ? "активный" : "неактивный")} спрайт!");
                
                // Fallback на цвет если спрайты не назначены
                if (useColorAnimation)
                {
                    Color targetColor = isActive ? activeButtonColor : inactiveButtonColor;
                    button.image.color = targetColor;
                }
            }
        }
        
        // Анимация масштаба
        Vector3 targetScale = isActive ? Vector3.one * 1.1f : Vector3.one;
        
        if (animated && Application.isPlaying)
        {
            // Анимируем масштаб кнопки
            button.transform.DOScale(targetScale, buttonAnimationDuration).SetEase(Ease.OutBack);
        }
        else
        {
            // Мгновенно устанавливаем масштаб
            button.transform.localScale = targetScale;
        }
    }

    void ScrollToCategory(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count) return;
        
        float targetScrollPosition = categories[categoryIndex].scrollPosition;
        
        Debug.Log($"Прокручиваем к позиции: {targetScrollPosition:F2}");
        
        // Плавная анимация прокрутки с помощью DOTween
        currentScrollTween = DOTween.To(
            () => scrollRect.verticalNormalizedPosition,
            value => scrollRect.verticalNormalizedPosition = value,
            targetScrollPosition,
            scrollDuration
        )
        .SetEase(scrollEase)
        .OnComplete(() => {
            currentScrollTween = null;
            Debug.Log($"Прокрутка завершена к категории: {categories[categoryIndex].categoryName}");
        });
    }

    #region Public Methods

    /// <summary>
    /// Настроить спрайты для всех кнопок сразу (если они одинаковые)
    /// </summary>
    public void SetGlobalSprites(Sprite activeSprite, Sprite inactiveSprite)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            categories[i].activeSprite = activeSprite;
            categories[i].inactiveSprite = inactiveSprite;
        }
        
        // Обновляем отображение текущей активной кнопки
        if (currentCategoryIndex >= 0 && currentCategoryIndex < categories.Count)
        {
            SetButtonActive(currentCategoryIndex, true, false);
            
            // Обновляем остальные кнопки
            for (int i = 0; i < categories.Count; i++)
            {
                if (i != currentCategoryIndex)
                {
                    SetButtonActive(i, false, false);
                }
            }
        }
        
        Debug.Log("Установлены глобальные спрайты для всех кнопок");
    }

    /// <summary>
    /// Настроить спрайты для конкретной категории
    /// </summary>
    public void SetCategorySprites(int categoryIndex, Sprite activeSprite, Sprite inactiveSprite)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Count) return;
        
        categories[categoryIndex].activeSprite = activeSprite;
        categories[categoryIndex].inactiveSprite = inactiveSprite;
        
        // Обновляем отображение если эта кнопка сейчас активна
        if (categoryIndex == currentCategoryIndex)
        {
            SetButtonActive(categoryIndex, true, false);
        }
        else
        {
            SetButtonActive(categoryIndex, false, false);
        }
        
        Debug.Log($"Установлены спрайты для категории '{categories[categoryIndex].categoryName}'");
    }

    /// <summary>
    /// Переключиться на категорию по имени
    /// </summary>
    public void SwitchToCategory(string categoryName)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i].categoryName == categoryName)
            {
                OnCategoryButtonClicked(i);
                return;
            }
        }
        
        Debug.LogWarning($"Категория '{categoryName}' не найдена!");
    }

    /// <summary>
    /// Переключиться на следующую категорию
    /// </summary>
    public void NextCategory()
    {
        int nextIndex = (currentCategoryIndex + 1) % categories.Count;
        OnCategoryButtonClicked(nextIndex);
    }

    /// <summary>
    /// Переключиться на предыдущую категорию
    /// </summary>
    public void PreviousCategory()
    {
        int prevIndex = (currentCategoryIndex - 1 + categories.Count) % categories.Count;
        OnCategoryButtonClicked(prevIndex);
    }

    /// <summary>
    /// Получить название текущей активной категории
    /// </summary>
    public string GetCurrentCategoryName()
    {
        if (currentCategoryIndex >= 0 && currentCategoryIndex < categories.Count)
        {
            return categories[currentCategoryIndex].categoryName;
        }
        return "";
    }

    /// <summary>
    /// Изменить размер шага между категориями
    /// </summary>
    public void SetStepSize(float newStepSize)
    {
        stepSize = Mathf.Clamp01(newStepSize);
        SetSimplePositions();
        Debug.Log($"Новый размер шага: {stepSize:F2}");
    }

    /// <summary>
    /// Проверить, выполняется ли сейчас анимация прокрутки
    /// </summary>
    public bool IsAnimating()
    {
        return currentScrollTween != null && currentScrollTween.IsActive();
    }

    /// <summary>
    /// Принудительно остановить текущую анимацию прокрутки
    /// </summary>
    public void StopCurrentAnimation()
    {
        if (currentScrollTween != null && currentScrollTween.IsActive())
        {
            currentScrollTween.Kill();
            currentScrollTween = null;
            Debug.Log("Анимация прокрутки принудительно остановлена");
        }
    }

    /// <summary>
    /// Принудительно установить первую категорию как активную (для инициализации)
    /// </summary>
    public void ForceSetFirstCategoryActive()
    {
        if (categories.Count > 0)
        {
            currentCategoryIndex = 0;
            SetButtonActive(0, true, false);
            
            // Устанавливаем все остальные как неактивные
            for (int i = 1; i < categories.Count; i++)
            {
                SetButtonActive(i, false, false);
            }
            
            Debug.Log("Принудительно установлена первая категория как активная");
        }
    }

    #endregion

    #region Debug Methods

    #if UNITY_EDITOR
    [ContextMenu("Setup Simple Positions")]
    void TestSetupPositions()
    {
        SetSimplePositions();
    }
    
    [ContextMenu("Force Setup Everything")]
    void ForceSetupEverything()
    {
        SetupButtons();
        SetSimplePositions();
        SetActiveCategory(0, false);
        Debug.Log("Принудительная настройка завершена!");
    }
    
    [ContextMenu("Force First Category Active")]
    void ForceFirstCategoryActiveTest()
    {
        ForceSetFirstCategoryActive();
    }
    
    [ContextMenu("Test Category 1 (Daily Deals)")]
    void TestCategory1()
    {
        OnCategoryButtonClicked(0);
    }
    
    [ContextMenu("Test Category 2 (Items)")]
    void TestCategory2()
    {
        OnCategoryButtonClicked(1);
    }
    
    [ContextMenu("Test Category 3 (Cases)")]
    void TestCategory3()
    {
        OnCategoryButtonClicked(2);
    }
    
    [ContextMenu("Test Category 4 (Resources)")]
    void TestCategory4()
    {
        OnCategoryButtonClicked(3);
    }
    
    [ContextMenu("Initialize Shop Panel")]
    void TestInitialize()
    {
        OnShopPanelOpened();
    }
    
    [ContextMenu("Set Step 20%")]
    void TestStep20()
    {
        SetStepSize(0.2f);
    }
    
    [ContextMenu("Set Step 30%")]
    void TestStep30()
    {
        SetStepSize(0.3f);
    }
    
    [ContextMenu("Stop Current Animation")]
    void TestStopAnimation()
    {
        StopCurrentAnimation();
    }
    
    [ContextMenu("Print Current State")]
    void PrintCurrentState()
    {
        Debug.Log($"Текущая категория: {GetCurrentCategoryName()} (индекс {currentCategoryIndex})");
        Debug.Log($"Размер шага: {stepSize:F2}");
        Debug.Log($"Инициализирован: {isInitialized}");
        Debug.Log($"Анимация активна: {IsAnimating()}");
        if (scrollRect != null)
        {
            Debug.Log($"Текущая позиция скролла: {scrollRect.verticalNormalizedPosition:F2}");
        }
        
        Debug.Log("=== ВСЕ ПОЗИЦИИ ===");
        for (int i = 0; i < categories.Count; i++)
        {
            string name = categories[i].categoryName ?? "Unnamed";
            Debug.Log($"{i}: {name} -> {categories[i].scrollPosition:F2}");
        }
    }
    
    [ContextMenu("Test Sprite Setup")]
    void TestSpriteSetup()
    {
        Debug.Log("=== ПРОВЕРКА НАСТРОЙКИ СПРАЙТОВ ===");
        for (int i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            string activeSpriteInfo = category.activeSprite != null ? category.activeSprite.name : "НЕ НАЗНАЧЕН";
            string inactiveSpriteInfo = category.inactiveSprite != null ? category.inactiveSprite.name : "НЕ НАЗНАЧЕН";
            
            Debug.Log($"Категория '{category.categoryName}':");
            Debug.Log($"  - Активный спрайт: {activeSpriteInfo}");
            Debug.Log($"  - Неактивный спрайт: {inactiveSpriteInfo}");
            
            // Показываем текущее состояние кнопки
            if (category.categoryButton != null && category.categoryButton.image != null)
            {
                string currentSprite = category.categoryButton.image.sprite != null ? category.categoryButton.image.sprite.name : "НЕТ СПРАЙТА";
                Debug.Log($"  - Текущий спрайт: {currentSprite}");
            }
        }
    }
    
    [ContextMenu("Refresh Button Sprites")]
    void RefreshButtonSprites()
    {
        for (int i = 0; i < categories.Count; i++)
        {
            bool isActive = (i == currentCategoryIndex);
            SetButtonActive(i, isActive, false);
        }
        Debug.Log("Спрайты кнопок обновлены");
    }
    #endif

    #endregion
}