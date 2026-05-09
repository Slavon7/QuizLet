using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private Transform messageContainer;  // Content в ScrollRect
    [SerializeField] private GameObject messagePrefab;    // ChatMessageItem prefab
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private int maxMessages = 50;

    private void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener(_ => OnSendClicked());
    }

    private void OnSendClicked()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        quizManager.SendChatMessage(inputField.text);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void AddMessage(string nickName, string text, int avatarIndex)
    {
        while (messageContainer.childCount >= maxMessages)
            Destroy(messageContainer.GetChild(0).gameObject);

        GameObject item = Instantiate(messagePrefab, messageContainer);
        item.GetComponent<ChatMessageItem>().Setup(nickName, text, avatarIndex);

        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            messageContainer.GetComponent<RectTransform>());
        
        // Ждём ещё кадр после rebuild
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}