using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PanelSlider : MonoBehaviour
{
    [Header("Панелі меню")]
    public RectTransform[] panels;

    [Header("Кнопки меню зі спрайтами")]
    public MenuButtonData[] menuButtons;

    [Header("Параметри анімації")]
    public float slideDuration = 0.4f;
    public Ease slideEase = Ease.InOutCubic;
    
    [Header("Налаштування відстані анімації")]
    public bool useCustomSlideDistance = false;
    public float customSlideDistance = 1920f; // Кастомна відстань для анімації
    public bool useParentWidth = true; // Використовувати ширину батьківського контейнера

    [Header("Параметри кнопок")]
    public float buttonScaleAnimation = 1.1f;
    public float buttonAnimationDuration = 0.2f;

    private RectTransform currentPanel;
    private bool isAnimating = false;
    private int currentIndex = 0;

    private void Start()
    {
        // Ініціалізація панелей
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].gameObject.SetActive(i == 0);
        }

        currentPanel = panels[0];
        
        // Додаємо слухачів для кнопок
        SetupButtonListeners();
        
        // Оновлюємо спрайти на початку
        UpdateButtonSprites(0);
    }

    private void SetupButtonListeners()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i].button != null)
            {
                int buttonIndex = i; // Локальна копія для замикання
                menuButtons[i].button.onClick.AddListener(() => ShowPanel(buttonIndex));
            }
        }
    }

    public void ShowPanel(int targetIndex)
    {
        if (isAnimating || targetIndex < 0 || targetIndex >= panels.Length || targetIndex == currentIndex)
            return;

        RectTransform nextPanel = panels[targetIndex];
        nextPanel.gameObject.SetActive(true);

        int direction = currentIndex < targetIndex ? 1 : -1;
        
        // Вычисляем расстояние для анимации
        float slideWidth = GetSlideDistance();
        Vector2 slideDistance = new Vector2(slideWidth, 0);
        
        nextPanel.anchoredPosition = direction * slideDistance;

        isAnimating = true;

        // Анімація кнопки
        AnimateButton(targetIndex);

        // Анімація панелей
        currentPanel.DOAnchorPos(-direction * slideDistance, slideDuration).SetEase(slideEase);
        nextPanel.DOAnchorPos(Vector2.zero, slideDuration).SetEase(slideEase)
            .OnComplete(() =>
            {
                currentPanel.gameObject.SetActive(false);
                currentPanel = nextPanel;
                currentIndex = targetIndex;
                isAnimating = false;
            });

        // Оновлюємо спрайти одразу при початку анімації
        UpdateButtonSprites(targetIndex);
    }

    private float GetSlideDistance()
    {
        // Если установлена кастомная дистанция, используем её
        if (useCustomSlideDistance)
        {
            return customSlideDistance;
        }

        // Если нужно использовать ширину родительского контейнера
        if (useParentWidth && currentPanel != null)
        {
            RectTransform parentRect = currentPanel.parent as RectTransform;
            if (parentRect != null)
            {
                return parentRect.rect.width;
            }
        }

        // По умолчанию используем ширину панели
        if (currentPanel != null)
        {
            return currentPanel.rect.width;
        }

        // Fallback - используем размер экрана
        return Screen.width;
    }

    private void UpdateButtonSprites(int activeIndex)
    {
        // Спочатку повертаємо всі кнопки до нормального стану
        for (int i = 0; i < menuButtons.Length; i++)
        {
            SetButtonSprite(i, false);
        }

        // Потім встановлюємо активний спрайт тільки для поточної кнопки
        if (activeIndex >= 0 && activeIndex < menuButtons.Length)
        {
            SetButtonSprite(activeIndex, true);
        }
    }

    private void SetButtonSprite(int buttonIndex, bool isActive)
    {
        if (buttonIndex < 0 || buttonIndex >= menuButtons.Length || menuButtons[buttonIndex].button == null)
            return;

        // Знаходимо іконку кнопки
        Transform iconTransform = menuButtons[buttonIndex].button.transform.Find("Icon");
        if (iconTransform == null)
        {
            // Якщо іконка не знайдена, шукаємо Image компонент на самій кнопці
            iconTransform = menuButtons[buttonIndex].button.transform;
        }

        Image iconImage = iconTransform.GetComponent<Image>();
        if (iconImage != null)
        {
            // Встановлюємо правильний спрайт
            Sprite targetSprite = isActive ? menuButtons[buttonIndex].activeSprite : menuButtons[buttonIndex].normalSprite;
            
            if (targetSprite != null)
            {
                iconImage.sprite = targetSprite;
            }
            else
            {
                Debug.LogWarning($"Відсутній {(isActive ? "активний" : "нормальний")} спрайт для кнопки {buttonIndex}");
            }
        }
        else
        {
            Debug.LogWarning($"Не знайдено Image компонент для кнопки {buttonIndex}");
        }
    }

    private void AnimateButton(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= menuButtons.Length || menuButtons[buttonIndex].button == null)
            return;

        Transform buttonTransform = menuButtons[buttonIndex].button.transform;
        
        // Анімація натискання кнопки
        buttonTransform.DOScale(buttonScaleAnimation, buttonAnimationDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                buttonTransform.DOScale(1f, buttonAnimationDuration * 0.5f)
                    .SetEase(Ease.InQuad);
            });
    }

    // Метод для програмного переходу до конкретної панелі
    public void GoToPanel(int panelIndex)
    {
        ShowPanel(panelIndex);
    }

    // Метод для отримання поточного індексу
    public int GetCurrentPanelIndex()
    {
        return currentIndex;
    }

    // Метод для перевірки, чи відбувається анімація
    public bool IsAnimating()
    {
        return isAnimating;
    }

    private void OnValidate()
    {
        // Перевіряємо відповідність кількості панелей та кнопок
        if (panels != null && menuButtons != null && panels.Length != menuButtons.Length)
        {
            Debug.LogWarning("Кількість панелей не співпадає з кількістю кнопок меню!");
        }
    }
}

[System.Serializable]
public class MenuButtonData
{
    [Header("Основні компоненти")]
    public Button button;
    
    [Header("Спрайти для станів")]
    public Sprite normalSprite;
    public Sprite activeSprite;
    
    [Header("Додаткові налаштування (опціонально)")]
    public Color normalColor = Color.white;
    public Color activeColor = Color.white;
}