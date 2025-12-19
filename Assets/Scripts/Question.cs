using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestion", menuName = "Quiz/Question")]
public class Question : ScriptableObject
{
    [TextArea]
    public string questionText;
    public string[] answers = new string[4];
    public int correctAnswerIndex;
}