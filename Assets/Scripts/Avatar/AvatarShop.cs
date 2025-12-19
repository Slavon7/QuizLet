using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class AvatarShop : MonoBehaviour
{
    [Header("Shop UI")]
    public Transform shopContent;
    public GameObject avatarShopItemPrefab;
    public GameObject dailyGiftItemPrefab; // Префаб для ежедневного подарка
    
    [Header("Purchase Confirmation")]
    [Tooltip("Префаб окна подтверждения покупки")]
    public GameObject purchaseConfirmationPrefab;
    [Tooltip("Родительский объект для окна подтверждения (обычно Canvas)")]
    public Transform confirmationParent;
    [Tooltip("Показывать подтверждение для бесплатных покупок")]
    public bool showConfirmationForFree = false;
    [Tooltip("Показывать подтверждение для ежедневных подарков")]
    public bool showConfirmationForDailyGift = false;
    
    [Header("Audio Settings")]
    [Tooltip("Название UI звука для успешной покупки")]
    public string purchaseSuccessSound = "purchase_success";
    [Tooltip("Название UI звука для получения ежедневного подарка")]
    public string dailyGiftClaimSound = "daily_gift_claim";
    [Tooltip("Название UI звука для ошибки (недостаточно средств)")]
    public string purchaseErrorSound = "purchase_error";
    [Tooltip("Название UI звука при нажатии кнопки покупки")]
    public string purchaseClickSound = "purchase_click";
    [Tooltip("Воспроизводить звуки покупок")]
    public bool enablePurchaseSounds = true;
    [Tooltip("Воспроизводить звук при нажатии кнопки покупки (до подтверждения)")]
    public bool playClickSoundOnPurchase = true;
    [Tooltip("Воспроизводить звук успеха после завершения покупки")]
    public bool playSuccessSoundAfterPurchase = true;
    
    [Header("Confirmation Prefab Paths")]
    [Tooltip("Путь к Image аватара в окне подтверждения")]
    public string confirmationAvatarImagePath = "Content/AvatarIcon";
    [Tooltip("Путь к тексту названия в окне подтверждения")]
    public string confirmationNameTextPath = "Content/NameText";
    [Tooltip("Путь к тексту цены в окне подтверждения")]
    public string confirmationPriceTextPath = "Content/PriceText";
    [Tooltip("Путь к кнопке подтверждения")]
    public string confirmationBuyButtonPath = "Content/BuyButton";
    [Tooltip("Путь к кнопке отмены")]
    public string confirmationCancelButtonPath = "Content/CancelButton";
    [Tooltip("Путь к фону окна")]
    public string confirmationBackgroundPath = "Background";
    
    [Header("Daily Gift Settings")]
    public Sprite dailyGiftSprite; // Спрайт для ежедневного подарка
    [Tooltip("Если не указан dailyGiftSprite, будет использован спрайт гема из DailyDealsManager")]
    public bool useGemSpriteFromManager = true;
    
    [Header("Daily Deals")]
    public bool isDailyDealsCategory = false; // Отметьте это для Daily категории
    public TextMeshProUGUI dailyDealsTimerText; // Таймер обновления
    public GameObject discountBadgePrefab; // Префаб бейджа скидки (опционально)
    
    [Header("Shop Item Prefab Components")]
    [Tooltip("Путь к Image компоненту в префабе для аватара")]
    public string avatarImagePath = "PlayerIcon/PlayerIconMask/PlayerIcon_Image";
    [Tooltip("Путь к TextMeshProUGUI компоненту в префабе для имени")]
    public string nameTextPath = "NameText";
    [Tooltip("Путь к TextMeshProUGUI компоненту в префабе для цены")]
    public string priceTextPath = "PriceText";
    [Tooltip("Путь к Button компоненту в префабе для покупки")]
    public string buyButtonPath = "BuyButton";
    [Tooltip("Путь к GameObject в префабе для индикатора покупки")]
    public string purchasedIndicatorPath = "PurchasedIndicator";
    [Tooltip("Путь к GameObject иконки ресурса (гема) в PriceContainer")]
    public string priceResourceIconPath = "PriceContainer/Resource";
    
    [Header("Animation Settings")]
    [Tooltip("Продолжительность анимации нажатия")]
    public float clickAnimationDuration = 0.15f;
    [Tooltip("Масштаб при нажатии (0.9 = уменьшение на 10%)")]
    public float clickScaleMultiplier = 0.95f;
    [Tooltip("Использовать анимации")]
    public bool useClickAnimations = true;
    [Tooltip("Использовать анимацию появления карточек только при первой загрузке")]
    public bool useAppearanceAnimation = true;
    
    private bool isFirstLoad = true; // Флаг для отслеживания первой загрузки

    [Tooltip("Путь к GameObject анимации PriceContainer для ежедневного подарка")]
    public string priceContainerAnimationPath = "PriceContainerAnimation";
    [Tooltip("Путь к Image компоненту в префабе подарка для иконки")]
    public string giftImagePath = "GiftIcon/GiftIcon_Image";
    [Tooltip("Путь к TextMeshProUGUI компоненту в префабе подарка для описания")]
    public string giftDescriptionPath = "GiftDescription";
    [Tooltip("Путь к Button компоненту в префабе подарка для кнопки забрать")]
    public string claimButtonPath = "ClaimButton";
    [Tooltip("Путь к GameObject в префабе подарка для индикатора получения")]
    public string claimedIndicatorPath = "ClaimedIndicator";
    
    private List<GameObject> shopItems = new List<GameObject>();
    private GameObject currentConfirmationWindow;

    private void Start()
    {
        RefreshShop();

        // Если это Daily Deals категория, подписываемся на обновления
        if (isDailyDealsCategory)
        {
            if (DailyDealsManager.Instance != null)
            {
                DailyDealsManager.Instance.OnDealsRefreshed += OnDailyDealsRefreshed;
            }
        }
    }

    #region Audio Helper Methods

    /// <summary>
    /// Воспроизводит UI звук если включены звуки покупок
    /// </summary>
    private void PlayUISound(string soundName)
    {
        if (!enablePurchaseSounds || string.IsNullOrEmpty(soundName)) return;
        
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
    /// Воспроизводит звук успешной покупки
    /// </summary>
    private void PlayPurchaseSuccessSound()
    {
        if (playSuccessSoundAfterPurchase)
        {
            PlayUISound(purchaseSuccessSound);
        }
    }

    /// <summary>
    /// Воспроизводит звук получения ежедневного подарка
    /// </summary>
    private void PlayDailyGiftClaimSound()
    {
        PlayUISound(dailyGiftClaimSound);
    }

    /// <summary>
    /// Воспроизводит звук ошибки покупки
    /// </summary>
    private void PlayPurchaseErrorSound()
    {
        PlayUISound(purchaseErrorSound);
    }

    /// <summary>
    /// Воспроизводит звук нажатия кнопки покупки
    /// </summary>
    private void PlayPurchaseClickSound()
    {
        if (playClickSoundOnPurchase)
        {
            PlayUISound(purchaseClickSound);
        }
    }

    #endregion

    #region Purchase Confirmation System

    /// <summary>
    /// Показывает окно подтверждения покупки аватара
    /// </summary>
    private void ShowPurchaseConfirmation(AvatarData avatarData, int avatarIndex, System.Action onConfirm, bool isDailyDeal = false, DailyDeal deal = null)
    {
        if (purchaseConfirmationPrefab == null)
        {
            Debug.LogWarning("Префаб окна подтверждения не назначен! Выполняем покупку без подтверждения.");
            onConfirm?.Invoke();
            return;
        }

        // Закрываем предыдущее окно если есть
        CloseConfirmationWindow();

        // Определяем родителя
        Transform parent = confirmationParent != null ? confirmationParent : transform;
        
        // Создаем окно подтверждения
        currentConfirmationWindow = Instantiate(purchaseConfirmationPrefab, parent);
        
        // Проверяем, достаточно ли средств
        bool canAfford = CanAffordPurchase(avatarData, isDailyDeal, deal);
        
        // Настраиваем компоненты окна
        SetupConfirmationWindow(currentConfirmationWindow, avatarData, avatarIndex, onConfirm, isDailyDeal, deal, canAfford);
        
        // Анимация появления окна
        AnimateConfirmationWindow(currentConfirmationWindow, true);
    }

    /// <summary>
    /// Показывает окно подтверждения для ежедневного подарка
    /// </summary>
    private void ShowDailyGiftConfirmation(DailyGift dailyGift, System.Action onConfirm)
    {
        if (purchaseConfirmationPrefab == null || !showConfirmationForDailyGift)
        {
            onConfirm?.Invoke();
            return;
        }

        // Закрываем предыдущее окно если есть
        CloseConfirmationWindow();

        // Определяем родителя
        Transform parent = confirmationParent != null ? confirmationParent : transform;
        
        // Создаем окно подтверждения
        currentConfirmationWindow = Instantiate(purchaseConfirmationPrefab, parent);
        
        // Настраиваем компоненты окна для подарка
        SetupDailyGiftConfirmationWindow(currentConfirmationWindow, dailyGift, onConfirm);
        
        // Анимация появления окна
        AnimateConfirmationWindow(currentConfirmationWindow, true);
    }

    /// <summary>
    /// Настраивает компоненты окна подтверждения для аватара
    /// </summary>
    private void SetupConfirmationWindow(GameObject window, AvatarData avatarData, int avatarIndex, System.Action onConfirm, bool isDailyDeal = false, DailyDeal deal = null, bool canAfford = true)
    {
        // Аватар
        Image avatarImage = FindComponentByPath<Image>(window, confirmationAvatarImagePath);
        if (avatarImage != null)
        {
            avatarImage.sprite = avatarData.sprite;
        }

        // Название
        TextMeshProUGUI nameText = FindComponentByPath<TextMeshProUGUI>(window, confirmationNameTextPath);
        if (nameText != null)
        {
            nameText.text = avatarData.name;
        }

        // Цена и текущее количество гемов
        TextMeshProUGUI priceText = FindComponentByPath<TextMeshProUGUI>(window, confirmationPriceTextPath);
        if (priceText != null)
        {
            int currentGems = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetGems() : 0;
            int requiredGems = 0;

            if (isDailyDeal && deal != null)
            {
                requiredGems = deal.discountedPrice;
                if (deal.discountedPrice == 0)
                {
                    priceText.text = "БЕЗКОШТОВНО";
                }
                else
                {
                    // Показываем цену и текущие гемы
                    if (canAfford)
                    {
                        priceText.text = $"{deal.discountedPrice} 💎";
                        if (deal.originalPrice > deal.discountedPrice)
                        {
                            priceText.text = $"<s>{deal.originalPrice}</s> {deal.discountedPrice} 💎";
                        }
                    }
                    else
                    {
                        priceText.text = $"{deal.discountedPrice}";
                    }
                }
            }
            else
            {
                requiredGems = avatarData.gemCost;
                if (avatarData.gemCost == 0)
                {
                    priceText.text = "БЕЗКОШТОВНО";
                }
                else
                {
                    // Показываем цену и текущие гемы
                    if (canAfford)
                    {
                        priceText.text = $"{avatarData.gemCost}";
                    }
                    else
                    {
                        priceText.text = $"{avatarData.gemCost}";
                    }
                }
            }
        }

        // Кнопка подтверждения
        Button confirmButton = FindComponentByPath<Button>(window, confirmationBuyButtonPath);
        if (confirmButton != null)
        {
            // Настраиваем кнопку в зависимости от наличия средств
            TextMeshProUGUI buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = confirmButton.GetComponent<Image>();

            if (canAfford)
            {
                // Достаточно средств - обычная покупка
                confirmButton.interactable = true;
                
                // Показываем цену на кнопке
                if (buttonText != null)
                {
                    if (isDailyDeal && deal != null)
                    {
                        if (deal.discountedPrice == 0)
                        {
                            buttonText.text = "БЕЗКОШТОВНО";
                        }
                        else
                        {
                            buttonText.text = $"{deal.discountedPrice}";
                        }
                    }
                    else
                    {
                        if (avatarData.gemCost == 0)
                        {
                            buttonText.text = "БЕЗКОШТОВНО";
                        }
                        else
                        {
                            buttonText.text = $"{avatarData.gemCost}";
                        }
                    }
                }

                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => {
                    AnimateButtonClick(confirmButton.gameObject, () => {
                        CloseConfirmationWindow();
                        onConfirm?.Invoke();
                    });
                });
            }
            else
            {
                // Недостаточно средств - показываем сообщение
                confirmButton.interactable = true; // Оставляем активной для показа сообщения
                
                // Показываем цену на кнопке (красным цветом)
                if (buttonText != null)
                {
                    if (isDailyDeal && deal != null)
                    {
                        buttonText.text = $"{deal.discountedPrice}";
                    }
                    else
                    {
                        buttonText.text = $"{avatarData.gemCost}";
                    }
                }

                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(() => {
                    AnimateButtonClick(confirmButton.gameObject, () => {
                        // Вычисляем сколько гемов не хватает
                        int currentGems = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetGems() : 0;
                        int requiredGems = isDailyDeal && deal != null ? deal.discountedPrice : avatarData.gemCost;
                        int missingGems = requiredGems - currentGems;
                        
                        // Выводим в консоль сколько не хватает
                        Debug.Log($"💎 Недостаточно гемов для покупки {avatarData.name}! Не хватает: {missingGems} гемов (нужно {requiredGems}, есть {currentGems})");
                        
                        // Воспроизводим звук ошибки
                        PlayPurchaseErrorSound();
                        
                        // Анимация ошибки
                        AnimateError(confirmButton.gameObject);
                        
                        // Не закрываем окно сразу, чтобы игрок понял проблему
                        DOVirtual.DelayedCall(1.0f, () => {
                            CloseConfirmationWindow();
                        });
                    });
                });
            }
        }

        // Кнопка отмены
        Button cancelButton = FindComponentByPath<Button>(window, confirmationCancelButtonPath);
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => {
                AnimateButtonClick(cancelButton.gameObject, () => {
                    CloseConfirmationWindow();
                });
            });
        }

        // Фон (закрытие по клику на фон)
        GameObject background = FindGameObjectByPath(window, confirmationBackgroundPath);
        if (background != null)
        {
            Button backgroundButton = background.GetComponent<Button>();
            if (backgroundButton == null)
            {
                backgroundButton = background.AddComponent<Button>();
            }
            
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(() => {
                CloseConfirmationWindow();
            });
        }
    }

    /// <summary>
    /// Настраивает окно подтверждения для ежедневного подарка
    /// </summary>
    private void SetupDailyGiftConfirmationWindow(GameObject window, DailyGift dailyGift, System.Action onConfirm)
    {
        // Иконка подарка
        Image avatarImage = FindComponentByPath<Image>(window, confirmationAvatarImagePath);
        if (avatarImage != null)
        {
            Sprite giftSprite = GetDailyGiftSprite();
            if (giftSprite != null)
            {
                avatarImage.sprite = giftSprite;
            }
        }

        // Название
        TextMeshProUGUI nameText = FindComponentByPath<TextMeshProUGUI>(window, confirmationNameTextPath);
        if (nameText != null)
        {
            nameText.text = "Ежедневный подарок";
        }

        // Количество гемов
        TextMeshProUGUI priceText = FindComponentByPath<TextMeshProUGUI>(window, confirmationPriceTextPath);
        if (priceText != null)
        {
            priceText.text = $"+{dailyGift.gemAmount} 💎";
        }

        // Кнопка подтверждения
        Button confirmButton = FindComponentByPath<Button>(window, confirmationBuyButtonPath);
        if (confirmButton != null)
        {
            // Меняем текст кнопки
            TextMeshProUGUI buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "ЗАБРАТИ";
            }

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => {
                AnimateButtonClick(confirmButton.gameObject, () => {
                    CloseConfirmationWindow();
                    onConfirm?.Invoke();
                });
            });
        }

        // Кнопка отмены
        Button cancelButton = FindComponentByPath<Button>(window, confirmationCancelButtonPath);
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => {
                AnimateButtonClick(cancelButton.gameObject, () => {
                    CloseConfirmationWindow();
                });
            });
        }

        // Фон (закрытие по клику на фон)
        GameObject background = FindGameObjectByPath(window, confirmationBackgroundPath);
        if (background != null)
        {
            Button backgroundButton = background.GetComponent<Button>();
            if (backgroundButton == null)
            {
                backgroundButton = background.AddComponent<Button>();
            }
            
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(() => {
                CloseConfirmationWindow();
            });
        }
    }

    /// <summary>
    /// Анимирует появление/исчезновение окна подтверждения
    /// </summary>
    private void AnimateConfirmationWindow(GameObject window, bool show)
    {
        if (!useClickAnimations || window == null) return;

        RectTransform rectTransform = window.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = window.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = window.AddComponent<CanvasGroup>();
        }

        if (show)
        {
            // Начальное состояние
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.zero;
            }
            canvasGroup.alpha = 0f;

            // Анимация появления
            if (rectTransform != null)
            {
                rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
            canvasGroup.DOFade(1f, 0.2f);
        }
        else
        {
            // Анимация исчезновения
            if (rectTransform != null)
            {
                rectTransform.DOScale(0.8f, 0.2f).SetEase(Ease.InBack);
            }
            canvasGroup.DOFade(0f, 0.2f).OnComplete(() => {
                if (window != null)
                {
                    Destroy(window);
                }
            });
        }
    }

    /// <summary>
    /// Закрывает текущее окно подтверждения
    /// </summary>
    private void CloseConfirmationWindow()
    {
        if (currentConfirmationWindow != null)
        {
            AnimateConfirmationWindow(currentConfirmationWindow, false);
            currentConfirmationWindow = null;
        }
    }

    /// <summary>
    /// Проверяет, достаточно ли средств для покупки
    /// </summary>
    private bool CanAffordPurchase(AvatarData avatarData, bool isDailyDeal = false, DailyDeal deal = null)
    {
        if (CurrencyManager.Instance == null) return false;

        int requiredGems = 0;
        
        if (isDailyDeal && deal != null)
        {
            requiredGems = deal.discountedPrice;
        }
        else
        {
            requiredGems = avatarData.gemCost;
        }

        // Бесплатные покупки всегда доступны
        if (requiredGems == 0) return true;

        // Проверяем наличие достаточного количества гемов
        return CurrencyManager.Instance.HasEnoughGems(requiredGems);
    }

    /// <summary>
    /// Проверяет, нужно ли показывать подтверждение для покупки
    /// </summary>
    private bool ShouldShowConfirmation(AvatarData avatarData, bool isDailyDeal = false)
    {
        if (purchaseConfirmationPrefab == null) return false;
        
        // Для Daily Deals всегда показываем подтверждение
        if (isDailyDeal) return true;
        
        // Для бесплатных аватаров проверяем настройку
        if (avatarData.gemCost == 0) return showConfirmationForFree;
        
        // Для платных аватаров всегда показываем
        return true;
    }

    #endregion

    // Утилитарный метод для поиска карточки по индексу аватара
    private GameObject FindShopItemByAvatarIndex(int avatarIndex)
    {
        // Простая реализация - можно улучшить добавив ID к карточкам
        // Пока возвращаем null, но логика анимации все равно будет работать
        return null;
    }

    // Анимация нажатия кнопки
    private void AnimateButtonClick(GameObject button, System.Action onComplete = null)
    {
        if (!useClickAnimations || button == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Останавливаем предыдущие анимации на этом объекте
        button.transform.DOKill();

        // Анимация нажатия: уменьшение -> возврат к нормальному размеру -> выполнение действия
        button.transform.DOScale(clickScaleMultiplier, clickAnimationDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                button.transform.DOScale(1f, clickAnimationDuration * 0.5f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        onComplete?.Invoke();
                    });
            });
    }

    // Анимация успешной покупки
    private void AnimateSuccessfulPurchase(GameObject item)
    {
        if (!useClickAnimations || item == null) return;

        // Останавливаем предыдущие анимации
        item.transform.DOKill();

        // Анимация успеха: небольшое увеличение с bouncy эффектом
        item.transform.DOScale(1.1f, 0.2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                item.transform.DOScale(1f, 0.3f)
                    .SetEase(Ease.OutBounce);
            });
    }

    // Анимация ошибки
    private void AnimateError(GameObject item)
    {
        if (!useClickAnimations || item == null) return;

        // Останавливаем предыдущие анимации
        item.transform.DOKill();

        // Анимация тряски
        item.transform.DOShakePosition(0.5f, strength: 10f, vibrato: 20, randomness: 90f)
            .SetEase(Ease.OutQuad);
    }

    // Анимация появления карточки при создании
    private void AnimateCardAppearance(GameObject card, float delay = 0f)
    {
        if (!useClickAnimations || !useAppearanceAnimation || !isFirstLoad || card == null) return;

        // Начальное состояние
        card.transform.localScale = Vector3.zero;
        
        // Анимация появления с задержкой
        card.transform.DOScale(1f, 0.4f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack);
            
            // Обновляем таймер каждую секунду
            InvokeRepeating(nameof(UpdateDailyDealsTimer), 1f, 1f);
        
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (isDailyDealsCategory && DailyDealsManager.Instance != null)
        {
            DailyDealsManager.Instance.OnDealsRefreshed -= OnDailyDealsRefreshed;
        }
        
        // Закрываем окно подтверждения
        CloseConfirmationWindow();
    }

    private void OnDailyDealsRefreshed()
    {
        Debug.Log("Daily Deals обновлены! Обновляем магазин...");
        RefreshShop();
    }

    private void UpdateDailyDealsTimer()
    {
        if (dailyDealsTimerText != null && DailyDealsManager.Instance != null)
        {
            var timeUntilRefresh = DailyDealsManager.Instance.GetTimeUntilRefresh();
            dailyDealsTimerText.text = FormatTimeSpan(timeUntilRefresh);
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        List<string> parts = new List<string>();
        
        // Добавляем часы если больше 0
        if (timeSpan.Hours > 0)
        {
            parts.Add($"{timeSpan.Hours}г");
        }
        
        // Добавляем минуты если больше 0 или если есть часы
        if (timeSpan.Minutes > 0 || timeSpan.Hours > 0)
        {
            parts.Add($"{timeSpan.Minutes}хв");
        }
        
        // Добавляем секунды если это последний компонент или меньше минуты
        if (parts.Count == 0 || (timeSpan.Hours == 0 && timeSpan.Minutes == 0))
        {
            parts.Add($"{timeSpan.Seconds}с");
        }
        
        return string.Join(" ", parts);
    }

    public void RefreshShop()
    {
        ClearShop();
        CreateShopItems();
        
        // После первого обновления отключаем анимацию появления
        isFirstLoad = false;
    }

    private void ClearShop()
    {
        foreach (var item in shopItems)
        {
            if (item != null) Destroy(item);
        }
        shopItems.Clear();
    }

    private void CreateShopItems()
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return;
        }

        if (isDailyDealsCategory)
        {
            CreateDailyDealsItems();
        }
        else
        {
            CreateRegularShopItems();
        }
    }

    private void CreateDailyDealsItems()
    {
        if (DailyDealsManager.Instance == null)
        {
            Debug.LogError("DailyDealsManager недоступен!");
            return;
        }

        // Сначала создаем ежедневный подарок
        CreateDailyGiftItem();
        
        // Затем создаем остальные предложения
        var currentDeals = DailyDealsManager.Instance.GetCurrentDeals();
        
        foreach (var deal in currentDeals)
        {
            var avatarData = AvatarManager.Instance.GetAvatarData(deal.avatarIndex);
            if (avatarData == null) continue;
            
            CreateDailyDealItem(avatarData, deal.avatarIndex, deal);
        }

        Debug.Log($"Создано 1 ежедневный подарок и {currentDeals.Count} Daily Deals в магазине");
    }

    private void CreateRegularShopItems()
    {
        var allAvatars = AvatarManager.Instance.GetAllAvatarData();
        
        for (int i = 0; i < allAvatars.Length; i++)
        {
            var avatarData = allAvatars[i];
            if (avatarData == null) continue;
            
            CreateShopItem(avatarData, i);
        }
    }

    private void CreateDailyDealItem(AvatarData avatarData, int avatarIndex, DailyDeal deal)
    {
        GameObject shopItem = Instantiate(avatarShopItemPrefab, shopContent);
        shopItems.Add(shopItem);

        // Настраиваем компоненты для Daily Deal
        SetupDailyDealComponents(shopItem, avatarData, avatarIndex, deal);
        
        // Добавляем бейдж скидки
        AddDiscountBadge(shopItem, deal.discountPercent);
        
        // Анимация появления с задержкой
        float delay = shopItems.Count * 0.05f;
        AnimateCardAppearance(shopItem, delay);
    }

    private void SetupDailyGiftComponents(GameObject giftItem, DailyGift dailyGift)
    {
        // Определяем какой спрайт использовать
        Sprite spriteToUse = GetDailyGiftSprite();

        // Иконка подарка
        Image giftImage = FindComponentByPath<Image>(giftItem, giftImagePath);
        if (giftImage == null)
        {
            // Fallback на путь аватара если специальный путь не найден
            giftImage = FindComponentByPath<Image>(giftItem, avatarImagePath);
        }

        if (giftImage != null && spriteToUse != null)
        {
            giftImage.sprite = spriteToUse;
            Debug.Log($"Установлен спрайт подарка: {spriteToUse.name}");
        }
        else if (giftImage != null)
        {
            Debug.LogWarning("Спрайт для ежедневного подарка не найден!");
        }

        // Описание подарка
        TextMeshProUGUI descriptionText = FindComponentByPath<TextMeshProUGUI>(giftItem, giftDescriptionPath);
        if (descriptionText == null)
        {
            // Fallback на путь имени
            descriptionText = FindComponentByPath<TextMeshProUGUI>(giftItem, nameTextPath);
        }

        if (descriptionText != null)
        {
            descriptionText.text = $"{dailyGift.gemAmount} гемів";
        }

        // Кнопка забрать
        Button claimButton = FindComponentByPath<Button>(giftItem, claimButtonPath);
        if (claimButton == null)
        {
            // Fallback на обычную кнопку покупки или весь объект
            claimButton = FindComponentByPath<Button>(giftItem, buyButtonPath);
            if (claimButton == null)
            {
                claimButton = giftItem.GetComponent<Button>();
            }
        }

        // Текст цены/кнопки
        TextMeshProUGUI priceText = FindComponentByPath<TextMeshProUGUI>(giftItem, priceTextPath);

        // Индикатор получения
        GameObject claimedIndicator = FindGameObjectByPath(giftItem, claimedIndicatorPath);
        if (claimedIndicator == null)
        {
            // Fallback на обычный индикатор покупки
            claimedIndicator = FindGameObjectByPath(giftItem, purchasedIndicatorPath);
        }

        // НОВОЕ: Находим объект анимации PriceContainer
        GameObject priceContainerAnimation = FindGameObjectByPath(giftItem, priceContainerAnimationPath);

        // Настраиваем состояние подарка
        if (dailyGift.isClaimed)
        {
            // Подарок уже забран
            if (priceText != null)
            {
                priceText.text = "ОТРИМАНО";
            }

            if (claimButton != null)
            {
                claimButton.interactable = true; // Делаем кнопку активной

                var buttonImage = claimButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.3f, 0.8f, 0.3f, 1f); // Зеленый
                }

                // Добавляем обработчик нажатия с анимацией и сообщением
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(() =>
                {
                    AnimateButtonClick(claimButton.gameObject, () =>
                    {
                        Debug.Log("🎁 Ежедневный подарок уже забран сегодня! Возвращайтесь завтра за новым подарком.");
                    });
                });
            }

            if (claimedIndicator != null) claimedIndicator.SetActive(true);

            // НОВОЕ: Отключаем анимацию если подарок уже забран
            if (priceContainerAnimation != null)
            {
                priceContainerAnimation.SetActive(false);
                Debug.Log("🔇 Анимация PriceContainer отключена - подарок уже забран");
            }
        }
        else
        {
            // Подарок можно забрать
            if (priceText != null)
            {
                priceText.text = "ЗАБРАТИ";
                priceText.color = Color.white;
            }

            if (claimButton != null)
            {
                claimButton.interactable = true;

                var buttonImage = claimButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(1f, 0.8f, 0.2f, 1f); // Золотистый
                }

                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(() =>
                {
                    // ВОСПРОИЗВОДИМ ЗВУК СРАЗУ ПРИ НАЖАТИИ
                    PlayDailyGiftClaimSound();
                    
                    AnimateButtonClick(claimButton.gameObject, () =>
                    {
                        // Проверяем, нужно ли показывать подтверждение
                        if (showConfirmationForDailyGift && purchaseConfirmationPrefab != null)
                        {
                            ShowDailyGiftConfirmation(dailyGift, () => ClaimDailyGift());
                        }
                        else
                        {
                            ClaimDailyGift();
                        }
                    });
                });
            }

            if (claimedIndicator != null) claimedIndicator.SetActive(false);

            // НОВОЕ: Включаем анимацию если подарок доступен для получения
            if (priceContainerAnimation != null)
            {
                priceContainerAnimation.SetActive(true);
                Debug.Log("🔊 Анимация PriceContainer включена - подарок доступен!");
            }
        }
    }

        /// <summary>
    /// Управляет анимацией PriceContainer для ежедневного подарка
    /// </summary>
    /// <param name="giftItem">Карточка подарка</param>
    /// <param name="enable">Включить или выключить анимацию</param>
    private void SetPriceContainerAnimation(GameObject giftItem, bool enable)
    {
        if (giftItem == null) return;
        
        GameObject priceContainerAnimation = FindGameObjectByPath(giftItem, priceContainerAnimationPath);
        if (priceContainerAnimation != null)
        {
            priceContainerAnimation.SetActive(enable);
            Debug.Log($"🎭 Анимация PriceContainer {(enable ? "включена" : "отключена")}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Объект анимации не найден по пути: {priceContainerAnimationPath}");
        }
    }

    /// <summary>
    /// Отключает анимацию для всех ежедневных подарков (полезно для отладки)
    /// </summary>
    [ContextMenu("Disable All Gift Animations")]
    public void DisableAllGiftAnimations()
    {
        if (!isDailyDealsCategory) return;
        
        foreach (var shopItem in shopItems)
        {
            if (shopItem != null)
            {
                SetPriceContainerAnimation(shopItem, false);
            }
        }
        Debug.Log("🔇 Все анимации подарков отключены");
    }

    /// <summary>
    /// Включает анимацию только для доступных подарков
    /// </summary>
    [ContextMenu("Enable Available Gift Animations")]
    public void EnableAvailableGiftAnimations()
    {
        if (!isDailyDealsCategory || DailyDealsManager.Instance == null) return;
        
        var dailyGift = DailyDealsManager.Instance.GetDailyGift();
        if (dailyGift != null && !dailyGift.isClaimed && shopItems.Count > 0)
        {
            // Первый элемент обычно ежедневный подарок
            SetPriceContainerAnimation(shopItems[0], true);
            Debug.Log("🔊 Анимация доступного подарка включена");
        }
    }

    /// <summary>
    /// Получает спрайт для ежедневного подарка
    /// </summary>
    /// <returns>Спрайт для отображения в подарке</returns>
    private Sprite GetDailyGiftSprite()
    {
        // Сначала проверяем, задан ли спрайт в инспекторе
        if (dailyGiftSprite != null)
        {
            return dailyGiftSprite;
        }

        // Если не задан и включена опция использования спрайта из менеджера
        if (useGemSpriteFromManager && DailyDealsManager.Instance != null)
        {
            var gemSprite = DailyDealsManager.Instance.GetGemSprite();
            if (gemSprite != null)
            {
                return gemSprite;
            }
        }

        // Если ничего не найдено, возвращаем null
        Debug.LogWarning("Не удалось найти спрайт для ежедневного подарка! Проверьте настройки dailyGiftSprite или DailyDealsManager.GetGemSprite()");
        return null;
    }

    /// <summary>
    /// Устанавливает новый спрайт для ежедневного подарка
    /// </summary>
    /// <param name="sprite">Новый спрайт</param>
    public void SetDailyGiftSprite(Sprite sprite)
    {
        dailyGiftSprite = sprite;
        
        // Если это Daily Deals категория, обновляем магазин
        if (isDailyDealsCategory)
        {
            RefreshShop();
        }
    }

    private void ClaimDailyGift()
    {
        if (DailyDealsManager.Instance == null)
        {
            Debug.LogError("DailyDealsManager недоступен!");
            return;
        }

        var dailyGift = DailyDealsManager.Instance.GetDailyGift();
        if (dailyGift == null)
        {
            Debug.LogError("Ежедневный подарок недоступен!");
            return;
        }

        Debug.Log($"Забираем ежедневный подарок: {dailyGift.gemAmount} гемов");

        if (DailyDealsManager.Instance.ClaimDailyGift())
        {
            Debug.Log($"🎁 Ежедневный подарок забран! Получено {dailyGift.gemAmount} гемов!");
            
            // Находим карточку подарка для анимации успеха
            if (shopItems.Count > 0)
            {
                AnimateSuccessfulPurchase(shopItems[0]); // Подарок обычно первый
            }
            
            // Обновляем через небольшую задержку после анимации
            DOVirtual.DelayedCall(0.6f, RefreshShop);
        }
        else
        {
            Debug.LogWarning("Не удалось забрать ежедневный подарок");
            
            // Воспроизводим звук ошибки
            PlayPurchaseErrorSound();
            
            // Анимация ошибки
            if (shopItems.Count > 0)
            {
                AnimateError(shopItems[0]);
            }
        }
    }

    private void CreateDailyGiftItem()
    {
        var dailyGift = DailyDealsManager.Instance.GetDailyGift();
        if (dailyGift == null)
        {
            Debug.LogWarning("Ежедневный подарок недоступен!");
            return;
        }

        // Используем префаб подарка или обычный префаб аватара
        GameObject prefabToUse = dailyGiftItemPrefab != null ? dailyGiftItemPrefab : avatarShopItemPrefab;
        GameObject giftItem = Instantiate(prefabToUse, shopContent);
        shopItems.Add(giftItem);

        // Настраиваем компоненты подарка
        SetupDailyGiftComponents(giftItem, dailyGift);
        
        // Анимация появления
        AnimateCardAppearance(giftItem, 0f);
    }

    private void CreateShopItem(AvatarData avatarData, int avatarIndex)
    {
        GameObject shopItem = Instantiate(avatarShopItemPrefab, shopContent);
        shopItems.Add(shopItem);

        // Настраиваем обычные компоненты
        SetupShopItemComponents(shopItem, avatarData, avatarIndex);
        
        // Анимация появления с небольшой задержкой
        float delay = shopItems.Count * 0.05f; // Постепенное появление карточек
        AnimateCardAppearance(shopItem, delay);
    }

    private void SetupDailyDealComponents(GameObject shopItem, AvatarData avatarData, int avatarIndex, DailyDeal deal)
    {
        // Аватар
        Image avatarImage = FindComponentByPath<Image>(shopItem, avatarImagePath);
        if (avatarImage != null)
        {
            avatarImage.sprite = avatarData.sprite;
        }

        // Имя (если есть)
        TextMeshProUGUI nameText = FindComponentByPath<TextMeshProUGUI>(shopItem, nameTextPath);
        if (nameText != null)
        {
            nameText.text = avatarData.name;
        }

        // Кнопка (весь префаб или отдельная кнопка)
        Button buyButton = shopItem.GetComponent<Button>();
        if (buyButton == null)
        {
            buyButton = FindComponentByPath<Button>(shopItem, buyButtonPath);
        }

        // Цена
        TextMeshProUGUI priceText = FindComponentByPath<TextMeshProUGUI>(shopItem, priceTextPath);
        GameObject purchasedIndicator = FindGameObjectByPath(shopItem, purchasedIndicatorPath);

        bool isUnlocked = AvatarManager.Instance.IsAvatarUnlocked(avatarIndex);

        if (isUnlocked || deal.isPurchased)
        {
            // Аватар уже куплен
            if (priceText != null) priceText.text = "ПРИДБАНО";
            if (buyButton != null) 
            {
                buyButton.interactable = true;
                
                // Зеленый цвет = куплено, можно выбрать
                var buttonImage = buyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.3f, 0.8f, 0.3f, 1f);
                }
                
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => {
                    AnimateButtonClick(buyButton.gameObject, () => SelectAvatar(avatarIndex));
                });
            }
            if (purchasedIndicator != null) purchasedIndicator.SetActive(true);
        }
        else
        {
            // Daily Deal можно купить
            if (priceText != null)
            {
                // Показываем зачеркнутую старую цену и новую с валютой
                if (deal.discountedPrice == 0)
                {
                    priceText.text = $"БЕЗКОШТОВНО";
                }
                else
                {
                    priceText.text = $"{deal.discountedPrice}";
                }
                priceText.color = new Color(1f, 0.8f, 0.2f); // Золотистый для скидки
            }
            
            if (buyButton != null)
            {
                bool canPurchase = DailyDealsManager.Instance.CanPurchaseDailyDeal(avatarIndex);
                buyButton.interactable = true; // Всегда делаем кнопку активной
                
                // Оранжевый цвет для Daily Deals
                var buttonImage = buyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(1f, 0.6f, 0.1f, 1f); // Оранжевый
                }
                
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => {
                    // ВОСПРОИЗВОДИМ ЗВУК НАЖАТИЯ СРАЗУ
                    PlayPurchaseClickSound();
                    
                    AnimateButtonClick(buyButton.gameObject, () => {
                        // ВСЕГДА показываем подтверждение перед покупкой Daily Deal
                        ShowPurchaseConfirmation(avatarData, avatarIndex, () => PurchaseDailyDeal(avatarIndex), true, deal);
                    });
                });
            }
            
            if (purchasedIndicator != null) purchasedIndicator.SetActive(false);
        }
    }

    private void SetupShopItemComponents(GameObject shopItem, AvatarData avatarData, int avatarIndex)
    {
        // Аватар
        Image avatarImage = FindComponentByPath<Image>(shopItem, avatarImagePath);
        if (avatarImage != null)
        {
            avatarImage.sprite = avatarData.sprite;
        }

        // Имя
        TextMeshProUGUI nameText = FindComponentByPath<TextMeshProUGUI>(shopItem, nameTextPath);
        if (nameText != null)
        {
            nameText.text = avatarData.name;
        }

        // Цена и кнопка покупки
        TextMeshProUGUI priceText = FindComponentByPath<TextMeshProUGUI>(shopItem, priceTextPath);

        Button buyButton = shopItem.GetComponent<Button>();
        if (buyButton == null)
        {
            buyButton = FindComponentByPath<Button>(shopItem, buyButtonPath);
        }

        GameObject purchasedIndicator = FindGameObjectByPath(shopItem, purchasedIndicatorPath);
        
        // Иконка ресурса (гема)
        GameObject priceResourceIcon = FindGameObjectByPath(shopItem, priceResourceIconPath);

        bool isUnlocked = AvatarManager.Instance.IsAvatarUnlocked(avatarIndex);

        if (isUnlocked)
        {
            // Аватар уже куплен - скрываем иконку гема
            if (priceText != null) priceText.text = "ПРИДБАНО";
            if (priceResourceIcon != null) priceResourceIcon.SetActive(false);
            
            if (buyButton != null)
            {
                buyButton.interactable = true;

                var buttonImage = buyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.3f, 0.8f, 0.3f, 1f); // Зеленый
                }

                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => {
                    AnimateButtonClick(buyButton.gameObject, () => SelectAvatar(avatarIndex));
                });
            }
            if (purchasedIndicator != null) purchasedIndicator.SetActive(true);
        }
        else
        {
            // Обычный аватар можно купить
            if (priceText != null)
            {
                if (avatarData.gemCost == 0)
                {
                    priceText.text = "БЕЗКОШТОВНО";
                    // Если бесплатный - скрываем иконку гема
                    if (priceResourceIcon != null) priceResourceIcon.SetActive(false);
                }
                else
                {
                    priceText.text = $"{avatarData.gemCost}";
                    // Если платный - показываем иконку гема
                    if (priceResourceIcon != null) priceResourceIcon.SetActive(true);
                }
            }

            if (buyButton != null)
            {
                bool canPurchase = CanPurchaseRegularAvatar(avatarIndex);
                buyButton.interactable = true; // Всегда делаем кнопку активной

                var buttonImage = buyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f); // Синий
                }

                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => {
                    // ВОСПРОИЗВОДИМ ЗВУК НАЖАТИЯ СРАЗУ
                    PlayPurchaseClickSound();
                    
                    AnimateButtonClick(buyButton.gameObject, () => {
                        // ВСЕГДА показываем подтверждение перед покупкой обычного аватара
                        ShowPurchaseConfirmation(avatarData, avatarIndex, () => PurchaseAvatar(avatarIndex), false);
                    });
                });
            }

            if (purchasedIndicator != null) purchasedIndicator.SetActive(false);
        }
    }

    private bool CanPurchaseRegularAvatar(int avatarIndex)
    {
        if (AvatarManager.Instance == null) return false;

        var avatarData = AvatarManager.Instance.GetAvatarData(avatarIndex);
        if (avatarData == null) return false;

        // Если аватар уже разблокирован, покупать не нужно
        if (AvatarManager.Instance.IsAvatarUnlocked(avatarIndex)) return false;

        // Если аватар бесплатный, всегда можно "купить"
        if (avatarData.gemCost == 0) return true;

        // Если платный, проверяем наличие гемов
        if (CurrencyManager.Instance == null) return false;
        return CurrencyManager.Instance.HasEnoughGems(avatarData.gemCost);
    }

    private void PurchaseDailyDeal(int avatarIndex)
    {
        if (DailyDealsManager.Instance == null)
        {
            Debug.LogError("DailyDealsManager недоступен!");
            return;
        }

        var deal = DailyDealsManager.Instance.GetDealForAvatar(avatarIndex);
        if (deal == null)
        {
            Debug.LogError($"Daily Deal для аватара {avatarIndex} не найден!");
            return;
        }

        var avatarData = AvatarManager.Instance.GetAvatarData(avatarIndex);
        if (avatarData == null)
        {
            Debug.LogError($"Данные аватара {avatarIndex} не найдены!");
            return;
        }

        Debug.Log($"Покупаем Daily Deal: {avatarData.name} за {deal.discountedPrice} гемов (скидка {deal.discountPercent:F1}%)");

        if (DailyDealsManager.Instance.PurchaseDailyDeal(avatarIndex))
        {
            Debug.Log($"🎉 Daily Deal успешно куплен! Сэкономлено {deal.originalPrice - deal.discountedPrice} гемов!");
            
            // ВОСПРОИЗВОДИМ ЗВУК УСПЕШНОЙ ПОКУПКИ
            PlayPurchaseSuccessSound();
            
            // Найти карточку для анимации успеха
            var shopItem = FindShopItemByAvatarIndex(avatarIndex);
            if (shopItem != null)
            {
                AnimateSuccessfulPurchase(shopItem);
            }
            
            DOVirtual.DelayedCall(0.6f, RefreshShop);
        }
        else
        {
            Debug.LogWarning($"Не удалось купить Daily Deal");
            
            // Воспроизводим звук ошибки
            PlayPurchaseErrorSound();
            
            // Анимация ошибки
            var shopItem = FindShopItemByAvatarIndex(avatarIndex);
            if (shopItem != null)
            {
                AnimateError(shopItem);
            }
        }
    }

    private void PurchaseAvatar(int avatarIndex)
    {
        Debug.Log($"Попытка купить аватар {avatarIndex}");
        
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return;
        }
        
        var avatarData = AvatarManager.Instance.GetAvatarData(avatarIndex);
        if (avatarData == null)
        {
            Debug.LogError($"Данные аватара {avatarIndex} не найдены!");
            return;
        }
        
        // Проверяем, не разблокирован ли уже
        if (AvatarManager.Instance.IsAvatarUnlocked(avatarIndex))
        {
            Debug.LogWarning($"Аватар {avatarIndex} уже разблокирован!");
            RefreshShop();
            return;
        }
        
        // Если аватар бесплатный, сразу разблокируем
        if (avatarData.gemCost == 0)
        {
            Debug.Log($"Разблокируем бесплатный аватар: {avatarData.name}");
            AvatarManager.Instance.UnlockAvatar(avatarIndex);
            
            // ВОСПРОИЗВОДИМ ЗВУК УСПЕШНОЙ ПОКУПКИ (даже для бесплатных)
            PlayPurchaseSuccessSound();
            
            var shopItem = FindShopItemByAvatarIndex(avatarIndex);
            if (shopItem != null)
            {
                AnimateSuccessfulPurchase(shopItem);
            }
            
            DOVirtual.DelayedCall(0.6f, RefreshShop);
            Debug.Log($"✅ Бесплатный аватар {avatarData.name} разблокирован!");
            return;
        }
        
        // Если платный, используем стандартную логику покупки
        if (AvatarManager.Instance.PurchaseAvatar(avatarIndex))
        {
            Debug.Log($"✅ Аватар {avatarData.name} успешно куплен за {avatarData.gemCost} гемов!");
            
            // ВОСПРОИЗВОДИМ ЗВУК УСПЕШНОЙ ПОКУПКИ
            PlayPurchaseSuccessSound();
            
            var shopItem = FindShopItemByAvatarIndex(avatarIndex);
            if (shopItem != null)
            {
                AnimateSuccessfulPurchase(shopItem);
            }
            
            DOVirtual.DelayedCall(0.6f, RefreshShop);
        }
        else
        {
            Debug.LogWarning($"❌ Не удалось купить аватар {avatarData.name}");
            
            // Воспроизводим звук ошибки
            PlayPurchaseErrorSound();
            
            var shopItem = FindShopItemByAvatarIndex(avatarIndex);
            if (shopItem != null)
            {
                AnimateError(shopItem);
            }
            
            // Показываем причину неудачи
            if (CurrencyManager.Instance != null)
            {
                int currentGems = CurrencyManager.Instance.GetGems();
                if (currentGems < avatarData.gemCost)
                {
                    Debug.Log($"Недостаточно гемов: нужно {avatarData.gemCost}, есть {currentGems}");
                }
            }
        }
    }

    private void SelectAvatar(int avatarIndex)
    {
        if (ProfileManager.Instance != null)
        {
            if (ProfileManager.Instance.TryChangeAvatar(avatarIndex))
            {
                Debug.Log($"✅ Выбран аватар {avatarIndex}");
            }
        }
    }

    private void AddDiscountBadge(GameObject shopItem, float discountPercent)
    {
        if (discountBadgePrefab == null) return;
        
        GameObject badge = Instantiate(discountBadgePrefab, shopItem.transform);
        
        // Настраиваем текст скидки
        TextMeshProUGUI badgeText = badge.GetComponentInChildren<TextMeshProUGUI>();
        if (badgeText != null)
        {
            badgeText.text = $"-{discountPercent:F0}%";
        }
        
        // Позиционируем бейдж в правом верхнем углу
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        if (badgeRect != null)
        {
            badgeRect.anchorMin = new Vector2(1, 1);
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.anchoredPosition = new Vector2(-10, -10);
            badgeRect.sizeDelta = new Vector2(50, 30);
        }
    }

    // Утилитарные методы
    private T FindComponentByPath<T>(GameObject parent, string path) where T : Component
    {
        if (string.IsNullOrEmpty(path)) return null;
        Transform target = parent.transform.Find(path);
        return target?.GetComponent<T>();
    }

    private GameObject FindGameObjectByPath(GameObject parent, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        Transform target = parent.transform.Find(path);
        return target?.gameObject;
    }

    // Публичные методы для управления
    public void ForceRefreshDailyDeals()
    {
        if (isDailyDealsCategory && DailyDealsManager.Instance != null)
        {
            DailyDealsManager.Instance.ForceRefreshDeals();
        }
    }

    /// <summary>
    /// Принудительно закрывает окно подтверждения (для внешнего использования)
    /// </summary>
    public void ForceCloseConfirmation()
    {
        CloseConfirmationWindow();
    }

    [ContextMenu("Debug Shop State")]
    public void DebugShopState()
    {
        Debug.Log($"=== AVATAR SHOP DEBUG ===");
        Debug.Log($"Is Daily Deals Category: {isDailyDealsCategory}");
        Debug.Log($"Shop Items Count: {shopItems.Count}");
        Debug.Log($"DailyDealsManager: {(DailyDealsManager.Instance != null ? "OK" : "NULL")}");
        Debug.Log($"Daily Gift Sprite: {(dailyGiftSprite != null ? dailyGiftSprite.name : "NULL")}");
        Debug.Log($"Use Gem Sprite From Manager: {useGemSpriteFromManager}");
        Debug.Log($"Purchase Confirmation Prefab: {(purchaseConfirmationPrefab != null ? "OK" : "NULL")}");
        Debug.Log($"Show Confirmation For Free: {showConfirmationForFree}");
        Debug.Log($"Show Confirmation For Daily Gift: {showConfirmationForDailyGift}");
        
        if (isDailyDealsCategory && DailyDealsManager.Instance != null)
        {
            var deals = DailyDealsManager.Instance.GetCurrentDeals();
            var gift = DailyDealsManager.Instance.GetDailyGift();
            
            Debug.Log($"Daily Gift: {gift?.gemAmount ?? 0} гемов (Claimed: {gift?.isClaimed ?? false})");
            Debug.Log($"Current Daily Deals: {deals.Count}");
            
            foreach (var deal in deals)
            {
                var avatarData = AvatarManager.Instance.GetAvatarData(deal.avatarIndex);
                Debug.Log($"Deal: {avatarData?.name} | {deal.originalPrice} → {deal.discountedPrice} | Purchased: {deal.isPurchased}");
            }
        }
    }

    [ContextMenu("Force Claim Daily Gift")]
    public void ForceClaimDailyGift()
    {
        if (isDailyDealsCategory)
        {
            ClaimDailyGift();
        }
    }

    [ContextMenu("Test Daily Gift Sprite")]
    public void TestDailyGiftSprite()
    {
        var sprite = GetDailyGiftSprite();
        if (sprite != null)
        {
            Debug.Log($"✅ Daily Gift Sprite найден: {sprite.name}");
        }
        else
        {
            Debug.LogWarning("❌ Daily Gift Sprite не найден!");
        }
    }

    [ContextMenu("Reset First Load Flag")]
    public void ResetFirstLoadFlag()
    {
        isFirstLoad = true;
        Debug.Log("Флаг первой загрузки сброшен. Следующий RefreshShop() покажет анимацию появления.");
    }

    [ContextMenu("Test Purchase Confirmation")]
    public void TestPurchaseConfirmation()
    {
        if (AvatarManager.Instance != null && purchaseConfirmationPrefab != null)
        {
            var avatarData = AvatarManager.Instance.GetAvatarData(0);
            if (avatarData != null)
            {
                ShowPurchaseConfirmation(avatarData, 0, () => {
                    Debug.Log("✅ Тестовое подтверждение покупки выполнено!");
                });
            }
        }
        else
        {
            Debug.LogWarning("❌ Не удалось протестировать подтверждение покупки. Проверьте AvatarManager и purchaseConfirmationPrefab.");
        }
    }
}