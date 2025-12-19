using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Универсальный компонент для добавления поддержки клавиши Enter к InputField.
/// Автоматически "нажимает" указанную кнопку при нажатии Enter в поле ввода.
/// </summary>
[RequireComponent(typeof(TMP_InputField))]
public class EnterKeySubmit : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Button targetButton;
    [SerializeField] private bool alsoClearFieldAfterSubmit = false;
    
    private TMP_InputField inputField;
    
    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        
        if (inputField == null)
        {
            Debug.LogError("EnterKeySubmit: TMP_InputField не найден на объекте " + gameObject.name);
        }
    }
    
    private void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnEndEdit);
        }
        
        if (targetButton == null)
        {
            Debug.LogWarning("EnterKeySubmit: Target Button не назначена для " + gameObject.name);
        }
    }
    
    private void OnEndEdit(string value)
    {
        // Проверяем нажатие Enter (обычный и на NumPad)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitInput();
        }
    }
    
    private void SubmitInput()
    {
        // Проверяем, что кнопка существует и доступна для нажатия
        if (targetButton != null && targetButton.interactable && targetButton.gameObject.activeInHierarchy)
        {
            // Вызываем событие кнопки
            targetButton.onClick.Invoke();
            
            // Опционально очищаем поле после отправки
            if (alsoClearFieldAfterSubmit && inputField != null)
            {
                inputField.text = "";
            }
            
            Debug.Log("EnterKeySubmit: Кнопка активирована через Enter");
        }
        else
        {
            Debug.LogWarning("EnterKeySubmit: Кнопка недоступна для активации");
        }
    }
    
    /// <summary>
    /// Публичный метод для программной установки целевой кнопки
    /// </summary>
    public void SetTargetButton(Button button)
    {
        targetButton = button;
    }
    
    /// <summary>
    /// Публичный метод для программной активации (можно вызвать из других скриптов)
    /// </summary>
    public void TriggerSubmit()
    {
        SubmitInput();
    }
    
    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnEndEdit);
        }
    }
}