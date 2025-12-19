// QuestionDatabaseImporter.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class QuestionDatabaseImporter : EditorWindow
{
    private string jsonFilePath = "";
    private QuestionDatabase targetDatabase;
    private bool appendQuestions = true;
    private Vector2 scrollPosition;
    private string previewText = "";
    private bool showPreview = false;

    [MenuItem("Tools/Quiz/Question Importer")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(QuestionDatabaseImporter), false, "Question Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Import Questions from JSON", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // File selection
        EditorGUILayout.BeginHorizontal();
        jsonFilePath = EditorGUILayout.TextField("JSON File Path:", jsonFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select JSON file", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                jsonFilePath = path;
                LoadPreview();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Target database selection
        targetDatabase = (QuestionDatabase)EditorGUILayout.ObjectField(
            "Target Database:", 
            targetDatabase, 
            typeof(QuestionDatabase), 
            false);

        // Import options
        appendQuestions = EditorGUILayout.Toggle("Append Questions", appendQuestions);

        // Preview section
        EditorGUILayout.Space();
        showPreview = EditorGUILayout.Foldout(showPreview, "JSON Preview");
        if (showPreview && !string.IsNullOrEmpty(previewText))
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(previewText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // Import button
        EditorGUILayout.Space();
        GUI.enabled = !string.IsNullOrEmpty(jsonFilePath) && targetDatabase != null;
        
        if (GUILayout.Button("Import Questions"))
        {
            ImportQuestions();
        }
        
        GUI.enabled = true;
    }

    private void LoadPreview()
    {
        if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath))
        {
            previewText = "Invalid file path";
            return;
        }

        try
        {
            previewText = File.ReadAllText(jsonFilePath);
            if (previewText.Length > 2000)
            {
                previewText = previewText.Substring(0, 2000) + "... [текст обрезан]";
            }
        }
        catch (System.Exception e)
        {
            previewText = "Error loading file: " + e.Message;
        }
    }

    private void ImportQuestions()
    {
        if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath) || targetDatabase == null)
        {
            EditorUtility.DisplayDialog("Import Error", "Please select a valid JSON file and target database.", "OK");
            return;
        }

        try
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(jsonFilePath);
            
            // Parse JSON into question data
            QuestionDataList dataList = JsonUtility.FromJson<QuestionDataList>(jsonContent);
            
            if (dataList == null || dataList.questions == null || dataList.questions.Count == 0)
            {
                EditorUtility.DisplayDialog("Import Error", 
                    "Failed to parse questions from JSON. Make sure the format is correct.", "OK");
                return;
            }

            // Create serialized object to modify the target
            SerializedObject serializedObject = new SerializedObject(targetDatabase);
            SerializedProperty questionsProperty = serializedObject.FindProperty("questions");

            // Clear existing questions if not appending
            if (!appendQuestions)
            {
                questionsProperty.ClearArray();
            }

            // Start size of the array
            int startSize = questionsProperty.arraySize;
            
            // Add new questions
            foreach (QuestionData questionData in dataList.questions)
            {
                questionsProperty.arraySize++;
                SerializedProperty newElement = questionsProperty.GetArrayElementAtIndex(questionsProperty.arraySize - 1);
                
                SerializedProperty questionProp = newElement.FindPropertyRelative("question");
                SerializedProperty optionsProp = newElement.FindPropertyRelative("options");
                SerializedProperty correctAnswerProp = newElement.FindPropertyRelative("correctAnswer");
                SerializedProperty isNumericalProp = newElement.FindPropertyRelative("isNumerical");
                
                questionProp.stringValue = questionData.question;
                correctAnswerProp.stringValue = questionData.correctAnswer;
                isNumericalProp.boolValue = questionData.isNumerical;
                
                // Set options array
                int optionsCount = questionData.options?.Length ?? 0;
                optionsProp.ClearArray();
                optionsProp.arraySize = optionsCount;
                
                for (int i = 0; i < optionsCount; i++)
                {
                    SerializedProperty optionElement = optionsProp.GetArrayElementAtIndex(i);
                    optionElement.stringValue = questionData.options[i];
                }
            }
            
            // Apply changes
            serializedObject.ApplyModifiedProperties();
            
            // Mark the database as dirty to ensure Unity saves it
            EditorUtility.SetDirty(targetDatabase);

            // Display success message
            int importedCount = dataList.questions.Count;
            EditorUtility.DisplayDialog("Import Successful", 
                $"Successfully imported {importedCount} questions. Database now has {startSize + importedCount} questions.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error importing questions: " + e.Message);
            EditorUtility.DisplayDialog("Import Error", 
                "Failed to import questions: " + e.Message, "OK");
        }
    }

    // Helper class for JSON deserialization
    [System.Serializable]
    private class QuestionDataList
    {
        public List<QuestionData> questions = new List<QuestionData>();
    }
}

public class QuestionDatabaseExporter : EditorWindow
{
    private QuestionDatabase sourceDatabase;
    private string exportFilePath = "";

    [MenuItem("Tools/Quiz/Question Exporter")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(QuestionDatabaseExporter), false, "Question Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Questions to JSON", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Source database selection
        sourceDatabase = (QuestionDatabase)EditorGUILayout.ObjectField(
            "Source Database:", 
            sourceDatabase, 
            typeof(QuestionDatabase), 
            false);

        // Export path
        EditorGUILayout.BeginHorizontal();
        exportFilePath = EditorGUILayout.TextField("Export Path:", exportFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFilePanel("Save JSON file", "", "questions.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                exportFilePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Export button
        EditorGUILayout.Space();
        GUI.enabled = !string.IsNullOrEmpty(exportFilePath) && sourceDatabase != null;
        
        if (GUILayout.Button("Export Questions"))
        {
            ExportQuestions();
        }
        
        GUI.enabled = true;
    }

    private void ExportQuestions()
    {
        if (sourceDatabase == null || string.IsNullOrEmpty(exportFilePath))
        {
            EditorUtility.DisplayDialog("Export Error", "Please select a database and export path.", "OK");
            return;
        }

        try
        {
            // Create a temporary list for JSON serialization
            QuestionDataList dataList = new QuestionDataList();
            dataList.questions = sourceDatabase.GetAllQuestions();

            // Convert to JSON
            string jsonContent = JsonUtility.ToJson(dataList, true); // true for pretty print

            // Write to file
            File.WriteAllText(exportFilePath, jsonContent);

            EditorUtility.DisplayDialog("Export Successful", 
                $"Successfully exported {dataList.questions.Count} questions to {exportFilePath}", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error exporting questions: " + e.Message);
            EditorUtility.DisplayDialog("Export Error", 
                "Failed to export questions: " + e.Message, "OK");
        }
    }

    // Helper class for JSON serialization
    [System.Serializable]
    private class QuestionDataList
    {
        public List<QuestionData> questions = new List<QuestionData>();
    }
}
#endif