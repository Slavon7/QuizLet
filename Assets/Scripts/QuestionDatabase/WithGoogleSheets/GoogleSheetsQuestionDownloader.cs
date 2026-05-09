using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.IO;

[System.Serializable]
public class QuestionListWrapper
{
    public List<QuestionData> questions;
}

public class GoogleSheetsQuestionDownloader : MonoBehaviour
{
    public static GoogleSheetsQuestionDownloader Instance;
    public string spreadsheetId = "1KPktDfa0yg4C_SbjaJfgrJE_ImN6K0cgJfhM60G08HQ";
    public string sheetName = "Questions";
    public string dataRange = "A2:G";
    public string hashCellRange = "J2:J2";
    public string apiKey = "AIzaSyCvkgPfIKi3jujrlRP0GMwq12OevMS80ZU";

    public List<QuestionData> loadedQuestions = new List<QuestionData>();

    private string cachePath => Path.Combine(Application.persistentDataPath, "questions.json");
    private string hashPath => Path.Combine(Application.persistentDataPath, "questions_hash.txt");

    // Добавляем событие — подписывайся снаружи, чтобы запустить подключение к Photon
    public event Action OnQuestionsLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(CheckAndDownload());
    }

    IEnumerator CheckAndDownload()
    {
        string hashUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}!{hashCellRange}?key={apiKey}";
        using UnityWebRequest wwwHash = UnityWebRequest.Get(hashUrl);
        yield return wwwHash.SendWebRequest();

        if (wwwHash.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Ошибка загрузки хеша Google Sheets: " + wwwHash.error);
            yield break;
        }

        string hashJson = wwwHash.downloadHandler.text;
        JSONNode hashRoot = JSON.Parse(hashJson);
        string remoteHash = hashRoot["values"]?[0]?[0]?.Value ?? "";

        if (string.IsNullOrEmpty(remoteHash))
        {
            Debug.LogError("Хеш из таблицы пустой или отсутствует.");
            yield break;
        }

        string localHash = "";
        if (File.Exists(hashPath))
        {
            localHash = File.ReadAllText(hashPath);
        }

        if (remoteHash == localHash && File.Exists(cachePath))
        {
            Debug.Log("Хеш совпал — дешифруем вопросы из кеша");
            string encryptedJson = File.ReadAllText(cachePath);
            
            try 
            {
                string decryptedJson = EncryptionUtils.Decrypt(encryptedJson);
                LoadQuestionsFromJson(decryptedJson);
                OnQuestionsLoaded?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("Помилка дешифрування: " + e.Message);
                // Якщо файл пошкоджено або ключ не підходить — краще видалити кеш і завантажити заново
            }
            yield break;
        }

        string dataUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{sheetName}!{dataRange}?key={apiKey}";
        using UnityWebRequest wwwData = UnityWebRequest.Get(dataUrl);
        yield return wwwData.SendWebRequest();

        if (wwwData.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Ошибка загрузки данных Google Sheets: " + wwwData.error);
            yield break;
        }

        string dataJson = wwwData.downloadHandler.text;
        JSONNode dataRoot = JSON.Parse(dataJson);
        JSONArray values = dataRoot["values"].AsArray;

        loadedQuestions.Clear();

        foreach (JSONNode row in values)
        {
            if (row.Count < 6)
            {
                Debug.LogWarning("Пропущена строка: недостаточно данных");
                continue;
            }

            var q = new QuestionData
            {
                question = row[0],
                options = new string[]
                {
                    row[1],
                    row[2],
                    row[3],
                    row[4]
                },
                correctAnswer = row[5],
                isNumerical = row.Count > 6 && row[6].Value.ToLower() == "true"
            };

            loadedQuestions.Add(q);
        }

        Debug.Log($"Загружено {loadedQuestions.Count} вопросов из Google Sheets");

        SaveCache(loadedQuestions, remoteHash);
        OnQuestionsLoaded?.Invoke(); // Вызов события
    }

    void SaveCache(List<QuestionData> questions, string hash)
    {
        var wrapper = new QuestionListWrapper { questions = questions };
        string json = JsonUtility.ToJson(wrapper, true);

        // ШИФРУЄМО ПЕРЕД ЗБЕРЕЖЕННЯМ
        string encryptedJson = EncryptionUtils.Encrypt(json);

        File.WriteAllText(cachePath, encryptedJson);
        File.WriteAllText(hashPath, hash);

        Debug.Log("Дані зашифровано та збережено в кеш.");
    }

    void LoadQuestionsFromJson(string json)
    {
        var wrapper = JsonUtility.FromJson<QuestionListWrapper>(json);
        loadedQuestions = wrapper.questions;
        Debug.Log($"Загружено из кеша {loadedQuestions.Count} вопросов");
    }
    
    // корутин CheckAndDownload
    void OnDownloadComplete()
    {
        // после загрузки вопросов
        SceneManager.LoadScene("GameScene");
    }
}