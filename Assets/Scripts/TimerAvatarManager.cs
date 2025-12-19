using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;

public class TimerAvatarManager : MonoBehaviour
{
    [Header("Timer References")]
    [SerializeField] private Slider timerSlider;
    [SerializeField] private RectTransform sliderFillArea; // Область слайдера для размещения аватаров

    [Header("Avatar Prefab")]
    [SerializeField] private GameObject avatarMarkerPrefab; // Префаб с Image компонентом для аватара

    [Header("Settings")]
    [SerializeField] private float avatarSize = 30f;
    [SerializeField] private float verticalOffset = 5f; // Смещение аватаров по вертикали от слайдера

    // Словарь для хранения маркеров аватаров игроков
    private Dictionary<int, GameObject> playerAvatarMarkers = new Dictionary<int, GameObject>();

    // Список всех активных маркеров для текущего вопроса
    private List<GameObject> currentQuestionMarkers = new List<GameObject>();

    private float maxQuestionTime;

    private void Awake()
    {
        // Получаем максимальное время вопроса из QuizManager
        QuizManager quizManager = Object.FindAnyObjectByType<QuizManager>();
        if (quizManager != null)
        {
            maxQuestionTime = quizManager.MaxQuestionTime;
        }
        else
        {
            maxQuestionTime = 5f; // Значение по умолчанию
        }
    }

    /// <summary>
    /// Добавляет аватар игрока на таймер в позиции, соответствующей времени ответа
    /// </summary>
    /// <param name="playerId">ID игрока</param>
    /// <param name="remainingTime">Оставшееся время когда игрок ответил</param>
    public void AddPlayerAvatarToTimer(int playerId, float remainingTime)
    {
        // Проверяем наличие необходимых компонентов
        if (timerSlider == null || avatarMarkerPrefab == null)
        {
            Debug.LogError("TimerAvatarManager: отсутствуют необходимые ссылки!");
            return;
        }

        // Вычисляем позицию на слайдере (от 0 до 1)
        float normalizedPosition = remainingTime / maxQuestionTime;
        normalizedPosition = Mathf.Clamp01(normalizedPosition);

        // Создаем маркер аватара
        GameObject avatarMarker = CreateAvatarMarker(playerId, normalizedPosition);

        if (avatarMarker != null)
        {
            // Сохраняем ссылку на маркер
            currentQuestionMarkers.Add(avatarMarker);

            Debug.Log($"Добавлен аватар игрока {playerId} на позицию {normalizedPosition:F2} (время: {remainingTime:F2})");
        }
    }

    /// <summary>
    /// Создает маркер аватара на слайдере
    /// </summary>
    private GameObject CreateAvatarMarker(int playerId, float normalizedPosition)
    {
        // Создаем экземпляр префаба
        GameObject marker = Instantiate(avatarMarkerPrefab);

        // Устанавливаем родителем область слайдера
        RectTransform parentTransform = sliderFillArea != null ? sliderFillArea : timerSlider.transform as RectTransform;
        marker.transform.SetParent(parentTransform, false);

        // Настраиваем RectTransform маркера
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        if (markerRect != null)
        {
            // Устанавливаем размер
            markerRect.sizeDelta = new Vector2(avatarSize, avatarSize);

            // Вычисляем позицию по X (позиция на слайдере)
            float sliderWidth = parentTransform.rect.width;
            float xPosition = (normalizedPosition * sliderWidth) - (sliderWidth * 0.5f);

            // Устанавливаем позицию с учетом вертикального смещения
            markerRect.anchoredPosition = new Vector2(xPosition, verticalOffset);

            // Устанавливаем якорь по центру
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        }

        // Загружаем аватар игрока
        LoadPlayerAvatar(marker, playerId);

        // Добавляем компонент для анимации (опционально)
        AddMarkerAnimation(marker);

        return marker;
    }

    /// <summary>
    /// Загружает аватар игрока и устанавливает его в маркер
    /// </summary>
    private void LoadPlayerAvatar(GameObject marker, int playerId)
    {
        Image avatarImage = marker.GetComponent<Image>();
        if (avatarImage == null)
        {
            Debug.LogError("TimerAvatarManager: префаб маркера должен содержать компонент Image!");
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
    /// Добавляет простую анимацию появления маркера
    /// </summary>
    private void AddMarkerAnimation(GameObject marker)
    {
        // Начинаем с нулевого масштаба
        marker.transform.localScale = Vector3.zero;

        // Анимируем появление (можно использовать DOTween если доступен)
        StartCoroutine(AnimateMarkerAppearance(marker));
    }

    private System.Collections.IEnumerator AnimateMarkerAppearance(GameObject marker)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration && marker != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Простая анимация масштабирования с easing
            float scale = Mathf.Lerp(0f, 1f, EaseOutBack(progress));
            marker.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (marker != null)
        {
            marker.transform.localScale = Vector3.one;
        }
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>
    /// Очищает все маркеры аватаров с таймера (вызывается при переходе к новому вопросу)
    /// </summary>
    public void ClearAllAvatarMarkers()
    {
        foreach (GameObject marker in currentQuestionMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        currentQuestionMarkers.Clear();
        Debug.Log("Очищены все маркеры аватаров с таймера");
    }

    /// <summary>
    /// Устанавливает максимальное время вопроса (если нужно изменить динамически)
    /// </summary>
    public void SetMaxQuestionTime(float time)
    {
        maxQuestionTime = time;
    }
    
    /// <summary>
    /// Скрывает все аватары игроков на таймере (не уничтожает)
    /// </summary>
    public void HideAllAvatars()
    {
        foreach (GameObject marker in currentQuestionMarkers)
        {
            if (marker != null)
            {
                marker.SetActive(false);
            }
        }

        Debug.Log("Все аватары на таймере скрыты");
    }
}