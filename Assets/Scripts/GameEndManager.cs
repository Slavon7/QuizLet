using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameEndManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private ParticleSystem winnerParticles;
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Button exitButton;
    [SerializeField] private BlurController blurController;
    [SerializeField] private GameOverUIController gameOverUI;
    private Dictionary<int, int> _correctAnswers = new Dictionary<int, int>();
    private int _lastCupsChange = 0;

    private void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToLobby);

        gameEndPanel.SetActive(false); // Ховаємо панель на початку
    }

    /// <summary>
    /// Вивід переможця на основі переданих очок.
    /// </summary>
    public void DisplayWinner(Dictionary<int, int> scores, Dictionary<int, int> correctAnswers)
    {
        _correctAnswers = correctAnswers;
        int topScore = -1;
        List<int> topPlayers = new List<int>();

        // Находим игроков с максимальным счётом
        foreach (var kvp in scores)
        {
            if (kvp.Value > topScore)
            {
                topScore = kvp.Value;
                topPlayers.Clear();
                topPlayers.Add(kvp.Key);
            }
            else if (kvp.Value == topScore)
            {
                topPlayers.Add(kvp.Key);
            }
        }

        gameEndPanel.SetActive(true);

        if (blurController != null)
            blurController.StartFadeIn();
        else
            Debug.LogWarning("BlurController не подключен к GameEndManager");

        if (topPlayers.Count > 0)
        {
            string winnerNames = "";

            foreach (int playerId in topPlayers)
            {
                Player winner = GetPlayerById(playerId);
                if (winner != null)
                {
                    winnerNames += winner.NickName + ", ";
                }
                else
                {
                    winnerNames += $"Player {playerId}, ";
                }
            }

            if (winnerNames.EndsWith(", "))
                winnerNames = winnerNames.Substring(0, winnerNames.Length - 2);

            winnerText.text = topPlayers.Count == 1
                ? $"{winnerNames}"
                : $"Нічия між: {winnerNames} — по {topScore} очок!";

            if (winnerParticles != null)
                winnerParticles.Play();
            else
                Debug.LogWarning("Не назначено Particle System для победителя");
        }

        _lastCupsChange = AwardCupsToLocalPlayer(scores);
        _lastCupsChange += 5; // AwardTestCups
        AwardTestCups();

        // --- НАЧИСЛЕНИЕ ОПЫТА ---
        AwardGemsToLocalPlayer(scores, correctAnswers);
        AwardExperienceToLocalPlayer(scores);
        ShowGameOverSection(scores);
    }

    private void AwardTestCups()
    {
        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.AddCups(5);
            Debug.Log("[TEST] Начислено +5 кубков после игры");
        }
        else
        {
            Debug.LogWarning("[TEST] ProfileManager.Instance не найден");
        }
    }

    private void AwardGemsToLocalPlayer(Dictionary<int, int> scores, Dictionary<int, int> correctAnswers)
    {
        int localId = PhotonNetwork.LocalPlayer.ActorNumber;

        if (!scores.TryGetValue(localId, out int localScore)) return;

        // Місце гравця
        List<int> ordered = new List<int>(scores.Values);
        ordered.Sort((a, b) => b.CompareTo(a));
        int place = ordered.IndexOf(localScore) + 1;

        // Бонус за місце
        int placeBonus = place switch
        {
            1 => 5,
            2 => 3,
            3 => 2,
            <= 8 => 1,
            _ => 0
        };

        int correct = correctAnswers.TryGetValue(localId, out int count) ? count : 0;
        int totalGems = correct + placeBonus;

        // Додаємо геми
        CurrencyManager.Instance.AddGems(totalGems, $"Гра завершена: {correct} правильних + бонус за місце {place}");

        Debug.Log($"Гравець {localId} отримав {totalGems} гемів ({correct} за відповіді, {placeBonus} за місце)");
    }

    private void ShowGameOverSection(Dictionary<int, int> scores)
    {
        int localId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!scores.TryGetValue(localId, out int xpGained)) xpGained = 0;

        List<int> ordered = new List<int>(scores.Values);
        ordered.Sort((a, b) => b.CompareTo(a));
        int place = ordered.IndexOf(xpGained) + 1;

        int currentXP = PlayerPrefs.GetInt("CurrentXP", 0);
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        int xpToLevelUp = 5000; // если динамически — вынеси
        _correctAnswers.TryGetValue(localId, out int correctAnswers);
        int placeBonus = place switch
        {
            1 => 5,
            2 => 3,
            3 => 2,
            <= 8 => 1,
            _ => 0
        };
        int gemsEarned = correctAnswers + placeBonus;

        if (gameOverUI != null)
            gameOverUI.ShowInfo(place, xpGained, currentXP, xpToLevelUp, currentLevel, gemsEarned, _lastCupsChange);
    }


    private int AwardCupsToLocalPlayer(Dictionary<int, int> scores)
    {
        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;

        int localId = PhotonNetwork.LocalPlayer.ActorNumber;
        if (!scores.TryGetValue(localId, out int myScore)) return 0;

        List<int> sortedScores = new List<int>(scores.Values);
        sortedScores.Sort((a, b) => b.CompareTo(a));

        int myRank = sortedScores.IndexOf(myScore) + 1;
        int cupsChange = CalculateTrophyChange(playerCount, myRank);

        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.AddCups(cupsChange);
            Debug.Log($"[Trophies] Players={playerCount}, Rank={myRank}, Change={cupsChange}, TotalNow={ProfileManager.Instance.GetCups()}");
        }
        else
        {
            Debug.LogWarning("ProfileManager.Instance не найден — кубки не начислены.");
        }

        return cupsChange;
    }

    // 🔥 Формула расчета (Та самая Python логика переведенная в C#)
    private int CalculateTrophyChange(int totalPlayers, int rank)
    {
        // Базовая ставка: для 8 игроков = 50.
        // (totalPlayers / 8f) дает коэффициент.
        float maxRewardFloat = 50f * ((float)totalPlayers / 8f);
        int maxReward = Mathf.RoundToInt(maxRewardFloat);

        // Спец. условие для дуэли (чтобы было интереснее чем +12)
        if (totalPlayers == 2) maxReward = 15;

        // Если игрок всего 1 (ошибка логики), возвращаем 0
        if (totalPlayers < 2) return 0;

        // Шаг изменения
        float step = (maxReward * 2f) / (totalPlayers - 1);

        // Формула: Max - (Место - 1) * Шаг
        float change = maxReward - (rank - 1) * step;

        return Mathf.RoundToInt(change);
    }
    
    private void AwardExperienceToLocalPlayer(Dictionary<int, int> scores)
    {
        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if (scores.TryGetValue(localActorNumber, out int localScore))
        {
            // Сохраняем опыт в CustomProperties игрока (или PlayerPrefs)
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["ExperienceToAdd"] = localScore;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log($"Опыт {localScore} сохранён в CustomProperties локального игрока");
        }
        else
        {
            Debug.LogWarning("Очки локального игрока не найдены в словаре scores");
        }
    }

    private Player GetPlayerById(int actorNumber)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber == actorNumber)
                return player;
        }

        return null;
    }

    private void ExitToLobby()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene"); // Замінити на актуальну назву сцени
    }
}