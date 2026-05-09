using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuizManager : MonoBehaviourPunCallbacks
{
    
    [Header("Components")]
    [SerializeField] private QuestionManager questionManager;

    [Header("Timer Avatar System")]
    [SerializeField] private TimerAvatarManager timerAvatarManager;

    [Header("Numerical Answer Wheel")]
    [SerializeField] private NumericalAnswerWheelManager numericalAnswerWheel;
    
    [Header("Mode UI")]
    [SerializeField] private QuizModeDisplay modeDisplay;
    
    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Slider timerSlider;
    
    [Header("Quiz Settings")]
    [SerializeField] private float maxQuestionTime = 5f;
    [SerializeField] private int maxQuestions = 2;
    [SerializeField] private float answerRevealTime = 2f;

    [Header("Chat")]
    [SerializeField] private ChatUI chatUI;

    private bool hasPlayedFinalSecondsSound = false;
    
    [SerializeField] private PlayerScoreDisplay playerScoreDisplay;

    private int consecutiveCorrectAnswers = 0;
    
    // Приватные переменные
    private Dictionary<int, PlayerAnswerData> playerAnswersData = new Dictionary<int, PlayerAnswerData>();
    private Dictionary<int, int> correctAnswerCounts = new Dictionary<int, int>();

    private Dictionary<int, bool> playerAnswers = new Dictionary<int, bool>();
    private Dictionary<int, int> allPlayerScores = new Dictionary<int, int>();
    private Dictionary<int, string> playerNumericalAnswers = new Dictionary<int, string>();
    private List<int> answerOrder = new List<int>();
    private List<int> usedQuestionIndices = new List<int>();
    private int questionCounter = 1;
    private int score;
    private float questionTimer;
    private float answerDisplayTimer; // Новый таймер для показа ответа
    private bool isTimerRunning;
    private bool isAnswerDisplayRunning; // Флаг для таймера показа ответа
    private bool isRouletteFinished = false;
    private bool isQuizStarted = false;
    private bool canAcceptAnswers = false;
    private QuizMode currentMode = QuizMode.Normal;
    
    public class PlayerAnswerData
    {
        public bool IsCorrect;
        public string NumericalAnswer;
        public string SelectedAnswerText;
        public float RemainingTime;
    }

    private void Start()
    {
        InitializeComponents();
        questionManager.Initialize(maxQuestions);
        InitializeGame();
    }

    public int MaxQuestions => maxQuestions;
    public float MaxQuestionTime => maxQuestionTime;

    private void Update()
    {
        UpdateQuestionTimer();
        UpdateAnswerDisplayTimer();
    }

    private void UpdateQuestionTimer()
    {
        if (!isTimerRunning) return;

        questionTimer -= Time.deltaTime;
        UpdateTimerUI();

        // Изменение скорости таймера для режима "ShortTime"
        if (currentMode == QuizMode.ShortTime)
        {
            questionTimer -= Time.deltaTime * 1f;
        }

        // 🔊 Проигрываем звук за последние 5 секунд
        if (!hasPlayedFinalSecondsSound && questionTimer <= 2.5f)
        {
            hasPlayedFinalSecondsSound = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("timer");
            }
            else
            {
                Debug.LogWarning("AudioManager.Instance не найден для воспроизведения звука 'timer'");
            }
        }

        if (questionManager.IsCurrentQuestionNumerical())
        {
            questionManager.UpdateNumericalQuestionUI(true);
        }

        if (questionTimer <= 0)
        {
            HandleTimerExpired();
        }
    }

    private void UpdateAnswerDisplayTimer()
    {
        if (!isAnswerDisplayRunning) return;

        answerDisplayTimer -= Time.deltaTime;
        
        // Обновляем слайдер для показа оставшегося времени показа ответа
        if (timerSlider != null)
        {
            timerSlider.value = answerDisplayTimer;
        }

        if (answerDisplayTimer <= 0)
        {
            HandleAnswerDisplayExpired();
        }
    }

    private void HandleAnswerDisplayExpired()
    {
        isAnswerDisplayRunning = false;
        
        if (PhotonNetwork.IsMasterClient)
        {
            NextQuestion();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerSlider != null && !isAnswerDisplayRunning)
        {
            timerSlider.value = questionTimer;
        }
    }

    private void InitializeComponents()
    {
        // Инициализируем QuestionManager если не был найден
        if (questionManager == null)
            questionManager = GetComponent<QuestionManager>();
        
        if (questionManager == null)
        {
            Debug.LogError("QuestionManager не найден! Добавьте компонент на тот же объект или укажите ссылку в инспекторе.");
            return;
        }
        
        // Инициализируем UI компоненты
        if (timerSlider != null)
        {
            timerSlider.maxValue = maxQuestionTime;
            timerSlider.value = maxQuestionTime;
        }
        
        // Инициализируем отображение режима, если компонент присутствует
        if (modeDisplay != null)
        {
            modeDisplay.UpdateModeDisplay(currentMode);
        }
        
        // Инициализируем TimerAvatarManager
        if (timerAvatarManager == null)
            timerAvatarManager = GetComponent<TimerAvatarManager>();
        
        if (timerAvatarManager == null)
        {
            Debug.LogError("TimerAvatarManager не найден! Добавьте компонент на тот же объект или укажите ссылку в инспекторе.");
        }

        // Инициализируем дисплей счета игроков
        if (playerScoreDisplay != null)
        {
            playerScoreDisplay.InitializePlayerList();
        }

        // Инициализируем NumericalAnswerWheelManager
        if (numericalAnswerWheel == null)
            numericalAnswerWheel = GetComponent<NumericalAnswerWheelManager>();
        
        if (numericalAnswerWheel == null)
        {
            Debug.LogWarning("NumericalAnswerWheelManager не найден! Добавьте компонент на тот же объект или укажите ссылку в инспекторе.");
        }
        
        // Подписываемся на события QuestionManager
        questionManager.OnQuestionAnswered += HandleQuestionAnswered;
        questionManager.OnNumericalQuestionAnswered += HandleNumericalQuestionAnswered;

    }

    private void InitializeGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Подождем завершения рулетки перед стартом
            StartCoroutine(WaitForRouletteThenStart());
        }
    }

    private IEnumerator WaitForRouletteThenStart()
    {
        yield return new WaitUntil(() => isRouletteFinished);
        // Можно добавить дополнительную логику перед запуском викторины
    }

    public void SetMode(QuizMode mode)
    {
        currentMode = mode;
        Debug.Log("Установлен режим: " + mode);
        
        // Обновляем отображение режима в UI
        if (modeDisplay != null)
        {
            modeDisplay.UpdateModeDisplay(mode);
        }
    }

    // Также добавьте метод для сброса статистики при начале новой игры:
    public void StartQuiz()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            consecutiveCorrectAnswers = 0; // Сбрасываем счетчик последовательных ответов
            NextQuestion();
        }
    }

    private void HandleTimerExpired()
    {
        isTimerRunning = false;
        
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ShowCorrectAnswer", RpcTarget.All);
            StartAnswerDisplayTimer(); // Запускаем таймер показа ответа
        }
    }

    private void StartAnswerDisplayTimer()
    {
        answerDisplayTimer = answerRevealTime;
        isAnswerDisplayRunning = true;
        
        // Настраиваем слайдер для показа времени ответа
        if (timerSlider != null)
        {
            timerSlider.maxValue = answerRevealTime;
            timerSlider.value = answerRevealTime;
        }
        
        // Синхронизируем запуск таймера показа ответа на всех клиентах
        photonView.RPC("StartAnswerDisplayTimerRPC", RpcTarget.All);
    }

    [PunRPC]
    private void StartAnswerDisplayTimerRPC()
    {
        answerDisplayTimer = answerRevealTime;
        isAnswerDisplayRunning = true;
        
        // Настраиваем слайдер для показа времени ответа
        if (timerSlider != null)
        {
            timerSlider.maxValue = answerRevealTime;
            timerSlider.value = answerRevealTime;
        }
    }

    [PunRPC]
    private void SetQuestion(int index, int questionNumber)
    {
        // Очищаем маркеры аватаров с предыдущего вопроса
        if (timerAvatarManager != null)
        {
            timerAvatarManager.ClearAllAvatarMarkers();
        }
        
        // Очищаем колесо числовых ответов
        if (numericalAnswerWheel != null)
        {
            numericalAnswerWheel.ClearAllMarkers();
        }
        
        // Сбрасываем данные ответов для всех игроков перед каждым новым вопросом
        playerAnswersData.Clear();
        playerAnswers.Clear();
        playerNumericalAnswers.Clear();
        answerOrder.Clear();
        
        // Устанавливаем вопрос через QuestionManager
        questionManager.SetQuestion(index, questionNumber);
        
        StartQuestionTimer();
    }

    private void StartQuestionTimer()
    {
        questionTimer = maxQuestionTime;
        answerDisplayTimer = 0f;
        isAnswerDisplayRunning = false; // Останавливаем таймер показа ответа
        
        if (timerSlider != null) 
        {
            timerSlider.maxValue = maxQuestionTime;
            timerSlider.value = maxQuestionTime;
        }
        
        hasPlayedFinalSecondsSound = false;
        isTimerRunning = true;
        canAcceptAnswers = true;
        
        // Включаем кнопки ответов для нового вопроса
        if (questionManager != null)
        {
            questionManager.SetAnswerButtonsInteractable(true);
        }
    }

    private void HandleQuestionAnswered(int playerID, bool isCorrect, string selectedAnswerText, float remainingTime)
    {
        // КРИТИЧЕСКИ ВАЖНАЯ ПРОВЕРКА: не принимаем ответы если они заблокированы
        if (!canAcceptAnswers)
        {
            Debug.Log($"Ответ игрока {playerID} отклонен - время истекло или ответ уже показан");
            return;
        }
        
        // Используем текущее время таймера для расчета оставшегося времени
        remainingTime = questionTimer;
        
        // Добавляем игрока в порядок ответивших
        photonView.RPC("RecordPlayerAnswerOrder", RpcTarget.MasterClient, playerID);

        // Отправляем информацию о ответе мастер-клиенту
        photonView.RPC("PlayerAnswered", RpcTarget.MasterClient, playerID, isCorrect, selectedAnswerText);
    }

    private void HandleNumericalQuestionAnswered(int playerID, bool isCorrect, string answer, float remainingTime)
    {
        // КРИТИЧЕСКИ ВАЖНАЯ ПРОВЕРКА: не принимаем ответы если они заблокированы
        if (!canAcceptAnswers)
        {
            Debug.Log($"Числовой ответ игрока {playerID} отклонен - время истекло или ответ уже показан");
            return;
        }
        
        // Используем текущее время таймера для расчета оставшегося времени
        remainingTime = questionTimer;
        
        // Добавляем игрока в порядок ответивших
        photonView.RPC("RecordPlayerAnswerOrder", RpcTarget.MasterClient, playerID);

        // Отправляем информацию о числовом ответе мастер-клиенту
        photonView.RPC("PlayerAnsweredNumerical", RpcTarget.MasterClient, playerID, isCorrect, answer);
    }

    [PunRPC]
    private void RecordPlayerAnswerOrder(int playerID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Если игрок еще не в списке ответивших, добавляем его
        if (!answerOrder.Contains(playerID))
        {
            answerOrder.Add(playerID);
        }
        
        // Синхронизируем порядок ответов для всех клиентов
        int[] orderArray = answerOrder.ToArray();
        photonView.RPC("SyncAnswerOrder", RpcTarget.All, orderArray);
    }

    [PunRPC]
    private void SyncAnswerOrder(int[] orderArray)
    {
        // Обновляем локальный список порядка ответов
        answerOrder.Clear();
        answerOrder.AddRange(orderArray);
    }

    [PunRPC]
    private void PlayerAnswered(int playerID, bool isCorrect, string selectedAnswerText)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var answerData = new PlayerAnswerData
        {
            IsCorrect = isCorrect,
            SelectedAnswerText = selectedAnswerText,
            RemainingTime = questionTimer
        };

        if (isCorrect)
        {
            if (!correctAnswerCounts.ContainsKey(playerID))
                correctAnswerCounts[playerID] = 0;
            correctAnswerCounts[playerID]++;
        }

        playerAnswersData[playerID] = answerData;

        photonView.RPC("SyncPlayerAnswers", RpcTarget.All, playerID, isCorrect, selectedAnswerText, questionTimer);

        Debug.Log($"Ответили {playerAnswersData.Count} из {PhotonNetwork.CurrentRoom.PlayerCount} игроков");

        if (playerAnswersData.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("Все игроки ответили, показываем правильный ответ");
            photonView.RPC("StopTimerAndShowCorrectAnswer", RpcTarget.All);
            StartAnswerDisplayTimer();
        }
    }

    [PunRPC]
    private void PlayerAnsweredNumerical(int playerID, bool isCorrect, string numericalAnswer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Сохраняем ответ игрока вместе с оставшимся временем
        var answerData = new PlayerAnswerData
        {
            IsCorrect = isCorrect,
            NumericalAnswer = numericalAnswer,
            RemainingTime = questionTimer
        };

        if (isCorrect)
        {
            if (!correctAnswerCounts.ContainsKey(playerID))
                correctAnswerCounts[playerID] = 0;
            correctAnswerCounts[playerID]++;
        }
        
        playerAnswersData[playerID] = answerData;
        photonView.RPC("SyncPlayerAnswersNumerical", RpcTarget.All, playerID, isCorrect, numericalAnswer, numericalAnswer, questionTimer);

        // Выводим отладочную информацию о количестве ответивших игроков
        Debug.Log($"Ответили {playerAnswersData.Count} из {PhotonNetwork.CurrentRoom.PlayerCount} игроков (числовой вопрос)");

        // Проверяем, ответили ли все игроки
        if (playerAnswersData.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("Все игроки ответили, показываем правильный ответ (числовой вопрос)");
            photonView.RPC("StopTimerAndShowCorrectAnswer", RpcTarget.All);
            StartAnswerDisplayTimer(); // Запускаем таймер показа ответа
        }
    }

    [PunRPC]
    private void SyncPlayerAnswersNumerical(int playerID, bool isCorrect, string numericalAnswer, string selectedAnswerText, float remainingTime)
    {
        if (!playerAnswersData.ContainsKey(playerID))
        {
            playerAnswersData[playerID] = new PlayerAnswerData();
        }

        playerAnswersData[playerID].IsCorrect = isCorrect;
        playerAnswersData[playerID].NumericalAnswer = numericalAnswer;
        playerAnswersData[playerID].SelectedAnswerText = selectedAnswerText;
        playerAnswersData[playerID].RemainingTime = remainingTime;

        if (timerAvatarManager != null)
        {
            timerAvatarManager.AddPlayerAvatarToTimer(playerID, remainingTime);
        }
    }

    [PunRPC]
    private void StopTimerAndShowCorrectAnswer()
    {
        isTimerRunning = false; // Останавливаем таймер вопроса
        canAcceptAnswers = false; // ВАЖНО: Блокируем прием новых ответов
        questionTimer = 0f; // Обнуляем таймер вопроса

        // Отключаем кнопки ответов
        if (questionManager != null)
        {
            questionManager.SetAnswerButtonsInteractable(false);
        }

        // Показываем правильный ответ
        ShowCorrectAnswer();
    }

    [PunRPC]
    private void SyncPlayerAnswers(int playerID, bool isCorrect, string selectedAnswerText, float remainingTime)
    {
        if (!playerAnswersData.ContainsKey(playerID))
        {
            playerAnswersData[playerID] = new PlayerAnswerData();
        }

        playerAnswersData[playerID].IsCorrect = isCorrect;
        playerAnswersData[playerID].SelectedAnswerText = selectedAnswerText;
        playerAnswersData[playerID].RemainingTime = remainingTime;

        if (timerAvatarManager != null)
        {
            timerAvatarManager.AddPlayerAvatarToTimer(playerID, remainingTime);
        }
    }

    [PunRPC]
    private void ShowCorrectAnswer()
    {
        canAcceptAnswers = false;

        // Отключаем кнопки ответов
        if (questionManager != null)
        {
            questionManager.SetAnswerButtonsInteractable(false);
        }

        // Скрываем аватары игроков на таймере
        if (timerAvatarManager != null)
        {
            timerAvatarManager.HideAllAvatars();
        }

        // Проверяем тип текущего вопроса
        QuestionData currentQuestion = questionManager.GetCurrentQuestion();
        
        if (currentQuestion != null && currentQuestion.isNumerical)
        {
            // Для числовых вопросов показываем колесо ответов
            ShowNumericalAnswerWheel();
        }
        else
        {
            // Для обычных вопросов показываем аватары на кнопках
            questionManager.ShowPlayerAvatarsOnAnswers(playerAnswersData);
        }

        questionManager.ShowCorrectAnswer();
        UpdatePlayerScore();
    }

    private void ShowNumericalAnswerWheel()
    {
        if (numericalAnswerWheel == null)
        {
            Debug.LogWarning("NumericalAnswerWheelManager не настроен!");
            return;
        }

        // Собираем ответы игроков из playerAnswersData
        Dictionary<int, string> playerNumericalAnswers = new Dictionary<int, string>();
        
        foreach (var kvp in playerAnswersData)
        {
            int playerId = kvp.Key;
            PlayerAnswerData answerData = kvp.Value;
            
            // Для числовых вопросов используем NumericalAnswer
            if (!string.IsNullOrEmpty(answerData.NumericalAnswer))
            {
                playerNumericalAnswers[playerId] = answerData.NumericalAnswer;
            }
        }

        // Получаем правильный ответ
        QuestionData currentQuestion = questionManager.GetCurrentQuestion();
        if (currentQuestion != null)
        {
            numericalAnswerWheel.ShowPlayerAnswersOnWheel(currentQuestion.correctAnswer, playerNumericalAnswers);
            Debug.Log($"Показано колесо ответов для {playerNumericalAnswers.Count} игроков");
        }
    }

    // 🔥 BOMB MODE: обнуляем очки при неправильном ответе или отсутствии ответа
    private void UpdatePlayerScore()
    {
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;

        // 1. Получаем текущий вопрос СРАЗУ, чтобы использовать ниже
        QuestionData currentQuestion = questionManager.GetCurrentQuestion();

        // 2. Проверяем, есть ли данные об ответе игрока
        bool hasAnswerData = playerAnswersData.TryGetValue(playerId, out PlayerAnswerData answerData);

        /* ------------------------------------------------------------------
        🔥 3. BOMB MODE: обнуляем очки в следующих случаях:
            • игрок вообще не ответил (нет данных об ответе) ИЛИ
            • ответ неверный И (не числовой ИЛИ ошибка по числу > 20%)
        ------------------------------------------------------------------ */
        if (currentMode == QuizMode.Bomb)
        {
            bool gaveAnswer = hasAnswerData;
            bool correct = gaveAnswer && answerData.IsCorrect;

            // Если игрок вообще не ответил - сбрасываем очки
            if (!gaveAnswer)
            {
                Debug.Log($"Игрок {playerId} не ответил в Bomb-режиме — очки сбрасываются");
                score = 0;
                scoreText.text = "Очки: " + score;
                photonView.RPC("UpdateScore", RpcTarget.All, playerId, score);
                return; // ⛔ прекращаем дальнейшее начисление
            }

            // Если ответ неправильный - проверяем дополнительные условия
            if (!correct)
            {
                bool isNumerical = currentQuestion != null && currentQuestion.isNumerical;
                bool isInAllowedRange = false;

                if (isNumerical && !string.IsNullOrEmpty(answerData.NumericalAnswer))
                {
                    if (float.TryParse(answerData.NumericalAnswer, out float playerNum) &&
                        float.TryParse(currentQuestion.correctAnswer, out float correctNum))
                    {
                        float error = Mathf.Abs(playerNum - correctNum) / Mathf.Abs(correctNum) * 100f;
                        isInAllowedRange = error <= 20f;
                        Debug.Log($"[Bomb Mode] Числовой ответ. Ошибка: {error:F1}% → в допустимом диапазоне? {isInAllowedRange}");
                    }
                }

                // Сбрасываем очки, если НЕ в диапазоне
                if (!isInAllowedRange)
                {
                    Debug.Log($"Игрок {playerId} ошибся в Bomb-режиме — очки сбрасываются");
                    score = 0;
                    scoreText.text = "Очки: " + score;
                    photonView.RPC("UpdateScore", RpcTarget.All, playerId, score);
                    return; // ⛔ прекращаем дальнейшее начисление
                }
            }
        }

        // Если нет данных об ответе (и это не Bomb режим), просто выходим
        if (!hasAnswerData)
        {
            Debug.LogWarning($"Нет данных об ответе игрока {playerId}");
            return;
        }

        /* ------------------------------------------------------------------
        4. Начисляем очки (обычная логика)
        ------------------------------------------------------------------ */
        int totalPoints = 0;

        // ✅ Правильный ответ
        if (answerData.IsCorrect)
        {
            consecutiveCorrectAnswers++;

            bool isQuickAnswer = answerData.RemainingTime > (maxQuestionTime - 3f);
            TaskManager.Instance?.OnCorrectAnswer(isQuickAnswer);

            int basePoints = currentQuestion.isNumerical ? 200 : 100;
            totalPoints += basePoints;

            int orderPoints = CalculateOrderPoints(playerId);
            totalPoints += orderPoints;

            float timePercentage = Mathf.Clamp01(answerData.RemainingTime / maxQuestionTime);
            int timeBonus = Mathf.RoundToInt(50 * timePercentage);
            totalPoints += timeBonus;

            Debug.Log($"Игрок {playerId} — правильный ответ! Базовые: {basePoints}, за очередь: {orderPoints}, за время: {timeBonus}");
        }
        else // ❌ Неправильный ответ
        {
            consecutiveCorrectAnswers = 0;
            TaskManager.Instance?.OnIncorrectAnswer();

            if (currentQuestion.isNumerical)
            {
                int proximityPoints = CalculateProximityPoints(playerId);
                if (proximityPoints > 0)
                {
                    totalPoints += proximityPoints;
                    Debug.Log($"Игрок {playerId} — очки за близость: {proximityPoints}");
                }
                else
                {
                    Debug.Log($"Игрок {playerId} — слишком далеко, очки не начислены");
                }
            }
            else
            {
                Debug.Log($"Игрок {playerId} — неправильный ответ на обычный вопрос");
            }
        }

        // 5. Double Points режим
        if (currentMode == QuizMode.DoublePoints && totalPoints > 0)
        {
            totalPoints *= 2;
            Debug.Log($"[DoublePoints] Итоговые очки удвоены: {totalPoints}");
        }

        // 6. Финальное обновление счёта
        if (totalPoints > 0)
        {
            score += totalPoints;
            scoreText.text = "Очки: " + score;
            photonView.RPC("UpdateScore", RpcTarget.All, playerId, score);
        }
    }

    private int CalculateOrderPoints(int playerID)
    {
        // Определяем позицию игрока в порядке ответов
        int position = answerOrder.IndexOf(playerID);
        
        if (position < 0) return 0; // Если игрок не в списке ответивших
        
        // Назначаем очки в зависимости от позиции
        switch (position)
        {
            case 0: return 50; // Первый ответивший
            case 1: return 25; // Второй ответивший
            case 2: return 10; // Третий ответивший
            default: return 5; // Все остальные
        }
    }

    private int CalculateProximityPoints(int playerID)
    {
        // Получаем ответ игрока из playerAnswersData
        if (!playerAnswersData.TryGetValue(playerID, out PlayerAnswerData answerData) || 
            string.IsNullOrEmpty(answerData.NumericalAnswer))
        {
            Debug.Log($"Нет числового ответа для игрока {playerID}");
            return 0;
        }
        
        string playerAnswer = answerData.NumericalAnswer;
        string correctAnswer = questionManager.GetCurrentQuestion().correctAnswer;
        
        // Преобразуем ответы в числа для сравнения
        if (!float.TryParse(playerAnswer, out float playerNum) || 
            !float.TryParse(correctAnswer, out float correctNum))
        {
            Debug.Log($"Не удалось преобразовать ответы в числа. Игрок: {playerAnswer}, Правильный: {correctAnswer}");
            return 0;
        }
        
        // Если ответ точный, возвращаем 0 (основные очки уже начислены)
        if (Mathf.Abs(playerNum - correctNum) < 0.001f)
        {
            return 0;
        }
        
        // Вычисляем абсолютную разность
        float difference = Mathf.Abs(playerNum - correctNum);
        
        // Вычисляем процентную ошибку относительно правильного ответа
        float percentageError = (difference / Mathf.Abs(correctNum)) * 100f;
        
        // Если ошибка больше 20%, очков не даём
        if (percentageError > 20f)
        {
            Debug.Log($"Игрок {playerID}: ошибка {percentageError:F1}% > 20%, очки не начисляются");
            return 0;
        }
        
        // Рассчитываем очки в зависимости от близости
        // Максимум 150 очков при минимальной ошибке
        // Линейное уменьшение до 0 очков при 20% ошибке
        float proximityRatio = 1f - (percentageError / 20f); // от 1 (0% ошибки) до 0 (20% ошибки)
        int proximityPoints = Mathf.RoundToInt(150f * proximityRatio);
        
        Debug.Log($"Игрок {playerID}: ответ {playerNum}, правильный {correctNum}, ошибка {percentageError:F1}%, очки за близость: {proximityPoints}");
        
        return proximityPoints;
    }

    [PunRPC]
    private void UpdateScore(int playerId, int newScore)
    {
        allPlayerScores[playerId] = newScore;

        if (playerId == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            scoreText.text = "Очки: " + newScore;
        }

        playerScoreDisplay?.UpdateScoreDisplay(allPlayerScores);
    }

    private void NextQuestion()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Проверяем, не достигли ли мы максимального количества вопросов
        if (questionCounter >= maxQuestions)
        {
            EndGame();
            return;
        }
        
        // Увеличиваем счетчик только если квиз уже начался
        if (isQuizStarted) {
            questionCounter++;
        } else {
            isQuizStarted = true;
        }

        // Получаем уникальный индекс вопроса из QuestionManager
        int randomIndex = questionManager.GetUniqueQuestionIndex();

        // Отправляем индекс вопроса и номер текущего вопроса всем игрокам
        photonView.RPC("SetQuestion", RpcTarget.All, randomIndex, questionCounter);
    }

    private void EndGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        Debug.Log("Game ended! Showing results...");
        
        // ДОБАВЛЯЕМ: Отслеживание сыгранной игры
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnGamePlayed();
            
            // Проверяем, выиграл ли локальный игрок
            int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
            if (allPlayerScores.ContainsKey(localPlayerId))
            {
                // Проверяем, является ли локальный игрок победителем
                int maxScore = 0;
                foreach (var scoreKvp in allPlayerScores)
                {
                    if (scoreKvp.Value > maxScore)
                        maxScore = scoreKvp.Value;
                }
                
                if (allPlayerScores[localPlayerId] == maxScore)
                {
                    TaskManager.Instance.OnGameWon();
                }
            }
        }
        
        // Serialize scores and correct answers to send via RPC
        string serializedScores = SerializeScores(allPlayerScores);
        string serializedCorrectAnswers = SerializeCorrectAnswers(correctAnswerCounts);
        
        // Send RPC to all players to show end game screen
        photonView.RPC("ShowEndGameScreen", RpcTarget.All, serializedScores, serializedCorrectAnswers);
    }
    
    // Helper method to serialize scores
    private string SerializeScores(Dictionary<int, int> scores)
    {
        string result = "";
        foreach (var kvp in scores)
        {
            if (result.Length > 0) result += ";";
            result += kvp.Key + ":" + kvp.Value;
        }
        return result;
    }

    // Helper method to serialize correct answers
    private string SerializeCorrectAnswers(Dictionary<int, int> correctAnswers)
    {
        string result = "";
        foreach (var kvp in correctAnswers)
        {
            if (result.Length > 0) result += ";";
            result += kvp.Key + ":" + kvp.Value;
        }
        return result;
    }

    // Helper method to deserialize correct answers
    private Dictionary<int, int> DeserializeCorrectAnswers(string serializedCorrectAnswers)
    {
        Dictionary<int, int> correctAnswers = new Dictionary<int, int>();
        
        if (string.IsNullOrEmpty(serializedCorrectAnswers))
            return correctAnswers;
        
        string[] pairs = serializedCorrectAnswers.Split(';');
        foreach (string pair in pairs)
        {
            string[] parts = pair.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int playerId) && int.TryParse(parts[1], out int count))
                {
                    correctAnswers[playerId] = count;
                }
            }
        }
        
        return correctAnswers;
    }

    // Helper method to deserialize scores
    private Dictionary<int, int> DeserializeScores(string serializedScores)
    {
        Dictionary<int, int> scores = new Dictionary<int, int>();

        if (string.IsNullOrEmpty(serializedScores))
            return scores;

        string[] pairs = serializedScores.Split(';');
        foreach (string pair in pairs)
        {
            string[] parts = pair.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int playerId) && int.TryParse(parts[1], out int score))
                {
                    scores[playerId] = score;
                }
            }
        }

        return scores;
    }

    [PunRPC]
    private void ShowEndGameScreen(string serializedScores, string serializedCorrectAnswers)
    {
        var scores = DeserializeScores(serializedScores);
        var correctAnswers = DeserializeCorrectAnswers(serializedCorrectAnswers);
        
        GameEndManager endManager = GetComponent<GameEndManager>();
        if (endManager != null)
        {
            endManager.DisplayWinner(scores, correctAnswers);
        }
        else
        {
            Debug.LogWarning("GameEndManager не найден для отображения результатов игры");
        }
    }

    public void SetRouletteFinished()
    {
        isRouletteFinished = true;
    }

    // Публичный метод для проверки, можно ли принимать ответы (для использования в QuestionManager)
    public bool CanAcceptAnswers()
    {
        return canAcceptAnswers;
    }

    // Публичный метод — вызывается из ChatUI при нажатии кнопки
    public void SendChatMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var local = PhotonNetwork.LocalPlayer;
        int avatarIndex = local.CustomProperties.ContainsKey("AvatarIndex")
            ? (int)local.CustomProperties["AvatarIndex"]
            : 0;

        photonView.RPC(nameof(ReceiveChatMessage), RpcTarget.All,
            local.NickName, text.Trim(), avatarIndex);
    }

    [PunRPC]
    private void ReceiveChatMessage(string nickName, string text, int avatarIndex)
    {
        chatUI.AddMessage(nickName, text, avatarIndex);
    }
}