using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RewardAnimationEffects : MonoBehaviour
{
    [Header("Animation Settings")]
    public float punchScaleDuration = 0.5f;
    public float punchScaleStrength = 1.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);
    
    [Header("Color Animation")]
    public bool animateTextColor = true;
    public Color startColor = Color.yellow;
    public Color endColor = Color.white;
    public float colorAnimationDuration = 1f;
    
    [Header("Particle Effects")]
    public ParticleSystem gemSparkles;
    public ParticleSystem levelUpBurst;
    
    [Header("Sound Effects")]
    public AudioSource audioSource;
    public AudioClip gemCountSound;
    public AudioClip levelUpSound;
    public AudioClip rewardClaimSound;
    
    private TextMeshProUGUI targetText;
    private RectTransform targetTransform;
    private Vector3 originalScale;
    
    void Awake()
    {
        targetText = GetComponent<TextMeshProUGUI>();
        targetTransform = GetComponent<RectTransform>();
        
        if (targetTransform != null)
        {
            originalScale = targetTransform.localScale;
        }
    }
    
    public void PlayLevelUpAnimation()
    {
        // Звуковой эффект
        PlaySound(levelUpSound);
        
        // Эффект частиц
        if (levelUpBurst != null)
        {
            levelUpBurst.Play();
        }
        
        // Анимация масштабирования
        StartCoroutine(PunchScaleAnimation());
    }
    
    public void PlayGemCountAnimation()
    {
        // Звук счетчика (можно воспроизводить периодически)
        if (gemCountSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.8f, 1.2f); // Вариация высоты звука
            audioSource.PlayOneShot(gemCountSound, 0.3f);
        }
        
        // Эффект искр гемов
        if (gemSparkles != null && !gemSparkles.isPlaying)
        {
            gemSparkles.Play();
        }
    }
    
    public void PlayRewardClaimAnimation()
    {
        PlaySound(rewardClaimSound);
        
        // Останавливаем эффекты
        if (gemSparkles != null)
        {
            gemSparkles.Stop();
        }
        
        // Финальная анимация получения награды
        StartCoroutine(ClaimScaleAnimation());
    }
    
    private IEnumerator PunchScaleAnimation()
    {
        if (targetTransform == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < punchScaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / punchScaleDuration;
            
            // Используем кривую для более интересной анимации
            float scaleMultiplier = scaleCurve.Evaluate(progress);
            float currentScale = Mathf.Lerp(1f, punchScaleStrength, scaleMultiplier);
            
            targetTransform.localScale = originalScale * currentScale;
            
            yield return null;
        }
        
        // Возвращаем к исходному размеру
        targetTransform.localScale = originalScale;
    }
    
    private IEnumerator ClaimScaleAnimation()
    {
        if (targetTransform == null) yield break;
        
        float duration = 0.3f;
        float elapsedTime = 0f;
        
        // Увеличиваем размер
        while (elapsedTime < duration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (duration / 2);
            float scale = Mathf.Lerp(1f, 1.3f, progress);
            
            targetTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        // Уменьшаем до исходного
        elapsedTime = 0f;
        while (elapsedTime < duration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (duration / 2);
            float scale = Mathf.Lerp(1.3f, 1f, progress);
            
            targetTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        targetTransform.localScale = originalScale;
    }
    
    public IEnumerator AnimateTextColor()
    {
        if (!animateTextColor || targetText == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < colorAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / colorAnimationDuration;
            
            // Ping-pong эффект для цвета
            float pingPong = Mathf.PingPong(progress * 2, 1);
            Color currentColor = Color.Lerp(startColor, endColor, pingPong);
            
            targetText.color = currentColor;
            
            yield return null;
        }
        
        // Возвращаем к исходному цвету
        targetText.color = endColor;
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Метод для создания числового эффекта с паузами между цифрами
    public IEnumerator AnimateCounterWithSteps(int startValue, int targetValue, float duration, System.Action<int> updateCallback)
    {
        int delta = targetValue - startValue;
        if (delta == 0) yield break;

        int steps = Mathf.Min(Mathf.Abs(delta), 20); // максимум 20 шагов
        float stepDuration = duration / steps;

        for (int i = 1; i <= steps; i++)
        {
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, (float)i / steps));
            updateCallback?.Invoke(currentValue);
            PlayGemCountAnimation();
            yield return new WaitForSeconds(stepDuration);
        }

        updateCallback?.Invoke(targetValue);
    }
}