using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameOverUIController : MonoBehaviour
{
    [SerializeField] private Image placeImage; // новая иконка для места
    [SerializeField] private Sprite[] medalSprites; // массив медалей: 0 = 1-е место, 1 = 2-е и т.д.
    
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Slider levelSlider;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text gemsEarnedText;

    private int _currentXP;
    private int _xpGained;
    private int _xpToLevelUp;
    private int _currentLevel;

    public void ShowInfo(int place, int xpGained, int currentXP, int xpToLevelUp, int level, int gemsEarned)
    {
        int clampedPlace = Mathf.Clamp(place, 1, 8);
        placeImage.sprite = medalSprites[clampedPlace - 1];

        expText.text = $"+{xpGained} XP";
        levelSlider.maxValue = xpToLevelUp;
        levelText.text = $"LEVEL {level}";
        levelSlider.value = currentXP;

        _currentXP = currentXP;
        _xpGained = xpGained;
        _xpToLevelUp = xpToLevelUp;
        _currentLevel = level;

        if (gemsEarnedText != null)
            gemsEarnedText.text = $"+{gemsEarned} 💎";
    }

    public void PlaySliderAnimation()
    {
        Debug.Log("PlaySliderAnimation вызван в " + Time.time);
        StopAllCoroutines();
        StartCoroutine(AnimateSlider());
    }

    public void OnPlaySliderAnimationSignal()
    {
        PlaySliderAnimation();
    }

    private IEnumerator AnimateSlider()
    {
        int targetXP = _currentXP + _xpGained;
        int currentXP = _currentXP;
        int currentLevel = _currentLevel;
        int xpToLevelUp = _xpToLevelUp;

        float animationDuration = 1.5f;
        float elapsed = 0f;

        while (currentXP < targetXP)
        {
            int nextLevelXP = xpToLevelUp;

            if (targetXP >= nextLevelXP)
            {
                float startVal = currentXP;
                float endVal = nextLevelXP;
                elapsed = 0f;

                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animationDuration);
                    levelSlider.value = Mathf.Lerp(startVal, endVal, t);
                    yield return null;
                }

                currentLevel++;
                levelText.text = $"LEVEL {currentLevel}";
                currentXP = 0;
                targetXP -= nextLevelXP;
                levelSlider.value = 0;
            }
            else
            {
                float startVal = currentXP;
                float endVal = targetXP;
                elapsed = 0f;

                while (elapsed < animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animationDuration);
                    levelSlider.value = Mathf.Lerp(startVal, endVal, t);
                    yield return null;
                }

                currentXP = targetXP;
            }
        }

        _currentXP = currentXP;
        _currentLevel = currentLevel;
        levelSlider.value = currentXP;
        levelText.text = $"LEVEL {_currentLevel}";
    }
}
