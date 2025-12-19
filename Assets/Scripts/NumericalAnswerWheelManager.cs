using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun;

/// <summary>
/// Менеджер для отображения ответов игроков на круглом "колесе" для числовых вопросов
/// </summary>
public class NumericalAnswerWheelManager : MonoBehaviour
{
    [Header("Wheel Settings")]
    [SerializeField] private RectTransform wheelCenter; // Центр колеса (где правильный ответ)
    [SerializeField] private float wheelRadius = 200f; // Радиус колеса
    [SerializeField] private float minDistanceFromCenter = 50f; // Минимальное расстояние от центра
    
    [Header("Avatar Settings")]
    [SerializeField] private GameObject playerAvatarPrefab; // Префаб с Image для аватара и TMP_Text для ответа
    [SerializeField] private float avatarSize = 60f;
    [SerializeField] private float minAngleBetweenAvatars = 15f; // Минимальный угол между аватарами (в градусах)
    
    [Header("Answer Display")]
    [SerializeField] private float accuracyThreshold = 0.8f; // 20% порог для отображения
    
    // Список всех созданных маркеров для текущего вопроса
    private List<GameObject> currentQuestionMarkers = new List<GameObject>();
    
    // Список занятых углов для предотвращения пересечений
    private List<float> occupiedAngles = new List<float>();

    /// <summary>
    /// Отображает ответы всех игроков на колесе
    /// </summary>
    /// <param name="correctAnswer">Правильный ответ</param>
    /// <param name="playerAnswers">Словарь с ответами игроков: playerID -> answer</param>
    public void ShowPlayerAnswersOnWheel(string correctAnswer, Dictionary<int, string> playerAnswers)
    {
        // Очищаем предыдущие маркеры
        ClearAllMarkers();
        
        if (!float.TryParse(correctAnswer, out float correctValue))
        {
            Debug.LogError($"Не удалось преобразовать правильный ответ '{correctAnswer}' в число");
            return;
        }

        foreach (var kvp in playerAnswers)
        {
            int playerId = kvp.Key;
            string answerText = kvp.Value;
            
            if (!float.TryParse(answerText, out float playerAnswer))
            {
                Debug.LogWarning($"Не удалось преобразовать ответ игрока {playerId} '{answerText}' в число");
                continue;
            }

            // Вычисляем точность ответа
            float accuracy = CalculateAccuracy(correctValue, playerAnswer);
            
            // Проверяем, стоит ли показывать этого игрока
            if (accuracy < accuracyThreshold)
            {
                Debug.Log($"Игрок {playerId} не показан на колесе (точность: {accuracy:P2})");
                continue;
            }

            // Создаем маркер для игрока
            CreatePlayerMarker(playerId, answerText, accuracy, correctValue == playerAnswer);
        }
    }

    /// <summary>
    /// Вычисляет точность ответа (от 0 до 1, где 1 = точный ответ)
    /// </summary>
    private float CalculateAccuracy(float correctValue, float playerAnswer)
    {
        if (correctValue == 0f)
        {
            return playerAnswer == 0f ? 1f : 0f;
        }

        float difference = Mathf.Abs(correctValue - playerAnswer);
        float relativeError = difference / Mathf.Abs(correctValue);
        
        // Преобразуем в точность (1 - относительная ошибка, но не меньше 0)
        return Mathf.Max(0f, 1f - relativeError);
    }

    /// <summary>
    /// Создает маркер игрока на колесе
    /// </summary>
    private void CreatePlayerMarker(int playerId, string answerText, float accuracy, bool isExact)
    {
        GameObject marker = Instantiate(playerAvatarPrefab);
        marker.transform.SetParent(wheelCenter, false);

        // Настраиваем RectTransform
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        if (markerRect != null)
        {
            markerRect.sizeDelta = new Vector2(avatarSize, avatarSize);
        }

        // Определяем позицию
        Vector2 position;
        if (isExact)
        {
            // Точный ответ - в центр
            position = Vector2.zero;
        }
        else
        {
            // Вычисляем расстояние от центра на основе точности
            float distanceFromCenter = Mathf.Lerp(wheelRadius, minDistanceFromCenter, accuracy);
            
            // Получаем свободный угол в правой части (от -90 до +90 градусов)
            float angle = GetAvailableAngle();
            
            // Преобразуем в позицию
            position = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distanceFromCenter,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distanceFromCenter
            );
        }

        // Устанавливаем позицию
        if (markerRect != null)
        {
            markerRect.anchoredPosition = position;
        }

        // Загружаем аватар игрока
        LoadPlayerAvatar(marker, playerId);
        
        // Устанавливаем текст ответа
        SetAnswerText(marker, answerText);

        // Добавляеманимацию появления
        AddMarkerAnimation(marker);

        // Сохраняем маркер
        currentQuestionMarkers.Add(marker);

        Debug.Log($"Создан маркер для игрока {playerId}: ответ '{answerText}', точность {accuracy:P2}, позиция {position}");
    }

    /// <summary>
    /// Получает свободный угол в правой части круга
    /// </summary>
    private float GetAvailableAngle()
    {
        float angle;
        int attempts = 0;
        int maxAttempts = 50;

        do
        {
            // Генерируем случайный угол в правой части (-90° до +90°)
            angle = Random.Range(-90f, 90f);
            attempts++;
            
            if (attempts > maxAttempts)
            {
                Debug.LogWarning("Не удалось найти свободный угол, используем случайный");
                break;
            }
            
        } while (!IsAngleAvailable(angle));

        // Добавляем угол в список занятых
        occupiedAngles.Add(angle);
        
        return angle;
    }

    /// <summary>
    /// Проверяет, свободен ли угол
    /// </summary>
    private bool IsAngleAvailable(float angle)
    {
        foreach (float occupiedAngle in occupiedAngles)
        {
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(angle, occupiedAngle));
            if (angleDifference < minAngleBetweenAvatars)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Загружает аватар игрока
    /// </summary>
    private void LoadPlayerAvatar(GameObject marker, int playerId)
    {
        Image avatarImage = marker.GetComponent<Image>();
        if (avatarImage == null)
        {
            // Попробуем найти Image в дочерних объектах
            avatarImage = marker.GetComponentInChildren<Image>();
        }

        if (avatarImage == null)
        {
            Debug.LogError("NumericalAnswerWheelManager: не найден компонент Image в префабе маркера!");
            return;
        }

        // Получаем данные игрока из Photon
        var player = PhotonNetwork.CurrentRoom.GetPlayer(playerId);
        if (player == null)
        {
            Debug.LogWarning($"Игрок с ID {playerId} не найден в комнате");
            return;
        }

        // Получаем индекс аватара из кастомных свойств игрока
        if (player.CustomProperties.TryGetValue("AvatarIndex", out object avatarIndexObj))
        {
            int avatarIndex = (int)avatarIndexObj;

            // Загружаем спрайт аватара через AvatarManager
            if (AvatarManager.Instance != null)
            {
                Sprite avatarSprite = AvatarManager.Instance.GetAvatar(avatarIndex);
                if (avatarSprite != null)
                {
                    avatarImage.sprite = avatarSprite;
                }
                else
                {
                    Debug.LogWarning($"Не удалось загрузить аватар с индексом {avatarIndex}");
                }
            }
            else
            {
                Debug.LogError("AvatarManager.Instance недоступен!");
            }
        }
        else
        {
            Debug.LogWarning($"У игрока {playerId} отсутствует информация об аватаре");
        }
    }

    /// <summary>
    /// Устанавливает текст ответа игрока
    /// </summary>
    private void SetAnswerText(GameObject marker, string answerText)
    {
        TMP_Text answerTextComponent = marker.GetComponentInChildren<TMP_Text>();
        if (answerTextComponent != null)
        {
            answerTextComponent.text = answerText;
        }
        else
        {
            Debug.LogWarning("NumericalAnswerWheelManager: не найден компонент TMP_Text в префабе маркера!");
        }
    }

    /// <summary>
    /// Добавляет анимацию появления маркера
    /// </summary>
    private void AddMarkerAnimation(GameObject marker)
    {
        // Начинаем с нулевого масштаба
        marker.transform.localScale = Vector3.zero;

        // Анимируем появление
        StartCoroutine(AnimateMarkerAppearance(marker));
    }

    private IEnumerator AnimateMarkerAppearance(GameObject marker)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration && marker != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Анимация масштабирования с easing
            float scale = Mathf.Lerp(0f, 1f, EaseOutElastic(progress));
            marker.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (marker != null)
        {
            marker.transform.localScale = Vector3.one;
        }
    }

    private float EaseOutElastic(float t)
    {
        float c4 = (2f * Mathf.PI) / 3f;
        return t == 0f ? 0f : t == 1f ? 1f : 
               Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    /// <summary>
    /// Очищает все маркеры с колеса
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (GameObject marker in currentQuestionMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        currentQuestionMarkers.Clear();
        occupiedAngles.Clear();
        
        Debug.Log("Очищены все маркеры с колеса ответов");
    }

    /// <summary>
    /// Скрывает все маркеры (не уничтожает)
    /// </summary>
    public void HideAllMarkers()
    {
        foreach (GameObject marker in currentQuestionMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(false);
            }
        }

        Debug.Log("Все маркеры на колесе скрыты");
    }

    /// <summary>
    /// Показывает все ранее скрытые маркеры
    /// </summary>
    public void ShowAllMarkers()
    {
        foreach (GameObject marker in currentQuestionMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(true);
            }
        }

        Debug.Log("Все маркеры на колесе показаны");
    }

    /// <summary>
    /// Устанавливает новые параметры колеса
    /// </summary>
    public void SetWheelParameters(float radius, float minDistance, float threshold)
    {
        wheelRadius = radius;
        minDistanceFromCenter = minDistance;
        accuracyThreshold = threshold;
    }
    
    /// <summary>
    /// Получает количество игроков на колесе
    /// </summary>
    public int GetPlayersOnWheelCount()
    {
        return currentQuestionMarkers.Count;
    }
}