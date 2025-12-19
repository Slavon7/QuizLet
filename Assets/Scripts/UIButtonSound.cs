using UnityEngine;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour
{

    public void PlayUISound(string clipName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(clipName);
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance не найден!");
        }
    }

    public void PlayUISoundByIndex(int clipIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISoundByIndex(clipIndex);
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance не найден!");
        }
    }
}