
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Создает комнату с заданными параметрами.
    /// </summary>
    /// <param name="roomName">Название комнаты</param>
    /// <param name="maxPlayers">Максимальное количество игроков</param>
    /// <param name="isVisible">Видна ли комната в списке</param>
    /// <param name="isOpen">Можно ли заходить в комнату</param>
    public void CreateRoom(string roomName, byte maxPlayers, bool isVisible, bool isOpen)
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = isVisible,
            IsOpen = isOpen
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Комната {PhotonNetwork.CurrentRoom.Name} успешно создана!");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Ошибка создания комнаты: {message}");
    }
}
