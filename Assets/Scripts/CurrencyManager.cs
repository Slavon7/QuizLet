using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class CurrencyChangeEvent : UnityEvent<int, int> { } // old value, new value

public class CurrencyManager : MonoBehaviourPun, IPunObservable
{
    [Header("Currency Settings")]
    [SerializeField] private int coins = 0;
    [SerializeField] private int gems = 0;
    
    [Header("UI Elements")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gemsText;
    
    [Header("Events")]
    public CurrencyChangeEvent OnCoinsChanged;
    public CurrencyChangeEvent OnGemsChanged;
    public UnityEvent<string> OnCurrencyError; // для показа ошибок
    
    [Header("Anti-Cheat Settings")]
    [SerializeField] private bool enableAntiCheat = true;
    [SerializeField] private int maxCurrencyChange = 10000; // максимальное изменение за раз
    
    // Для синхронизации через Photon
    private int networkCoins;
    private int networkGems;
    
    // Система проверки на читерство
    private Dictionary<string, int> lastTransactionIds = new Dictionary<string, int>();
    private float lastSyncTime;
    private const float SYNC_INTERVAL = 5f; // синхронизация каждые 5 секунд
    
    public static CurrencyManager Instance { get; private set; }
    
    // Properties для безопасного доступа
    public int Coins 
    { 
        get => coins; 
        private set 
        { 
            int oldValue = coins;
            coins = value;
            OnCoinsChanged?.Invoke(oldValue, coins);
            UpdateUI();
            SaveToPlayerPrefs();
        } 
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();
        UpdateUI();
    }

    
    public int Gems
    {
        get => gems;
        private set
        {
            int oldValue = gems;
            gems = value;
            OnGemsChanged?.Invoke(oldValue, gems);
            UpdateUI();
            SaveToPlayerPrefs();
        }
    }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromPlayerPrefs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindUIReferences();
        UpdateUI();
        SyncWithNetwork();
    }

    void Update()
    {
        // Периодическая синхронизация с сервером
        if (Time.time - lastSyncTime > SYNC_INTERVAL)
        {
            SyncWithNetwork();
            lastSyncTime = Time.time;
        }
    }

    #region Currency Operations
    
    public bool AddCoins(int amount, string reason = "")
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Попытка добавить неположительное количество монет");
            return false;
        }
        
        if (enableAntiCheat && amount > maxCurrencyChange)
        {
            Debug.LogError($"Подозрительная попытка добавить {amount} монет. Максимум: {maxCurrencyChange}");
            OnCurrencyError?.Invoke("Обнаружена подозрительная активность");
            return false;
        }
        
        Coins += amount;
        Debug.Log($"Добавлено {amount} монет. Причина: {reason}");
        
        // Отправляем изменение через Photon для синхронизации
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("SyncCurrencyChange", RpcTarget.Others, "coins", amount, reason);
        }
        
        return true;
    }
    
    public bool SpendCoins(int amount, string reason = "")
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Попытка потратить неположительное количество монет");
            return false;
        }
        
        if (coins < amount)
        {
            Debug.Log($"Недостаточно монет. Требуется: {amount}, Доступно: {coins}");
            OnCurrencyError?.Invoke("Недостаточно монет");
            return false;
        }
        
        Coins -= amount;
        Debug.Log($"Потрачено {amount} монет. Причина: {reason}");
        
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("SyncCurrencyChange", RpcTarget.Others, "coins", -amount, reason);
        }
        
        return true;
    }
    
    public RewardAnimationEffects rewardEffect;

    public bool AddGems(int amount, string reason = "")
    {
        if (amount <= 0) return false;

        if (enableAntiCheat && amount > maxCurrencyChange)
        {
            Debug.LogError($"Подозрительная попытка добавить {amount} гемов");
            OnCurrencyError?.Invoke("Обнаружена подозрительная активность");
            return false;
        }

        int previousGems = Gems;
        Gems += amount;
        Debug.Log($"Добавлено {amount} гемов. Причина: {reason}");

        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("SyncCurrencyChange", RpcTarget.Others, "gems", amount, reason);
        }
        else if (PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("photonView не инициализирован!");
        }

        if (rewardEffect != null && gemsText != null)
        {
            rewardEffect.StopAllCoroutines(); // Остановить любую предыдущую анимацию
            rewardEffect.StartCoroutine(rewardEffect.AnimateCounterWithSteps(
                previousGems,
                Gems,
                1.5f,
                (val) => gemsText.text = val.ToString()
            ));
        }
        else
        {
            if (rewardEffect == null) Debug.LogWarning("rewardEffect не назначен!");
            if (gemsText == null) Debug.LogWarning("gemsText не назначен!");

            if (gemsText != null) gemsText.text = Gems.ToString();
        }

        return true;
    }
    
    public bool SpendGems(int amount, string reason = "")
    {
        if (amount <= 0) 
        {
            Debug.LogWarning("Попытка потратить неположительное количество гемов");
            return false;
        }
        
        if (gems < amount)
        {
            Debug.Log($"Недостаточно гемов. Требуется: {amount}, Доступно: {gems}");
            
            // Проверяем что OnCurrencyError не null перед вызовом
            OnCurrencyError?.Invoke("Недостаточно гемов");
            
            return false;
        }
        
        Gems -= amount;
        Debug.Log($"Потрачено {amount} гемов. Причина: {reason}");
        
        // Проверяем подключение к Photon и наличие photonView
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RPC("SyncCurrencyChange", RpcTarget.Others, "gems", -amount, reason);
        }
        else
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogWarning("Не подключен к Photon Network при попытке синхронизации валюты");
            }
            if (photonView == null)
            {
                Debug.LogWarning("photonView равен null при попытке синхронизации валюты");
            }
        }
        
        return true;
    }
    
    private void FindUIReferences()
    {
        coinsText = GameObject.Find("CoinsText")?.GetComponent<TextMeshProUGUI>();
        gemsText = GameObject.Find("GemsText")?.GetComponent<TextMeshProUGUI>();

        if (coinsText == null)
            Debug.LogWarning("Не найден coinsText в сцене");
        if (gemsText == null)
            Debug.LogWarning("Не найден gemsText в сцене");
    }

    
    #endregion

    #region Photon Integration

    [PunRPC]
    void SyncCurrencyChange(string currencyType, int amount, string reason)
    {
        // Этот RPC получают другие игроки для синхронизации
        Debug.Log($"Получено изменение валюты от другого игрока: {currencyType} {amount} ({reason})");
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправляем данные о валюте
            stream.SendNext(coins);
            stream.SendNext(gems);
        }
        else
        {
            // Получаем данные о валюте от других игроков
            networkCoins = (int)stream.ReceiveNext();
            networkGems = (int)stream.ReceiveNext();
        }
    }
    
    private void SyncWithNetwork()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer.CustomProperties != null)
        {
            // Синхронизируем валюту через CustomProperties для долгосрочного хранения
            var props = new Hashtable();
            props["PlayerCoins"] = coins;
            props["PlayerGems"] = gems;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }
    
    #endregion

    #region Data Persistence
    
    private void LoadFromPlayerPrefs()
    {
        coins = PlayerPrefs.GetInt("PlayerCoins", 0);
        gems = PlayerPrefs.GetInt("PlayerGems", 0);
        
        // Дополнительная проверка на целостность данных
        if (enableAntiCheat)
        {
            int savedHash = PlayerPrefs.GetInt("CurrencyHash", 0);
            int currentHash = (coins * 7 + gems * 13) % 100000; // Простая хеш-функция
            
            if (savedHash != 0 && savedHash != currentHash)
            {
                Debug.LogWarning("Обнаружено изменение валютных данных!");
                // Можно сбросить валюту или запросить у сервера
            }
        }
    }
    
    private void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("PlayerCoins", coins);
        PlayerPrefs.SetInt("PlayerGems", gems);
        
        if (enableAntiCheat)
        {
            int hash = (coins * 7 + gems * 13) % 100000;
            PlayerPrefs.SetInt("CurrencyHash", hash);
        }
        
        PlayerPrefs.Save();
    }
    
    #endregion

    #region UI Updates
    
    private void UpdateUI()
    {
        if (coinsText != null)
            coinsText.text = FormatCurrency(coins);
            
        if (gemsText != null)
            gemsText.text = FormatCurrency(gems);
    }
    
    private string FormatCurrency(int amount)
    {
        if (amount >= 1000000)
            return $"{amount / 1000000f:F1}M";
        else if (amount >= 1000)
            return $"{amount / 1000f:F1}K";
        else
            return amount.ToString();
    }
    
    #endregion

    #region Public Utility Methods
    
    public bool HasEnoughCoins(int amount) => coins >= amount;
    public bool HasEnoughGems(int amount) => gems >= amount;
    
    // ✅ ДОБАВЛЕННЫЕ ГЕТТЕРЫ:
    public int GetCoins() => coins;
    public int GetGems() => gems;
    
    public void ResetCurrency()
    {
        Coins = 0;
        Gems = 0;
        Debug.Log("Валюта сброшена");
    }
    
    // Метод для админов/отладки
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void AddCurrencyDebug(int coinsAmount, int gemsAmount)
    {
        AddCoins(coinsAmount, "Debug");
        AddGems(gemsAmount, "Debug");
    }
    
    #endregion
}