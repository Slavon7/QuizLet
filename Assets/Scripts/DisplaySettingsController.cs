using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplaySettingsController : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private TMP_Text resolutionText;
    [SerializeField] private Button resolutionPrevButton;
    [SerializeField] private Button resolutionNextButton;

    [Header("Window Mode Buttons")]
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private Button borderlessButton;
    [SerializeField] private Button windowedButton;

    [Header("Window Mode Sprites")]
    [SerializeField] private Sprite buttonActiveSprite;
    [SerializeField] private Sprite buttonInactiveSprite;

    private static readonly (int w, int h)[] Resolutions =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    };

    private int _resolutionIndex;
    private FullScreenMode _windowMode;

    private const string ResIndexKey = "DisplayResIndex";
    private const string WindowModeKey = "DisplayWindowMode";

    private void Start()
    {
        _resolutionIndex = PlayerPrefs.GetInt(ResIndexKey, 2); // default 1920x1080
        _windowMode = (FullScreenMode)PlayerPrefs.GetInt(WindowModeKey, (int)FullScreenMode.ExclusiveFullScreen);

        resolutionPrevButton?.onClick.AddListener(PrevResolution);
        resolutionNextButton?.onClick.AddListener(NextResolution);
        fullscreenButton?.onClick.AddListener(() => SetWindowMode(FullScreenMode.ExclusiveFullScreen));
        borderlessButton?.onClick.AddListener(() => SetWindowMode(FullScreenMode.FullScreenWindow));
        windowedButton?.onClick.AddListener(() => SetWindowMode(FullScreenMode.Windowed));

        ApplyCurrentSettings(save: false);
        RefreshUI();
    }

    private void PrevResolution()
    {
        _resolutionIndex = (_resolutionIndex - 1 + Resolutions.Length) % Resolutions.Length;
        ApplyCurrentSettings();
    }

    private void NextResolution()
    {
        _resolutionIndex = (_resolutionIndex + 1) % Resolutions.Length;
        ApplyCurrentSettings();
    }

    private void SetWindowMode(FullScreenMode mode)
    {
        _windowMode = mode;
        ApplyCurrentSettings();
    }

    private void ApplyCurrentSettings(bool save = true)
    {
        var (w, h) = Resolutions[_resolutionIndex];
        Screen.SetResolution(w, h, _windowMode);

        if (save)
        {
            PlayerPrefs.SetInt(ResIndexKey, _resolutionIndex);
            PlayerPrefs.SetInt(WindowModeKey, (int)_windowMode);
            PlayerPrefs.Save();
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        var (w, h) = Resolutions[_resolutionIndex];
        if (resolutionText != null)
            resolutionText.text = $"{w}x{h}";

        SetWindowButtonSprites();
    }

    private void SetWindowButtonSprites()
    {
        if (buttonActiveSprite == null || buttonInactiveSprite == null) return;

        SetButtonSprite(fullscreenButton, _windowMode == FullScreenMode.ExclusiveFullScreen);
        SetButtonSprite(borderlessButton, _windowMode == FullScreenMode.FullScreenWindow);
        SetButtonSprite(windowedButton,   _windowMode == FullScreenMode.Windowed);
    }

    private void SetButtonSprite(Button btn, bool isActive)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.sprite = isActive ? buttonActiveSprite : buttonInactiveSprite;
    }
}
