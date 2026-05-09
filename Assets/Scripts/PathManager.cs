using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }

    [Header("Settings")]
    public GameObject rewardPrefab;

    [Header("League Settings")]
    [Tooltip("Трофеев до следующей лиги")]
    public int trophiesPerLeague = 500;
    [Tooltip("Количество лиг (макс. трофеев = лиг × трофеев за лигу)")]
    public int leagueCount = 6;
    [Tooltip("Иконки и названия лиг (от слабой к сильной)")]
    public List<LeagueData> leagues = new();

    public int MaxTrophies => trophiesPerLeague * leagueCount;

    public int GetLeagueIndex(int cups)
    {
        if (trophiesPerLeague <= 0 || leagues.Count == 0) return 0;
        return Mathf.Clamp(cups / trophiesPerLeague, 0, leagues.Count - 1);
    }

    public LeagueData GetLeague(int cups)
    {
        int idx = GetLeagueIndex(cups);
        return leagues.Count > 0 ? leagues[idx] : null;
    }

    [Header("Parents")]
    public RectTransform scrollContent;
    public RectTransform rewardsContent;
    public RectTransform trackParent;

    [Header("Progress UI")]
    public RectTransform mainTrackBackground;
    public RectTransform blueFillLine;
    public RectTransform playerMarker;
    public TextMeshProUGUI playerMarkerText;

    [Header("Data")]
    public List<RewardData> rewards = new();

    private readonly List<RectTransform> points = new();
    private readonly List<RewardUIItem> rewardItems = new();
    private readonly List<float> pointXs = new(); // кэш X в координатах trackParent

    private float x0, xLast;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        BuildPath();
        UpdateProgressBar(ProfileManager.Instance != null ? ProfileManager.Instance.GetCups() : 0);
    }

    void OnEnable()
    {
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.OnCupsChanged += HandleCupsChanged;
    }

    void OnDisable()
    {
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.OnCupsChanged -= HandleCupsChanged;
    }

    private void HandleCupsChanged(int cups)
    {
        UpdateProgressBar(cups);
        RefreshRewards(cups);
    }

    int GetCurrentCups() => ProfileManager.Instance != null ? ProfileManager.Instance.GetCups() : 0;

    private void LoadClaimedStates()
    {
        foreach (var reward in rewards)
            reward.isClaimed = PlayerPrefs.GetInt(RewardUIItem.ClaimKey(reward.cupsRequired), 0) == 1;
    }

    public void BuildPath()
    {
        LoadClaimedStates();
        int currentCups = GetCurrentCups();

        // Очистка
        for (int i = rewardsContent.childCount - 1; i >= 0; i--)
            Destroy(rewardsContent.GetChild(i).gameObject);

        // Инстанс наград
        rewardItems.Clear();

        for (int i = 0; i < rewards.Count; i++)
        {
            var go = Instantiate(rewardPrefab, rewardsContent);
            var ui = go.GetComponent<RewardUIItem>();
            if (ui != null)
            {
                Sprite leagueSprite = GetLeagueSpriteForReward(rewards[i]);
                ui.Setup(rewards[i], currentCups, leagueSprite);
                rewardItems.Add(ui);
            }
        }
        
        // Форсим layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rewardsContent);

        // Автоматически расширяем Content если нужно
        AdjustContentSize();

        // Собираем точки
        points.Clear();
        foreach (Transform child in rewardsContent)
        {
            var ui = child.GetComponent<RewardUIItem>();
            if (ui != null && ui.progressPoint != null)
                points.Add(ui.progressPoint);
        }

        if (points.Count < 2)
        {
            Debug.LogError($"points.Count={points.Count}. Проверь progressPoint в префабе.");
            return;
        }

        // Кэшируем X точек с учётом их ширины
        CachePointXs();

        // Строим трек: от правого края первой точки до левого края последней
        x0 = pointXs[0];
        xLast = pointXs[^1];
        float width = xLast - x0;

        if (mainTrackBackground != null)
        {
            mainTrackBackground.anchoredPosition = new Vector2(x0, mainTrackBackground.anchoredPosition.y);
            mainTrackBackground.sizeDelta = new Vector2(width, mainTrackBackground.sizeDelta.y);
        }

        if (blueFillLine != null)
        {
            blueFillLine.anchoredPosition = new Vector2(x0, blueFillLine.anchoredPosition.y);
            blueFillLine.sizeDelta = new Vector2(0f, blueFillLine.sizeDelta.y);
        }
    }

    private Sprite GetLeagueSpriteForReward(RewardData reward)
    {
        if (!reward.isLeagueMilestone || trophiesPerLeague <= 0) return null;
        int leagueIndex = reward.cupsRequired / trophiesPerLeague - 1;
        if (leagueIndex >= 0 && leagueIndex < leagues.Count)
            return leagues[leagueIndex].leagueIcon;
        return null;
    }

    private void RefreshRewards(int cups)
    {
        foreach (var item in rewardItems)
        {
            if (item != null)
                item.RefreshState(cups);
        }
    }

    void CachePointXs()
    {
        pointXs.Clear();
        
        for (int i = 0; i < points.Count; i++)
        {
            float centerX = GetXInParent(points[i], trackParent);
            float halfWidth = points[i].rect.width * 0.5f;
            
            // Для первой точки берём ЛЕВЫЙ край, для последней - ПРАВЫЙ, для остальных - центр
            if (i == 0)
                pointXs.Add(centerX - halfWidth); // Левый край первой точки
            else if (i == points.Count - 1)
                pointXs.Add(centerX + halfWidth); // Правый край последней точки
            else
                pointXs.Add(centerX); // Центр промежуточных точек
        }
    }

    void AdjustContentSize()
    {
        if (rewardsContent.childCount == 0 || scrollContent == null) return;

        // Получаем Layout Group для расчёта spacing и padding
        var layoutGroup = rewardsContent.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null) return;

        // Считаем минимальную ширину для всех наград
        float totalWidth = layoutGroup.padding.left + layoutGroup.padding.right;
        float spacing = layoutGroup.spacing;

        for (int i = 0; i < rewardsContent.childCount; i++)
        {
            RectTransform child = rewardsContent.GetChild(i) as RectTransform;
            if (child != null)
            {
                totalWidth += child.rect.width;
                if (i < rewardsContent.childCount - 1)
                    totalWidth += spacing;
            }
        }

        // Добавляем запас для комфортного скролла (например, +500px с обеих сторон)
        float finalWidth = totalWidth + 1000f;

        // Устанавливаем ширину Content (родитель скролла)
        scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);

        Debug.Log($"Scroll Content width установлен: {finalWidth} (ширина всех наград: {totalWidth})");
    }

    float GetXInParent(RectTransform point, RectTransform parent)
    {
        Vector3 world = point.TransformPoint(point.rect.center);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(null, world),
            null,
            out local
        );
        return local.x;
    }

    public void UpdateProgressBar(int currentCups)
    {
        if (rewards.Count < 2 || points.Count < 2 || pointXs.Count < 2) return;

        float targetX = x0;

        for (int i = 0; i < rewards.Count - 1; i++)
        {
            float from = rewards[i].cupsRequired;
            float to = rewards[i + 1].cupsRequired;

            float xi = pointXs[i];
            float xj = pointXs[i + 1];

            if (currentCups >= from && currentCups < to)
            {
                float t = (currentCups - from) / (to - from);
                targetX = Mathf.Lerp(xi, xj, t);
                break;
            }

            if (currentCups >= to && i == rewards.Count - 2)
                targetX = xLast;
        }

        if (blueFillLine != null)
        {
            float fillWidth = Mathf.Clamp(targetX - x0, 0f, xLast - x0);
            blueFillLine.sizeDelta = new Vector2(fillWidth, blueFillLine.sizeDelta.y);
        }

        if (playerMarker != null)
            playerMarker.anchoredPosition = new Vector2(targetX, playerMarker.anchoredPosition.y);

        if (playerMarkerText != null)
            playerMarkerText.text = currentCups.ToString();
    }
}