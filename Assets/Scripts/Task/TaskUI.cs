using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TaskUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text taskTitleText;
    [SerializeField] private TMP_Text taskDescriptionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button claimRewardButton;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private GameObject completedIcon;
    [SerializeField] private GameObject claimedIcon;
    
    [Header("Visual Settings")]
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color incompleteColor = Color.gray;
    [SerializeField] private Color claimedColor = Color.yellow;
    
    private TaskData currentTask;
    
    // События
    public System.Action<TaskData> OnRewardClaimed;
    
    private void Start()
    {
        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.AddListener(ClaimReward);
        }
    }
    
    public void Initialize(TaskData task)
    {
        currentTask = task;
        UpdateUI();
    }
    
    public void UpdateProgress(TaskData task)
    {
        currentTask = task;
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (currentTask == null) return;
        
        // Обновляем текст
        if (taskTitleText != null)
            taskTitleText.text = currentTask.taskTitle;
            
        if (taskDescriptionText != null)
            taskDescriptionText.text = currentTask.taskDescription;
        
        // Обновляем прогресс
        if (progressText != null)
            progressText.text = $"{currentTask.currentValue}/{currentTask.targetValue}";
        
        if (progressSlider != null)
        {
            progressSlider.maxValue = currentTask.targetValue;
            progressSlider.value = currentTask.currentValue;
        }
        
        // Обновляем информацию о награде
        if (rewardText != null)
        {
            rewardText.text = GetRewardDisplayText();
        }
        
        // Обновляем состояние кнопки и иконок
        UpdateVisualState();
    }
    
    private string GetRewardDisplayText()
    {
        if (currentTask?.reward == null) return "";
        
        var rewardParts = new System.Collections.Generic.List<string>();
        
        // Добавляем гемы (основная валюта)
        if (currentTask.reward.gems > 0)
        {
            rewardParts.Add($"{currentTask.reward.gems}");
        }
        
        // Добавляем монеты (если есть)
        if (currentTask.reward.coins > 0)
        {
            rewardParts.Add($"{currentTask.reward.coins}");
        }
        
        // Добавляем опыт
        if (currentTask.reward.experience > 0)
        {
            rewardParts.Add($" {currentTask.reward.experience}");
        }
        
        // Соединяем все части
        return string.Join(" ", rewardParts);
    }
    
    private void UpdateVisualState()
    {
        // Обновляем состояние иконок
        if (completedIcon != null)
            completedIcon.SetActive(currentTask.isCompleted && !currentTask.isRewardClaimed);
            
        if (claimedIcon != null)
            claimedIcon.SetActive(currentTask.isRewardClaimed);
        
        // Обновляем состояние кнопки
        if (claimRewardButton != null)
        {
            claimRewardButton.interactable = currentTask.isCompleted && !currentTask.isRewardClaimed;
            claimRewardButton.gameObject.SetActive(!currentTask.isRewardClaimed);
        }
        
        // Обновляем цвета
        UpdateColors();
    }
    
    private void UpdateColors()
    {
        Color targetColor = incompleteColor;
        
        if (currentTask.isRewardClaimed)
        {
            targetColor = claimedColor;
        }
        else if (currentTask.isCompleted)
        {
            targetColor = completedColor;
        }
        
        // Применяем цвет к прогресс-бару
        if (progressSlider != null)
        {
            var fillArea = progressSlider.fillRect.GetComponent<Image>();
            if (fillArea != null)
                fillArea.color = targetColor;
        }
        
        // Применяем цвет к тексту заголовка
        if (taskTitleText != null)
            taskTitleText.color = targetColor;
    }
    
    private void ClaimReward()
    {
        if (currentTask != null && currentTask.isCompleted && !currentTask.isRewardClaimed)
        {
            OnRewardClaimed?.Invoke(currentTask);

            // Воспроизводим звук получения награды (если есть AudioManager)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("reward_claimed");
            }

            // Можно добавить анимацию или эффекты
            PlayClaimAnimation();

            // Показываем уведомление о полученной награде
            ShowRewardNotification();
            
            AnimateAndRemoveTask();
        }
    }
    
    public void AnimateAndRemoveTask()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.interactable = false;

        transform.DOScale(0.9f, 0.25f);
        canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    public void AnimateMoveToBottom()
    {
        // Сохраняем старую позицию
        Vector3 oldPosition = transform.position;

        // Перемещаем в иерархии в самый низ
        transform.SetAsLastSibling();

        // После перемещения получаем новую позицию
        Canvas.ForceUpdateCanvases(); // важно для обновления layout
        Vector3 newPosition = transform.position;

        // Возвращаем на старую позицию, а потом анимируем
        transform.position = oldPosition;

        // Анимируем в новую
        transform.DOMove(newPosition, 0.35f).SetEase(Ease.InOutQuad);
    }
    
    private void ShowRewardNotification()
    {
        if (currentTask?.reward == null) return;

        string notification = "Получена награда: ";

        if (currentTask.reward.gems > 0)
            notification += $"{currentTask.reward.gems} гемов ";

        if (currentTask.reward.coins > 0)
            notification += $"{currentTask.reward.coins} монет ";

        if (currentTask.reward.experience > 0)
            notification += $"{currentTask.reward.experience} опыта";

        Debug.Log(notification);

        // Здесь можно добавить показ UI уведомления
        // Например, через NotificationManager если он у вас есть
    }
    
    private void PlayClaimAnimation()
    {
        // Простая анимация масштабирования
        if (claimRewardButton != null)
        {
            StartCoroutine(ScaleAnimation());
        }
    }
    
    private System.Collections.IEnumerator ScaleAnimation()
    {
        Vector3 originalScale = claimRewardButton.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        // Увеличиваем
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            claimRewardButton.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        // Уменьшаем обратно
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            claimRewardButton.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        claimRewardButton.transform.localScale = originalScale;
    }
    
    private void OnDestroy()
    {
        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.RemoveListener(ClaimReward);
        }
    }
    
    // Метод для обновления только текста награды (полезно для динамических изменений)
    public void UpdateRewardDisplay()
    {
        if (rewardText != null && currentTask?.reward != null)
        {
            rewardText.text = GetRewardDisplayText();
        }
    }
    
    // Метод для установки пользовательских иконок через код
    public void SetRewardIcons( )
    {
        UpdateRewardDisplay();
    }
}