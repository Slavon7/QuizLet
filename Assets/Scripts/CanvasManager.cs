using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CanvasManager : MonoBehaviour
{
    public GameObject targetCanvas;

    public void OpenCanvas()
    {
        if (targetCanvas != null)
        {
            targetCanvas.SetActive(true);
        }
        else
        {
            Debug.LogError("Canvas не установлен в CanvasManager!");
        }
    }

    public void CloseCanvas()
    {
        if (targetCanvas != null)
        {
            targetCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("Canvas не установлен в CanvasManager!");
        }
    }

    public void ToggleCanvas()
    {
        if (targetCanvas != null)
        {
            bool isActive = targetCanvas.activeSelf;
            targetCanvas.SetActive(!isActive);
        }
        else
        {
            Debug.LogError("Canvas не установлен в CanvasManager!");
        }
    }
}