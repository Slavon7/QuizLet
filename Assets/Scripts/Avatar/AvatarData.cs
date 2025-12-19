using UnityEngine;

[System.Serializable]
public class AvatarData
{
    [Header("Basic Info")]
    public int id;
    public string name;
    public Sprite sprite;
    
    [Header("Purchase Settings")]
    public bool isDefault = false; // Доступен по умолчанию (бесплатный)
    public int gemCost = 0; // Стоимость в гемах (0 = бесплатный)
    public bool isUnlocked = false; // Разблокирован ли у игрока
    
    [Header("Display Settings")]
    public string description = "";
    public AvatarRarity rarity = AvatarRarity.Common;
    
    // Конструктор для обратной совместимости
    public AvatarData(int id, Sprite sprite)
    {
        this.id = id;
        this.sprite = sprite;
        this.isDefault = true;
        this.isUnlocked = true;
        this.name = $"Avatar {id}";
    }
}

public enum AvatarRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}