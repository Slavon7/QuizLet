using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "AvatarManager", menuName = "Avatar System/Avatar Manager")]
public class AvatarManager : ScriptableObject
{
    [Header("Avatar Collection")]
    [SerializeField] private AvatarData[] avatars;
    
    private static AvatarManager _instance;
    public static AvatarManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<AvatarManager>("AvatarManager");
                if (_instance == null)
                {
                    Debug.LogError("AvatarManager not found in Resources folder!");
                }
                else
                {
                    _instance.InitializeDefaults();
                }
            }
            return _instance;
        }
    }

    private void InitializeDefaults()
    {
        // Устанавливаем первый аватар как разблокированный по умолчанию
        if (avatars != null && avatars.Length > 0 && avatars[0] != null)
        {
            avatars[0].isDefault = true;
            avatars[0].isUnlocked = true;
        }
    }

    #region Backward Compatibility Methods (Не трогаем!)
    
    /// <summary>
    /// Получить аватар по индексу (обратная совместимость)
    /// </summary>
    public Sprite GetAvatar(int index)
    {
        if (avatars == null || index < 0 || index >= avatars.Length)
        {
            Debug.LogWarning($"Avatar index {index} out of range!");
            return avatars?[0]?.sprite;
        }
        
        return avatars[index]?.sprite;
    }
    
    /// <summary>
    /// Получить все аватары как массив спрайтов (обратная совместимость)
    /// </summary>
    public Sprite[] GetAllAvatars()
    {
        if (avatars == null) return new Sprite[0];
        
        return avatars.Select(avatar => avatar?.sprite).ToArray();
    }
    
    /// <summary>
    /// Получить количество аватаров (обратная совместимость)
    /// </summary>
    public int GetAvatarCount()
    {
        return avatars?.Length ?? 0;
    }
    
    #endregion

    #region New Avatar System Methods
    
    /// <summary>
    /// Получить данные аватара по индексу
    /// </summary>
    public AvatarData GetAvatarData(int index)
    {
        if (avatars == null || index < 0 || index >= avatars.Length)
        {
            Debug.LogWarning($"Avatar index {index} out of range!");
            return null;
        }
        
        return avatars[index];
    }
    
    /// <summary>
    /// Получить все данные аватаров
    /// </summary>
    public AvatarData[] GetAllAvatarData()
    {
        return avatars ?? new AvatarData[0];
    }
    
    /// <summary>
    /// Получить только разблокированные аватары
    /// </summary>
    public List<AvatarData> GetUnlockedAvatars()
    {
        if (avatars == null) return new List<AvatarData>();
        
        List<AvatarData> unlockedAvatars = new List<AvatarData>();
        
        foreach (var avatar in avatars)
        {
            if (avatar != null && IsAvatarUnlocked(avatar.id))
            {
                unlockedAvatars.Add(avatar);
            }
        }
        
        return unlockedAvatars;
    }
    
    /// <summary>
    /// Получить только разблокированные аватары как индексы
    /// </summary>
    public List<int> GetUnlockedAvatarIndices()
    {
        List<int> unlockedIndices = new List<int>();
        
        if (avatars == null) return unlockedIndices;
        
        for (int i = 0; i < avatars.Length; i++)
        {
            if (avatars[i] != null && IsAvatarUnlocked(i))
            {
                unlockedIndices.Add(i);
            }
        }
        
        return unlockedIndices;
    }
    
    /// <summary>
    /// Проверить, разблокирован ли аватар у игрока
    /// </summary>
    public bool IsAvatarUnlocked(int avatarIndex)
    {
        if (avatars == null || avatarIndex < 0 || avatarIndex >= avatars.Length)
            return false;
            
        var avatarData = avatars[avatarIndex];
        if (avatarData == null) return false;
        
        // Если аватар по умолчанию - он всегда разблокирован
        if (avatarData.isDefault) return true;
        
        // Проверяем в PlayerPrefs
        string key = $"Avatar_Unlocked_{avatarIndex}";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }
    
    /// <summary>
    /// Разблокировать аватар для игрока
    /// </summary>
    public void UnlockAvatar(int avatarIndex)
    {
        if (avatars == null || avatarIndex < 0 || avatarIndex >= avatars.Length)
        {
            Debug.LogWarning($"Cannot unlock avatar: invalid index {avatarIndex}");
            return;
        }
        
        string key = $"Avatar_Unlocked_{avatarIndex}";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        
        Debug.Log($"Avatar {avatarIndex} unlocked!");
    }
    
    /// <summary>
    /// Проверить, можно ли купить аватар
    /// </summary>
    public bool CanPurchaseAvatar(int avatarIndex)
    {
        if (IsAvatarUnlocked(avatarIndex)) return false;
        
        var avatarData = GetAvatarData(avatarIndex);
        if (avatarData == null) return false;
        
        // Проверяем, хватает ли гемов
        if (CurrencyManager.Instance != null)
        {
            return CurrencyManager.Instance.GetGems() >= avatarData.gemCost;
        }
        
        return false;
    }
    
    /// <summary>
    /// Купить аватар
    /// </summary>
    public bool PurchaseAvatar(int avatarIndex)
    {
        Debug.Log($"Попытка купить аватар {avatarIndex}");
        
        // Проверяем CurrencyManager
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager.Instance равен null!");
            return false;
        }
        
        if (!CanPurchaseAvatar(avatarIndex)) 
        {
            Debug.LogWarning($"Нельзя купить аватар {avatarIndex}");
            return false;
        }
        
        var avatarData = GetAvatarData(avatarIndex);
        if (avatarData == null) 
        {
            Debug.LogError($"AvatarData для индекса {avatarIndex} равна null!");
            return false;
        }
        
        Debug.Log($"Покупаем аватар '{avatarData.name}' за {avatarData.gemCost} гемов");
        
        // Пытаемся списать гемы
        try
        {
            if (CurrencyManager.Instance.SpendGems(avatarData.gemCost, $"Покупка аватара: {avatarData.name}"))
            {
                UnlockAvatar(avatarIndex);
                Debug.Log($"Аватар {avatarIndex} успешно куплен!");
                return true;
            }
            else
            {
                Debug.LogWarning($"Не удалось списать {avatarData.gemCost} гемов");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка при покупке аватара: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Получить первый разблокированный аватар (для установки по умолчанию)
    /// </summary>
    public int GetFirstUnlockedAvatarIndex()
    {
        if (avatars == null) return 0;
        
        for (int i = 0; i < avatars.Length; i++)
        {
            if (IsAvatarUnlocked(i))
            {
                return i;
            }
        }
        
        return 0; // Fallback
    }
    
    /// <summary>
    /// Проверить, валиден ли индекс аватара и разблокирован ли он
    /// </summary>
    public bool IsValidUnlockedAvatar(int avatarIndex)
    {
        return avatarIndex >= 0 && 
               avatarIndex < GetAvatarCount() && 
               IsAvatarUnlocked(avatarIndex);
    }
    
    #endregion
    
    #region Debug/Development Methods
    
    /// <summary>
    /// [Только для разработки] Разблокировать все аватары
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void UnlockAllAvatars()
    {
        if (avatars == null) return;
        
        for (int i = 0; i < avatars.Length; i++)
        {
            UnlockAvatar(i);
        }
        
        Debug.Log("All avatars unlocked!");
    }
    
    /// <summary>
    /// [Только для разработки] Сбросить все разблокированные аватары
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ResetAllAvatars()
    {
        if (avatars == null) return;
        
        for (int i = 0; i < avatars.Length; i++)
        {
            string key = $"Avatar_Unlocked_{i}";
            PlayerPrefs.DeleteKey(key);
        }
        
        PlayerPrefs.Save();
        Debug.Log("All avatar unlocks reset!");
    }
    
    #endregion
}