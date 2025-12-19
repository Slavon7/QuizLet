using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CreateAssetMenu(fileName = "AvatarSystemInstaller", menuName = "Avatar System/Install Avatar System")]
public class AvatarSystemInstaller : ScriptableObject
{
    [Header("Migration Settings")]
    [SerializeField] private bool migrateExistingAvatars = true;
    [SerializeField] private int defaultGemCost = 100;
    
    [Header("Default Avatar Names")]
    [SerializeField] private string[] avatarNames = new string[]
    {
        "Starter Avatar",
        "Cool Cat",
        "Brave Wolf",
        "Swift Eagle",
        "Mighty Bear"
    };

    [ContextMenu("Setup Avatar System")]
    public void SetupAvatarSystem()
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogError("Сначала создайте AvatarManager ScriptableObject!");
            return;
        }

        if (migrateExistingAvatars)
        {
            MigrateFromOldSystem();
        }
    }

    private void MigrateFromOldSystem()
    {
        // Находим ProfileManager в сцене для получения существующих аватаров
        ProfileManager profileManager = FindFirstObjectByType<ProfileManager>();
        if (profileManager == null)
        {
            Debug.LogWarning("ProfileManager не найден в сцене");
            return;
        }

        // Получаем все существующие аватары
        Sprite[] existingAvatars = AvatarManager.Instance.GetAllAvatars();
        
        if (existingAvatars.Length == 0)
        {
            Debug.LogWarning("Существующие аватары не найдены");
            return;
        }

        Debug.Log($"Migrating {existingAvatars.Length} existing avatars to new system...");

        // Создаем новый массив AvatarData
        AvatarData[] newAvatarData = new AvatarData[existingAvatars.Length];

        for (int i = 0; i < existingAvatars.Length; i++)
        {
            newAvatarData[i] = new AvatarData(i, existingAvatars[i]);
            
            // Настраиваем дополнительные свойства
            newAvatarData[i].name = i < avatarNames.Length ? avatarNames[i] : $"Avatar {i + 1}";
            newAvatarData[i].isDefault = i == 0; // Первый аватар - дефолтный
            newAvatarData[i].gemCost = i == 0 ? 0 : defaultGemCost;
            newAvatarData[i].isUnlocked = i == 0; // Первый аватар разблокирован
            newAvatarData[i].description = $"Avatar #{i + 1}";
            newAvatarData[i].rarity = GetRandomRarity(i);
        }

        // Сохраняем в AvatarManager (через сериализацию)
        SerializedObject avatarManagerSO = new SerializedObject(AvatarManager.Instance);
        SerializedProperty avatarsProperty = avatarManagerSO.FindProperty("avatars");
        
        avatarsProperty.arraySize = newAvatarData.Length;
        
        for (int i = 0; i < newAvatarData.Length; i++)
        {
            SerializedProperty avatarElement = avatarsProperty.GetArrayElementAtIndex(i);
            
            avatarElement.FindPropertyRelative("id").intValue = newAvatarData[i].id;
            avatarElement.FindPropertyRelative("name").stringValue = newAvatarData[i].name;
            avatarElement.FindPropertyRelative("sprite").objectReferenceValue = newAvatarData[i].sprite;
            avatarElement.FindPropertyRelative("isDefault").boolValue = newAvatarData[i].isDefault;
            avatarElement.FindPropertyRelative("gemCost").intValue = newAvatarData[i].gemCost;
            avatarElement.FindPropertyRelative("isUnlocked").boolValue = newAvatarData[i].isUnlocked;
            avatarElement.FindPropertyRelative("description").stringValue = newAvatarData[i].description;
            avatarElement.FindPropertyRelative("rarity").enumValueIndex = (int)newAvatarData[i].rarity;
        }
        
        avatarManagerSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(AvatarManager.Instance);
        
        Debug.Log("Migration completed successfully!");
    }

    private AvatarRarity GetRandomRarity(int index)
    {
        if (index == 0) return AvatarRarity.Common; // Первый всегда Common
        if (index % 4 == 0) return AvatarRarity.Legendary;
        if (index % 3 == 0) return AvatarRarity.Epic;
        if (index % 2 == 0) return AvatarRarity.Rare;
        return AvatarRarity.Common;
    }
}
#endif