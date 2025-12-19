using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using System;

public class ConnectToServer : MonoBehaviourPunCallbacks
{
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string offlineSceneName = "OfflineScene";

    [Header("Daily Login Settings")]
    [SerializeField] private bool enableDailyLoginTracking = true;
    [SerializeField] private bool showDailyLoginReward = true;

    private GoogleSheetsQuestionDownloader questionDownloader;
    private bool hasTrackedDailyLogin = false;

    void Start()
    {
        questionDownloader = GameObject.FindFirstObjectByType<GoogleSheetsQuestionDownloader>();

        if (questionDownloader == null)
        {
            Debug.LogError("Не найден GoogleSheetsQuestionDownloader в сцене!");
            return;
        }

        // Отслеживаем ежедневный вход сразу при старте
        if (enableDailyLoginTracking)
        {
            TrackDailyLogin();
        }

        // Подписываемся на событие — подключаемся когда вопросы загружены
        questionDownloader.OnQuestionsLoaded += StartPhotonConnection;
    }

    void OnDestroy()
    {
        if (questionDownloader != null)
            questionDownloader.OnQuestionsLoaded -= StartPhotonConnection;
    }

    private void TrackDailyLogin()
    {
        if (hasTrackedDailyLogin) return;

        try
        {
            // Получаем текущую дату
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string lastLoginDate = PlayerPrefs.GetString("LastDailyLoginDate", "");

            Debug.Log($"Проверка ежедневного входа: последний вход {lastLoginDate}, текущая дата {currentDate}");

            // Проверяем, заходил ли игрок сегодня
            if (lastLoginDate != currentDate)
            {
                // Первый заход за день
                PlayerPrefs.SetString("LastDailyLoginDate", currentDate);
                PlayerPrefs.Save();

                // Отслеживаем расширенную статистику входов
                TrackExtendedLoginStats();

                // Уведомляем TaskManager о ежедневном входе
                NotifyDailyLogin();

                // Показываем уведомление о ежедневном входе
                if (showDailyLoginReward)
                {
                    ShowDailyLoginNotification();
                }

                hasTrackedDailyLogin = true;
                Debug.Log("✅ Ежедневный вход зарегистрирован!");
            }
            else
            {
                Debug.Log("Игрок уже заходил сегодня");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при отслеживании ежедневного входа: {e.Message}");
        }
    }

    private void NotifyDailyLogin()
    {
        // Уведомляем TaskManager о ежедневном входе
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnDailyLogin();
            Debug.Log("TaskManager уведомлен о ежедневном входе");
        }
        else
        {
            // Если TaskManager еще не готов, попробуем позже
            Invoke(nameof(DelayedDailyLoginNotification), 1f);
        }

        // Также уведомляем DailyTaskManager если он есть
        if (DailyTaskManager.Instance != null)
        {
            // DailyTaskManager автоматически проверит ежедневные таски
            Debug.Log("DailyTaskManager найден и готов");
        }
        else
        {
            Debug.Log("DailyTaskManager еще не инициализирован");
        }
    }

    private void DelayedDailyLoginNotification()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnDailyLogin();
            Debug.Log("TaskManager уведомлен о ежедневном входе (отложенно)");
        }
        else
        {
            Debug.LogWarning("TaskManager по-прежнему не найден для уведомления о ежедневном входе");
            // Попробуем еще раз через 2 секунды
            Invoke(nameof(DelayedDailyLoginNotification), 2f);
        }
    }

    private void ShowDailyLoginNotification()
    {
        int streak = GetLoginStreak();
        int totalDays = GetTotalLoginDays();
        
        // Улучшенное уведомление с информацией о серии
        if (streak == 1)
        {
            Debug.Log($"🎉 Добро пожаловать! Ежедневный вход засчитан! (День {totalDays})");
        }
        else
        {
            Debug.Log($"🎉 Добро пожаловать! Серия входов: {streak} дней подряд! (Всего дней: {totalDays})");
        }
    }

    private void StartPhotonConnection()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("Нет подключения к интернету. Загружаем оффлайн-сцену...");
            SceneManager.LoadScene(offlineSceneName);
            return;
        }

        Debug.Log("Вопросы загружены, начинаем подключение к Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Подключились к лобби, загружаем сцену " + lobbySceneName);
        
        // Дополнительная проверка ежедневного входа после подключения к лобби
        if (enableDailyLoginTracking && !hasTrackedDailyLogin)
        {
            TrackDailyLogin();
        }
        
        SceneManager.LoadScene(lobbySceneName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Отключено от Photon: {cause}. Загружаем оффлайн-сцену...");
        SceneManager.LoadScene(offlineSceneName);
    }

    // Публичные методы для управления
    public void ForceTrackDailyLogin()
    {
        hasTrackedDailyLogin = false;
        TrackDailyLogin();
    }

    public bool HasLoggedInToday()
    {
        string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        string lastLoginDate = PlayerPrefs.GetString("LastDailyLoginDate", "");
        return lastLoginDate == currentDate;
    }

    public string GetLastLoginDate()
    {
        return PlayerPrefs.GetString("LastDailyLoginDate", "Никогда");
    }

    // Метод для получения статистики входов
    public int GetTotalLoginDays()
    {
        return PlayerPrefs.GetInt("TotalLoginDays", 0);
    }

    public void IncrementTotalLoginDays()
    {
        int currentTotal = GetTotalLoginDays();
        PlayerPrefs.SetInt("TotalLoginDays", currentTotal + 1);
        PlayerPrefs.Save();
    }

    // Расширенная система отслеживания входов
    private void TrackExtendedLoginStats()
    {
        // Увеличиваем общий счетчик дней входа
        IncrementTotalLoginDays();

        // Отслеживаем серию последовательных входов
        TrackLoginStreak();

        // Сохраняем время входа
        PlayerPrefs.SetString("LastLoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PlayerPrefs.Save();
        
        Debug.Log("Расширенная статистика входов обновлена");
    }

    private void TrackLoginStreak()
    {
        string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        string lastLoginDate = PlayerPrefs.GetString("LastDailyLoginDate", "");
        int currentStreak = PlayerPrefs.GetInt("LoginStreak", 0);

        if (string.IsNullOrEmpty(lastLoginDate))
        {
            // Первый вход
            currentStreak = 1;
        }
        else
        {
            try
            {
                DateTime lastLogin = DateTime.Parse(lastLoginDate);
                DateTime today = DateTime.Parse(currentDate);
                
                int daysDifference = (today - lastLogin).Days;

                if (daysDifference == 1)
                {
                    // Последовательный вход
                    currentStreak++;
                }
                else if (daysDifference > 1)
                {
                    // Пропустили дни, сбрасываем серию
                    currentStreak = 1;
                }
                // Если daysDifference == 0, значит уже заходили сегодня (но этого не должно быть)
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при парсинге даты входа: {e.Message}");
                currentStreak = 1; // Безопасное значение
            }
        }

        PlayerPrefs.SetInt("LoginStreak", currentStreak);
        PlayerPrefs.Save();

        Debug.Log($"Серия ежедневных входов: {currentStreak} дней");

        // Проверяем награды за серии входов
        CheckStreakRewards(currentStreak);
    }

    private void CheckStreakRewards(int streak)
    {
        // Награды за серии входов
        if (streak == 7)
        {
            Debug.Log("🎉 Неделя ежедневных входов! Особая награда!");
            GiveStreakReward(7, 100); // 100 гемов за неделю
        }
        else if (streak == 30)
        {
            Debug.Log("🎉 Месяц ежедневных входов! Эпическая награда!");
            GiveStreakReward(30, 500); // 500 гемов за месяц
        }
        else if (streak % 10 == 0 && streak > 0)
        {
            Debug.Log($"🎉 {streak} дней подряд! Награда за постоянство!");
            GiveStreakReward(streak, streak * 5); // 5 гемов за каждый день серии
        }
    }

    private void GiveStreakReward(int streak, int gems)
    {
        // Выдаем награду через CurrencyManager
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGems(gems, $"Награда за серию входов ({streak} дней)");
            Debug.Log($"Выдана награда: {gems} гемов за серию {streak} дней");
        }
        else
        {
            // Fallback через PlayerPrefs
            int currentGems = PlayerPrefs.GetInt("PlayerGems", 0);
            PlayerPrefs.SetInt("PlayerGems", currentGems + gems);
            PlayerPrefs.Save();
            Debug.Log($"Награда сохранена в PlayerPrefs: {gems} гемов");
        }
    }

    public int GetLoginStreak()
    {
        return PlayerPrefs.GetInt("LoginStreak", 0);
    }

    // Метод для отладки - сброс данных входа
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ResetLoginData()
    {
        PlayerPrefs.DeleteKey("LastDailyLoginDate");
        PlayerPrefs.DeleteKey("TotalLoginDays");
        PlayerPrefs.DeleteKey("LoginStreak");
        PlayerPrefs.DeleteKey("LastLoginTime");
        hasTrackedDailyLogin = false;
        Debug.Log("[DEBUG] Данные ежедневного входа сброшены");
    }

    // Метод для получения подробной информации о входах
    public string GetLoginInfo()
    {
        string lastLogin = GetLastLoginDate();
        int streak = GetLoginStreak();
        int totalDays = GetTotalLoginDays();
        bool todayLogin = HasLoggedInToday();

        return $"Последний вход: {lastLogin}\n" +
               $"Серия: {streak} дней\n" +
               $"Всего дней: {totalDays}\n" +
               $"Сегодня заходил: {(todayLogin ? "Да" : "Нет")}";
    }

    // Метод для принудительного тестирования (только в редакторе)
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestDailyLogin()
    {
        Debug.Log("[TEST] Принудительный тест ежедневного входа");
        Debug.Log($"[TEST] Текущая информация:\n{GetLoginInfo()}");
        
        // Симулируем новый день
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        PlayerPrefs.SetString("LastDailyLoginDate", yesterday);
        
        hasTrackedDailyLogin = false;
        TrackDailyLogin();
    }
}