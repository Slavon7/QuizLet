using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestionSet", menuName = "Quiz/Question Set")]
public class QuestionSet : ScriptableObject
{
    public Question[] questions;
}