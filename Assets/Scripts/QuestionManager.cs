using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// Класс для управления вопросами и их отображением в викторине
/// </summary>
public class QuestionManager : MonoBehaviourPunCallbacks
{
    private List<QuestionData> questions;
    
    [Header("UI References")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button[] answerButtons;
    // [SerializeField] private Sprite selectedSprite;
    [SerializeField] private TMP_Text questionCounterText;
    
    [Header("Numerical Question Settings")]
    [SerializeField] private TMP_InputField numberAnswerInput;
    [SerializeField] private Button confirmAnswerButton;
    [SerializeField] private TMP_Text correctAnswerText;

    [Header("Numerical Answer Wheel")]
    [SerializeField] private NumericalAnswerWheelManager numericalAnswerWheel;

    private Dictionary<int, string> numericalPlayerAnswers = new Dictionary<int, string>();

    [Header("Player Avatar Marker")]
    [SerializeField] private GameObject playerAvatarMarkerPrefab;

    [Header("Visual Feedback")]
    [SerializeField] private Sprite defaultButtonSprite; // Обычный спрайт кнопки
    [SerializeField] private Sprite selectedSprite; // Спрайт для выбранной кнопки
    [SerializeField] private Color defaultButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = Color.yellow; // Цвет для выбранной кнопки

    
    // Приватные переменные
    private QuestionData currentQuestion;
    private List<int> usedQuestionIndices = new List<int>();
    private int currentQuestionIndex = 0;
    private int totalQuestions;
    private bool hasAnswered = false;
    private bool lastQuestionWasNumerical = false;

    [SerializeField] private GameObject correctAnswerCanvas;
    [SerializeField] private TMP_Text correctAnswerDisplayText;

    private int maxQuestions;
    
    // События для взаимодействия с QuizManager
    public delegate void QuestionAnsweredEventHandler(int playerID, bool isCorrect, string selectedAnswerText, float remainingTime);
    public event QuestionAnsweredEventHandler OnQuestionAnswered;
    
    public delegate void NumericalQuestionAnsweredEventHandler(int playerID, bool isCorrect, string answer, float remainingTime);
    public event NumericalQuestionAnsweredEventHandler OnNumericalQuestionAnswered;

    public void Initialize(int maxQuestions)
    {
        this.maxQuestions = maxQuestions;

        if (numberAnswerInput != null)
        {
            numberAnswerInput.onEndEdit.AddListener(HandleNumberInput);
            numberAnswerInput.onValidateInput += ValidateDigitOnly;
        }

        if (confirmAnswerButton != null)
        {
            confirmAnswerButton.onClick.AddListener(SubmitNumberAnswer);
        }

        // Загружаем вопросы из GoogleSheetsQuestionDownloader
        questions = GoogleSheetsQuestionDownloader.Instance.loadedQuestions;
        totalQuestions = questions.Count;

        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("Вопросы не загружены или список пуст");
            return;
        }

        UpdateQuestionCounter();
    }

    private char ValidateDigitOnly(string text, int charIndex, char addedChar)
    {
        return char.IsDigit(addedChar) ? addedChar : '\0';
    }
  
    public void SetQuestion(int index, int questionNumber)
    {
        ResetQuestionState();

        currentQuestionIndex = questionNumber - 1;

        if (index < 0 || index >= questions.Count)
        {
            Debug.LogError($"Неверный индекс вопроса: {index}");
            return;
        }

        currentQuestion = questions[index];

        if (currentQuestion == null)
        {
            Debug.LogError($"Вопрос по индексу {index} — null");
            return;
        }

        if (currentQuestion.isNumerical)
        {
            SetupNumericalQuestion();
        }
        else
        {
            SetupMultipleChoiceQuestion();
        }

        if (correctAnswerCanvas != null)
        {
            correctAnswerCanvas.SetActive(false);
        }

        // Очищаем все AnswerAvatarContainer у всех кнопок
        for (int i = 0; i < answerButtons.Length; i++)
        {
            Transform container = answerButtons[i].transform.Find("AnswerAvatarContainer");

            if (container != null)
            {
                foreach (Transform child in container)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        UpdateQuestionCounter();
    }
    
    public void ResetQuestionState()
    {
        // Reset button states
        foreach (Button btn in answerButtons) 
        {
            btn.gameObject.SetActive(false);
            btn.interactable = true;
            
            // Сбрасываем визуальное состояние кнопки
            Image buttonImage = btn.GetComponent<Image>();
            if (defaultButtonSprite != null)
                buttonImage.sprite = defaultButtonSprite;
            buttonImage.color = defaultButtonColor;
        }
        
        // Reset input elements
        if (numberAnswerInput != null)
        {
            numberAnswerInput.gameObject.SetActive(false);
            numberAnswerInput.interactable = true;
            numberAnswerInput.text = "";
        }
        
        if (confirmAnswerButton != null)
        {
            confirmAnswerButton.gameObject.SetActive(false);
            confirmAnswerButton.interactable = true;
        }
        
        if (correctAnswerText != null)
        {
            correctAnswerText.gameObject.SetActive(false);
        }
        
        // Очищаем ответы игроков на числовые вопросы
        numericalPlayerAnswers.Clear();
        
        // Очищаем колесо ответов
        if (numericalAnswerWheel != null)
        {
            numericalAnswerWheel.ClearAllMarkers();
        }

        // Reset internal state
        hasAnswered = false;
    }

    private void SetupNumericalQuestion()
    {
        questionText.text = currentQuestion.question;

        numberAnswerInput.gameObject.SetActive(true);
        confirmAnswerButton.gameObject.SetActive(true);
        numberAnswerInput.interactable = true;
        confirmAnswerButton.interactable = false;
        StartCoroutine(FocusInputField());
    }
    
    private IEnumerator FocusInputField()
    {
        yield return null;

        numberAnswerInput.Select();
        numberAnswerInput.ActivateInputField();
    }
    
    private void SetupMultipleChoiceQuestion()
    {
        questionText.text = currentQuestion.question;

        // Создаём копию массива ответов и перемешиваем
        List<string> shuffledOptions = new List<string>(currentQuestion.options);
        Shuffle(shuffledOptions);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < shuffledOptions.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<TMP_Text>().text = shuffledOptions[i];

                // Устанавливаем начальное визуальное состояние
                Image buttonImage = answerButtons[i].GetComponent<Image>();
                if (defaultButtonSprite != null)
                    buttonImage.sprite = defaultButtonSprite;
                buttonImage.color = defaultButtonColor;

                answerButtons[i].interactable = true;
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }
    
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]); // Меняем местами элементы
        }
    }
    
    public void Answer(int index)
    {
        if (!answerButtons[index].interactable || hasAnswered) return;

        hasAnswered = true;

        // Запускаємо анімацію вибору (IsConfirmedPress)
        Animator animator = answerButtons[index].GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("IsConfirmedPress");
        }

        DisableAllButtons();

        bool isCorrect = answerButtons[index].GetComponentInChildren<TMP_Text>().text == currentQuestion.correctAnswer;

        string selectedAnswerText = answerButtons[index].GetComponentInChildren<TMP_Text>().text;
        OnQuestionAnswered?.Invoke(PhotonNetwork.LocalPlayer.ActorNumber, isCorrect, selectedAnswerText, 0f);
    }

    private void DisableAllButtons()
    {
        foreach (Button button in answerButtons)
        {
            button.interactable = false;
        }
    }
    
    private void HandleNumberInput(string input)
    {
        if (currentQuestion == null || !currentQuestion.isNumerical) return;
        
        confirmAnswerButton.interactable = !string.IsNullOrEmpty(input);
    }
    
    private void SubmitNumberAnswer()
    {
        if (hasAnswered || !numberAnswerInput.gameObject.activeSelf) return;

        string answer = numberAnswerInput.text.Trim();
        bool isCorrect = answer == currentQuestion.correctAnswer;
        
        hasAnswered = true;
        confirmAnswerButton.interactable = false;
        numberAnswerInput.interactable = false;

        // Вызываем событие для QuizManager
        if (OnNumericalQuestionAnswered != null)
        {
            OnNumericalQuestionAnswered(PhotonNetwork.LocalPlayer.ActorNumber, isCorrect, answer, 0f); // Оставшееся время будет установлено в QuizManager
        }
    }
    
    public void UpdateNumericalQuestionUI(bool enabled)
    {
        if (confirmAnswerButton != null)
        {
            confirmAnswerButton.interactable = 
                enabled && !string.IsNullOrEmpty(numberAnswerInput.text) && 
                !hasAnswered;
        }
    }
    
    public void ShowCorrectAnswer()
    {
        if (currentQuestion.isNumerical)
        {
            ShowCorrectNumericalAnswer();
        }
        else
        {
            ShowCorrectMultipleChoiceAnswer();
        }
    }
    
    private void ShowCorrectNumericalAnswer()
    {
        if (correctAnswerCanvas != null && correctAnswerDisplayText != null)
        {
            correctAnswerDisplayText.text = $"{currentQuestion.correctAnswer}";
            correctAnswerCanvas.SetActive(true);
        }

        // Показываем ответы игроков на колесе (если есть менеджер колеса)
        if (numericalAnswerWheel != null && numericalPlayerAnswers.Count > 0)
        {
            numericalAnswerWheel.ShowPlayerAnswersOnWheel(currentQuestion.correctAnswer, numericalPlayerAnswers);
        }

        // Скрываем инпут и кнопку подтверждения
        if (numberAnswerInput != null)
        {
            numberAnswerInput.gameObject.SetActive(false);
        }

        if (confirmAnswerButton != null)
        {
            confirmAnswerButton.gameObject.SetActive(false);
        }
    }
    
    public void AddNumericalPlayerAnswer(int playerId, string answer)
    {
        numericalPlayerAnswers[playerId] = answer;
        Debug.Log($"Сохранен ответ игрока {playerId}: {answer}");
    }

    private void ShowCorrectMultipleChoiceAnswer()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i].gameObject.activeSelf &&
                answerButtons[i].GetComponentInChildren<TMP_Text>().text == currentQuestion.correctAnswer)
            {
                Animator animator = answerButtons[i].GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("CorrectAnswer");
                }
            }
        }
    }

    // public void ShowPlayerAvatarsOnCorrectAnswer(List<int> playerIdsWhoAnsweredCorrect)
    // {
    //     // Находим индекс кнопки с правильным ответом
    //     int correctButtonIndex = -1;
    //     for (int i = 0; i < answerButtons.Length; i++)
    //     {
    //         if (answerButtons[i].gameObject.activeSelf &&
    //             answerButtons[i].GetComponentInChildren<TMP_Text>().text == currentQuestion.correctAnswer)
    //         {
    //             correctButtonIndex = i;
    //             break;
    //         }
    //     }

    //     if (correctButtonIndex == -1)
    //     {
    //         Debug.LogWarning("Не найдена кнопка с правильным ответом для отображения аватаров.");
    //         return;
    //     }

    //     // Находим контейнер прямо внутри кнопки
    //     Transform container = answerButtons[correctButtonIndex].transform.Find("AnswerAvatarContainer");

    //     if (container == null)
    //     {
    //         Debug.LogError("AnswerAvatarContainer не найден внутри кнопки!");
    //         return;
    //     }

    //     // Очищаем контейнер перед добавлением
    //     foreach (Transform child in container)
    //     {
    //         Destroy(child.gameObject);
    //     }

    //     // Для каждого игрока создаём маркер
    //     foreach (int playerId in playerIdsWhoAnsweredCorrect)
    //     {
    //         GameObject avatarMarker = Instantiate(playerAvatarMarkerPrefab, container);

    //         // Пример: текст с playerId (можно заменить на картинку)
    //         TMP_Text avatarText = avatarMarker.GetComponentInChildren<TMP_Text>();
    //         if (avatarText != null)
    //         {
    //             avatarText.text = playerId.ToString();
    //         }
    //     }

    //     Debug.Log($"Добавил {playerIdsWhoAnsweredCorrect.Count} аватаров в AnswerAvatarContainer кнопки {correctButtonIndex}");
    // }

    public void ShowPlayerAvatarsOnAnswers(Dictionary<int, QuizManager.PlayerAnswerData> playerAnswersData)
    {
        // Для числовых вопросов не показываем аватары на кнопках
        if (currentQuestion != null && currentQuestion.isNumerical)
        {
            Debug.Log("Числовой вопрос - аватары будут показаны на колесе, а не на кнопках");
            return;
        }

        // Очищаем контейнеры аватаров на всех кнопках
        for (int i = 0; i < answerButtons.Length; i++)
        {
            Transform container = answerButtons[i].transform.Find("AnswerAvatarContainer");

            if (container != null)
            {
                foreach (Transform child in container)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // Показываем аватары только для обычных вопросов
        foreach (var kvp in playerAnswersData)
        {
            int playerId = kvp.Key;
            string selectedAnswerText = kvp.Value.SelectedAnswerText;

            // Ищем кнопку с соответствующим ответом
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i].gameObject.activeSelf &&
                    answerButtons[i].GetComponentInChildren<TMP_Text>().text == selectedAnswerText)
                {
                    Transform container = answerButtons[i].transform.Find("AnswerAvatarContainer");

                    if (container != null)
                    {
                        GameObject avatarMarker = Instantiate(playerAvatarMarkerPrefab, container);

                        RectTransform rt = avatarMarker.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.sizeDelta = new Vector2(30f, 30f);
                        }

                        Image avatarImage = avatarMarker.GetComponent<Image>();
                        if (avatarImage != null)
                        {
                            var player = PhotonNetwork.CurrentRoom.GetPlayer(playerId);
                            if (player != null && player.CustomProperties.TryGetValue("AvatarIndex", out object avatarIndexObj))
                            {
                                int avatarIndex = (int)avatarIndexObj;
                                Sprite avatarSprite = AvatarManager.Instance.GetAvatar(avatarIndex);
                                if (avatarSprite != null)
                                {
                                    avatarImage.sprite = avatarSprite;
                                }
                            }
                        }
                    }

                    break;
                }
            }
        }

        Debug.Log("Показаны аватары на кнопках для обычного вопроса.");
    }
    
    public int GetUniqueQuestionIndex()
    {
        int newIndex;
        int attempts = 0;
        int maxAttempts = 100; // предотвращаем бесконечный цикл

        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("Вопросы не загружены.");
            return 0;
        }

        do
        {
            newIndex = Random.Range(0, questions.Count);
            attempts++;

            // Если слишком много попыток, просто используем последний индекс
            if (attempts > maxAttempts)
            {
                Debug.LogWarning("Достигнуто максимальное количество попыток поиска подходящего вопроса");
                break;
            }
            
        } while ((usedQuestionIndices.Contains(newIndex) && usedQuestionIndices.Count < questions.Count) || 
                (lastQuestionWasNumerical && questions[newIndex].isNumerical));

        // Добавляем индекс в список использованных
        usedQuestionIndices.Add(newIndex);

        // Запоминаем тип текущего вопроса для следующего раза
        lastQuestionWasNumerical = questions[newIndex].isNumerical;

        // Если использовали все вопросы из базы, очищаем список
        if (usedQuestionIndices.Count >= questions.Count)
        {
            usedQuestionIndices.Clear();
            // Сбрасываем флаг при очистке списка использованных вопросов
            lastQuestionWasNumerical = false;
        }

        Debug.Log($"Выбран вопрос с индексом {newIndex}, числовой: {questions[newIndex].isNumerical}");
        return newIndex;
    }

    public void SetAnswerButtonsInteractable(bool interactable)
    {
        // Отключаем/включаем обычные кнопки ответов
        if (answerButtons != null)
        {
            foreach (Button button in answerButtons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
        
        // Отключаем/включаем кнопку отправки для числовых вопросов
        if (confirmAnswerButton != null)
        {
            confirmAnswerButton.interactable = interactable;
        }
        
        Debug.Log($"Кнопки ответов установлены в состояние: {interactable}");
    }

    // Также добавьте метод для сброса состояния (если потребуется):
    public void ResetQuestionHistory()
    {
        usedQuestionIndices.Clear();
        lastQuestionWasNumerical = false;
        Debug.Log("История вопросов сброшена");
    }
    
    public void UpdateQuestionCounter()
    {
        if (questionCounterText != null)
        {
            questionCounterText.text = $"{currentQuestionIndex + 1} / {maxQuestions}";
        }
    }
    
    public QuestionData GetCurrentQuestion()
    {
        return currentQuestion;
    }
    
    public bool IsCurrentQuestionNumerical()
    {
        return currentQuestion != null && currentQuestion.isNumerical;
    }
}