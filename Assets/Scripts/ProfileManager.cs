using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ProfileManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nicknameInputField;
    public TextMeshProUGUI currentNicknameText;
    public Image avatarImage;

    [Header("Level & XP UI")]
    public Slider experienceSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI experienceText; // Новое поле для отображения опыта

    [Header("Level Up Rewards")]
    public GameObject levelUpPanel; // Панель с наградой за уровень
    public TextMeshProUGUI levelUpText; // "Новый уровень!"
    public TextMeshProUGUI gemRewardText; // Текст с количеством гемов
    public float rewardAnimationDuration = 2f; // Длительность анимации счетчика

    public static ProfileManager Instance { get; private set; }

    private string currentNickname;
    private Sprite currentAvatar;
    private int currentAvatarIndex = 0;
    private string[] defaultNicknames = new string[]
    {
        "SpeedyFox", "MightyBear", "SilentWolf", "CleverOwl", "BraveLion",
        "WildHawk", "SwiftTiger", "GentleDeer", "FierceEagle", "PlayfulPanda"
    };

    private int currentLevel = 1;
    private int currentXP = 0;
    private int xpToLevelUp = 5000;

    private int pendingGemReward = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (PlayerPrefs.HasKey("AvatarIndex"))
        {
            currentAvatarIndex = PlayerPrefs.GetInt("AvatarIndex");
            Debug.Log("Awake: Загружен индекс аватара: " + currentAvatarIndex);
        }

        InitializeDefaultAvatars();

        // Загружаем уровень и опыт из PlayerPrefs (если есть)
        if (PlayerPrefs.HasKey("CurrentLevel"))
            currentLevel = PlayerPrefs.GetInt("CurrentLevel");

        if (PlayerPrefs.HasKey("CurrentXP"))
            currentXP = PlayerPrefs.GetInt("CurrentXP");
    }

    void Start()
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager не найден! Убедитесь, что создали ScriptableObject и поместили его в Resources");
            return;
        }

        ValidateCurrentAvatar();

        if (PlayerPrefs.HasKey("Nickname"))
        {
            currentNickname = PlayerPrefs.GetString("Nickname");
        }
        else
        {
            currentNickname = GenerateRandomNickname();
            PlayerPrefs.SetString("Nickname", currentNickname);
        }

        PhotonNetwork.NickName = currentNickname;
        nicknameInputField.text = currentNickname;

        if (PlayerPrefs.HasKey("AvatarIndex"))
        {
            currentAvatarIndex = PlayerPrefs.GetInt("AvatarIndex");
            currentAvatar = AvatarManager.Instance.GetAvatar(currentAvatarIndex);
            SyncPlayerCustomProperties();
        }
        else
        {
            currentAvatarIndex = 0;
            currentAvatar = AvatarManager.Instance.GetAvatar(0);
            PlayerPrefs.SetInt("AvatarIndex", 0);
        }

        // Скрываем панель награды изначально
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        if (AvatarManager.Instance != null)
        {
            Debug.Log($"Количество аватаров в AvatarManager: {AvatarManager.Instance.GetAvatarCount()}");
            
            for (int i = 0; i < AvatarManager.Instance.GetAvatarCount(); i++)
            {
                var sprite = AvatarManager.Instance.GetAvatar(i);
                var data = AvatarManager.Instance.GetAvatarData(i);
                
                Debug.Log($"Индекс {i}: Спрайт = {(sprite ? sprite.name : "NULL")}, " +
                        $"Данные = {(data != null ? data.name : "NULL")}");
            }
        }

        AddPendingExperience();
        UpdateProfileUI();
    }

    /// <summary>
    /// Инициализация дефолтных аватаров при первом запуске
    /// </summary>
    private void InitializeDefaultAvatars()
    {
        if (!PlayerPrefs.HasKey("AvatarsInitialized"))
        {
            if (AvatarManager.Instance != null)
            {
                // Разблокируем все дефолтные аватары
                var allAvatars = AvatarManager.Instance.GetAllAvatarData();
                for (int i = 0; i < allAvatars.Length; i++)
                {
                    if (allAvatars[i] != null && allAvatars[i].isDefault)
                    {
                        AvatarManager.Instance.UnlockAvatar(i);
                    }
                }

                PlayerPrefs.SetInt("AvatarsInitialized", 1);
                PlayerPrefs.Save();
                Debug.Log("Default avatars initialized");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            AddTestLevel(1); // Добавляем +1 уровень по нажатию L
        }
    }

    public void OnEnable()
    {
        // Подписываемся на событие успешного подключения к серверу
        PhotonNetwork.NetworkingClient.StateChanged += NetworkingClientOnStateChanged;
    }

    public void OnDisable()
    {
        // Отписываемся при уничтожении объекта
        PhotonNetwork.NetworkingClient.StateChanged -= NetworkingClientOnStateChanged;
    }

    private void NetworkingClientOnStateChanged(Photon.Realtime.ClientState previousState, Photon.Realtime.ClientState currentState)
    {
        // Когда соединение успешно установлено, синхронизируем свойства
        if (currentState == Photon.Realtime.ClientState.JoinedLobby ||
            currentState == Photon.Realtime.ClientState.Joined)
        {
            SyncPlayerCustomProperties();
        }
    }

    private void SyncPlayerCustomProperties()
    {
        if (PhotonNetwork.IsConnected)
        {
            // Синхронизируем как никнейм, так и индекс аватара
            Hashtable props = new Hashtable();
            props.Add("Nickname", currentNickname);
            props.Add("AvatarIndex", currentAvatarIndex);

            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log($"SyncPlayerCustomProperties: установлены свойства - Nickname: {currentNickname}, AvatarIndex: {currentAvatarIndex}");
        }
        else
        {
            Debug.LogWarning("SyncPlayerCustomProperties: не подключен к серверу Photon, нельзя синхронизировать свойства");
        }
    }

    public void ChangeNickname()
    {
        string newNickname = nicknameInputField.text;

        if (!string.IsNullOrEmpty(newNickname))
        {
            currentNickname = newNickname;
            PlayerPrefs.SetString("Nickname", currentNickname);
            PhotonNetwork.NickName = currentNickname; // Обновляем никнейм в Photon

            SyncPlayerCustomProperties(); // Используем общий метод синхронизации

            UpdateProfileUI();
            Debug.Log("Никнейм изменён на: " + currentNickname);
        }
        else
        {
            Debug.LogWarning("Никнейм не может быть пустым!");
        }
    }

    public void ChangeAvatar(int avatarIndex)
    {
        // Проверяем валидность через AvatarManager
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return;
        }

        int maxAvatars = AvatarManager.Instance.GetAvatarCount();
        if (avatarIndex >= 0 && avatarIndex < maxAvatars)
        {
            currentAvatarIndex = avatarIndex;
            currentAvatar = AvatarManager.Instance.GetAvatar(avatarIndex);
            PlayerPrefs.SetInt("AvatarIndex", avatarIndex);

            // Синхронизируем кастомные свойства
            SyncPlayerCustomProperties();

            UpdateProfileUI();
            Debug.Log("Аватар изменён на индекс: " + avatarIndex);
        }
        else
        {
            Debug.LogWarning($"Некорректный индекс аватарки: {avatarIndex}. Доступно аватарок: {maxAvatars}");
        }
    }

    private string GenerateRandomNickname()
    {
        int randomIndex = Random.Range(0, defaultNicknames.Length);
        return defaultNicknames[randomIndex];
    }

    /// <summary>
    /// Получить текущий индекс аватара (для совместимости с новой системой)
    /// </summary>
    public int GetCurrentAvatarIndex()
    {
        return currentAvatarIndex;
    }

    /// <summary>
    /// Обновленный метод NextAvatar с проверкой разблокировки
    /// </summary>
    public void NextAvatar()
    {
        NextUnlockedAvatar();
    }

    /// <summary>
    /// Обновленный метод PreviousAvatar с проверкой разблокировки
    /// </summary>
    public void PreviousAvatar()
    {
        PreviousUnlockedAvatar();
    }

    private void AddPendingExperience()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ExperienceToAdd", out object expObj))
        {
            int expToAdd = (int)expObj;

            if (expToAdd > 0)
            {
                AddExperience(expToAdd);

                // После добавления обнуляем опыт для добавления
                Hashtable resetProps = new Hashtable();
                resetProps["ExperienceToAdd"] = 0;
                PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);

                Debug.Log($"Добавлено {expToAdd} опыта из CustomProperties после загрузки лобби");
            }
        }
    }

    public void AddExperience(int amount)
    {
        currentXP += amount;
        int levelsGained = 0;

        while (currentXP >= xpToLevelUp)
        {
            currentXP -= xpToLevelUp;
            currentLevel++;
            levelsGained++;
            Debug.Log($"Поздравляем! Новый уровень: {currentLevel}");
        }

        // Сохраняем данные
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentXP", currentXP);
        PlayerPrefs.Save();

        UpdateProfileUI();

        // Если получили новые уровни, показываем награду
        if (levelsGained > 0)
        {
            ShowLevelUpReward(levelsGained);
        }
    }

    private IEnumerator AnimateGemCounter(int targetAmount)
    {
        // Получаем компонент эффектов, если есть
        RewardAnimationEffects effects = gemRewardText?.GetComponent<RewardAnimationEffects>();

        if (effects != null)
        {
            int fromValue = 0; // начальное значение счетчика (можно изменить при необходимости)

            // Используем продвинутую анимацию с эффектами
            yield return StartCoroutine(effects.AnimateCounterWithSteps(
                fromValue,
                targetAmount,
                rewardAnimationDuration,
                (currentValue) =>
                {
                    if (gemRewardText != null)
                    {
                        gemRewardText.text = $"+{currentValue} гемов";
                    }
                }));

            // Запускаем цветовую анимацию
            StartCoroutine(effects.AnimateTextColor());
        }
        else
        {
            // Базовая анимация без эффектов
            float elapsedTime = 0f;
            int currentDisplayed = 0;

            while (elapsedTime < rewardAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / rewardAnimationDuration;

                // Используем плавную кривую для более приятной анимации
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                currentDisplayed = Mathf.RoundToInt(targetAmount * smoothProgress);

                if (gemRewardText != null)
                {
                    gemRewardText.text = $"+{currentDisplayed} гемов";
                }

                yield return null;
            }

            // Убеждаемся, что показываем финальное значение
            if (gemRewardText != null)
            {
                gemRewardText.text = $"+{targetAmount} гемов";
            }
        }
    }

    public void AddTestLevel(int amount)
    {
        int targetXP = 5000 - currentXP; // Сколько не хватает до следующего уровня
        AddExperience(targetXP + 1); // Добавляем чуть больше, чтобы точно повысился уровень
        Debug.Log($"[ЧИТ] Повышен уровень на {amount}. Текущий уровень: {currentLevel}");
    }

    private void ShowLevelUpReward(int levelsGained)
    {
        // Вычисляем награду (50 гемов за уровень)
        pendingGemReward = levelsGained * 50;

        // Показываем панель награды (если есть UI)
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
        }

        // Обновляем текст уровня
        if (levelUpText != null)
        {
            string text = levelsGained == 1 ? "Новый уровень!" : $"Новые уровни! (+{levelsGained})";
            levelUpText.text = text;

            RewardAnimationEffects levelEffects = levelUpText.GetComponent<RewardAnimationEffects>();
            if (levelEffects != null)
            {
                levelEffects.PlayLevelUpAnimation();
            }
        }

        // Запускаем анимацию гемов
        if (gemRewardText != null)
        {
            StartCoroutine(AnimateGemCounter(pendingGemReward));
        }

        // Добавляем гемы сразу
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGems(pendingGemReward, $"Награда за повышение уровня до {currentLevel}");
            Debug.Log($"Автоматически добавлено {pendingGemReward} гемов за повышение уровня.");
        }
        else
        {
            Debug.LogWarning("CurrencyManager не найден! Невозможно выдать гемы.");
        }

        pendingGemReward = 0;

        // Автоматически скрываем панель награды через несколько секунд
        StartCoroutine(HideRewardPanelDelayed(1.5f));
    }

    private IEnumerator HideRewardPanelDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        pendingGemReward = 0;

        Debug.Log($"Получена награда: {pendingGemReward} гемов за повышение уровня!");
    }

    private void UpdateProfileUI()
    {
        if (currentNicknameText != null)
            currentNicknameText.text = currentNickname;

        if (avatarImage != null && currentAvatar != null)
            avatarImage.sprite = currentAvatar;

        if (experienceSlider != null)
        {
            experienceSlider.maxValue = xpToLevelUp;
            experienceSlider.value = currentXP;
        }

        if (levelText != null)
            levelText.text = $"{currentLevel}";

        // Обновляем текст опыта в формате "текущий/максимум" в сотнях
        if (experienceText != null)
        {
            // Делим на 100 для отображения в сотнях
            int displayCurrentXP = Mathf.RoundToInt(currentXP / 10f);
            int displayMaxXP = Mathf.RoundToInt(xpToLevelUp / 10f);

            experienceText.text = $"{displayCurrentXP}/{displayMaxXP}";
        }
    }

    // Дополнительные методы для получения данных об опыте
    public int GetCurrentXP()
    {
        return currentXP;
    }

    public int GetXPToLevelUp()
    {
        return xpToLevelUp;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    // Метод для получения процента заполнения опыта (полезно для анимаций)
    public float GetXPProgress()
    {
        return (float)currentXP / xpToLevelUp;
    }

    // Методы для получения отображаемых значений в сотнях
    public int GetDisplayCurrentXP()
    {
        return Mathf.RoundToInt(currentXP / 10f);
    }

    public int GetDisplayMaxXP()
    {
        return Mathf.RoundToInt(xpToLevelUp / 10f);
    }

    // Метод для получения текста опыта в нужном формате
    public string GetXPDisplayText()
    {
        int displayCurrent = GetDisplayCurrentXP();
        int displayMax = GetDisplayMaxXP();
        return $"{displayCurrent}/{displayMax}";
    }
    
        #region Avatar System Integration
    
    /// <summary>
    /// Безопасное изменение аватара с проверкой разблокировки
    /// </summary>
    public bool TryChangeAvatar(int avatarIndex)
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return false;
        }

        // Проверяем, разблокирован ли аватар
        if (!AvatarManager.Instance.IsValidUnlockedAvatar(avatarIndex))
        {
            Debug.LogWarning($"Аватар {avatarIndex} не разблокирован или недоступен!");
            return false;
        }

        // Используем существующий метод ChangeAvatar
        ChangeAvatar(avatarIndex);
        return true;
    }
    
    /// <summary>
    /// Получить список доступных аватаров для UI
    /// </summary>
    public List<int> GetAvailableAvatarIndices()
    {
        if (AvatarManager.Instance == null) return new List<int> { 0 };
        
        return AvatarManager.Instance.GetUnlockedAvatarIndices();
    }
    
    /// <summary>
    /// Проверить и исправить текущий аватар если он не разблокирован
    /// </summary>
    private void ValidateCurrentAvatar()
    {
        if (AvatarManager.Instance == null) return;
        
        Debug.Log($"Проверяем аватар индекс {currentAvatarIndex}");
        
        // Проверяем что индекс в допустимых пределах
        int maxAvatars = AvatarManager.Instance.GetAvatarCount();
        if (currentAvatarIndex >= maxAvatars)
        {
            Debug.LogWarning($"Индекс аватара {currentAvatarIndex} больше доступного количества {maxAvatars}");
            currentAvatarIndex = 0;
            PlayerPrefs.SetInt("AvatarIndex", 0);
        }
        
        // Проверяем что аватар разблокирован
        if (!AvatarManager.Instance.IsValidUnlockedAvatar(currentAvatarIndex))
        {
            int firstUnlockedIndex = AvatarManager.Instance.GetFirstUnlockedAvatarIndex();
            Debug.Log($"Текущий аватар {currentAvatarIndex} недоступен. Переключаемся на {firstUnlockedIndex}");
            ChangeAvatar(firstUnlockedIndex);
        }
        
        // Выводим финальную информацию
        var currentSprite = AvatarManager.Instance.GetAvatar(currentAvatarIndex);
        var currentData = AvatarManager.Instance.GetAvatarData(currentAvatarIndex);
        Debug.Log($"Итоговый аватар: индекс {currentAvatarIndex}, " +
                $"спрайт {(currentSprite ? currentSprite.name : "NULL")}, " +
                $"данные {(currentData != null ? currentData.name : "NULL")}");
    }
    
    /// <summary>
    /// Обновленная навигация по аватарам (только разблокированные)
    /// </summary>
    public void NextUnlockedAvatar()
    {
        if (AvatarManager.Instance == null) return;
        
        var unlockedIndices = AvatarManager.Instance.GetUnlockedAvatarIndices();
        if (unlockedIndices.Count <= 1) return; // Нет смысла переключать, если аватар один
        
        int currentIndex = unlockedIndices.IndexOf(currentAvatarIndex);
        if (currentIndex == -1) currentIndex = 0;
        
        int nextIndex = (currentIndex + 1) % unlockedIndices.Count;
        TryChangeAvatar(unlockedIndices[nextIndex]);
    }
    
    public void PreviousUnlockedAvatar()
    {
        if (AvatarManager.Instance == null) return;
        
        var unlockedIndices = AvatarManager.Instance.GetUnlockedAvatarIndices();
        if (unlockedIndices.Count <= 1) return;
        
        int currentIndex = unlockedIndices.IndexOf(currentAvatarIndex);
        if (currentIndex == -1) currentIndex = 0;
        
        int prevIndex = (currentIndex - 1 + unlockedIndices.Count) % unlockedIndices.Count;
        TryChangeAvatar(unlockedIndices[prevIndex]);
    }
    
    #endregion
}