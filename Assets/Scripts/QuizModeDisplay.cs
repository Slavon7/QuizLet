using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class QuizModeDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Mode Icon Settings")]
    [SerializeField] private Image modeIcon;
    [SerializeField] private Sprite normalModeSprite;
    [SerializeField] private Sprite doublePointsSprite;
    [SerializeField] private Sprite shortTimeSprite;
    [SerializeField] private Sprite bombSprite;
    
    [Header("Tooltip Settings")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;
    
    [Header("Mode Descriptions")]
    [SerializeField] [TextArea(2, 3)] private string normalModeDescription = "Стандартний режим гри.";
    [SerializeField] [TextArea(2, 3)] private string doublePointsDescription = "Усі бали подвоюються!";
    [SerializeField] [TextArea(2, 3)] private string shortTimeDescription = "Час пришвидшується! Встигніть відповісти!";
    [SerializeField] [TextArea(2, 3)] private string bombDescription = "Ви втрачаєте бали за неправильну відповідь!";

    
    private QuizMode currentMode = QuizMode.Normal;
    private QuizManager quizManager;
    
    private void Start()
    {
        // Находим QuizManager в сцене
        quizManager = Object.FindAnyObjectByType<QuizManager>();
        
        // Скрываем подсказку при старте
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        
        // Устанавливаем начальный режим
        UpdateModeDisplay(QuizMode.Normal);
    }
    
    // Метод, который будет вызываться из QuizManager при изменении режима
    public void UpdateModeDisplay(QuizMode mode)
    {
        currentMode = mode;
        
        // Обновляем иконку в зависимости от режима
        if (modeIcon != null)
        {
            switch (mode)
            {
                case QuizMode.Normal:
                    modeIcon.sprite = normalModeSprite;
                    break;
                case QuizMode.DoublePoints:
                    modeIcon.sprite = doublePointsSprite;
                    break;
                case QuizMode.ShortTime:
                    modeIcon.sprite = shortTimeSprite;
                    break;
                case QuizMode.Bomb:
                    modeIcon.sprite = bombSprite;
                    break;
            }
        }
    }
    
    // Метод вызывается при наведении указателя на иконку
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }
    
    // Метод вызывается при уходе указателя с иконки
    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }
    
    private void ShowTooltip()
    {
        // Заполняем текст подсказки в зависимости от текущего режима
        if (tooltipTitle != null && tooltipDescription != null)
        {
            switch (currentMode)
            {
                case QuizMode.Normal:
                    tooltipTitle.text = "Common";
                    tooltipDescription.text = normalModeDescription;
                    break;
                case QuizMode.DoublePoints:
                    tooltipTitle.text = "Double Points";
                    tooltipDescription.text = doublePointsDescription;
                    break;
                case QuizMode.ShortTime:
                    tooltipTitle.text = "Short Time";
                    tooltipDescription.text = shortTimeDescription;
                    break;
                case QuizMode.Bomb:
                    tooltipTitle.text = "Bomb Mode";
                    tooltipDescription.text = bombDescription;
                    break;
            }
        }
        
        // Показываем панель подсказки
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
        }
    }
    
    private void HideTooltip()
    {
        // Скрываем панель подсказки
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}