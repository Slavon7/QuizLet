using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;

public class ExitGame : MonoBehaviour
{
    public void LeaveGame()
    {
        StartCoroutine(LeaveAndLoadLobby());
    }

    private IEnumerator LeaveAndLoadLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        while (PhotonNetwork.InRoom)
        {
            yield return null;
        }

        SceneManager.LoadScene("Lobby");
    }
}