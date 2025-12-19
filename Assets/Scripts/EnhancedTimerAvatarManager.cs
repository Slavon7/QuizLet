using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class EnhancedTimerAvatarManager : MonoBehaviour
{
    [Header("Timer References")]
    [SerializeField] private Slider timerSlider;
    [SerializeField] private RectTransform avatarContainer; // Отдельный контейнер для аватаров
    
    [Header("Avatar Prefab & Settings")]
    [SerializeField] private GameObject avatarMarkerPrefab;
    [SerializeField] private float avatarSize = 25f;
    [SerializeField] private float verticalOffset = 40f;
    [SerializeField] private bool stackOverlappingAvatars = true;
    [SerializeField] private float stackOffset = 15f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color correctAnswerGlow = Color.green;
    [SerializeField] private Color incorrectAnswerGlow = Color.red;
    [SerializeField] private bool showAnswerCorrectness = true;
    [SerializeField] private bool showPlayerNumbers = false;
    
    // Данные о маркерах
    private List<AvatarMarkerData> currentMarkers = new List<AvatarMarkerData>();
    private float maxQuestionTime;
    
    [System.Serializable]
    private class AvatarMarkerData
    {
        public GameObject markerObject;
        public int playerId;
        public float timePosition;
        public bool isCorrectAnswer;
        public int stackLevel;
    }

    private void Awake()
    {
        QuizManager quizManager = Object.FindAnyObjectByType<QuizManager>();
        if (quizManager != null)
        {
            maxQuestionTime = quizManager.MaxQuestionTime;
        }
        else
        {
            maxQuestionTime = 5f;
        }
        
        // Создаем контейнер для аватаров если не указан
        if (avatarContainer == null)
        {
            CreateAvatarContainer();
        }
    }

    private void CreateAvatarContainer()
    {
        GameObject container = new GameObject("AvatarContainer");
        avatarContainer = container.AddComponent<RectTransform>();
        
        // Устанавливаем контейнер как дочерний к слайдеру
        avatarContainer.SetParent(timerSlider.transform, false);
        
        // Настраиваем размер контейнера
        avatarContainer.anchorMin = Vector2.zero;
        avatarContainer.anchorMax = Vector2.one;
        avatarContainer.sizeDelta = Vector2.zero;
        avatarContainer.anchoredPosition = Vector2.zero;
    }

    public void AddPlayerAvatarToTimer(int playerId, float remainingTime, bool isCorrectAnswer = true)
    {
        if (timerSlider == null || avatarMarkerPrefab == null)
        {
            Debug.LogError("EnhancedTimerAvatarManager: отсутствуют необходимые ссылки!");
            return;
        }

        float normalizedPosition = remainingTime / maxQuestionTime;
        normalizedPosition = Mathf.Clamp01(normalizedPosition);

        // Проверяем наложения с существующими маркерами
        int stackLevel = 0;
        if (stackOverlappingAvatars)
        {
            stackLevel = CalculateStackLevel(normalizedPosition);
        }

        GameObject marker = CreateEnhancedAvatarMarker(playerId, normalizedPosition, isCorrectAnswer, stackLevel);
        
        if (marker != null)
        {
            var markerData = new AvatarMarkerData
            {
                markerObject = marker,
                playerId = playerId,
                timePosition = normalizedPosition,
                isCorrectAnswer = isCorrectAnswer,
                stackLevel = stackLevel
            };
            
            currentMarkers.Add(markerData);
            Debug.Log($"Добавлен аватар игрока {playerId} на позицию {normalizedPosition:F2}");
        }
    }

    private int CalculateStackLevel(float targetPosition)
    {
        const float overlapThreshold = 0.05f; // 5% от ширины слайдера
        
        var overlappingMarkers = currentMarkers
            .Where(m => Mathf.Abs(m.timePosition - targetPosition) < overlapThreshold)
            .ToList();
        
        if (overlappingMarkers.Count == 0)
            return 0;
        
        // Находим максимальный уровень стека и добавляем 1
        return overlappingMarkers.Max(m => m.stackLevel) + 1;
    }

    private GameObject CreateEnhancedAvatarMarker(int playerId, float normalizedPosition, bool isCorrectAnswer, int stackLevel)
    {
        GameObject marker = Instantiate(avatarMarkerPrefab);
        marker.transform.SetParent(avatarContainer, false);
        
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        if (markerRect != null)
        {
            markerRect.sizeDelta = new Vector2(avatarSize, avatarSize);
            
            // Вычисляем позицию
            float containerWidth = avatarContainer.rect.width;
            float xPosition = (normalizedPosition * containerWidth) - (containerWidth * 0.5f);
            float yPosition = verticalOffset + (stackLevel * stackOffset);
            
            markerRect.anchoredPosition = new Vector2(xPosition, yPosition);
            markerRect.anchorMin = new Vector2(0.5f, 0f);
            markerRect.anchorMax = new Vector2(0.5f, 0f);
        }
        
        // Настраиваем визуальные эффекты
        SetupMarkerVisuals(marker, playerId, isCorrectAnswer);
        
        // Анимация появления
        AddEnhancedAnimation(marker, stackLevel);
        
        return marker;
    }

    private void SetupMarkerVisuals(GameObject marker, int playerId, bool isCorrectAnswer)
    {
        // Основное изображение аватара
        Image avatarImage = marker.GetComponent<Image>();
        LoadPlayerAvatar(avatarImage, playerId);
        
        // Добавляем эффект свечения для правильности ответа
        if (showAnswerCorrectness)
        {
            AddGlowEffect(marker, isCorrectAnswer);
        }
        
        // Добавляем номер игрока
        if (showPlayerNumbers)
        {
            AddPlayerNumberText(marker, playerId);
        }
    }

    private void LoadPlayerAvatar(Image avatarImage, int playerId)
    {
        if (avatarImage == null) return;

        var player = PhotonNetwork.CurrentRoom.GetPlayer(playerId);
        if (player?.CustomProperties.TryGetValue("AvatarIndex", out object avatarIndexObj) == true)
        {
            int avatarIndex = (int)avatarIndexObj;
            
            if (AvatarManager.Instance != null)
            {
                Sprite avatarSprite = AvatarManager.Instance.GetAvatar(avatarIndex);
                if (avatarSprite != null)
                {
                    avatarImage.sprite = avatarSprite;
                }
            }
        }
    }

    private void AddGlowEffect(GameObject marker, bool isCorrectAnswer)
    {
        // Создаем объект для эффекта свечения
        GameObject glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(marker.transform, false);
        
        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.sprite = marker.GetComponent<Image>().sprite;
        glowImage.color = isCorrectAnswer ? correctAnswerGlow : incorrectAnswerGlow;
        
        // Настраиваем RectTransform для свечения
        RectTransform glowRect = glowObject.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.sizeDelta = Vector2.one * 5f; // Немного больше основного изображения
        glowRect.anchoredPosition = Vector2.zero;
        
        // Размещаем за основным изображением
        glowObject.transform.SetSiblingIndex(0);
        
        // Добавляем анимацию пульсации
        StartCoroutine(AnimateGlow(glowImage));
    }

    private System.Collections.IEnumerator AnimateGlow(Image glowImage)
    {
        while (glowImage != null)
        {
            float alpha = (Mathf.Sin(Time.time * 3f) + 1f) * 0.25f + 0.1f;
            Color color = glowImage.color;
            color.a = alpha;
            glowImage.color = color;
            yield return null;
        }
    }

    private void AddPlayerNumberText(GameObject marker, int playerId)
    {
        GameObject textObject = new GameObject("PlayerNumber");
        textObject.transform.SetParent(marker.transform, false);
        
        Text numberText = textObject.AddComponent<Text>();
        numberText.text = playerId.ToString();
        numberText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        numberText.fontSize = 8;
        numberText.color = Color.white;
        numberText.alignment = TextAnchor.MiddleCenter;
        
        // Настраиваем позицию текста
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Добавляем обводку для читаемости
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, 1);
    }

    private void AddEnhancedAnimation(GameObject marker, int stackLevel)
    {
        marker.transform.localScale = Vector3.zero;
        
        // Задержка анимации в зависимости от уровня стека
        float delay = stackLevel * 0.1f;
        
        StartCoroutine(AnimateMarkerAppearanceWithDelay(marker, delay));
    }

    private System.Collections.IEnumerator AnimateMarkerAppearanceWithDelay(GameObject marker, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        float duration = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < duration && marker != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            float scale = Mathf.Lerp(0f, 1f, EaseOutBounce(progress));
            marker.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        if (marker != null)
        {
            marker.transform.localScale = Vector3.one;
        }
    }

    private float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)
        {
            return 7.5625f * t * t;
        }
        else if (t < 2f / 2.75f)
        {
            return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        }
        else
        {
            return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
        }
    }

    public void ClearAllAvatarMarkers()
    {
        foreach (var markerData in currentMarkers)
        {
            if (markerData.markerObject != null)
            {
                Destroy(markerData.markerObject);
            }
        }
        
        currentMarkers.Clear();
        Debug.Log("Очищены все маркеры аватаров с таймера");
    }

    public void SetMaxQuestionTime(float time)
    {
        maxQuestionTime = time;
    }

    // Дополнительные методы для получения информации
    public List<int> GetPlayersWhoAnswered()
    {
        return currentMarkers.Select(m => m.playerId).ToList();
    }

    public int GetCorrectAnswersCount()
    {
        return currentMarkers.Count(m => m.isCorrectAnswer);
    }
}