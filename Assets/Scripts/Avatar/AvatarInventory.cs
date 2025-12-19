using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AvatarInventory : MonoBehaviour
{
    [Header("Inventory UI")]
    public Transform inventoryContent;
    public GameObject avatarInventoryItemPrefab;
    
    [Header("Current Selection Display")]
    public Image currentAvatarImage;
    public TextMeshProUGUI currentAvatarNameText;
    
    [Header("Navigation Buttons")]
    public Button previousButton;
    public Button nextButton;
    
    [Header("Inventory Item Prefab Components")]
    public string avatarImagePath = "PlayerIcon/PlayerIconMask/PlayerIcon_Image";
    public string nameTextPath = "";
    public string selectButtonPath = "";
    public string selectedIndicatorPath = "";
    
    private List<GameObject> inventoryItems = new List<GameObject>();
    private List<int> unlockedAvatarIndices = new List<int>();

    private void Start()
    {
        SetupNavigation();
        RefreshInventory();
    }

    private void SetupNavigation()
    {
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(() => {
                if (ProfileManager.Instance != null)
                {
                    ProfileManager.Instance.PreviousUnlockedAvatar();
                    RefreshSelection();
                }
            });
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => {
                if (ProfileManager.Instance != null)
                {
                    ProfileManager.Instance.NextUnlockedAvatar();
                    RefreshSelection();
                }
            });
        }
    }

    public void RefreshInventory()
    {
        ClearInventory();
        LoadUnlockedAvatars();
        CreateInventoryItems();
        RefreshSelection();
    }

    private void ClearInventory()
    {
        foreach (var item in inventoryItems)
        {
            if (item != null) Destroy(item);
        }
        inventoryItems.Clear();
    }

    private void LoadUnlockedAvatars()
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("AvatarManager недоступен!");
            return;
        }

        unlockedAvatarIndices = AvatarManager.Instance.GetUnlockedAvatarIndices();
    }

    private void CreateInventoryItems()
    {
        foreach (int avatarIndex in unlockedAvatarIndices)
        {
            var avatarData = AvatarManager.Instance.GetAvatarData(avatarIndex);
            if (avatarData != null)
            {
                CreateInventoryItem(avatarData, avatarIndex);
            }
        }
    }

    private void CreateInventoryItem(AvatarData avatarData, int avatarIndex)
    {
        GameObject inventoryItem = Instantiate(avatarInventoryItemPrefab, inventoryContent);
        inventoryItems.Add(inventoryItem);

        SetupInventoryItemComponents(inventoryItem, avatarData, avatarIndex);
    }

    private void SetupInventoryItemComponents(GameObject inventoryItem, AvatarData avatarData, int avatarIndex)
    {
        // Аватар
        Image avatarImage = FindComponentByPath<Image>(inventoryItem, avatarImagePath);
        if (avatarImage != null)
        {
            avatarImage.sprite = avatarData.sprite;
        }

        // Имя
        TextMeshProUGUI nameText = FindComponentByPath<TextMeshProUGUI>(inventoryItem, nameTextPath);
        if (nameText != null)
        {
            nameText.text = avatarData.name;
        }

        // Кнопка выбора
        Button selectButton = FindComponentByPath<Button>(inventoryItem, selectButtonPath);
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => SelectAvatar(avatarIndex));
        }

        // Индикатор выбора (обновится в RefreshSelection)
    }

    public void SelectAvatar(int avatarIndex)
    {
        if (ProfileManager.Instance != null)
        {
            if (ProfileManager.Instance.TryChangeAvatar(avatarIndex))
            {
                RefreshSelection();
                Debug.Log($"Выбран аватар: {avatarIndex}");
            }
        }
    }

    private void RefreshSelection()
    {
        if (ProfileManager.Instance == null) return;

        int currentAvatarIndex = ProfileManager.Instance.GetCurrentAvatarIndex();
        
        // Обновляем отображение текущего аватара
        UpdateCurrentAvatarDisplay(currentAvatarIndex);
        
        // Обновляем индикаторы выбора в инвентаре
        UpdateSelectionIndicators(currentAvatarIndex);
        
        // Обновляем кнопки навигации
        UpdateNavigationButtons();
    }

    private void UpdateCurrentAvatarDisplay(int avatarIndex)
    {
        var avatarData = AvatarManager.Instance?.GetAvatarData(avatarIndex);
        
        if (currentAvatarImage != null && avatarData != null)
        {
            currentAvatarImage.sprite = avatarData.sprite;
        }
        
        if (currentAvatarNameText != null && avatarData != null)
        {
            currentAvatarNameText.text = avatarData.name;
        }
    }

    private void UpdateSelectionIndicators(int selectedAvatarIndex)
    {
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            GameObject item = inventoryItems[i];
            if (item == null) continue;

            GameObject selectedIndicator = FindGameObjectByPath(item, selectedIndicatorPath);
            if (selectedIndicator != null)
            {
                // Проверяем, соответствует ли этот элемент выбранному аватару
                int itemAvatarIndex = unlockedAvatarIndices[i];
                selectedIndicator.SetActive(itemAvatarIndex == selectedAvatarIndex);
            }
        }
    }

    private void UpdateNavigationButtons()
    {
        bool hasMultipleAvatars = unlockedAvatarIndices.Count > 1;
        
        if (previousButton != null)
        {
            previousButton.interactable = hasMultipleAvatars;
        }
        
        if (nextButton != null)
        {
            nextButton.interactable = hasMultipleAvatars;
        }
    }

    // Утилитарные методы
    private T FindComponentByPath<T>(GameObject parent, string path) where T : Component
    {
        Transform target = parent.transform.Find(path);
        return target?.GetComponent<T>();
    }

    private GameObject FindGameObjectByPath(GameObject parent, string path)
    {
        Transform target = parent.transform.Find(path);
        return target?.gameObject;
    }

    // Публичные методы для внешнего использования
    public void OnEnable()
    {
        RefreshInventory();
    }

    public int GetUnlockedAvatarCount()
    {
        return unlockedAvatarIndices.Count;
    }

    public bool HasMultipleAvatars()
    {
        return unlockedAvatarIndices.Count > 1;
    }
}