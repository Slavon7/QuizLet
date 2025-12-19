using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class Room : MonoBehaviour
{
    public TMP_Text Name;
    public TMP_Text PlayerCount;
    public Transform playerNameListContainer;
    public GameObject playerNamePrefab;
    public Sprite defaultAvatar;
    public GameObject kickButtonPrefab;
    public Sprite[] availableAvatars;
    
    private Button joinButton;

    private void Start()
    {
        // Кэшируем кнопку входа
        joinButton = GetComponent<Button>();

        // Попробуем получить аватары из ProfileManager, если они не назначены вручную
        if (availableAvatars == null || availableAvatars.Length == 0)
        {
            ProfileManager profileManager = FindAnyObjectByType<ProfileManager>();
            if (AvatarManager.Instance != null)
            {
                availableAvatars = AvatarManager.Instance.GetAllAvatars();
                Debug.Log("Room: Аватары загружены из AvatarManager: " + availableAvatars.Length);
            }
            else
            {
                Debug.LogWarning("Room: Не удалось найти AvatarManager!");
            }
        }
        InitializeAvatars();
    }

    private void InitializeAvatars()
    {
        if (availableAvatars == null || availableAvatars.Length == 0)
        {
            if (AvatarManager.Instance != null)
            {
                availableAvatars = AvatarManager.Instance.GetAllAvatars();
                Debug.Log("Room: Аватары загружены из AvatarManager: " + availableAvatars.Length);
            }
            else
            {
                Debug.LogWarning("Room: Не удалось найти AvatarManager!");
                // Попробуем найти через некоторое время
                Invoke(nameof(RetryInitializeAvatars), 0.5f);
            }
        }
    }

    private void RetryInitializeAvatars()
    {
        if ((availableAvatars == null || availableAvatars.Length == 0) && AvatarManager.Instance != null)
        {
            availableAvatars = AvatarManager.Instance.GetAllAvatars();
            Debug.Log("Room: Аватары загружены из AvatarManager (повторная попытка): " + availableAvatars.Length);
        }
    }

    public void JoinRoom()
    {
        CreateAndJoin createAndJoin = Object.FindAnyObjectByType<CreateAndJoin>();
        if (createAndJoin != null)
        {
            createAndJoin.JoinRoomInList(Name.text);
        }
        else
        {
            Debug.LogError("Room: Не найден компонент CreateAndJoin!");
        }
    }

    public void UpdatePlayerCount(int current, int max)
    {
        if (PlayerCount != null)
        {
            PlayerCount.text = $"{current}/{max}";
        }
    }

    public void DisableJoinButton()
    {
        if (joinButton == null)
        {
            joinButton = GetComponent<Button>();
        }

        if (AvatarManager.Instance != null)
        {
            availableAvatars = AvatarManager.Instance.GetAllAvatars();
            Debug.Log("Room: Принудительно загружены аватары из AvatarManager: " + availableAvatars.Length);
        }
        else
        {
            Debug.LogWarning("Room: AvatarManager недоступен!");
        }
        
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners(); // Удаляем все обработчики
            joinButton.interactable = false; // Делаем неактивной
        }
        else
        {
            Debug.LogWarning("Room: Не удалось найти кнопку входа для отключения!");
        }
    }

    public void UpdatePlayerNames()
    {
        if (playerNameListContainer == null)
        {
            Debug.LogError("Room: playerNameListContainer не назначен!");
            return;
        }

        // Удаляем все существующие элементы
        foreach (Transform child in playerNameListContainer)
        {
            Destroy(child.gameObject);
        }

        // Проверяем, находимся ли мы в комнате
        if (PhotonNetwork.InRoom)
        {
            // Проходим по всем игрокам в комнате
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (playerNamePrefab == null)
                {
                    Debug.LogError("Room: playerNamePrefab не назначен!");
                    continue;
                }

                GameObject playerNameGO = Instantiate(playerNamePrefab, playerNameListContainer);

                // Устанавливаем никнейм
                TMP_Text playerText = playerNameGO.GetComponentInChildren<TMP_Text>();
                if (playerText != null)
                {
                    playerText.text = player.NickName;
                }
                else
                {
                    Debug.LogError("Room: Не найден компонент TMP_Text в playerNamePrefab!");
                }

                // Устанавливаем аватар
                Transform avatarTransform = playerNameGO.transform.Find("PlayerObject/PlayerIcon/PlayerIconMask/AvatarImage");
                if (avatarTransform != null)
                {
                    Image avatarImage = avatarTransform.GetComponent<Image>();
                    if (avatarImage != null)
                    {
                        // Проверяем наличие индекса аватара в кастомных свойствах
                        int avatarIndex = 0;
                        if (player.CustomProperties.TryGetValue("AvatarIndex", out object avatarObj))
                        {
                            if (avatarObj is int index)
                            {
                                avatarIndex = index;
                                Debug.Log($"Player {player.NickName} has avatar index: {avatarIndex}");
                            }
                        }
                        
                        // Устанавливаем аватар на основе индекса
                        if (availableAvatars != null && availableAvatars.Length > 0 && 
                            avatarIndex >= 0 && avatarIndex < availableAvatars.Length)
                        {
                            avatarImage.sprite = availableAvatars[avatarIndex];
                            Debug.Log($"Установлен аватар {avatarIndex} для игрока {player.NickName}");
                        }
                        else
                        {
                            avatarImage.sprite = defaultAvatar;
                            Debug.Log($"Используется дефолтный аватар для игрока {player.NickName}, индекс {avatarIndex} некорректен");
                        }
                    }
                    else
                    {
                        Debug.LogError("Room: Не найден компонент Image для аватара!");
                    }
                }
                else
                {
                    Debug.LogError("Room: Не найден путь PlayerObject/PlayerIcon/PlayerIconMask/AvatarImage в префабе игрока!");
                }

                // Кнопка кика
                if (PhotonNetwork.IsMasterClient && player != PhotonNetwork.LocalPlayer && kickButtonPrefab != null)
                {
                    GameObject kickBtnGO = Instantiate(kickButtonPrefab, playerNameGO.transform);
                    Button kickButton = kickBtnGO.GetComponent<Button>();
                    if (kickButton != null)
                    {
                        int actorNumber = player.ActorNumber;

                        kickButton.onClick.AddListener(() =>
                        {
                            CreateAndJoin createAndJoin = Object.FindAnyObjectByType<CreateAndJoin>();
                            if (createAndJoin != null)
                            {
                                createAndJoin.KickPlayer(actorNumber);
                            }
                        });
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Room: UpdatePlayerNames вызван, когда не в комнате!");
        }
    }
}