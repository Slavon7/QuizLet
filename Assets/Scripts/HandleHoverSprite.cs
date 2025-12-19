using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class HandleHoverFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image normalImage;
    public Image hoverImage;

    [Header("Fade Settings")]
    public float fadeDuration = 0.2f;

    private void Start()
    {
        // Убедимся, что обычный вид активен, ховер — скрыт
        normalImage.color = new Color(1, 1, 1, 1);
        hoverImage.color = new Color(1, 1, 1, 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverImage.DOFade(1f, fadeDuration);
        normalImage.DOFade(0f, fadeDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverImage.DOFade(0f, fadeDuration);
        normalImage.DOFade(1f, fadeDuration);
    }
}
