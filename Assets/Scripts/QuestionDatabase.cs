using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestionDatabase", menuName = "Quiz/Question Database")]
public class QuestionDatabase : ScriptableObject
{
    [SerializeField]
    private List<QuestionData> questions = new List<QuestionData>();

    public int Count => questions.Count;

    public QuestionData GetQuestion(int index)
    {
        if (index >= 0 && index < questions.Count)
        {
            return questions[index];
        }
        return null;
    }

    public List<QuestionData> GetAllQuestions()
    {
        return questions;
    }

    public QuestionData GetRandomQuestion()
    {
        if (questions.Count == 0) return null;
        int randomIndex = Random.Range(0, questions.Count);
        return questions[randomIndex];
    }
}