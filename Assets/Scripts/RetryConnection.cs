using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;

public class RetryConnection : MonoBehaviourPunCallbacks
{
    [SerializeField] private string lobbySceneName = "Lobby";

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Уже подключены при старте. Загружаем лобби...");
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    public void OnRetryButtonClicked()
    {
        var state = PhotonNetwork.NetworkClientState;
        Debug.Log("Состояние Photon: " + state);

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("Уже подключены. Загружаем лоббі...");
            SceneManager.LoadScene(lobbySceneName);
            return;
        }

        if (state != ClientState.Disconnected)
        {
            Debug.Log("Уже идет подключение или другой процесс. Ждем...");
            return;
        }

        Debug.Log("Пробуем переподключиться к Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Подключились к мастер-серверу. Присоединяемся к лобби...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Вошли в лобби. Загружаем сцену...");
        SceneManager.LoadScene(lobbySceneName);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Отключено от Photon: {cause}");
    }
}