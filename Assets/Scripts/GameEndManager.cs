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
    [SerializeField] private PlayerScoreDisplay scoreDisplay;
    [SerializeField] private ParticleSystem winnerParticles;
    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Button exitButton;
    [SerializeField] private BlurController blurController;
    [SerializeField] private GameOverUIController gameOverUI;
    private Dictionary<int, int> _correctAnswers = new Dictionary<int, int>();

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

        // Показать финальный лидерборд
        if (scoreDisplay != null)
        {
            scoreDisplay.ShowEndGameLeaderboard();
        }
        else
        {
            Debug.LogError("PlayerScoreDisplay не назначен в GameEndManager!");
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

        // --- НАЧИСЛЕНИЕ ОПЫТА ---
        AwardGemsToLocalPlayer(scores, correctAnswers);
        AwardExperienceToLocalPlayer(scores);
        ShowGameOverSection(scores);
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
        int correctAnswers = 0;
        int gemsEarned = 0;
        if (_correctAnswers.TryGetValue(localId, out correctAnswers))
        {
            gemsEarned = correctAnswers + (place switch
            {
                1 => 5,
                2 => 3,
                3 => 2,
                <= 8 => 1,
                _ => 0
            });
        }

        if (gameOverUI != null)
            gameOverUI.ShowInfo(place, xpGained, currentXP, xpToLevelUp, currentLevel, gemsEarned);
    }


    /// <summary>
    /// Альтернативный метод для показа финального лидерборда без определения победителя
    /// (если нужно показать лидерборд отдельно)
    /// </summary>
    public void ShowFinalLeaderboard()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.ShowEndGameLeaderboard();
        }
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

    /// <summary>
    /// Метод для возврата к игровому лидерборду (если нужен)
    /// </summary>
    public void ShowGameplayLeaderboard()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.ShowGameplayLeaderboard();
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