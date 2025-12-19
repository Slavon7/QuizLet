using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum LeaderboardType
{
    Gameplay,
    EndGame
}

[System.Serializable]
public class LeaderboardContainer
{
    public LeaderboardType type;
    public Transform container;
    public GameObject itemPrefab;
}

public class PlayerScoreDisplay : MonoBehaviourPunCallbacks
{
    [SerializeField] private LeaderboardContainer[] leaderboardContainers;
    [SerializeField] private Color disconnectedPlayerColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [Header("Score Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private int animationStepsPerSecond = 30;
    
    [Header("DOTween Animation Settings")]
    [SerializeField] private float reorderAnimationDuration = 0.6f;
    [SerializeField] private float fadeAnimationDuration = 0.3f;
    [SerializeField] private float scaleAnimationDuration = 0.4f;
    [SerializeField] private Ease moveEase = Ease.OutQuart;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    // Словари для каждого типа лидерборда
    private Dictionary<LeaderboardType, Dictionary<int, GameObject>> scoreItemsByType = 
        new Dictionary<LeaderboardType, Dictionary<int, GameObject>>();
    
    private Dictionary<int, bool> playerConnectionStatus = new Dictionary<int, bool>();
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    
    // Словари для хранения оригинальных цветов
    private Dictionary<int, Color> originalNumberColors = new Dictionary<int, Color>();
    private Dictionary<int, Color> originalNickColors = new Dictionary<int, Color>();
    private Dictionary<int, Color> originalScoreColors = new Dictionary<int, Color>();
    private Dictionary<int, Color> originalBackgroundColors = new Dictionary<int, Color>();
    private Dictionary<int, Color> originalAvatarColors = new Dictionary<int, Color>();
    
    // Словари для отслеживания анимаций
    private Dictionary<int, Coroutine> runningAnimations = new Dictionary<int, Coroutine>();
    private Dictionary<int, int> displayedScores = new Dictionary<int, int>();
    
    // Словари для позиций элементов
    private Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, int> currentPositions = new Dictionary<int, int>();
    
    // Флаги для контроля анимаций
    private Dictionary<LeaderboardType, bool> isReordering = new Dictionary<LeaderboardType, bool>();

    private void Awake()
    {
        // Инициализируем словари для каждого типа лидерборда
        foreach (var container in leaderboardContainers)
        {
            scoreItemsByType[container.type] = new Dictionary<int, GameObject>();
            isReordering[container.type] = false;
        }
    }

    public void InitializePlayerList(LeaderboardType leaderboardType = LeaderboardType.Gameplay)
    {
        var targetContainer = GetLeaderboardContainer(leaderboardType);
        if (targetContainer == null) return;

        var scoreItems = scoreItemsByType[leaderboardType];

        // Сначала отмечаем всех игроков как отключенных
        foreach (var playerId in scoreItems.Keys)
        {
            playerConnectionStatus[playerId] = false;
        }

        // Затем помечаем подключенных игроков
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int playerId = player.ActorNumber;
            playerConnectionStatus[playerId] = true;
            
            if (!playerScores.ContainsKey(playerId))
            {
                playerScores[playerId] = 0;
                displayedScores[playerId] = 0;
            }
            
            if (!scoreItems.ContainsKey(playerId))
            {
                CreatePlayerItem(player, targetContainer, leaderboardType);
            }
        }

        ReorderPlayerList(leaderboardType);
    }

    private void CreatePlayerItem(Player player, LeaderboardContainer container, LeaderboardType leaderboardType)
    {
        int playerId = player.ActorNumber;
        var scoreItems = scoreItemsByType[leaderboardType];

        GameObject item = Instantiate(container.itemPrefab, container.container);
        scoreItems[playerId] = item;
        
        // Анимация появления нового элемента
        AnimateItemAppearance(item);
        
        // Находим компоненты
        Transform textGroupNumber = item.transform.Find("TextGroup");
        if (textGroupNumber == null)
        {
            Debug.LogError("Не найден объект 'TextGroup' для номера игрока в префабе!");
            return;
        }

        TMP_Text numberText = textGroupNumber.Find("NumberText")?.GetComponent<TMP_Text>();
        if (numberText == null)
        {
            Debug.LogError("Не найден 'NumberText' в 'TextGroup' для номера игрока.");
            return;
        }

        // Сохраняем оригинальные цвета только один раз
        if (!originalNumberColors.ContainsKey(playerId))
        {
            originalNumberColors[playerId] = numberText.color;
        }

        Transform textGroupNickScore = textGroupNumber.Find("TextGroup_NameAndScore");
        if (textGroupNickScore == null)
        {
            Debug.LogError("Не найден второй объект 'TextGroup_NameAndScore' для ника и очков в 'TextGroup'.");
            return;
        }

        TMP_Text nickText = textGroupNickScore.Find("NickText")?.GetComponent<TMP_Text>();
        TMP_Text scoreText = textGroupNickScore.Find("ScoreText")?.GetComponent<TMP_Text>();
        Image iconImage = GetPlayerIconImage(item);

        if (nickText == null || scoreText == null)
        {
            Debug.LogError("Не найдены необходимые элементы (NickText, ScoreText) в 'TextGroup_NameAndScore'.");
            return;
        }

        // Сохраняем оригинальные цвета только один раз
        if (!originalNickColors.ContainsKey(playerId))
        {
            originalNickColors[playerId] = nickText.color;
            originalScoreColors[playerId] = scoreText.color;
            
            if (iconImage != null)
            {
                originalAvatarColors[playerId] = iconImage.color;
            }
        }

        Image backgroundImage = item.GetComponent<Image>();
        if (backgroundImage != null && !originalBackgroundColors.ContainsKey(playerId))
        {
            originalBackgroundColors[playerId] = backgroundImage.color;
        }

        // Устанавливаем данные игрока
        nickText.text = player.NickName;
        scoreText.text = playerScores[playerId].ToString();
        
        // Устанавливаем аватарку через AvatarManager
        SetPlayerAvatar(player, iconImage);
    }

    private void AnimateItemAppearance(GameObject item)
    {
        // Сначала делаем элемент невидимым
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = item.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        item.transform.localScale = Vector3.one * 0.5f;
        
        // Анимируем появление
        Sequence appearSequence = DOTween.Sequence();
        appearSequence.Append(item.transform.DOScale(Vector3.one, scaleAnimationDuration).SetEase(scaleEase));
        appearSequence.Join(canvasGroup.DOFade(1f, fadeAnimationDuration).SetEase(fadeEase));
        
        // Небольшая задержка для более плавного эффекта
        appearSequence.SetDelay(0.1f);
    }

    private void AnimateItemDisappearance(GameObject item, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = item.AddComponent<CanvasGroup>();
        }
        
        Sequence disappearSequence = DOTween.Sequence();
        disappearSequence.Append(item.transform.DOScale(Vector3.one * 0.5f, scaleAnimationDuration).SetEase(Ease.InQuart));
        disappearSequence.Join(canvasGroup.DOFade(0f, fadeAnimationDuration).SetEase(Ease.InQuad));
        disappearSequence.OnComplete(() => onComplete?.Invoke());
    }

    private void SetPlayerAvatar(Player player, Image iconImage)
    {
        if (iconImage == null)
        {
            Debug.LogWarning("Icon Image не найден для игрока: " + player.NickName);
            return;
        }

        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен! Убедитесь, что создали ScriptableObject и поместили его в Resources");
            return;
        }

        int avatarIndex = 0;
        
        if (player.CustomProperties.ContainsKey("AvatarIndex"))
        {
            avatarIndex = (int)player.CustomProperties["AvatarIndex"];
            Debug.Log($"Получен индекс аватарки для игрока {player.NickName}: {avatarIndex}");
        }
        else
        {
            Debug.LogWarning($"Индекс аватарки не найден для игрока {player.NickName}, используется индекс по умолчанию: {avatarIndex}");
        }

        Sprite avatarSprite = AvatarManager.Instance.GetAvatar(avatarIndex);
        
        if (avatarSprite != null)
        {
            iconImage.sprite = avatarSprite;
            Debug.Log($"Установлена аватарка с индексом {avatarIndex} для игрока {player.NickName}");
        }
        else
        {
            Debug.LogError($"Не удалось получить аватарку с индексом {avatarIndex} для игрока {player.NickName}");
        }
    }

    private LeaderboardContainer GetLeaderboardContainer(LeaderboardType type)
    {
        foreach (var container in leaderboardContainers)
        {
            if (container.type == type)
                return container;
        }
        Debug.LogError($"Не найден контейнер для типа лидерборда: {type}");
        return null;
    }

    public void ShowGameplayLeaderboard()
    {
        AnimateLeaderboardTransition(LeaderboardType.Gameplay, true);
        AnimateLeaderboardTransition(LeaderboardType.EndGame, false);
    }

    public void ShowEndGameLeaderboard()
    {
        InitializePlayerList(LeaderboardType.EndGame);
        AnimateLeaderboardTransition(LeaderboardType.Gameplay, false);
        AnimateLeaderboardTransition(LeaderboardType.EndGame, true);
    }

    private void AnimateLeaderboardTransition(LeaderboardType type, bool show)
    {
        var container = GetLeaderboardContainer(type);
        if (container == null) return;

        GameObject containerObj = container.container.gameObject;
        CanvasGroup canvasGroup = containerObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = containerObj.AddComponent<CanvasGroup>();
        }

        if (show)
        {
            containerObj.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeAnimationDuration).SetEase(fadeEase);
            containerObj.transform.localScale = Vector3.one * 0.9f;
            containerObj.transform.DOScale(Vector3.one, scaleAnimationDuration).SetEase(scaleEase);
        }
        else
        {
            canvasGroup.DOFade(0f, fadeAnimationDuration).SetEase(fadeEase)
                .OnComplete(() => containerObj.SetActive(false));
        }
    }

    private void SetLeaderboardVisibility(LeaderboardType type, bool visible)
    {
        var container = GetLeaderboardContainer(type);
        if (container != null)
        {
            container.container.gameObject.SetActive(visible);
        }
    }

    public Dictionary<int, int> GetPlayerScores()
    {
        return new Dictionary<int, int>(playerScores);
    }

    private void ReorderPlayerList(LeaderboardType leaderboardType)
    {
        if (isReordering[leaderboardType]) return;
        
        var scoreItems = scoreItemsByType[leaderboardType];
        
        List<KeyValuePair<int, GameObject>> connectedPlayers = new List<KeyValuePair<int, GameObject>>();
        List<KeyValuePair<int, GameObject>> disconnectedPlayers = new List<KeyValuePair<int, GameObject>>();

        foreach (var kvp in scoreItems)
        {
            int playerId = kvp.Key;
            GameObject playerItem = kvp.Value;

            if (playerConnectionStatus.ContainsKey(playerId) && playerConnectionStatus[playerId])
            {
                connectedPlayers.Add(kvp);
            }
            else
            {
                disconnectedPlayers.Add(kvp);
            }
        }

        connectedPlayers.Sort((p1, p2) =>
        {
            int score1 = playerScores.ContainsKey(p1.Key) ? playerScores[p1.Key] : 0;
            int score2 = playerScores.ContainsKey(p2.Key) ? playerScores[p2.Key] : 0;

            if (score1 == score2)
                return p1.Key.CompareTo(p2.Key);

            return score2.CompareTo(score1);
        });

        disconnectedPlayers.Sort((p1, p2) => p1.Key.CompareTo(p2.Key));

        List<KeyValuePair<int, GameObject>> allPlayers = new List<KeyValuePair<int, GameObject>>();
        allPlayers.AddRange(connectedPlayers);
        allPlayers.AddRange(disconnectedPlayers);

        StartCoroutine(AnimateReorder(allPlayers, leaderboardType));
    }

    private IEnumerator AnimateReorder(List<KeyValuePair<int, GameObject>> sortedPlayers, LeaderboardType leaderboardType)
    {
        isReordering[leaderboardType] = true;
        
        // Сохраняем текущие позиции
        Dictionary<int, Vector3> currentPositions = new Dictionary<int, Vector3>();
        foreach (var kvp in sortedPlayers)
        {
            currentPositions[kvp.Key] = kvp.Value.transform.position;
        }

        // Устанавливаем новые позиции в иерархии без анимации
        int playerIndex = 1;
        foreach (var kvp in sortedPlayers)
        {
            GameObject playerItem = kvp.Value;
            int playerId = kvp.Key;
            bool isConnected = playerConnectionStatus.ContainsKey(playerId) && playerConnectionStatus[playerId];

            playerItem.transform.SetSiblingIndex(playerIndex - 1);

            TMP_Text numberText = playerItem.transform.Find("TextGroup/NumberText")?.GetComponent<TMP_Text>();
            if (numberText != null)
            {
                // Анимируем изменение номера
                AnimateNumberChange(numberText, playerIndex);
            }

            UpdatePlayerVisuals(playerId, playerItem, isConnected);
            playerIndex++;
        }

        // Принудительно обновляем layout
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetLeaderboardContainer(leaderboardType).container.GetComponent<RectTransform>());
        yield return new WaitForEndOfFrame();

        // Анимируем движение к новым позициям
        List<Tween> moveTweens = new List<Tween>();
        foreach (var kvp in sortedPlayers)
        {
            GameObject playerItem = kvp.Value;
            int playerId = kvp.Key;
            
            Vector3 targetPosition = playerItem.transform.position;
            playerItem.transform.position = currentPositions[playerId];
            
            // Создаем анимацию движения
            Tween moveTween = playerItem.transform.DOMove(targetPosition, reorderAnimationDuration)
                .SetEase(moveEase);
            moveTweens.Add(moveTween);
        }

        // Ждем завершения всех анимаций
        yield return new WaitForSeconds(reorderAnimationDuration);
        
        isReordering[leaderboardType] = false;
    }

    private void AnimateNumberChange(TMP_Text numberText, int newNumber)
    {
        if (numberText.text == newNumber.ToString()) return;

        // Анимация пульсации при изменении номера
        Sequence numberSequence = DOTween.Sequence();
        numberSequence.Append(numberText.transform.DOScale(Vector3.one * 1.2f, 0.1f));
        numberSequence.AppendCallback(() => numberText.text = newNumber.ToString());
        numberSequence.Append(numberText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
    }

    public void UpdateScoreDisplay(Dictionary<int, int> newPlayerScores, LeaderboardType leaderboardType = LeaderboardType.Gameplay)
    {
        bool scoresChanged = false;
        var scoreItems = scoreItemsByType[leaderboardType];
        
        foreach (var kvp in newPlayerScores)
        {
            int playerId = kvp.Key;
            int newScore = kvp.Value;
            
            if (!playerScores.ContainsKey(playerId) || playerScores[playerId] != newScore)
            {
                scoresChanged = true;
                int oldScore = playerScores.ContainsKey(playerId) ? playerScores[playerId] : 0;
                playerScores[playerId] = newScore;
                
                // Запускаем анимацию счета только если есть разница
                if (oldScore != newScore && scoreItems.TryGetValue(playerId, out GameObject item))
                {
                    StartScoreAnimation(playerId, oldScore, newScore, item, leaderboardType);
                }
            }
        }
        
        // Перестраиваем список только после завершения всех анимаций
        if (scoresChanged)
        {
            StartCoroutine(DelayedReorder(leaderboardType));
        }
    }

    private void StartScoreAnimation(int playerId, int fromScore, int toScore, GameObject item, LeaderboardType leaderboardType)
    {
        // Останавливаем предыдущую анимацию для этого игрока, если она запущена
        if (runningAnimations.ContainsKey(playerId) && runningAnimations[playerId] != null)
        {
            StopCoroutine(runningAnimations[playerId]);
        }

        // Запускаем новую анимацию
        runningAnimations[playerId] = StartCoroutine(AnimateScore(playerId, fromScore, toScore, item, leaderboardType));
    }

    private IEnumerator AnimateScore(int playerId, int fromScore, int toScore, GameObject item, LeaderboardType leaderboardType)
    {
        TMP_Text scoreText = item.transform.Find("TextGroup/TextGroup_NameAndScore/ScoreText")?.GetComponent<TMP_Text>();
        if (scoreText == null) yield break;

        // Анимация пульсации текста счета в начале
        Sequence scoreTextSequence = DOTween.Sequence();
        scoreTextSequence.Append(scoreText.transform.DOScale(Vector3.one * 1.1f, 0.1f));
        scoreTextSequence.Append(scoreText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));

        float stepDelay = 1f / animationStepsPerSecond;
        int totalSteps = Mathf.RoundToInt(animationDuration * animationStepsPerSecond);
        int scoreDifference = toScore - fromScore;

        if (Mathf.Abs(scoreDifference) < totalSteps)
        {
            totalSteps = Mathf.Abs(scoreDifference);
        }

        bool playedSound = false;
        
        // Создаем постоянную анимацию подпрыгивания во время подсчета
        Tween bounceTween = null;
        if (totalSteps > 0)
        {
            bounceTween = scoreText.transform.DOScale(Vector3.one * 1.05f, 0.1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutExpo);
        }

        for (int step = 0; step <= totalSteps; step++)
        {
            float t = totalSteps > 0 ? (float)step / totalSteps : 1f;
            float easeT = 1 - Mathf.Pow(1 - t, 3);

            int currentDisplayScore = Mathf.RoundToInt(Mathf.Lerp(fromScore, toScore, easeT));
            displayedScores[playerId] = currentDisplayScore;
            scoreText.text = currentDisplayScore.ToString();

            if (!playedSound && currentDisplayScore != fromScore)
            {
                AudioManager.Instance.PlaySFX("ScoreTick", 0.3f);
                playedSound = true;
            }

            if (step < totalSteps)
                yield return new WaitForSeconds(stepDelay);
        }

        // Останавливаем анимацию подпрыгивания
        if (bounceTween != null)
        {
            bounceTween.Kill();
        }

        // Возвращаем к нормальному размеру с финальной пульсацией
        scoreText.transform.DOScale(Vector3.one * 1.05f, 0.1f)
            .OnComplete(() => scoreText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutExpo));

        displayedScores[playerId] = toScore;
        scoreText.text = toScore.ToString();
        runningAnimations.Remove(playerId);
    }

    private IEnumerator DelayedReorder(LeaderboardType leaderboardType)
    {
        // Ждем, пока все анимации завершатся
        while (runningAnimations.Count > 0)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Теперь перестраиваем список
        ReorderPlayerList(leaderboardType);
    }

    private void UpdatePlayerVisuals(int playerId, GameObject item, bool isConnected)
    {
        Transform textGroup = item.transform.Find("TextGroup");
        if (textGroup != null)
        {
            Image backgroundImage = item.GetComponent<Image>();
            if (backgroundImage != null)
            {
                Color targetColor = isConnected 
                    ? originalBackgroundColors.ContainsKey(playerId) ? originalBackgroundColors[playerId] : backgroundImage.color
                    : disconnectedPlayerColor;
                
                // Анимируем изменение цвета
                backgroundImage.DOColor(targetColor, fadeAnimationDuration);
            }

            TMP_Text numberText = textGroup.Find("NumberText")?.GetComponent<TMP_Text>();
            if (numberText != null)
            {
                Color targetColor = isConnected 
                    ? originalNumberColors.ContainsKey(playerId) ? originalNumberColors[playerId] : numberText.color
                    : disconnectedPlayerColor;
                
                numberText.DOColor(targetColor, fadeAnimationDuration);
            }

            Transform nickScoreGroup = textGroup.Find("TextGroup_NameAndScore");
            if (nickScoreGroup != null)
            {
                TMP_Text nickText = nickScoreGroup.Find("NickText")?.GetComponent<TMP_Text>();
                TMP_Text scoreText = nickScoreGroup.Find("ScoreText")?.GetComponent<TMP_Text>();
                Image iconImage = nickScoreGroup.Find("Icon")?.GetComponent<Image>();

                if (nickText != null)
                {
                    Color targetColor = isConnected 
                        ? originalNickColors.ContainsKey(playerId) ? originalNickColors[playerId] : nickText.color
                        : disconnectedPlayerColor;
                    
                    nickText.DOColor(targetColor, fadeAnimationDuration);
                        
                    string baseName = nickText.text;
                    if (baseName.EndsWith(" (Disconnected)"))
                    {
                        baseName = baseName.Substring(0, baseName.Length - " (Disconnected)".Length);
                    }
                    nickText.text = isConnected ? baseName : baseName + " (Disconnected)";
                }

                if (scoreText != null)
                {
                    Color targetColor = isConnected 
                        ? originalScoreColors.ContainsKey(playerId) ? originalScoreColors[playerId] : scoreText.color
                        : disconnectedPlayerColor;
                    
                    scoreText.DOColor(targetColor, fadeAnimationDuration);
                }

                if (iconImage != null)
                {
                    Color targetColor = isConnected 
                        ? originalAvatarColors.ContainsKey(playerId) ? originalAvatarColors[playerId] : iconImage.color
                        : disconnectedPlayerColor;
                    
                    iconImage.DOColor(targetColor, fadeAnimationDuration);
                }
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        foreach (var container in leaderboardContainers)
        {
            if (container.container.gameObject.activeInHierarchy)
            {
                InitializePlayerList(container.type);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int playerId = otherPlayer.ActorNumber;
        playerConnectionStatus[playerId] = false;
        
        // Останавливаем анимацию для отключившегося игрока
        if (runningAnimations.ContainsKey(playerId) && runningAnimations[playerId] != null)
        {
            StopCoroutine(runningAnimations[playerId]);
            runningAnimations.Remove(playerId);
        }
        
        foreach (var container in leaderboardContainers)
        {
            if (container.container.gameObject.activeInHierarchy)
            {
                // Анимируем исчезновение элемента отключившегося игрока
                var scoreItems = scoreItemsByType[container.type];
                if (scoreItems.TryGetValue(playerId, out GameObject item))
                {
                    AnimateItemDisappearance(item, () => ReorderPlayerList(container.type));
                }
                else
                {
                    ReorderPlayerList(container.type);
                }
            }
        }
    }

    private Image GetPlayerIconImage(GameObject item)
    {
        return item.transform.Find("TextGroup/TextGroup_NameAndScore/PlayerIcon/PlayerIconMask/Icon")?.GetComponent<Image>();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("AvatarIndex"))
        {
            foreach (var container in leaderboardContainers)
            {
                if (container.container.gameObject.activeInHierarchy)
                {
                    var scoreItems = scoreItemsByType[container.type];
                    if (scoreItems.TryGetValue(targetPlayer.ActorNumber, out GameObject item))
                    {
                        Image iconImage = GetPlayerIconImage(item);
                        SetPlayerAvatar(targetPlayer, iconImage);
                        
                        // Анимируем обновление аватарки
                        if (iconImage != null)
                        {
                            iconImage.transform.DOScale(Vector3.one * 1.1f, 0.1f)
                                .OnComplete(() => iconImage.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
                        }
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Убиваем все активные твины при уничтожении объекта
        DOTween.KillAll();
    }
}