using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public class DailyDeal
{
    public int avatarIndex;
    public int originalPrice;
    public int discountedPrice;
    public float discountPercent;
    public bool isPurchased = false;
    
    public DailyDeal(int avatarIndex, int originalPrice, int discountedPrice)
    {
        this.avatarIndex = avatarIndex;
        this.originalPrice = originalPrice;
        this.discountedPrice = discountedPrice;
        this.discountPercent = (float)(originalPrice - discountedPrice) / originalPrice * 100f;
    }
}

[System.Serializable]
public class DailyGift
{
    public int gemAmount;
    public bool isClaimed = false;
    public string dateGenerated;
    public string playerUID; // Уникальный ID игрока
    
    public DailyGift(int gemAmount, string playerUID)
    {
        this.gemAmount = gemAmount;
        this.dateGenerated = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        this.playerUID = playerUID;
    }
    
    public DailyGift(int gemAmount, string dateGenerated, string playerUID)
    {
        this.gemAmount = gemAmount;
        this.dateGenerated = dateGenerated;
        this.playerUID = playerUID;
    }
}

public class DailyDealsManager : MonoBehaviour
{
    [Header("Daily Deals Settings")]
    [SerializeField] private int dealsCount = 2;
    [SerializeField] private float minDiscountPercent = 10f;
    [SerializeField] private float maxDiscountPercent = 50f;
    [SerializeField] private int minAvatarPrice = 100;
    [SerializeField] private int maxAvatarPrice = 2000;
    [SerializeField] private bool useGemListForPersonalGifts = true;
    
    [Header("Daily Gift Settings")]
    [SerializeField] private int[] possibleGemGifts = { 5, 10, 15, 20, 25, 40, 50 };
    [SerializeField] private Sprite gemSprite;
    
    [Header("Player-Specific Settings")]
    [SerializeField] private bool usePlayerSpecificGifts = true; // Включить индивидуальные подарки
    [SerializeField] private int minGiftAmount = 5;
    [SerializeField] private int maxGiftAmount = 100;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool forceRefreshOnStart = false;
    
    // Приватные данные
    private List<DailyDeal> currentDeals = new List<DailyDeal>();
    private DailyGift dailyGift;
    private DateTime lastRefreshDate;
    private string playerUID; // Уникальный ID текущего игрока
    
    // Ключи для сохранения
    private const string LAST_REFRESH_KEY = "DailyDeals_LastRefresh";
    private const string DEALS_DATA_KEY = "DailyDeals_Data";
    private const string GIFT_DATA_KEY = "DailyDeals_Gift";
    private const string LAST_CLAIM_DATE_KEY = "DailyDeals_LastClaimDate";
    private const string PLAYER_UID_KEY = "DailyDeals_PlayerUID";
    
    // Киевский часовой пояс
    private static readonly TimeZoneInfo kievTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
    
    public static DailyDealsManager Instance { get; private set; }
    
    // События
    public event System.Action OnDealsRefreshed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayerUID();
            LoadSavedDeals();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ValidateLoadedData();
        
        if (forceRefreshOnStart)
        {
            RefreshDeals();
        }
        else
        {
            CheckAndRefreshDeals();
        }
        
        InvokeRepeating(nameof(CheckAndRefreshDeals), 60f, 60f);
    }

    #region Player UID Management

    private void InitializePlayerUID()
    {
        // Проверяем, есть ли уже сохраненный UID
        playerUID = PlayerPrefs.GetString(PLAYER_UID_KEY, "");
        
        if (string.IsNullOrEmpty(playerUID))
        {
            // Создаем новый уникальный ID для игрока
            playerUID = GeneratePlayerUID();
            PlayerPrefs.SetString(PLAYER_UID_KEY, playerUID);
            PlayerPrefs.Save();
            
            if (enableDebugLogs)
            {
                Debug.Log($"DailyDeals: Создан новый Player UID: {playerUID}");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"DailyDeals: Загружен Player UID: {playerUID}");
            }
        }
    }

    private string GeneratePlayerUID()
    {
        // Создаем уникальный ID на основе времени, случайных чисел и системной информации
        var sb = new StringBuilder();
        
        // Текущее время в тиках
        sb.Append(DateTime.UtcNow.Ticks.ToString("X"));
        
        // Случайное число
        sb.Append(UnityEngine.Random.Range(1000, 9999).ToString("X"));
        
        // Хэш от системной информации (более стабильный между сессиями)
        string systemInfo = SystemInfo.deviceModel + SystemInfo.deviceName + SystemInfo.deviceUniqueIdentifier;
        sb.Append(systemInfo.GetHashCode().ToString("X"));
        
        return sb.ToString();
    }

    public string GetPlayerUID()
    {
        return playerUID;
    }

    #endregion

    #region Data Validation

    private void ValidateLoadedData()
    {
        DateTime kievToday = GetKievTime().Date;
        string todayString = kievToday.ToString("yyyy-MM-dd");
        
        // Проверяем, соответствует ли загруженный подарок текущей дате и игроку
        if (dailyGift != null)
        {
            bool isOutdated = string.IsNullOrEmpty(dailyGift.dateGenerated) || dailyGift.dateGenerated != todayString;
            bool isWrongPlayer = usePlayerSpecificGifts && dailyGift.playerUID != playerUID;
            
            if (isOutdated || isWrongPlayer)
            {
                if (enableDebugLogs)
                {
                    string reason = isOutdated ? "устарел" : "принадлежит другому игроку";
                    Debug.Log($"DailyDeals: Подарок {reason}. Сбрасываем.");
                }
                dailyGift = null;
            }
        }
        
        // Проверяем дату последнего забора подарка
        string lastClaimDateStr = PlayerPrefs.GetString(LAST_CLAIM_DATE_KEY, "");
        if (!string.IsNullOrEmpty(lastClaimDateStr) && lastClaimDateStr == todayString)
        {
            if (dailyGift != null && !dailyGift.isClaimed)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("DailyDeals: Обнаружено, что подарок уже забирался сегодня. Помечаем как забранный.");
                }
                dailyGift.isClaimed = true;
            }
        }
    }

    #endregion

    #region Daily Deals Logic

    public void CheckAndRefreshDeals()
    {
        DateTime kievNow = GetKievTime();
        DateTime todayMidnight = kievNow.Date;
        
        if (enableDebugLogs)
        {
            Debug.Log($"DailyDeals: Проверка времени. Киев: {kievNow:yyyy-MM-dd HH:mm:ss}");
            Debug.Log($"DailyDeals: Последнее обновление: {lastRefreshDate:yyyy-MM-dd HH:mm:ss}");
        }
        
        if (lastRefreshDate < todayMidnight || currentDeals.Count == 0 || dailyGift == null)
        {
            RefreshDeals();
        }
    }

    public void RefreshDeals()
    {
        if (enableDebugLogs)
            Debug.Log("DailyDeals: Обновляем ежедневные предложения...");
        
        currentDeals.Clear();
        
        // Создаем персональный ежедневный подарок
        GeneratePersonalDailyGift();
        
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("DailyDeals: AvatarManager недоступен!");
            return;
        }
        
        // Создаем персональные предложения по аватарам
        GeneratePersonalDeals();
        
        // Сохраняем время обновления
        lastRefreshDate = GetKievTime();
        SaveDeals();
        
        // Уведомляем о обновлении
        OnDealsRefreshed?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log($"DailyDeals: Создано {currentDeals.Count} ежедневных предложений и персональный подарок на {dailyGift.gemAmount} гемов");
    }

    private void GeneratePersonalDailyGift()
    {
        DateTime kievToday = GetKievTime().Date;
        string todayString = kievToday.ToString("yyyy-MM-dd");
        
        // Проверяем, есть ли уже подарок на сегодня для этого игрока
        if (dailyGift != null && dailyGift.dateGenerated == todayString && 
            (!usePlayerSpecificGifts || dailyGift.playerUID == playerUID))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"DailyDeals: Персональный подарок на сегодня уже существует ({dailyGift.gemAmount} гемов, забран: {dailyGift.isClaimed})");
            }
            return;
        }
        
        int gemAmount;
        
        if (usePlayerSpecificGifts)
        {
            // Генерируем персональное количество гемов на основе UID игрока и даты
            gemAmount = GeneratePersonalGemAmount(playerUID, todayString);
        }
        else
        {
            // Используем старую систему с предопределенными вариантами
            int seed = kievToday.Year * 10000 + kievToday.Month * 100 + kievToday.Day + 999;
            UnityEngine.Random.InitState(seed);
            gemAmount = possibleGemGifts[UnityEngine.Random.Range(0, possibleGemGifts.Length)];
        }
        
        dailyGift = new DailyGift(gemAmount, playerUID);
        
        // Проверяем, забирался ли уже подарок сегодня
        string lastClaimDateStr = PlayerPrefs.GetString(LAST_CLAIM_DATE_KEY, "");
        if (lastClaimDateStr == todayString)
        {
            dailyGift.isClaimed = true;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"DailyDeals: Сгенерирован персональный подарок - {gemAmount} гемов для игрока {playerUID.Substring(0, 8)}... (дата: {todayString})");
        }
    }

private int GeneratePersonalGemAmount(string playerUID, string dateString)
{
    // Создаем уникальный seed на основе UID игрока и даты
    int playerHashCode = playerUID.GetHashCode();
    int dateHashCode = dateString.GetHashCode();
    
    // Комбинируем хэши для получения уникального seed'а
    int combinedSeed = playerHashCode ^ dateHashCode;
    
    // Используем абсолютное значение для избежания отрицательных чисел
    combinedSeed = Mathf.Abs(combinedSeed);
    
    UnityEngine.Random.InitState(combinedSeed);
    
    int gemAmount;
    
    // ВЫБОР СИСТЕМЫ: список или диапазон
    if (useGemListForPersonalGifts && possibleGemGifts != null && possibleGemGifts.Length > 0)
    {
        // Выбираем из предопределенного списка
        int randomIndex = UnityEngine.Random.Range(0, possibleGemGifts.Length);
        gemAmount = possibleGemGifts[randomIndex];
        
        if (enableDebugLogs)
        {
            Debug.Log($"DailyDeals: Выбрано {gemAmount} гемов из списка (индекс {randomIndex}/{possibleGemGifts.Length})");
        }
    }
    else
    {
        // Генерируем случайное количество гемов в заданном диапазоне
        gemAmount = UnityEngine.Random.Range(minGiftAmount, maxGiftAmount + 1);
        
        if (enableDebugLogs)
        {
            Debug.Log($"DailyDeals: Сгенерировано {gemAmount} гемов из диапазона [{minGiftAmount}-{maxGiftAmount}]");
        }
    }
    
    // Можно добавить систему "удачных" дней (например, каждый 7-й день больше гемов)
    if (IsLuckyDay(combinedSeed))
    {
        if (useGemListForPersonalGifts && possibleGemGifts != null && possibleGemGifts.Length > 1)
        {
            // Для удачного дня выбираем элемент из верхней половины списка
            var sortedGifts = possibleGemGifts.OrderBy(x => x).ToArray();
            int upperHalfStart = sortedGifts.Length / 2;
            int luckyIndex = UnityEngine.Random.Range(upperHalfStart, sortedGifts.Length);
            gemAmount = sortedGifts[luckyIndex];
            
            if (enableDebugLogs)
            {
                Debug.Log($"DailyDeals: 🍀 УДАЧНЫЙ ДЕНЬ! Выбрано из топ-половины списка: {gemAmount} гемов");
            }
        }
        else
        {
            // Fallback на увеличение в 1.5 раза для диапазона
            gemAmount = Mathf.RoundToInt(gemAmount * 1.5f);
            gemAmount = Mathf.Min(gemAmount, maxGiftAmount * 2);
            
            if (enableDebugLogs)
            {
                Debug.Log($"DailyDeals: 🍀 УДАЧНЫЙ ДЕНЬ! Увеличен бонус: {gemAmount} гемов");
            }
        }
    }
    
    return gemAmount;
}

    private bool IsLuckyDay(int seed)
    {
        // 15% шанс на удачный день (можно настроить)
        UnityEngine.Random.InitState(seed + 12345); // Дополнительное смещение seed'а
        return UnityEngine.Random.Range(0f, 1f) < 0.15f;
    }

    private void GeneratePersonalDeals()
    {
        List<int> eligibleAvatars = GetEligibleAvatarsForDeals();
        
        if (eligibleAvatars.Count < dealsCount)
        {
            Debug.LogWarning($"DailyDeals: Недостаточно подходящих аватаров. Найдено: {eligibleAvatars.Count}, нужно: {dealsCount}");
        }
        
        // Выбираем персональные аватары для игрока
        List<int> selectedAvatars = SelectPersonalAvatars(eligibleAvatars, dealsCount);
        
        // Создаем предложения со скидками
        foreach (int avatarIndex in selectedAvatars)
        {
            var avatarData = AvatarManager.Instance.GetAvatarData(avatarIndex);
            if (avatarData != null)
            {
                // Генерируем персональную скидку
                float discountPercent = GeneratePersonalDiscount(avatarIndex);
                
                int originalPrice = avatarData.gemCost;
                int discountedPrice = Mathf.RoundToInt(originalPrice * (1f - discountPercent / 100f));
                discountedPrice = Mathf.Max(1, discountedPrice);
                
                DailyDeal deal = new DailyDeal(avatarIndex, originalPrice, discountedPrice);
                currentDeals.Add(deal);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"DailyDeals: Персональное предложение - {avatarData.name}: {originalPrice} → {discountedPrice} гемов (-{discountPercent:F1}%)");
                }
            }
        }
    }

    private List<int> SelectPersonalAvatars(List<int> eligibleAvatars, int count)
    {
        // Создаем персональный seed на основе UID игрока и даты
        DateTime today = GetKievTime().Date;
        int playerHash = playerUID.GetHashCode();
        int dateHash = (today.Year * 10000 + today.Month * 100 + today.Day).GetHashCode();
        int personalSeed = Mathf.Abs(playerHash ^ dateHash);
        
        UnityEngine.Random.InitState(personalSeed);
        
        List<int> available = new List<int>(eligibleAvatars);
        List<int> selected = new List<int>();
        
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, available.Count);
            selected.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }
        
        return selected;
    }

    private float GeneratePersonalDiscount(int avatarIndex)
    {
        // Генерируем персональную скидку на основе UID игрока и индекса аватара
        int playerHash = playerUID.GetHashCode();
        int avatarHash = avatarIndex.GetHashCode();
        int discountSeed = Mathf.Abs(playerHash ^ avatarHash);
        
        UnityEngine.Random.InitState(discountSeed);
        
        return UnityEngine.Random.Range(minDiscountPercent, maxDiscountPercent);
    }

    private List<int> GetEligibleAvatarsForDeals()
    {
        List<int> eligibleAvatars = new List<int>();
        
        for (int i = 0; i < AvatarManager.Instance.GetAvatarCount(); i++)
        {
            var avatarData = AvatarManager.Instance.GetAvatarData(i);
            
            if (avatarData != null && 
                !avatarData.isDefault &&
                avatarData.gemCost >= minAvatarPrice && 
                avatarData.gemCost <= maxAvatarPrice &&
                !AvatarManager.Instance.IsAvatarUnlocked(i))
            {
                eligibleAvatars.Add(i);
            }
        }
        
        return eligibleAvatars;
    }

    #endregion

    #region Purchase Logic

    public bool PurchaseDailyDeal(int avatarIndex)
    {
        var deal = currentDeals.Find(d => d.avatarIndex == avatarIndex);
        if (deal == null)
        {
            Debug.LogWarning($"DailyDeals: Предложение для аватара {avatarIndex} не найдено!");
            return false;
        }
        
        if (deal.isPurchased)
        {
            Debug.LogWarning($"DailyDeals: Предложение для аватара {avatarIndex} уже куплено!");
            return false;
        }
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("DailyDeals: CurrencyManager недоступен!");
            return false;
        }
        
        var avatarData = AvatarManager.Instance.GetAvatarData(avatarIndex);
        if (avatarData == null)
        {
            Debug.LogError($"DailyDeals: Данные аватара {avatarIndex} не найдены!");
            return false;
        }
        
        if (CurrencyManager.Instance.SpendGems(deal.discountedPrice, $"Daily Deal: {avatarData.name}"))
        {
            AvatarManager.Instance.UnlockAvatar(avatarIndex);
            deal.isPurchased = true;
            SaveDeals();
            
            if (enableDebugLogs)
            {
                Debug.Log($"DailyDeals: Куплен {avatarData.name} за {deal.discountedPrice} гемов (персональная скидка {deal.discountPercent:F1}%)");
            }
            
            return true;
        }
        
        return false;
    }

    public bool CanPurchaseDailyDeal(int avatarIndex)
    {
        var deal = currentDeals.Find(d => d.avatarIndex == avatarIndex);
        if (deal == null || deal.isPurchased) return false;
        
        if (CurrencyManager.Instance == null) return false;
        
        return CurrencyManager.Instance.HasEnoughGems(deal.discountedPrice) && 
               !AvatarManager.Instance.IsAvatarUnlocked(avatarIndex);
    }

    #endregion

    #region Daily Gift Logic

    public bool ClaimDailyGift()
    {
        if (dailyGift == null)
        {
            Debug.LogWarning("DailyDeals: Персональный ежедневный подарок недоступен!");
            return false;
        }
        
        if (dailyGift.isClaimed)
        {
            Debug.LogWarning("DailyDeals: Персональный ежедневный подарок уже забран!");
            return false;
        }
        
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("DailyDeals: CurrencyManager недоступен!");
            return false;
        }
        
        // Двойная проверка
        DateTime kievToday = GetKievTime().Date;
        string todayString = kievToday.ToString("yyyy-MM-dd");
        string lastClaimDateStr = PlayerPrefs.GetString(LAST_CLAIM_DATE_KEY, "");
        
        if (lastClaimDateStr == todayString)
        {
            Debug.LogWarning("DailyDeals: Персональный подарок уже забирался сегодня!");
            dailyGift.isClaimed = true;
            SaveDeals();
            return false;
        }
        
        // Добавляем персональное количество гемов
        CurrencyManager.Instance.AddGems(dailyGift.gemAmount, "Personal Daily Gift");
        dailyGift.isClaimed = true;
        
        // Сохраняем дату забора
        PlayerPrefs.SetString(LAST_CLAIM_DATE_KEY, todayString);
        PlayerPrefs.Save();
        
        SaveDeals();
        
        if (enableDebugLogs)
        {
            Debug.Log($"DailyDeals: Забран персональный ежедневный подарок - {dailyGift.gemAmount} гемов! (игрок: {playerUID.Substring(0, 8)}...)");
        }
        
        return true;
    }

    public bool CanClaimDailyGift()
    {
        if (dailyGift == null || dailyGift.isClaimed)
            return false;
            
        DateTime kievToday = GetKievTime().Date;
        string todayString = kievToday.ToString("yyyy-MM-dd");
        string lastClaimDateStr = PlayerPrefs.GetString(LAST_CLAIM_DATE_KEY, "");
        
        return lastClaimDateStr != todayString;
    }

    public DailyGift GetDailyGift()
    {
        return dailyGift;
    }

    public Sprite GetGemSprite()
    {
        return gemSprite;
    }

    #endregion

    #region Data Access

    public List<DailyDeal> GetCurrentDeals()
    {
        return new List<DailyDeal>(currentDeals);
    }

    public DailyDeal GetDealForAvatar(int avatarIndex)
    {
        return currentDeals.Find(d => d.avatarIndex == avatarIndex);
    }

    public DateTime GetNextRefreshTime()
    {
        DateTime kievNow = GetKievTime();
        DateTime nextMidnight = kievNow.Date.AddDays(1);
        return nextMidnight;
    }

    public TimeSpan GetTimeUntilRefresh()
    {
        DateTime nextRefresh = GetNextRefreshTime();
        DateTime kievNow = GetKievTime();
        return nextRefresh - kievNow;
    }

    #endregion

    #region Time Management

    private DateTime GetKievTime()
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kievTimeZone);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(2);
        }
    }

    #endregion

    #region Data Persistence

    private void SaveDeals()
    {
        try
        {
            string lastRefreshStr = lastRefreshDate.ToBinary().ToString();
            PlayerPrefs.SetString(LAST_REFRESH_KEY, lastRefreshStr);
            
            List<string> dealsData = new List<string>();
            foreach (var deal in currentDeals)
            {
                string dealStr = $"{deal.avatarIndex}|{deal.originalPrice}|{deal.discountedPrice}|{deal.isPurchased}";
                dealsData.Add(dealStr);
            }
            
            string dealsJson = string.Join(";", dealsData);
            PlayerPrefs.SetString(DEALS_DATA_KEY, dealsJson);
            
            if (dailyGift != null)
            {
                string giftData = $"{dailyGift.gemAmount}|{dailyGift.isClaimed}|{dailyGift.dateGenerated}|{dailyGift.playerUID}";
                PlayerPrefs.SetString(GIFT_DATA_KEY, giftData);
            }
            
            PlayerPrefs.Save();
            
            if (enableDebugLogs)
                Debug.Log("DailyDeals: Персональные данные сохранены");
        }
        catch (Exception ex)
        {
            Debug.LogError($"DailyDeals: Ошибка сохранения данных: {ex.Message}");
        }
    }

    private void LoadSavedDeals()
    {
        try
        {
            string lastRefreshStr = PlayerPrefs.GetString(LAST_REFRESH_KEY, "");
            if (!string.IsNullOrEmpty(lastRefreshStr))
            {
                long lastRefreshBinary = Convert.ToInt64(lastRefreshStr);
                lastRefreshDate = DateTime.FromBinary(lastRefreshBinary);
            }
            else
            {
                lastRefreshDate = DateTime.MinValue;
            }
            
            string dealsJson = PlayerPrefs.GetString(DEALS_DATA_KEY, "");
            if (!string.IsNullOrEmpty(dealsJson))
            {
                string[] dealsArray = dealsJson.Split(';');
                currentDeals.Clear();
                
                foreach (string dealStr in dealsArray)
                {
                    if (string.IsNullOrEmpty(dealStr)) continue;
                    
                    string[] parts = dealStr.Split('|');
                    if (parts.Length == 4)
                    {
                        int avatarIndex = int.Parse(parts[0]);
                        int originalPrice = int.Parse(parts[1]);
                        int discountedPrice = int.Parse(parts[2]);
                        bool isPurchased = bool.Parse(parts[3]);
                        
                        DailyDeal deal = new DailyDeal(avatarIndex, originalPrice, discountedPrice);
                        deal.isPurchased = isPurchased;
                        currentDeals.Add(deal);
                    }
                }
            }
            
            string giftData = PlayerPrefs.GetString(GIFT_DATA_KEY, "");
            if (!string.IsNullOrEmpty(giftData))
            {
                string[] parts = giftData.Split('|');
                if (parts.Length >= 3)
                {
                    int gemAmount = int.Parse(parts[0]);
                    bool isClaimed = bool.Parse(parts[1]);
                    string dateGenerated = parts[2];
                    string giftPlayerUID = parts.Length > 3 ? parts[3] : playerUID; // Обратная совместимость
                    
                    dailyGift = new DailyGift(gemAmount, dateGenerated, giftPlayerUID);
                    dailyGift.isClaimed = isClaimed;
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"DailyDeals: Загружено {currentDeals.Count} персональных предложений и подарок {dailyGift?.gemAmount ?? 0} гемов для игрока {playerUID.Substring(0, 8)}...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"DailyDeals: Ошибка загрузки данных: {ex.Message}");
            currentDeals.Clear();
            dailyGift = null;
            lastRefreshDate = DateTime.MinValue;
        }
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Force Refresh Deals")]
    public void ForceRefreshDeals()
    {
        RefreshDeals();
        Debug.Log("DailyDeals: Принудительное обновление персональных предложений выполнено");
    }

    [ContextMenu("Reset Daily Gift")]
    public void ResetDailyGift()
    {
        PlayerPrefs.DeleteKey(LAST_CLAIM_DATE_KEY);
        PlayerPrefs.DeleteKey(GIFT_DATA_KEY);
        dailyGift = null;
        GeneratePersonalDailyGift();
        SaveDeals();
        Debug.Log("DailyDeals: Персональный ежедневный подарок сброшен");
    }

    [ContextMenu("Generate New Player UID")]
    public void GenerateNewPlayerUID()
    {
        PlayerPrefs.DeleteKey(PLAYER_UID_KEY);
        InitializePlayerUID();
        RefreshDeals();
        Debug.Log($"DailyDeals: Создан новый Player UID: {playerUID}");
    }

    [ContextMenu("Debug Current Time")]
    public void DebugCurrentTime()
    {
        DateTime utc = DateTime.UtcNow;
        DateTime kiev = GetKievTime();
        DateTime nextRefresh = GetNextRefreshTime();
        TimeSpan timeUntil = GetTimeUntilRefresh();
        
        Debug.Log($"UTC: {utc:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"Киев: {kiev:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"Следующее обновление: {nextRefresh:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"До обновления: {timeUntil.Hours}ч {timeUntil.Minutes}м");
    }

    [ContextMenu("Debug Current Deals")]
    public void DebugCurrentDeals()
    {
        Debug.Log($"=== ТЕКУЩИЕ DAILY DEALS ({currentDeals.Count}) ===");
        
        if (dailyGift != null)
        {
            Debug.Log($"[ПОДАРОК] {dailyGift.gemAmount} гемов | Забран: {dailyGift.isClaimed}");
        }
        
        foreach (var deal in currentDeals)
        {
            var avatarData = AvatarManager.Instance?.GetAvatarData(deal.avatarIndex);
            string avatarName = avatarData?.name ?? "Unknown";
            
            Debug.Log($"[{deal.avatarIndex}] {avatarName}: {deal.originalPrice} → {deal.discountedPrice} гемов " +
                     $"(-{deal.discountPercent:F1}%) | Куплено: {deal.isPurchased}");
        }
    }

    #endregion
}