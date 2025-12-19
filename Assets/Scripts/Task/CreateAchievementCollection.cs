#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateAchievementCollection : MonoBehaviour
{
    [MenuItem("Tools/Create Achievement Collection")]
    public static void CreateCollection()
    {
        // Создаем коллекцию
        var collection = ScriptableObject.CreateInstance<AchievementCollection>();

        // Создаем папки при необходимости
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Achievements"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Achievements");

        // Сохраняем коллекцию
        string path = "Assets/ScriptableObjects/Achievements/MainAchievementCollection.asset";
        AssetDatabase.CreateAsset(collection, path);
        AssetDatabase.SaveAssets();

        // Выделяем файл
        Selection.activeObject = collection;
        EditorGUIUtility.PingObject(collection);

        Debug.Log($"✅ Achievement Collection создана: {path}");
    }

    [MenuItem("Tools/Create Achievement Collection with Basic Achievements")]
    public static void CreateCollectionWithBasicAchievements()
    {
        CreateCollection();

        var collection = Selection.activeObject as AchievementCollection;
        if (collection != null)
        {
            CreateBasicAchievements(collection);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log("✅ Добавлены базовые достижения!");
        }
    }

    private static void CreateBasicAchievements(AchievementCollection collection)
    {
        var templates = new[]
        {
            new { id = 1001, title = "Новичок", desc = "Ответьте правильно на 10 вопросов", type = TaskType.CorrectAnswers, target = 10, gems = 50, exp = 100, rarity = AchievementRarity.Common },
            new { id = 1002, title = "Знаток", desc = "Ответьте правильно на 100 вопросов", type = TaskType.CorrectAnswers, target = 100, gems = 200, exp = 500, rarity = AchievementRarity.Uncommon },
            new { id = 1003, title = "Эксперт", desc = "Ответьте правильно на 500 вопросов", type = TaskType.CorrectAnswers, target = 500, gems = 500, exp = 1000, rarity = AchievementRarity.Rare },
            new { id = 1004, title = "Мастер", desc = "Ответьте правильно на 1000 вопросов", type = TaskType.CorrectAnswers, target = 1000, gems = 1000, exp = 2000, rarity = AchievementRarity.Epic },
            new { id = 1005, title = "Игроман", desc = "Сыграйте 50 игр", type = TaskType.PlayGames, target = 50, gems = 150, exp = 300, rarity = AchievementRarity.Common },
            new { id = 1006, title = "Чемпион", desc = "Выиграйте 25 игр", type = TaskType.WinGames, target = 25, gems = 300, exp = 600, rarity = AchievementRarity.Uncommon },
            new { id = 1007, title = "Легенда", desc = "Выиграйте 100 игр", type = TaskType.WinGames, target = 100, gems = 2000, exp = 5000, rarity = AchievementRarity.Legendary },
            new { id = 1008, title = "Молниеносный", desc = "Ответьте быстро 20 раз", type = TaskType.AnswerInTime, target = 20, gems = 100, exp = 200, rarity = AchievementRarity.Common },
            new { id = 1009, title = "Серия успехов", desc = "Ответьте правильно 15 раз подряд", type = TaskType.ConsecutiveCorrect, target = 15, gems = 250, exp = 400, rarity = AchievementRarity.Rare },
            new { id = 1010, title = "Постоянство", desc = "Войдите в игру 7 дней подряд", type = TaskType.DailyLogin, target = 7, gems = 300, exp = 500, rarity = AchievementRarity.Uncommon }
        };

        collection.achievements = new List<AchievementData>();

        foreach (var t in templates)
        {
            AchievementData achievement = new AchievementData
            {
                achievementId = t.id,
                achievementTitle = t.title,
                achievementDescription = t.desc,
                taskType = t.type,
                targetValue = t.target,
                gemsReward = t.gems,
                experienceReward = t.exp,
                coinsReward = 0,
                rarity = t.rarity,
                isHidden = false
            };

            collection.achievements.Add(achievement);
        }
    }
}
#endif
