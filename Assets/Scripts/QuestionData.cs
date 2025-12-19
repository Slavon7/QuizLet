using UnityEngine;

[System.Serializable]
public class QuestionData
{
    [TextArea(3, 8)]
    public string question;
    public string[] options = new string[4];
    public string correctAnswer;
    public bool isNumerical;
}