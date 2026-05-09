using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProfileUIBinder : MonoBehaviour
{
    [Header("UI refs from Lobby Scene")]
    public TMP_InputField nicknameInputField;
    public TextMeshProUGUI currentNicknameText;
    public Image avatarImage;

    public Slider experienceSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI experienceText;

    public TextMeshProUGUI cupsText;

    public GameObject levelUpPanel;
    public TextMeshProUGUI levelUpText;
    public TextMeshProUGUI gemRewardText;

    void Start()
    {
        if (ProfileManager.Instance == null)
        {
            Debug.LogError("ProfileManager.Instance не найден. Убедись что ProfileManager создаётся до лобби.");
            return;
        }

        ProfileManager.Instance.BindUI(
            nicknameInputField,
            currentNicknameText,
            avatarImage,
            experienceSlider,
            levelText,
            experienceText,
            cupsText,
            levelUpPanel,
            levelUpText,
            gemRewardText
        );

        // на всякий случай
        ProfileManager.Instance.RefreshUI();
    }
}