using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using DG.Tweening;

public class CreateAndJoin : MonoBehaviourPunCallbacks
{
    public TMP_InputField input_Create;
    public TMP_InputField input_Join;
    public GameObject lobbyUI;
    public GameObject startButton;
    public GameObject exitRoomButton;
    public TextMeshProUGUI playerCountText;
    public GameObject joinPanel;
    
    public GameObject roomPrefab;
    [Header("Room Lists")]
    public Transform roomListContent;
    public Transform myRoomListContent;
    public GameObject myRoomHeader;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public Ease animationEase = Ease.OutBack;
    
    private bool isMaster = false;
    private int roomCounter = 1;
    private Dictionary<string, Room> roomUIMap = new Dictionary<string, Room>();

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();
    private Dictionary<int, string> playerNames = new Dictionary<int, string>();
    private Dictionary<int, int> playerAvatars = new Dictionary<int, int>();
    private Dictionary<string, RoomInfo> updatedRoomCache = new Dictionary<string, RoomInfo>();

    void Start()
    {
        // Инициализируем кэш комнат
        updatedRoomCache = new Dictionary<string, RoomInfo>();
        
        startButton.SetActive(false);
        exitRoomButton.SetActive(false);

        if (PhotonNetwork.IsConnected && PhotonNetwork.InLobby)
        {
            HideConnectUI();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            string roomName = roomCounter.ToString();
            roomCounter++;

            RoomOptions options = new RoomOptions()
            {
                MaxPlayers = 5,
                IsVisible = true,
                IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable(),
                CustomRoomPropertiesForLobby = new string[] { "Nicknames", "AvatarIndices" }
            };

            PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
            
            // Анимированное скрытие UI подключения
            AnimateHideConnectUI();
            
            // Анимированное появление кнопки выхода
            AnimateShowExitButton();
            
            isMaster = true;
            
            if (PhotonNetwork.IsMasterClient)
            {
                AnimateShowStartButton();
            }
            
            if (PhotonNetwork.CurrentRoom != null)
            {
                UpdatePlayerCount();
            }
        }
        else
        {
            Debug.LogError("Not connected to Master Server yet");
        }
    }

    private void AnimateHideConnectUI()
    {
        if (joinPanel != null)
        {
            joinPanel.transform.DOScale(0f, animationDuration)
                .SetEase(animationEase)
                .OnComplete(() => joinPanel.SetActive(false));
        }
        else
        {
            if (input_Create != null)
            {
                input_Create.transform.DOScale(0f, animationDuration)
                    .SetEase(animationEase)
                    .OnComplete(() => input_Create.gameObject.SetActive(false));
            }
            if (input_Join != null)
            {
                input_Join.transform.DOScale(0f, animationDuration)
                    .SetEase(animationEase)
                    .OnComplete(() => input_Join.gameObject.SetActive(false));
            }
        }
    }

    private void AnimateShowExitButton()
    {
        exitRoomButton.transform.localScale = Vector3.zero;
        exitRoomButton.SetActive(true);
        exitRoomButton.transform.DOScale(1f, animationDuration)
            .SetEase(animationEase)
            .SetDelay(0.1f);
    }

    private void AnimateShowStartButton()
    {
        startButton.transform.localScale = Vector3.zero;
        startButton.SetActive(true);
        startButton.transform.DOScale(1f, animationDuration)
            .SetEase(animationEase)
            .SetDelay(0.2f);
    }

    private void AnimateShowMyRoomHeader()
    {
        if (myRoomHeader != null)
        {
            myRoomHeader.transform.localScale = Vector3.zero;
            myRoomHeader.SetActive(true);
            myRoomHeader.transform.DOScale(1f, animationDuration)
                .SetEase(animationEase)
                .SetDelay(0.3f);
        }
    }

    private void AnimateRoomCreation(GameObject roomObject)
    {
        // Устанавливаем начальные значения для анимации
        roomObject.transform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = roomObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = roomObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;

        // Анимируем появление
        Sequence roomAnimation = DOTween.Sequence();
        roomAnimation.Append(roomObject.transform.DOScale(1f, animationDuration).SetEase(animationEase));
        roomAnimation.Join(canvasGroup.DOFade(1f, animationDuration));
        roomAnimation.SetDelay(0.1f);
    }

    // Анимация для списка комнат
    private void AnimateRoomListUpdate()
    {
        // Анимируем исчезновение старых комнат
        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in roomListContent)
        {
            childrenToDestroy.Add(child);
        }

        Sequence hideSequence = DOTween.Sequence();
        foreach (Transform child in childrenToDestroy)
        {
            hideSequence.Join(child.DOScale(0f, animationDuration * 0.5f).SetEase(Ease.InBack));
            hideSequence.Join(child.GetComponent<CanvasGroup>()?.DOFade(0f, animationDuration * 0.5f) ?? DOVirtual.DelayedCall(0, () => { }));
        }
        
        hideSequence.OnComplete(() =>
        {
            foreach (Transform child in childrenToDestroy)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
            
            // Затем создаем новые комнаты с анимацией
            StartCoroutine(CreateRoomsWithAnimation());
        });
    }

    private IEnumerator CreateRoomsWithAnimation()
    {
        float delayBetweenRooms = 0.1f;
        
        foreach (var kvp in updatedRoomCache)
        {
            RoomInfo room = kvp.Value;

            if (!room.IsOpen || !room.IsVisible || room.PlayerCount == 0)
                continue;

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == room.Name)
                continue;

            GameObject roomGO = Instantiate(roomPrefab, roomListContent);
            Room roomScript = roomGO.GetComponent<Room>();

            roomScript.Name.text = room.Name;
            roomScript.PlayerCount.text = $"{room.PlayerCount}/{room.MaxPlayers}";

            // Настраиваем игроков и аватары
            SetupRoomPlayersAndAvatars(roomScript, room);

            // Анимируем появление комнаты
            AnimateRoomCreation(roomGO);

            yield return new WaitForSeconds(delayBetweenRooms);
        }
    }
    
    private void SetupRoomPlayersAndAvatars(Room roomScript, RoomInfo room)
    {
        roomScript.playerNameListContainer.DetachChildren();

        // Получаем аватары из AvatarManager, если они не загружены в Room
        Sprite[] availableAvatars = roomScript.availableAvatars;
        if (availableAvatars == null || availableAvatars.Length == 0)
        {
            if (AvatarManager.Instance != null)
            {
                availableAvatars = AvatarManager.Instance.GetAllAvatars();
                Debug.Log("SetupRoomPlayersAndAvatars: Загружены аватары из AvatarManager: " + availableAvatars.Length);
            }
            else
            {
                Debug.LogWarning("SetupRoomPlayersAndAvatars: AvatarManager недоступен!");
            }
        }

        if (room.CustomProperties.ContainsKey("Nicknames") && room.CustomProperties["Nicknames"] is string[] nicknames)
        {
            int[] avatarIndices = null;
            if (room.CustomProperties.ContainsKey("AvatarIndices") && room.CustomProperties["AvatarIndices"] is int[] indices)
            {
                avatarIndices = indices;
            }

            for (int i = 0; i < nicknames.Length; i++)
            {
                GameObject nameGO = Instantiate(roomScript.playerNamePrefab, roomScript.playerNameListContainer);

                TMP_Text text = nameGO.GetComponentInChildren<TMP_Text>();
                text.text = nicknames[i];

                if (avatarIndices != null && i < avatarIndices.Length)
                {
                    Transform avatarTransform = nameGO.transform.Find("PlayerObject/PlayerIcon/PlayerIconMask/AvatarImage");
                    if (avatarTransform != null)
                    {
                        Image avatarImage = avatarTransform.GetComponent<Image>();
                        int avatarIndex = avatarIndices[i];

                        if (availableAvatars != null && availableAvatars.Length > 0 &&
                            avatarIndex >= 0 && avatarIndex < availableAvatars.Length)
                        {
                            avatarImage.sprite = availableAvatars[avatarIndex];
                            Debug.Log($"Установлен аватар {avatarIndex} для игрока {nicknames[i]} в списке комнат");
                        }
                        else
                        {
                            avatarImage.sprite = roomScript.defaultAvatar;
                            Debug.Log($"Используется дефолтный аватар для игрока {nicknames[i]}, индекс {avatarIndex} некорректен");
                        }
                    }
                }
            }
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Комната создана: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.JoinLobby();
    }

    public void JoinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(input_Join.text);
        }
        else
        {
            Debug.LogError("Not connected to Master Server yet");
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Вошел в лобби. Синхронизирую кастомные свойства игрока...");
        
        HideConnectUI();

        ProfileManager profileManager = Object.FindFirstObjectByType<ProfileManager>(); 
        if (profileManager != null)
        {
            profileManager.SendMessage("SyncPlayerCustomProperties", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning("Не найден ProfileManager при входе в лобби!");
        }
    }

    private void HideConnectUI()
    {
        if (joinPanel != null)
        {
            joinPanel.SetActive(false);
        }
        else
        {
            if (input_Create != null) input_Create.gameObject.SetActive(false);
            if (input_Join != null) input_Join.gameObject.SetActive(false);
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"Обновлены свойства игрока {targetPlayer.NickName}: {string.Join(", ", changedProps.Keys)}");
            
            if (changedProps.ContainsKey("AvatarIndex") && changedProps["AvatarIndex"] is int avatarIndex)
            {
                if (!playerAvatars.ContainsKey(targetPlayer.ActorNumber))
                    playerAvatars.Add(targetPlayer.ActorNumber, avatarIndex);
                else
                    playerAvatars[targetPlayer.ActorNumber] = avatarIndex;
                
                UpdateRoomAvatarIndicesProperty();
            }
            UpdateRoomUI();
        }
    }

    public void JoinRoomInList(string roomName)
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(roomName);
            Debug.Log("Игрок входит в комнату: " + roomName);
        }
        else
        {
            Debug.LogError("Не подключен к серверу.");
        }
    }

    IEnumerator DelayedUpdatePlayerCount()
    {
        yield return new WaitForSeconds(0.5f);
        UpdatePlayerCount();
    }

    public override void OnJoinedRoom()
    {
        // Анимированное скрытие UI подключения
        AnimateHideConnectUI();
        
        // Анимированное появление кнопок
        AnimateShowExitButton();
        
        isMaster = PhotonNetwork.IsMasterClient;
        if (PhotonNetwork.IsMasterClient)
            AnimateShowStartButton();

        UpdatePlayerCount();
        
        // Анимированное появление заголовка моей комнаты
        AnimateShowMyRoomHeader();
        
        MoveRoomToMyRoomList();
        
        UpdateRoomUI();
        UpdateRoomNicknamesProperty();
        UpdateRoomAvatarIndicesProperty();

        if (PhotonNetwork.IsConnected)
        {
            int myAvatarIndex = 0;
            if (PlayerPrefs.HasKey("AvatarIndex"))
            {
                myAvatarIndex = PlayerPrefs.GetInt("AvatarIndex");
            }
            
            photonView.RPC("RPC_SendNicknameAndAvatar", RpcTarget.AllBuffered, 
                PhotonNetwork.LocalPlayer.ActorNumber, 
                PhotonNetwork.NickName,
                myAvatarIndex);
        }
        
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_RequestDataSync", PhotonNetwork.MasterClient);
        }
    }

    private void MoveRoomToMyRoomList()
    {
        string currentRoomName = PhotonNetwork.CurrentRoom.Name;
        
        Room roomToMove = null;
        foreach (Transform child in roomListContent)
        {
            Room roomComponent = child.GetComponent<Room>();
            if (roomComponent != null && roomComponent.Name.text == currentRoomName)
            {
                roomToMove = roomComponent;
                break;
            }
        }
        
        if (roomToMove != null)
        {
            // Анимируем перемещение комнаты
            Vector3 originalPosition = roomToMove.transform.position;
            roomToMove.transform.SetParent(myRoomListContent);
            roomToMove.transform.localScale = Vector3.one;
            
            // Анимируем плавное перемещение
            roomToMove.transform.position = originalPosition;
            roomToMove.transform.DOMove(myRoomListContent.position, animationDuration)
                .SetEase(animationEase)
                .OnComplete(() =>
                {
                    roomToMove.transform.localPosition = Vector3.zero;
                    roomToMove.DisableJoinButton();
                });
            
            Debug.Log("Moved room to My Room list: " + currentRoomName);
        }
        else
        {
            GameObject newRoom = Instantiate(roomPrefab, myRoomListContent);
            Room roomComponent = newRoom.GetComponent<Room>();
            roomComponent.Name.text = PhotonNetwork.CurrentRoom.Name;
            roomComponent.PlayerCount.text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            roomComponent.DisableJoinButton();
            
            // Анимируем появление новой комнаты
            AnimateRoomCreation(newRoom);
            
            Debug.Log("Created room directly in My Room list: " + currentRoomName);
        }
        
        foreach (Transform child in myRoomListContent)
        {
            Room roomComponent = child.GetComponent<Room>();
            if (roomComponent != null && roomComponent.Name.text != currentRoomName)
            {
                // Анимируем исчезновение старых комнат
                child.DOScale(0f, animationDuration * 0.5f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(child.gameObject));
            }
        }
    }

    // Анимированное появление UI подключения при выходе из комнаты
    private void AnimateShowConnectUI()
    {
        if (joinPanel != null)
        {
            joinPanel.transform.localScale = Vector3.zero;
            joinPanel.SetActive(true);
            joinPanel.transform.DOScale(1f, animationDuration)
                .SetEase(animationEase);
        }
        else
        {
            if (input_Create != null)
            {
                input_Create.transform.localScale = Vector3.zero;
                input_Create.gameObject.SetActive(true);
                input_Create.transform.DOScale(1f, animationDuration)
                    .SetEase(animationEase);
            }
            if (input_Join != null)
            {
                input_Join.transform.localScale = Vector3.zero;
                input_Join.gameObject.SetActive(true);
                input_Join.transform.DOScale(1f, animationDuration)
                    .SetEase(animationEase)
                    .SetDelay(0.1f);
            }
        }
    }

    // Остальные методы остаются без изменений...
    [PunRPC]
    private void RPC_RequestDataSync()
    {
        Photon.Realtime.Player requestingPlayer = photonView.Owner;
        
        foreach (var entry in playerNames)
        {
            int avatarIndex = 0;
            if (playerAvatars.ContainsKey(entry.Key))
            {
                avatarIndex = playerAvatars[entry.Key];
            }
            
            photonView.RPC("RPC_SendNicknameAndAvatar", requestingPlayer, entry.Key, entry.Value, avatarIndex);
        }
    }

    private void CreateMyRoomUI()
    {
        Room existingRoom = FindRoomUIObject();
        if (existingRoom != null)
        {
            existingRoom.PlayerCount.text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            return;
        }

        GameObject newRoom = Instantiate(roomPrefab, roomListContent);
        Room roomComponent = newRoom.GetComponent<Room>();
        roomComponent.Name.text = PhotonNetwork.CurrentRoom.Name;
        roomComponent.PlayerCount.text = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
        
        // Анимируем появление
        AnimateRoomCreation(newRoom);
    }

    public void ExitRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        playerNames.Clear();
        playerAvatars.Clear();

        // Анимированное удаление комнат из myRoomListContent
        foreach (Transform child in myRoomListContent)
        {
            child.DOScale(0f, animationDuration * 0.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() => Destroy(child.gameObject));
        }

        if (myRoomHeader != null)
        {
            myRoomHeader.transform.DOScale(0f, animationDuration * 0.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() => myRoomHeader.SetActive(false));
        }

        lobbyUI.SetActive(true);
        
        // Анимированное скрытие кнопок
        startButton.transform.DOScale(0f, animationDuration * 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => startButton.SetActive(false));
            
        exitRoomButton.transform.DOScale(0f, animationDuration * 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => exitRoomButton.SetActive(false));

        // Анимированное появление UI подключения
        DOVirtual.DelayedCall(animationDuration * 0.5f, () => AnimateShowConnectUI());

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.PlayerList.Length > 0)
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.PlayerList[0]);
                AnimateShowStartButton();
            }
        }
        
        updatedRoomCache.Clear();
        UpdateRoomListUI();

        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_LoadGameScene", RpcTarget.All);
        }
    }

    public void MakeRoomPrivate()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            Debug.Log("Комната теперь приватная и невидимая.");
        }
        else
        {
            Debug.LogWarning("Вы не в комнате!");
        }
    }

    [PunRPC]
    private void RPC_LoadGameScene()
    {
        PhotonNetwork.LoadLevel("GamePlay");
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText != null && PhotonNetwork.CurrentRoom != null)
        {
            playerCountText.text = PhotonNetwork.CurrentRoom.PlayerCount.ToString() + "/5";
        }
    }

    private void UpdateRoomUIPanel()
    {
        if (PhotonNetwork.InRoom)
        {
            string currentRoomName = PhotonNetwork.CurrentRoom.Name;
            if (roomUIMap.ContainsKey(currentRoomName))
            {
                Room roomComponent = roomUIMap[currentRoomName];
                roomComponent.UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);
            }
        }
    }

    private void UpdateRoomUI()
    {
        Room currentRoom = FindRoomUIObject();
        if (currentRoom != null)
        {
            currentRoom.UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);
            currentRoom.UpdatePlayerNames();
            Debug.Log("Обновлен UI комнаты с новыми данными игроков");
        }
        else
        {
            Debug.LogWarning("Не найден UI объект комнаты для обновления!");
        }
    }

    private void UpdateRoomNicknamesProperty()
    {
        if (!PhotonNetwork.InRoom) return;

        List<string> nicknames = new List<string>();
        foreach (var player in PhotonNetwork.PlayerList)
        {
            nicknames.Add(player.NickName);
        }

        ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable();
        customProps["Nicknames"] = nicknames.ToArray();

        PhotonNetwork.CurrentRoom.SetCustomProperties(customProps);
    }

    private void UpdateRoomAvatarIndicesProperty()
    {
        if (!PhotonNetwork.InRoom) return;

        List<int> avatarIndices = new List<int>();
        
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int avatarIndex = 0;

            if (playerAvatars.ContainsKey(player.ActorNumber))
            {
                avatarIndex = playerAvatars[player.ActorNumber];
            }
            else if (player.CustomProperties.ContainsKey("AvatarIndex"))
            {
                avatarIndex = (int)player.CustomProperties["AvatarIndex"];
                if (!playerAvatars.ContainsKey(player.ActorNumber))
                {
                    playerAvatars.Add(player.ActorNumber, avatarIndex);
                }
            }
            
            avatarIndices.Add(avatarIndex);
        }

        ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable();
        customProps["AvatarIndices"] = avatarIndices.ToArray();
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(customProps);
        
        Debug.Log($"Обновлены аватарки комнаты: {string.Join(", ", avatarIndices)}");
    }

    private Room FindRoomUIObject()
    {
        foreach (Transform roomItem in myRoomListContent)
        {
            Room roomComponent = roomItem.GetComponent<Room>();
            if (roomComponent != null && roomComponent.Name.text == PhotonNetwork.CurrentRoom.Name)
            {
                return roomComponent;
            }
        }
        
        foreach (Transform roomItem in roomListContent)
        {
            Room roomComponent = roomItem.GetComponent<Room>();
            if (roomComponent != null && roomComponent.Name.text == PhotonNetwork.CurrentRoom.Name)
            {
                return roomComponent;
            }
        }
        
        return null;
    }
    
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        UpdatePlayerCount();
        UpdateRoomUI();
        UpdateRoomNicknamesProperty();
        UpdateRoomAvatarIndicesProperty();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (playerNames.ContainsKey(otherPlayer.ActorNumber))
        {
            playerNames.Remove(otherPlayer.ActorNumber);
        }
        
        if (playerAvatars.ContainsKey(otherPlayer.ActorNumber))
        {
            playerAvatars.Remove(otherPlayer.ActorNumber);
        }
        
        UpdatePlayerCount();
        UpdateRoomUI();
        UpdateRoomUIWithNicknamesAndAvatars();
        UpdateRoomNicknamesProperty();
        UpdateRoomAvatarIndicesProperty();

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.PlayerList.Length > 0)
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.PlayerList[0]);
                AnimateShowStartButton();
            }
            else
            {
                startButton.transform.DOScale(0f, animationDuration * 0.5f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => startButton.SetActive(false));
            }
        }
    }

public override void OnRoomListUpdate(List<RoomInfo> roomList)
{
    bool needsFullUpdate = false;
    
    foreach (RoomInfo info in roomList)
    {
        if (info.RemovedFromList)
        {
            if (updatedRoomCache.ContainsKey(info.Name))
            {
                updatedRoomCache.Remove(info.Name);
                needsFullUpdate = true; // Комната удалена - нужно полное обновление
            }
        }
        else
        {
            bool isNewRoom = !updatedRoomCache.ContainsKey(info.Name);
            updatedRoomCache[info.Name] = info;
            
            if (isNewRoom)
            {
                needsFullUpdate = true; // Новая комната - нужно полное обновление
            }
            else
            {
                // Обновляем существующую комнату без анимации
                UpdateExistingRoomUI(info);
            }
        }
    }

    // Полное обновление только если есть новые/удаленные комнаты
        if (needsFullUpdate)
        {
            AnimateRoomListUpdate();
        }
    }
    private void UpdateExistingRoomUI(RoomInfo room)
{
    // Пропускаем комнату, в которой мы находимся
    if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == room.Name)
        return;

    // Ищем существующую комнату в UI
    Room existingRoomUI = null;
    foreach (Transform child in roomListContent)
    {
        Room roomComponent = child.GetComponent<Room>();
        if (roomComponent != null && roomComponent.Name.text == room.Name)
        {
            existingRoomUI = roomComponent;
            break;
        }
    }

    // Если нашли - обновляем данные без пересоздания
    if (existingRoomUI != null)
    {
        // Обновляем счетчик игроков
        existingRoomUI.PlayerCount.text = $"{room.PlayerCount}/{room.MaxPlayers}";
        
        // Обновляем список игроков и аватары
        UpdateRoomPlayersData(existingRoomUI, room);
        
        Debug.Log($"Обновлена существующая комната {room.Name} без анимации");
    }
}

// Новый метод для обновления данных игроков в существующей комнате
private void UpdateRoomPlayersData(Room roomScript, RoomInfo room)
{
    // Удаляем старых игроков
    foreach (Transform child in roomScript.playerNameListContainer)
    {
        Destroy(child.gameObject);
    }

    // Получаем аватары
    Sprite[] availableAvatars = roomScript.availableAvatars;
    if (availableAvatars == null || availableAvatars.Length == 0)
    {
        if (AvatarManager.Instance != null)
        {
            availableAvatars = AvatarManager.Instance.GetAllAvatars();
        }
    }

    // Создаем новых игроков
    if (room.CustomProperties.ContainsKey("Nicknames") && room.CustomProperties["Nicknames"] is string[] nicknames)
    {
        int[] avatarIndices = null;
        if (room.CustomProperties.ContainsKey("AvatarIndices") && room.CustomProperties["AvatarIndices"] is int[] indices)
        {
            avatarIndices = indices;
        }

        for (int i = 0; i < nicknames.Length; i++)
        {
            GameObject nameGO = Instantiate(roomScript.playerNamePrefab, roomScript.playerNameListContainer);

            TMP_Text text = nameGO.GetComponentInChildren<TMP_Text>();
            text.text = nicknames[i];

            if (avatarIndices != null && i < avatarIndices.Length)
            {
                Transform avatarTransform = nameGO.transform.Find("PlayerObject/PlayerIcon/PlayerIconMask/AvatarImage");
                if (avatarTransform != null)
                {
                    Image avatarImage = avatarTransform.GetComponent<Image>();
                    int avatarIndex = avatarIndices[i];

                    if (availableAvatars != null && availableAvatars.Length > 0 &&
                        avatarIndex >= 0 && avatarIndex < availableAvatars.Length)
                    {
                        avatarImage.sprite = availableAvatars[avatarIndex];
                    }
                    else
                    {
                        avatarImage.sprite = roomScript.defaultAvatar;
                    }
                }
            }
        }
    }
}

    private void UpdateRoomListUI()
    {
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        StartCoroutine(CreateRoomsWithAnimation());
    }

    [PunRPC]
    private void RPC_SendNicknameAndAvatar(int actorNumber, string nickname, int avatarIndex)
    {
        if (!playerNames.ContainsKey(actorNumber))
            playerNames.Add(actorNumber, nickname);
        else
            playerNames[actorNumber] = nickname;
            
        if (!playerAvatars.ContainsKey(actorNumber))
            playerAvatars.Add(actorNumber, avatarIndex);
        else
            playerAvatars[actorNumber] = avatarIndex;

        UpdateRoomUIWithNicknamesAndAvatars();
        UpdateRoomAvatarIndicesProperty();
        
        if (PhotonNetwork.IsMasterClient && actorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            Photon.Realtime.Player newPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (newPlayer != null)
            {
                photonView.RPC("RPC_SendNicknameAndAvatar", 
                    newPlayer, 
                    PhotonNetwork.LocalPlayer.ActorNumber, 
                    PhotonNetwork.NickName,
                    playerAvatars.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber) ? 
                        playerAvatars[PhotonNetwork.LocalPlayer.ActorNumber] : 0);
                
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    if (player.ActorNumber != actorNumber && player.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        if (playerNames.ContainsKey(player.ActorNumber))
                        {
                            photonView.RPC("RPC_SendNicknameAndAvatar", 
                                newPlayer, 
                                player.ActorNumber, 
                                playerNames[player.ActorNumber],
                                playerAvatars.ContainsKey(player.ActorNumber) ? 
                                    playerAvatars[player.ActorNumber] : 0);
                        }
                    }
                }
            }
        }
    }

    private void UpdateRoomUIWithNicknamesAndAvatars()
    {
        Room currentRoom = FindRoomUIObject();
        if (currentRoom == null) return;

        List<string> nicknames = new List<string>();
        List<int> avatarIndices = new List<int>();
        
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (playerNames.ContainsKey(player.ActorNumber))
            {
                nicknames.Add(playerNames[player.ActorNumber]);
            }
            else
            {
                nicknames.Add(player.NickName);
                if (PhotonNetwork.IsConnected)
                {
                    photonView.RPC("RPC_SendNicknameAndAvatar", player, 
                        player.ActorNumber, player.NickName, 
                        playerAvatars.ContainsKey(player.ActorNumber) ? 
                            playerAvatars[player.ActorNumber] : 0);
                }
            }
            
            if (playerAvatars.ContainsKey(player.ActorNumber))
            {
                avatarIndices.Add(playerAvatars[player.ActorNumber]);
            }
            else if (player.CustomProperties.ContainsKey("AvatarIndex"))
            {
                int avatarIndex = (int)player.CustomProperties["AvatarIndex"];
                avatarIndices.Add(avatarIndex);
                
                if (!playerAvatars.ContainsKey(player.ActorNumber))
                {
                    playerAvatars.Add(player.ActorNumber, avatarIndex);
                }
            }
            else
            {
                avatarIndices.Add(0);
            }
        }

        currentRoom.UpdatePlayerNames();
    }

    public void KickPlayer(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Photon.Realtime.Player playerToKick = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (playerToKick != null)
            {
                photonView.RPC("RPC_KickPlayer", playerToKick);
            }
        }
    }

    [PunRPC]
    void RPC_KickPlayer()
    {
        PhotonNetwork.LeaveRoom();
    }
}