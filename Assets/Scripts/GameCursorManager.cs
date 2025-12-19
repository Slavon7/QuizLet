using UnityEngine;
using System.Collections.Generic;

public class GameCursorManager : MonoBehaviour
{
    [System.Serializable]
    public class CursorData
    {
        public string name;
        public Texture2D texture;
        public Vector2 hotSpot;
    }

    [Header("Game Cursors")]
    public CursorData[] cursors;
    public string defaultCursorName = "default";

    private Dictionary<string, CursorData> cursorDict;
    private string currentCursor;

    // Singleton для удобства
    public static GameCursorManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCursors();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeCursors()
    {
        cursorDict = new Dictionary<string, CursorData>();
        foreach (var cursor in cursors)
        {
            cursorDict[cursor.name] = cursor;
        }

        SetCursor(defaultCursorName);
    }

    public void SetCursor(string cursorName)
    {
        if (cursorDict.ContainsKey(cursorName) && currentCursor != cursorName)
        {
            var cursor = cursorDict[cursorName];
            Cursor.SetCursor(cursor.texture, cursor.hotSpot, CursorMode.Auto);
            currentCursor = cursorName;
        }
    }

    public void ResetToDefault()
    {
        SetCursor(defaultCursorName);
    }
}