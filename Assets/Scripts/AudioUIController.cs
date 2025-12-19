using UnityEngine;
using UnityEngine.UI;

public class AudioUIController : MonoBehaviour
{
    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider uiVolumeSlider;
    
    [Header("Mute Buttons")]
    public Button musicMuteButton;
    public Button sfxMuteButton;
    public Button uiMuteButton;
    
    [Header("Music Controls")]
    public Button playMusicButton;
    public Button stopMusicButton;
    public Button pauseMusicButton;
    public Button resumeMusicButton;
    
    [Header("Test SFX")]
    public Button testSfxButton;
    public string testSfxClipName = "TestSound";
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }
    
    private void InitializeUI()
    {
        if (AudioManager.Instance != null)
        {
            // Устанавливаем начальные значения слайдеров
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = AudioManager.Instance.masterVolume;
                
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = AudioManager.Instance.musicVolume;
                
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
                
            if (uiVolumeSlider != null)
                uiVolumeSlider.value = AudioManager.Instance.uiVolume;
        }
    }
    
    private void SetupEventListeners()
    {
        // Слайдеры громкости
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }
        
        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
        }
        
        // Кнопки выключения звука
        if (musicMuteButton != null)
        {
            musicMuteButton.onClick.AddListener(OnMusicMuteClicked);
        }
        
        if (sfxMuteButton != null)
        {
            sfxMuteButton.onClick.AddListener(OnSfxMuteClicked);
        }
        
        if (uiMuteButton != null)
        {
            uiMuteButton.onClick.AddListener(OnUIMuteClicked);
        }
        
        // Управление музыкой
        if (playMusicButton != null)
        {
            playMusicButton.onClick.AddListener(OnPlayMusicClicked);
        }
        
        if (stopMusicButton != null)
        {
            stopMusicButton.onClick.AddListener(OnStopMusicClicked);
        }
        
        if (pauseMusicButton != null)
        {
            pauseMusicButton.onClick.AddListener(OnPauseMusicClicked);
        }
        
        if (resumeMusicButton != null)
        {
            resumeMusicButton.onClick.AddListener(OnResumeMusicClicked);
        }
        
        // Тест SFX
        if (testSfxButton != null)
        {
            testSfxButton.onClick.AddListener(OnTestSfxClicked);
        }
    }
    
    #region Event Handlers
    
    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }
    
    private void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
        }
    }
    
    private void OnUIVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetUIVolume(value);
        }
    }
    
    private void OnMusicMuteClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusicMute();
            AudioManager.Instance.PlayUISound("ButtonClick");
        }
    }
    
    private void OnSfxMuteClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSfxMute();
            AudioManager.Instance.PlayUISound("ButtonClick");
        }
    }
    
    private void OnUIMuteClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleUIMute();
        }
    }
    
    private void OnPlayMusicClicked()
    {
        if (AudioManager.Instance != null)
        {
            // Воспроизводим первый трек из списка, если он есть
            if (AudioManager.Instance.musicClips.Length > 0)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.musicClips[0].name);
            }
            AudioManager.Instance.PlayUISound("ButtonClick");
        }
    }
    
    private void OnStopMusicClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.PlayUISound("ButtonClick");
        }
    }
    
    private void OnPauseMusicClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMusic();
            AudioManager.Instance.PlayUISound("ButtonClick");
        }
    }
    
    private void OnResumeMusicClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMusic();
            AudioManager.Instance.PlayUISound("ButtonClick");
        }
    }
    
    private void OnTestSfxClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(testSfxClipName);
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Очищаем слушатели событий
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            
        if (uiVolumeSlider != null)
            uiVolumeSlider.onValueChanged.RemoveAllListeners();
    }
}