using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
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
    public TextMeshProUGUI experienceText;

    [Header("Level Up Rewards")]
    public GameObject levelUpPanel;
    public TextMeshProUGUI levelUpText;
    public TextMeshProUGUI gemRewardText;
    public float rewardAnimationDuration = 2f;

    [Header("Cups UI")]
    public TextMeshProUGUI cupsText;

    // === Cups storage ===
    public const string CUPS_KEY = "PlayerCups"; // один ключ на весь проект
    private int currentCups = 0;

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

    public System.Action<int> OnCupsChanged;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Если хочешь, чтобы не уничтожался при смене сцен
        // DontDestroyOnLoad(gameObject);

        if (PlayerPrefs.HasKey("AvatarIndex"))
        {
            currentAvatarIndex = PlayerPrefs.GetInt("AvatarIndex");
            Debug.Log("Awake: Загружен индекс аватара: " + currentAvatarIndex);
        }

        InitializeDefaultAvatars();

        // Level / XP
        if (PlayerPrefs.HasKey("CurrentLevel"))
            currentLevel = PlayerPrefs.GetInt("CurrentLevel");
        if (PlayerPrefs.HasKey("CurrentXP"))
            currentXP = PlayerPrefs.GetInt("CurrentXP");

        // Cups
        currentCups = PlayerPrefs.GetInt(CUPS_KEY, 0);
    }

    void Start()
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager не найден! Убедитесь, что создали ScriptableObject и поместили его в Resources");
            return;
        }

        ValidateCurrentAvatar();

        // Nickname
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
        if (nicknameInputField != null)
            nicknameInputField.text = currentNickname;

        // Avatar
        if (PlayerPrefs.HasKey("AvatarIndex"))
        {
            currentAvatarIndex = PlayerPrefs.GetInt("AvatarIndex");
            currentAvatar = AvatarManager.Instance.GetAvatar(currentAvatarIndex);
        }
        else
        {
            currentAvatarIndex = 0;
            currentAvatar = AvatarManager.Instance.GetAvatar(0);
            PlayerPrefs.SetInt("AvatarIndex", 0);
        }

        // UI reward panel
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        // Подтягиваем опыт, который могли записать перед выходом из комнаты
        AddPendingExperience();

        // Обновляем UI сразу
        UpdateProfileUI();

        // И сразу синхронизируем свойства (если Photon уже подключен)
        SyncPlayerCustomProperties();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            AddTestLevel(1);
        }

        // НОВЫЙ БЛОК: Нажми 'C' в игре, чтобы добавить 30 кубков
        if (Input.GetKeyDown(KeyCode.C))
        {
            AddCups(30);
            Debug.Log("Тест: Добавлено 30 кубков. Теперь всего: " + GetCups());
        }
    }

    public void OnEnable()
    {
        if (PhotonNetwork.NetworkingClient != null)
            PhotonNetwork.NetworkingClient.StateChanged += NetworkingClientOnStateChanged;
    }

    public void OnDisable()
    {
        if (PhotonNetwork.NetworkingClient != null)
            PhotonNetwork.NetworkingClient.StateChanged -= NetworkingClientOnStateChanged;
    }

    private void NetworkingClientOnStateChanged(ClientState previousState, ClientState currentState)
    {
        if (currentState == ClientState.JoinedLobby || currentState == ClientState.Joined)
        {
            // Если в Photon уже есть Cups — можно подтянуть (опционально).
            // Я оставил "локальный источник истины" (PlayerPrefs).
            // Если хочешь наоборот — скажи, сделаю.

            SyncPlayerCustomProperties();
        }
    }

    void OnLevelWasLoaded(int level)
    {
        UpdateProfileUI();
    }

    public void BindUI(
        TMP_InputField nicknameInput,
        TextMeshProUGUI nicknameText,
        Image avatarImg,
        Slider xpSlider,
        TextMeshProUGUI lvlText,
        TextMeshProUGUI xpText,
        TextMeshProUGUI cupsTxt,
        GameObject lvlUpPanelObj,
        TextMeshProUGUI lvlUpTxt,
        TextMeshProUGUI gemTxt
    )
    {
        nicknameInputField = nicknameInput;
        currentNicknameText = nicknameText;
        avatarImage = avatarImg;

        experienceSlider = xpSlider;
        levelText = lvlText;
        experienceText = xpText;

        cupsText = cupsTxt;

        levelUpPanel = lvlUpPanelObj;
        levelUpText = lvlUpTxt;
        gemRewardText = gemTxt;

        // когда UI появился — сразу обновим
        UpdateProfileUI();
    }

    public void RefreshUI()
    {
        UpdateProfileUI();
    }

    private void SyncPlayerCustomProperties()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("SyncPlayerCustomProperties: не подключен к Photon");
            return;
        }

        Hashtable props = new Hashtable
        {
            { "Nickname", currentNickname },
            { "AvatarIndex", currentAvatarIndex },
            { "Cups", currentCups },
            { "LeagueIndex", GetCurrentLeagueIndex() }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"Sync props: Nickname={currentNickname}, AvatarIndex={currentAvatarIndex}, Cups={currentCups}");
    }

    // =========================
    // Cups API (Единственный способ менять кубки)
    // =========================

    public int GetCups() => currentCups;

    public int GetCurrentLeagueIndex()
    {
        if (PathManager.Instance != null)
            return PathManager.Instance.GetLeagueIndex(currentCups);
        return Mathf.Clamp(currentCups / 500, 0, 5);
    }

    public LeagueData GetCurrentLeague()
    {
        return PathManager.Instance?.GetLeague(currentCups);
    }

    public void SetCups(int newCups)
    {
        currentCups = Mathf.Max(0, newCups);

        PlayerPrefs.SetInt(CUPS_KEY, currentCups);
        PlayerPrefs.Save();

        UpdateProfileUI();
        SyncPlayerCustomProperties();

        OnCupsChanged?.Invoke(currentCups);
    }

    public void AddCups(int delta)
    {
        SetCups(currentCups + delta);
    }

    // =========================
    // Nickname / Avatar
    // =========================

    public void ChangeNickname()
    {
        if (nicknameInputField == null) return;

        string newNickname = nicknameInputField.text;

        if (!string.IsNullOrEmpty(newNickname))
        {
            currentNickname = newNickname;
            PlayerPrefs.SetString("Nickname", currentNickname);
            PhotonNetwork.NickName = currentNickname;

            SyncPlayerCustomProperties();
            UpdateProfileUI();
            Debug.Log("Никнейм изменён на: " + currentNickname);
        }
        else
        {
            Debug.LogWarning("Никнейм не може бути поронжім!");
        }
    }

    public void ChangeAvatar(int avatarIndex)
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return;
        }

        int maxAvatars = AvatarManager.Instance.GetAvatarCount();
        if (avatarIndex < 0 || avatarIndex >= maxAvatars)
        {
            Debug.LogWarning($"Некорректный индекс аватарки: {avatarIndex}. Доступно: {maxAvatars}");
            return;
        }

        currentAvatarIndex = avatarIndex;
        currentAvatar = AvatarManager.Instance.GetAvatar(avatarIndex);
        PlayerPrefs.SetInt("AvatarIndex", avatarIndex);

        SyncPlayerCustomProperties();
        UpdateProfileUI();
        Debug.Log("Аватар изменён на индекс: " + avatarIndex);
    }

    private string GenerateRandomNickname()
    {
        int randomIndex = Random.Range(0, defaultNicknames.Length);
        return defaultNicknames[randomIndex];
    }

    public int GetCurrentAvatarIndex() => currentAvatarIndex;

    // =========================
    // XP / Level
    // =========================

    private void AddPendingExperience()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null) return;

        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ExperienceToAdd", out object expObj))
        {
            int expToAdd = (int)expObj;

            if (expToAdd > 0)
            {
                AddExperience(expToAdd);

                Hashtable resetProps = new Hashtable
                {
                    { "ExperienceToAdd", 0 }
                };
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

        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentXP", currentXP);
        PlayerPrefs.Save();

        UpdateProfileUI();

        if (levelsGained > 0)
        {
            ShowLevelUpReward(levelsGained);
        }
    }

    private IEnumerator AnimateGemCounter(int targetAmount)
    {
        RewardAnimationEffects effects = gemRewardText?.GetComponent<RewardAnimationEffects>();

        if (effects != null)
        {
            int fromValue = 0;

            yield return StartCoroutine(effects.AnimateCounterWithSteps(
                fromValue,
                targetAmount,
                rewardAnimationDuration,
                (currentValue) =>
                {
                    if (gemRewardText != null)
                        gemRewardText.text = $"+{currentValue} гемов";
                }));

            StartCoroutine(effects.AnimateTextColor());
        }
        else
        {
            float elapsedTime = 0f;
            int currentDisplayed = 0;

            while (elapsedTime < rewardAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / rewardAnimationDuration;

                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                currentDisplayed = Mathf.RoundToInt(targetAmount * smoothProgress);

                if (gemRewardText != null)
                    gemRewardText.text = $"+{currentDisplayed} гемов";

                yield return null;
            }

            if (gemRewardText != null)
                gemRewardText.text = $"+{targetAmount} гемов";
        }
    }

    public void AddTestLevel(int amount)
    {
        int targetXP = 5000 - currentXP;
        AddExperience(targetXP + 1);
        Debug.Log($"[ЧИТ] Повышен уровень на {amount}. Текущий уровень: {currentLevel}");
    }

    private void ShowLevelUpReward(int levelsGained)
    {
        pendingGemReward = levelsGained * 50;

        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        if (levelUpText != null)
        {
            string text = levelsGained == 1 ? "Новый уровень!" : $"Новые уровни! (+{levelsGained})";
            levelUpText.text = text;

            RewardAnimationEffects levelEffects = levelUpText.GetComponent<RewardAnimationEffects>();
            if (levelEffects != null)
                levelEffects.PlayLevelUpAnimation();
        }

        if (gemRewardText != null)
            StartCoroutine(AnimateGemCounter(pendingGemReward));

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGems(pendingGemReward, $"Награда за повышение уровня до {currentLevel}");
            Debug.Log($"Автоматически добавлено {pendingGemReward} гемов за повышение уровня.");
        }
        else
        {
            Debug.LogWarning("CurrencyManager не найден! Невозможно выдать гемы.");
        }

        StartCoroutine(HideRewardPanelDelayed(1.5f, pendingGemReward));
        pendingGemReward = 0;
    }

    private IEnumerator HideRewardPanelDelayed(float delay, int rewardShown)
    {
        yield return new WaitForSeconds(delay);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        Debug.Log($"Получена награда: {rewardShown} гемов за повышение уровня!");
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

        if (cupsText != null)
            cupsText.text = currentCups.ToString();

        if (levelText != null)
            levelText.text = $"{currentLevel}";

        if (experienceText != null)
        {
            int displayCurrentXP = Mathf.RoundToInt(currentXP / 10f);
            int displayMaxXP = Mathf.RoundToInt(xpToLevelUp / 10f);
            experienceText.text = $"{displayCurrentXP}/{displayMaxXP}";
        }
    }

    // XP getters
    public int GetCurrentXP() => currentXP;
    public int GetXPToLevelUp() => xpToLevelUp;
    public int GetCurrentLevel() => currentLevel;

    public float GetXPProgress() => (float)currentXP / xpToLevelUp;

    public int GetDisplayCurrentXP() => Mathf.RoundToInt(currentXP / 10f);
    public int GetDisplayMaxXP() => Mathf.RoundToInt(xpToLevelUp / 10f);

    public string GetXPDisplayText()
    {
        int displayCurrent = GetDisplayCurrentXP();
        int displayMax = GetDisplayMaxXP();
        return $"{displayCurrent}/{displayMax}";
    }

    // =========================
    // Avatar system integration (твой блок)
    // =========================

    private void InitializeDefaultAvatars()
    {
        if (!PlayerPrefs.HasKey("AvatarsInitialized"))
        {
            if (AvatarManager.Instance != null)
            {
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

    public bool TryChangeAvatar(int avatarIndex)
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return false;
        }

        if (!AvatarManager.Instance.IsValidUnlockedAvatar(avatarIndex))
        {
            Debug.LogWarning($"Аватар {avatarIndex} не разблокирован или недоступен!");
            return false;
        }

        ChangeAvatar(avatarIndex);
        return true;
    }

    public List<int> GetAvailableAvatarIndices()
    {
        if (AvatarManager.Instance == null) return new List<int> { 0 };
        return AvatarManager.Instance.GetUnlockedAvatarIndices();
    }

    private void ValidateCurrentAvatar()
    {
        if (AvatarManager.Instance == null) return;

        Debug.Log($"Проверяем аватар индекс {currentAvatarIndex}");

        int maxAvatars = AvatarManager.Instance.GetAvatarCount();
        if (currentAvatarIndex >= maxAvatars)
        {
            Debug.LogWarning($"Индекс аватара {currentAvatarIndex} больше доступного количества {maxAvatars}");
            currentAvatarIndex = 0;
            PlayerPrefs.SetInt("AvatarIndex", 0);
        }

        if (!AvatarManager.Instance.IsValidUnlockedAvatar(currentAvatarIndex))
        {
            int firstUnlockedIndex = AvatarManager.Instance.GetFirstUnlockedAvatarIndex();
            Debug.Log($"Текущий аватар {currentAvatarIndex} недоступен. Переключаемся на {firstUnlockedIndex}");
            ChangeAvatar(firstUnlockedIndex);
        }

        var currentSprite = AvatarManager.Instance.GetAvatar(currentAvatarIndex);
        var currentData = AvatarManager.Instance.GetAvatarData(currentAvatarIndex);
        Debug.Log($"Итоговый аватар: индекс {currentAvatarIndex}, " +
                  $"спрайт {(currentSprite ? currentSprite.name : "NULL")}, " +
                  $"данные {(currentData != null ? currentData.name : "NULL")}");
    }

    public void NextAvatar() => NextUnlockedAvatar();
    public void PreviousAvatar() => PreviousUnlockedAvatar();

    public void NextUnlockedAvatar()
    {
        if (AvatarManager.Instance == null) return;

        var unlockedIndices = AvatarManager.Instance.GetUnlockedAvatarIndices();
        if (unlockedIndices.Count <= 1) return;

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
}