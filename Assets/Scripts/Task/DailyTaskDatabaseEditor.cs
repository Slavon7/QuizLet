#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(DailyTaskDatabase))]
public class DailyTaskDatabaseEditor : Editor
{
    private DailyTaskDatabase database;
    private bool showTaskTemplates = true;
    private bool showSeasonalEvents = true;
    private bool showPreview = false;
    
    private void OnEnable()
    {
        database = (DailyTaskDatabase)target;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Daily Task Database", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Основные настройки
        DrawMainSettings();
        
        EditorGUILayout.Space();
        
        // Кнопки управления
        DrawControlButtons();
        
        EditorGUILayout.Space();
        
        // Шаблоны тасков
        DrawTaskTemplates();
        
        EditorGUILayout.Space();
        
        // Сезонные события
        DrawSeasonalEvents();
        
        EditorGUILayout.Space();
        
        // Превью
        DrawPreview();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawMainSettings()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        
        SerializedProperty maxDailyTasks = serializedObject.FindProperty("maxDailyTasks");
        SerializedProperty randomizeDailyTasks = serializedObject.FindProperty("randomizeDailyTasks");
        SerializedProperty baseTaskId = serializedObject.FindProperty("baseTaskId");
        
        EditorGUILayout.PropertyField(maxDailyTasks, new GUIContent("Max Daily Tasks"));
        EditorGUILayout.PropertyField(randomizeDailyTasks, new GUIContent("Randomize Daily Tasks"));
        EditorGUILayout.PropertyField(baseTaskId, new GUIContent("Base Task ID"));
        
        if (randomizeDailyTasks.boolValue)
        {
            EditorGUILayout.HelpBox("Ежедневные таски будут выбираться случайно, но консистентно для каждого дня", MessageType.Info);
        }
    }
    
    private void DrawControlButtons()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Validate Database"))
        {
            bool isValid = database.ValidateDatabase();
            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation", "База данных корректна!", "OK");
            }
        }
        
        if (GUILayout.Button("Add Template"))
        {
            AddNewTemplate();
        }
        
        if (GUILayout.Button("Create Example Tasks"))
        {
            CreateExampleTasks();
        }
        
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Clear All Templates"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Удалить все шаблоны тасков?", "Да", "Нет"))
            {
                ClearAllTemplates();
            }
        }
        
        if (GUILayout.Button("Export to JSON"))
        {
            ExportToJSON();
        }
        
        if (GUILayout.Button("Import from JSON"))
        {
            ImportFromJSON();
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawTaskTemplates()
    {
        SerializedProperty taskTemplates = serializedObject.FindProperty("taskTemplates");
        
        showTaskTemplates = EditorGUILayout.Foldout(showTaskTemplates, 
            $"Task Templates ({taskTemplates.arraySize})", true);
        
        if (showTaskTemplates)
        {
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < taskTemplates.arraySize; i++)
            {
                SerializedProperty template = taskTemplates.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginVertical("box");
                
                // Заголовок шаблона
                SerializedProperty taskTitle = template.FindPropertyRelative("taskTitle");
                SerializedProperty isEnabled = template.FindPropertyRelative("isEnabled");
                
                GUILayout.BeginHorizontal();
                
                string headerText = string.IsNullOrEmpty(taskTitle.stringValue) ? 
                    $"Template {i + 1}" : taskTitle.stringValue;
                
                EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);
                
                GUI.enabled = isEnabled.boolValue;
                EditorGUILayout.PropertyField(isEnabled, GUIContent.none, GUILayout.Width(20));
                GUI.enabled = true;
                
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", $"Удалить шаблон '{headerText}'?", "Да", "Нет"))
                    {
                        taskTemplates.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                }
                
                GUILayout.EndHorizontal();
                
                if (!isEnabled.boolValue)
                {
                    GUI.color = Color.gray;
                }
                
                // Основная информация
                EditorGUILayout.PropertyField(template.FindPropertyRelative("taskTitle"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("taskDescription"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("taskType"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("targetValue"));
                
                EditorGUILayout.Space();
                
                // Награды
                EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(template.FindPropertyRelative("rewardGems"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("rewardExperience"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("rewardCoins"));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space();
                
                // Дополнительные настройки
                EditorGUILayout.PropertyField(template.FindPropertyRelative("priority"));
                
                SerializedProperty isSeasonalTask = template.FindPropertyRelative("isSeasonalTask");
                EditorGUILayout.PropertyField(isSeasonalTask);
                
                if (isSeasonalTask.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(template.FindPropertyRelative("seasonalEventName"));
                    EditorGUI.indentLevel--;
                }
                
                GUI.color = Color.white;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUI.indentLevel--;
            
            if (GUILayout.Button("Add New Template"))
            {
                AddNewTemplate();
            }
        }
    }
    
    private void DrawSeasonalEvents()
    {
        SerializedProperty seasonalEvents = serializedObject.FindProperty("seasonalEvents");
        
        showSeasonalEvents = EditorGUILayout.Foldout(showSeasonalEvents, 
            $"Seasonal Events ({seasonalEvents.arraySize})", true);
        
        if (showSeasonalEvents)
        {
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < seasonalEvents.arraySize; i++)
            {
                SerializedProperty seasonalEvent = seasonalEvents.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginVertical("box");
                
                // Заголовок события
                SerializedProperty eventName = seasonalEvent.FindPropertyRelative("eventName");
                SerializedProperty isEnabled = seasonalEvent.FindPropertyRelative("isEnabled");
                
                GUILayout.BeginHorizontal();
                
                string headerText = string.IsNullOrEmpty(eventName.stringValue) ? 
                    $"Event {i + 1}" : eventName.stringValue;
                
                EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);
                
                EditorGUILayout.PropertyField(isEnabled, GUIContent.none, GUILayout.Width(20));
                
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", $"Удалить событие '{headerText}'?", "Да", "Нет"))
                    {
                        seasonalEvents.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                }
                
                GUILayout.EndHorizontal();
                
                // Информация о событии
                EditorGUILayout.PropertyField(eventName);
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("eventDescription"));
                
                EditorGUILayout.Space();
                
                // Даты
                EditorGUILayout.LabelField("Date Range", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("startMonth"), GUILayout.Width(60));
                EditorGUILayout.LabelField("/", GUILayout.Width(10));
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("startDay"), GUILayout.Width(60));
                EditorGUILayout.LabelField("-", GUILayout.Width(10));
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("endMonth"), GUILayout.Width(60));
                EditorGUILayout.LabelField("/", GUILayout.Width(10));
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("endDay"), GUILayout.Width(60));
                GUILayout.EndHorizontal();
                
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("isYearly"));
                
                EditorGUILayout.Space();
                
                // Шаблон таска
                EditorGUILayout.PropertyField(seasonalEvent.FindPropertyRelative("taskTemplate"));
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUI.indentLevel--;
            
            if (GUILayout.Button("Add New Seasonal Event"))
            {
                AddNewSeasonalEvent();
            }
        }
    }
    
    private void DrawPreview()
    {
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview Generated Tasks", true);
        
        if (showPreview)
        {
            EditorGUI.indentLevel++;
            
            if (GUILayout.Button("Generate Preview"))
            {
                var generatedTasks = database.GenerateDailyTasks();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Generated {generatedTasks.Count} tasks:", EditorStyles.boldLabel);
                
                foreach (var task in generatedTasks)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"{task.taskTitle}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Description: {task.taskDescription}");
                    EditorGUILayout.LabelField($"Type: {task.taskType}");
                    EditorGUILayout.LabelField($"Target: {task.targetValue}");
                    EditorGUILayout.LabelField($"Reward: 💎{task.reward.gems} ⭐{task.reward.experience}");
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUI.indentLevel--;
        }
    }
    
    private void AddNewTemplate()
    {
        SerializedProperty taskTemplates = serializedObject.FindProperty("taskTemplates");
        taskTemplates.arraySize++;
        
        SerializedProperty newTemplate = taskTemplates.GetArrayElementAtIndex(taskTemplates.arraySize - 1);
        
        // Устанавливаем значения по умолчанию
        newTemplate.FindPropertyRelative("taskTitle").stringValue = "New Task";
        newTemplate.FindPropertyRelative("taskDescription").stringValue = "Task description";
        newTemplate.FindPropertyRelative("taskType").enumValueIndex = 0;
        newTemplate.FindPropertyRelative("targetValue").intValue = 1;
        newTemplate.FindPropertyRelative("rewardGems").intValue = 25;
        newTemplate.FindPropertyRelative("rewardExperience").intValue = 25;
        newTemplate.FindPropertyRelative("rewardCoins").intValue = 0;
        newTemplate.FindPropertyRelative("isEnabled").boolValue = true;
        newTemplate.FindPropertyRelative("priority").intValue = 0;
        newTemplate.FindPropertyRelative("isSeasonalTask").boolValue = false;
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void AddNewSeasonalEvent()
    {
        SerializedProperty seasonalEvents = serializedObject.FindProperty("seasonalEvents");
        seasonalEvents.arraySize++;
        
        SerializedProperty newEvent = seasonalEvents.GetArrayElementAtIndex(seasonalEvents.arraySize - 1);
        
        // Устанавливаем значения по умолчанию
        newEvent.FindPropertyRelative("eventName").stringValue = "New Event";
        newEvent.FindPropertyRelative("eventDescription").stringValue = "Event description";
        newEvent.FindPropertyRelative("startMonth").intValue = 1;
        newEvent.FindPropertyRelative("startDay").intValue = 1;
        newEvent.FindPropertyRelative("endMonth").intValue = 1;
        newEvent.FindPropertyRelative("endDay").intValue = 31;
        newEvent.FindPropertyRelative("isEnabled").boolValue = true;
        newEvent.FindPropertyRelative("isYearly").boolValue = true;
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void CreateExampleTasks()
    {
        SerializedProperty taskTemplates = serializedObject.FindProperty("taskTemplates");
        
        if (taskTemplates.arraySize > 0)
        {
            if (!EditorUtility.DisplayDialog("Confirm", "Заменить существующие шаблоны примерами?", "Да", "Нет"))
            {
                return;
            }
        }
        
        taskTemplates.arraySize = 0;
        
        // Создаем примеры тасков
        var examples = new[]
        {
            new { title = "Щоденний вхід", desc = "Увійди в гру сьогодні", type = 4, target = 1, gems = 25, exp = 25 },
            new { title = "Розумник дня", desc = "Дай 15 правильних відповідей", type = 0, target = 15, gems = 50, exp = 50 },
            new { title = "Активний гравець", desc = "Зіграй 5 ігор", type = 1, target = 5, gems = 75, exp = 75 },
            new { title = "Швидкість блискавки", desc = "Дай 5 швидких відповідей", type = 3, target = 5, gems = 100, exp = 100 },
            new { title = "Чемпіон дня", desc = "Вигради 3 гри", type = 2, target = 3, gems = 125, exp = 125 }
        };
        
        foreach (var example in examples)
        {
            taskTemplates.arraySize++;
            SerializedProperty template = taskTemplates.GetArrayElementAtIndex(taskTemplates.arraySize - 1);
            
            template.FindPropertyRelative("taskTitle").stringValue = example.title;
            template.FindPropertyRelative("taskDescription").stringValue = example.desc;
            template.FindPropertyRelative("taskType").enumValueIndex = example.type;
            template.FindPropertyRelative("targetValue").intValue = example.target;
            template.FindPropertyRelative("rewardGems").intValue = example.gems;
            template.FindPropertyRelative("rewardExperience").intValue = example.exp;
            template.FindPropertyRelative("rewardCoins").intValue = 0;
            template.FindPropertyRelative("isEnabled").boolValue = true;
            template.FindPropertyRelative("priority").intValue = 0;
            template.FindPropertyRelative("isSeasonalTask").boolValue = false;
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.DisplayDialog("Success", "Примеры тасков созданы!", "OK");
    }
    
    private void ClearAllTemplates()
    {
        SerializedProperty taskTemplates = serializedObject.FindProperty("taskTemplates");
        taskTemplates.arraySize = 0;
        serializedObject.ApplyModifiedProperties();
    }
    
    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel("Export Daily Tasks", "", "daily_tasks", "json");
        if (!string.IsNullOrEmpty(path))
        {
            var data = new DailyTaskExportData();
            data.templates = database.GetAllTemplates();
            
            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);
            
            EditorUtility.DisplayDialog("Success", $"Экспорт завершен: {path}", "OK");
        }
    }
    
    private void ImportFromJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import Daily Tasks", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                var data = JsonUtility.FromJson<DailyTaskExportData>(json);
                
                if (data.templates != null && data.templates.Count > 0)
                {
                    SerializedProperty taskTemplates = serializedObject.FindProperty("taskTemplates");
                    taskTemplates.arraySize = 0;
                    
                    foreach (var template in data.templates)
                    {
                        taskTemplates.arraySize++;
                        SerializedProperty newTemplate = taskTemplates.GetArrayElementAtIndex(taskTemplates.arraySize - 1);
                        
                        newTemplate.FindPropertyRelative("taskTitle").stringValue = template.taskTitle;
                        newTemplate.FindPropertyRelative("taskDescription").stringValue = template.taskDescription;
                        newTemplate.FindPropertyRelative("taskType").enumValueIndex = (int)template.taskType;
                        newTemplate.FindPropertyRelative("targetValue").intValue = template.targetValue;
                        newTemplate.FindPropertyRelative("rewardGems").intValue = template.rewardGems;
                        newTemplate.FindPropertyRelative("rewardExperience").intValue = template.rewardExperience;
                        newTemplate.FindPropertyRelative("rewardCoins").intValue = template.rewardCoins;
                        newTemplate.FindPropertyRelative("isEnabled").boolValue = template.isEnabled;
                        newTemplate.FindPropertyRelative("priority").intValue = template.priority;
                        newTemplate.FindPropertyRelative("isSeasonalTask").boolValue = template.isSeasonalTask;
                        newTemplate.FindPropertyRelative("seasonalEventName").stringValue = template.seasonalEventName;
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.DisplayDialog("Success", $"Импорт завершен: {data.templates.Count} шаблонов", "OK");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Ошибка импорта: {e.Message}", "OK");
            }
        }
    }
}

[System.Serializable]
public class DailyTaskExportData
{
    public List<DailyTaskTemplate> templates = new List<DailyTaskTemplate>();
}
#endif