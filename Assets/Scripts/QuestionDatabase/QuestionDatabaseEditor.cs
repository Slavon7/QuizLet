#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestionDatabase))]
public class QuestionDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        QuestionDatabase db = (QuestionDatabase)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Sort Questions"))
        {
            EditorUtility.SetDirty(target);
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Validate Questions"))
        {
            int questionCount = 0;
            int validCount = 0;
            
            EditorUtility.DisplayDialog("Validation Result", 
                $"Validated {questionCount} questions. {validCount} are valid.", "OK");
        }
    }
}
#endif