using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatMessageItem : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nickText;
    [SerializeField] private TMP_Text messageText;

    public void Setup(string nickName, string text, int avatarIndex)
    {
        nickText.text = nickName;
        messageText.text = text;

        if (AvatarManager.Instance != null)
        {
            Sprite avatar = AvatarManager.Instance.GetAvatar(avatarIndex);
            if (avatar != null)
                avatarImage.sprite = avatar;
        }
    }
}