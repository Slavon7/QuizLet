using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardUIItem : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI cupsText;
    public Image iconImage;
    
    public Image buttonBackgroundImage;
    public Sprite defaultButtonSprite;
    public Sprite availableButtonSprite;

    [Header("States")]
    public GameObject availableState;   
    public GameObject claimedState;

    [Header("Text Colors")]
    public Color defaultTextColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color availableTextColor = Color.white;
    [Header("Progress Point Settings")]
    public RectTransform progressPoint;
    public Image pointImage;
    public Sprite lockedPointSprite;
    public Sprite reachedPointSprite;

    [Header("League Milestone")]
    public Image leaguePointImage; // Иконка лиги вместо синей точки

    private RewardData data;
    private Sprite leagueSprite;

    public void Setup(RewardData rewardData, int currentCups, Sprite leagueIconSprite = null)
    {
        data = rewardData;
        leagueSprite = leagueIconSprite;

        if (amountText != null)
            amountText.text = data.amount.ToString();

        if (cupsText != null)
            cupsText.text = data.cupsRequired.ToString();

        if (iconImage != null)
            iconImage.sprite = data.icon;

        if (leaguePointImage != null)
        {
            bool isLeague = data.isLeagueMilestone && leagueSprite != null;
            leaguePointImage.gameObject.SetActive(isLeague);
            if (isLeague)
                leaguePointImage.sprite = leagueSprite;
        }

        RefreshState(currentCups);
    }

    public void RefreshState(int currentCups)
    {
        bool reached = currentCups >= data.cupsRequired;
        bool canClaim = reached && !data.isClaimed;

        // 1. Меняем спрайт кнопки (фиолетовый/желтый)
        if (buttonBackgroundImage != null && defaultButtonSprite != null && availableButtonSprite != null)
        {
            buttonBackgroundImage.sprite = canClaim ? availableButtonSprite : defaultButtonSprite;
        }

        // 2. --- МЕНЯЕМ ЦВЕТ ТОЛЬКО ДЛЯ amountText ---
        if (amountText != null)
        {
            amountText.color = canClaim ? availableTextColor : defaultTextColor;
        }

        // Текст cupsText (Required Trophy) не трогаем, он сохранит свой исходный цвет

        // 3. Обновляем точку прогресса: лига — иконка лиги, иначе — синяя/фиолетовая точка
        bool isLeagueMilestone = data.isLeagueMilestone && leagueSprite != null;
        if (pointImage != null)
        {
            pointImage.gameObject.SetActive(!isLeagueMilestone);
            if (!isLeagueMilestone && lockedPointSprite != null && reachedPointSprite != null)
                pointImage.sprite = reached ? reachedPointSprite : lockedPointSprite;
        }
        if (leaguePointImage != null && isLeagueMilestone)
        {
            leaguePointImage.sprite = leagueSprite;
        }

        // 4. Состояния (Available/Claimed)
        RewardState state = GetRewardState(currentCups);
        if (availableState != null) availableState.SetActive(state == RewardState.Available);
        if (claimedState != null) claimedState.SetActive(state == RewardState.Claimed);
    }

    private RewardState GetRewardState(int currentCups)
    {
        if (data.isClaimed)
            return RewardState.Claimed;

        if (currentCups >= data.cupsRequired)
            return RewardState.Available;

        return RewardState.Locked;
    }

    // Вызывается кнопкой Claim или при клике на карточку
    public void TryClaim()
    {
        int currentCups = ProfileManager.Instance != null ? ProfileManager.Instance.GetCups() : 0;
        
        if (data.isClaimed)
        {
            Debug.Log("Награда уже забрана");
            return;
        }

        if (currentCups < data.cupsRequired)
        {
            Debug.Log($"Недостаточно кубков: {currentCups}/{data.cupsRequired}");
            return;
        }

        // Забираем награду
        Claim();
    }

    private void Claim()
    {
        data.isClaimed = true;
        PlayerPrefs.SetInt(ClaimKey(data.cupsRequired), 1);
        PlayerPrefs.Save();

        GiveReward();
        RefreshState(ProfileManager.Instance.GetCups());
        PlayClaimAnimation();
    }

    public static string ClaimKey(int cupsRequired) => $"TrophyRoad_Claimed_{cupsRequired}";

    private void GiveReward()
    {
        switch (data.type)
        {
            case RewardType.Crystals:
                CurrencyManager.Instance?.AddGems(data.amount, "Trophy Road reward");
                break;

            case RewardType.XP:
                ProfileManager.Instance?.AddExperience(data.amount);
                break;

            // другие типы по необходимости
        }
    }

    private void PlayClaimAnimation()
    {
        // TODO: добавить анимацию забора награды
        // Например, тряска, вспышка, частицы и т.д.
    }
}

public enum RewardState
{
    Locked,     // Ещё не достигнуто нужное кол-во кубков
    Available,  // Можно забрать
    Claimed     // Уже забрано
}