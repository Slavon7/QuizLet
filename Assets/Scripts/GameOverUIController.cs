using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class GameOverUIController : MonoBehaviour
{
    [Header("Place")]
    [SerializeField] private Image placeImage;
    [SerializeField] private Sprite[] medalSprites;

    [Header("XP")]
    [SerializeField] private TMP_Text expText;
    [SerializeField] private Slider levelSlider;
    [SerializeField] private TMP_Text levelText;

    [Header("Rewards")]
    [SerializeField] private TMP_Text gemsEarnedText;
    [SerializeField] private TMP_Text trophiesEarnedText;

    [Header("Player")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nicknameText;

    [Header("League")]
    [SerializeField] private Image leagueImage;
    [SerializeField] private TMP_Text leagueNameText;
    [SerializeField] private List<LeagueData> leagues;
    [SerializeField] private int trophiesPerLeague = 500;

    private int _currentXP;
    private int _xpGained;
    private int _xpToLevelUp;
    private int _currentLevel;

    public void ShowInfo(int place, int xpGained, int currentXP, int xpToLevelUp, int level, int gemsEarned, int trophiesEarned = 0)
    {
        int clampedPlace = Mathf.Clamp(place, 1, 8);
        placeImage.sprite = medalSprites[clampedPlace - 1];

        expText.text = $"+{xpGained}";
        if (levelSlider != null)
        {
            levelSlider.maxValue = xpToLevelUp;
            levelSlider.value = currentXP;
        }
        if (levelText != null) levelText.text = $"LEVEL {level}";

        _currentXP = currentXP;
        _xpGained = xpGained;
        _xpToLevelUp = xpToLevelUp;
        _currentLevel = level;

        if (gemsEarnedText != null)
            gemsEarnedText.text = $"+{gemsEarned}";

        if (trophiesEarnedText != null)
        {
            string prefix = trophiesEarned >= 0 ? "+" : "";
            trophiesEarnedText.text = $"{prefix}{trophiesEarned}";
        }

        Debug.Log($"[GameOverUI] place={place} xp={xpGained} gems={gemsEarned} trophies={trophiesEarned}");

        if (ProfileManager.Instance != null)
        {
            if (avatarImage != null && AvatarManager.Instance != null)
            {
                Sprite avatar = AvatarManager.Instance.GetAvatar(ProfileManager.Instance.GetCurrentAvatarIndex());
                if (avatar != null) avatarImage.sprite = avatar;
            }

            if (nicknameText != null)
                nicknameText.text = PhotonNetwork.LocalPlayer.NickName;
        }

        if (leagues != null && leagues.Count > 0 && trophiesPerLeague > 0)
        {
            int cups = ProfileManager.Instance != null ? ProfileManager.Instance.GetCups() : 0;
            int idx = Mathf.Clamp(cups / trophiesPerLeague, 0, leagues.Count - 1);
            LeagueData league = leagues[idx];
            if (leagueImage != null) leagueImage.sprite = league.leagueIcon;
            if (leagueNameText != null) leagueNameText.text = league.leagueName;
        }
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
                    if (levelSlider != null) levelSlider.value = Mathf.Lerp(startVal, endVal, t);
                    yield return null;
                }

                currentLevel++;
                if (levelText != null) levelText.text = $"LEVEL {currentLevel}";
                currentXP = 0;
                targetXP -= nextLevelXP;
                if (levelSlider != null) levelSlider.value = 0;
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
                    if (levelSlider != null) levelSlider.value = Mathf.Lerp(startVal, endVal, t);
                    yield return null;
                }

                currentXP = targetXP;
            }
        }

        _currentXP = currentXP;
        _currentLevel = currentLevel;
        if (levelSlider != null) levelSlider.value = currentXP;
        if (levelText != null) levelText.text = $"LEVEL {_currentLevel}";
    }
}
