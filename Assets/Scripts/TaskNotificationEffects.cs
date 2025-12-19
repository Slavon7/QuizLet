using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TaskNotificationEffects : MonoBehaviour
{
    [Header("Notification Popup")]
    [SerializeField] private GameObject notificationPopupPrefab;
    [SerializeField] private Transform notificationParent;
    [SerializeField] private float popupDuration = 3f;
    
    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform floatingTextParent;
    
    [Header("Reward Animation")]
    [SerializeField] private Transform rewardContainer;
    [SerializeField] private GameObject gemIcon;
    [SerializeField] private GameObject coinIcon;
    [SerializeField] private GameObject expIcon;
    
    [Header("Sound Effects")]
    [SerializeField] private string taskCompleteSound = "task_complete";
    [SerializeField] private string achievementUnlockSound = "achievement_unlock";
    [SerializeField] private string rewardClaimSound = "reward_claim";
    
    private void Start()
    {
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskCompleted += ShowTaskCompletedEffect;
            TaskManager.Instance.OnAchievementUnlocked += ShowAchievementUnlockedEffect;
            TaskManager.Instance.OnTaskRewardClaimed += ShowRewardClaimedEffect;
        }
    }
    
    #region Task Effects
    
    public void ShowTaskCompletedEffect(TaskData task)
    {
        // Воспроизводим звук
        PlaySound(taskCompleteSound);
        
        // Показываем всплывающее уведомление
        ShowNotificationPopup($"Задание выполнено!", $"{task.taskTitle}", Color.green);
        
        // Эффект плавающего текста
        ShowFloatingText("Задание выполнено!", Color.green, Vector3.up * 50f);
    }
    
    public void ShowAchievementUnlockedEffect(TaskData achievement)
    {
        // Воспроизводим звук
        PlaySound(achievementUnlockSound);
        
        // Показываем специальное уведомление для достижений
        ShowNotificationPopup($"🏆 Достижение разблокировано!", $"{achievement.taskTitle}", Color.yellow);
        
        // Эффект плавающего текста с особой анимацией
        ShowFloatingText("🏆 ДОСТИЖЕНИЕ!", Color.yellow, Vector3.up * 80f, 1.5f);
        
        // Дополнительный визуальный эффект
        ShowAchievementBurst();
    }
    
    public void ShowRewardClaimedEffect(TaskData task)
    {
        // Воспроизводим звук
        PlaySound(rewardClaimSound);
        
        // Анимируем иконки наград
        AnimateRewardIcons(task.reward);
        
        // Показываем плавающий текст с наградами
        ShowRewardFloatingText(task.reward);
    }
    
    #endregion
    
    #region Notification Popup
    
    private void ShowNotificationPopup(string title, string description, Color accentColor)
    {
        if (notificationPopupPrefab == null || notificationParent == null) return;
        
        GameObject popup = Instantiate(notificationPopupPrefab, notificationParent);
        
        // Настраиваем текст
        var titleText = popup.transform.Find("Title")?.GetComponent<TMP_Text>();
        var descText = popup.transform.Find("Description")?.GetComponent<TMP_Text>();
        var background = popup.GetComponent<Image>();
        
        if (titleText != null) titleText.text = title;
        if (descText != null) descText.text = description;
        if (background != null) background.color = accentColor;
        
        // Анимация появления
        popup.transform.localScale = Vector3.zero;
        popup.transform.DOMoveY(popup.transform.position.y + 100f, 0.6f);
        
        Sequence popupSequence = DOTween.Sequence();
        popupSequence.Append(popup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
        popupSequence.AppendInterval(popupDuration);
        popupSequence.Append(popup.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
        popupSequence.Join(popup.GetComponent<CanvasGroup>()?.DOFade(0f, 0.3f));
        popupSequence.OnComplete(() => DestroyImmediate(popup));
    }
    
    #endregion
    
    #region Floating Text
    
    private void ShowFloatingText(string text, Color color, Vector3 direction, float scale = 1f)
    {
        if (floatingTextPrefab == null || floatingTextParent == null) return;
        
        GameObject floatingText = Instantiate(floatingTextPrefab, floatingTextParent);
        var textComponent = floatingText.GetComponent<TMP_Text>();
        
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
            textComponent.transform.localScale = Vector3.one * scale;
        }
        
        // Анимация плавающего текста
        Vector3 startPos = floatingText.transform.position;
        Vector3 endPos = startPos + direction;
        
        Sequence floatingSequence = DOTween.Sequence();
        floatingSequence.Append(floatingText.transform.DOMove(endPos, 2f).SetEase(Ease.OutQuart));
        floatingSequence.Join(floatingText.transform.DOScale(Vector3.one * scale * 1.2f, 0.3f)
            .SetEase(Ease.OutBack));
        floatingSequence.Append(floatingText.transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack).SetDelay(1.4f));
        floatingSequence.Join(textComponent.DOFade(0f, 0.3f).SetDelay(1.4f));
        floatingSequence.OnComplete(() => DestroyImmediate(floatingText));
    }
    
    private void ShowRewardFloatingText(TaskReward reward)
    {
        Vector3 baseDirection = Vector3.up * 60f;
        float delay = 0f;
        
        if (reward.gems > 0)
        {
            ShowFloatingTextWithDelay($"+{reward.gems} 💎", Color.cyan, baseDirection + Vector3.left * 30f, delay);
            delay += 0.2f;
        }
        
        if (reward.coins > 0)
        {
            ShowFloatingTextWithDelay($"+{reward.coins} 🪙", Color.yellow, baseDirection, delay);
            delay += 0.2f;
        }
        
        if (reward.experience > 0)
        {
            ShowFloatingTextWithDelay($"+{reward.experience} XP", Color.green, baseDirection + Vector3.right * 30f, delay);
        }
    }
    
    private void ShowFloatingTextWithDelay(string text, Color color, Vector3 direction, float delay)
    {
        DOVirtual.DelayedCall(delay, () => ShowFloatingText(text, color, direction));
    }
    
    #endregion
    
    #region Reward Animation
    
    private void AnimateRewardIcons(TaskReward reward)
    {
        if (rewardContainer == null) return;
        
        float delay = 0f;
        
        if (reward.gems > 0 && gemIcon != null)
        {
            AnimateRewardIcon(gemIcon, delay);
            delay += 0.15f;
        }
        
        if (reward.coins > 0 && coinIcon != null)
        {
            AnimateRewardIcon(coinIcon, delay);
            delay += 0.15f;
        }
        
        if (reward.experience > 0 && expIcon != null)
        {
            AnimateRewardIcon(expIcon, delay);
        }
    }
    
    private void AnimateRewardIcon(GameObject icon, float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            // Эффект "bounce" для иконки
            icon.transform.DOPunchScale(Vector3.one * 0.3f, 0.6f, 8, 0.5f);
            
            // Эффект вращения
            icon.transform.DORotate(new Vector3(0, 0, 360), 0.8f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuart);
                
            // Эффект свечения (если есть Image компонент)
            var image = icon.GetComponent<Image>();
            if (image != null)
            {
                Color originalColor = image.color;
                image.DOColor(Color.white, 0.1f)
                    .OnComplete(() => image.DOColor(originalColor, 0.5f));
            }
        });
    }
    
    #endregion
    
    #region Achievement Effects
    
    private void ShowAchievementBurst()
    {
        // Создаем эффект "взрыва" частиц для достижений
        GameObject burst = new GameObject("AchievementBurst");
        burst.transform.SetParent(transform);
        burst.transform.localPosition = Vector3.zero;
        
        // Создаем несколько звездочек, разлетающихся в разные стороны
        for (int i = 0; i < 8; i++)
        {
            CreateBurstParticle(burst.transform, i);
        }
        
        // Удаляем объект через 3 секунды
        Destroy(burst, 3f);
    }
    
    private void CreateBurstParticle(Transform parent, int index)
    {
        GameObject particle = new GameObject($"Particle_{index}");
        particle.transform.SetParent(parent);
        particle.transform.localPosition = Vector3.zero;
        
        // Добавляем Image компонент со звездочкой
        var image = particle.AddComponent<Image>();
        image.sprite = CreateStarSprite(); // Вы можете заменить на свой спрайт
        image.color = Color.yellow;
        
        // Вычисляем направление
        float angle = (360f / 8f) * index;
        Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.up;
        Vector3 targetPos = direction * 200f;
        
        // Анимация частицы
        Sequence particleSequence = DOTween.Sequence();
        particleSequence.Append(particle.transform.DOMove(particle.transform.position + targetPos, 1.5f)
            .SetEase(Ease.OutQuart));
        particleSequence.Join(particle.transform.DORotate(new Vector3(0, 0, 720), 1.5f, RotateMode.FastBeyond360));
        particleSequence.Join(particle.transform.DOScale(Vector3.zero, 0.5f).SetDelay(1f));
        particleSequence.Join(image.DOFade(0f, 0.5f).SetDelay(1f));
    }
    
    private Sprite CreateStarSprite()
    {
        // Создаем простую текстуру звездочки
        // В реальном проекте лучше использовать готовый спрайт
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        
        // Рисуем простую звездочку (можно заменить на более сложную логику)
        for (int x = 14; x <= 18; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                colors[y * 32 + x] = Color.yellow;
            }
        }
        
        for (int y = 14; y <= 18; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                colors[y * 32 + x] = Color.yellow;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    #endregion
    
    #region Sound Management
    
    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void TriggerTaskCompleteEffect(string taskTitle)
    {
        var taskData = new TaskData { taskTitle = taskTitle };
        ShowTaskCompletedEffect(taskData);
    }
    
    public void TriggerAchievementEffect(string achievementTitle)
    {
        var achievementData = new TaskData { taskTitle = achievementTitle };
        ShowAchievementUnlockedEffect(achievementData);
    }
    
    public void TriggerRewardEffect(int gems, int coins, int experience)
    {
        var reward = new TaskReward { gems = gems, coins = coins, experience = experience };
        var taskData = new TaskData { reward = reward };
        ShowRewardClaimedEffect(taskData);
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Останавливаем все DoTween анимации
        DOTween.Kill(transform);
        
        // Отписываемся от событий
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskCompleted -= ShowTaskCompletedEffect;
            TaskManager.Instance.OnAchievementUnlocked -= ShowAchievementUnlockedEffect;
            TaskManager.Instance.OnTaskRewardClaimed -= ShowRewardClaimedEffect;
        }
    }
}