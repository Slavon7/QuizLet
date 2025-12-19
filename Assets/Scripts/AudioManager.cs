using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Photon.Pun;
using System.Linq;

public class AudioManager : MonoBehaviourPun, IPunObservable
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float uiVolume = 1f;

    [Header("Audio Clips")]
    public AudioClip[] musicClips;
    public AudioClip[] sfxClips;
    public AudioClip[] uiClips;

    private Dictionary<string, AudioClip> audioClipDictionary;
    private bool isMusicMuted = false;
    private bool isSfxMuted = false;
    private bool isUiMuted = false;

    private Dictionary<string, AudioSource> activeSFXSources = new Dictionary<string, AudioSource>();
    private Dictionary<string, Coroutine> loopingSFXCoroutines = new Dictionary<string, Coroutine>();

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadVolumeSettings();

        // Автоматически запускаем первую музыку, если есть
        if (musicClips.Length > 0 && musicClips[0] != null)
        {
            PlayMusic(musicClips[0].name);
        }
    }

    private void InitializeAudioManager()
    {
        // Создаем словарь для быстрого поиска аудиоклипов
        audioClipDictionary = new Dictionary<string, AudioClip>();

        // Добавляем музыкальные клипы
        foreach (AudioClip clip in musicClips)
        {
            if (clip != null && !audioClipDictionary.ContainsKey(clip.name))
            {
                audioClipDictionary.Add(clip.name, clip);
            }
        }

        // Добавляем SFX клипы
        foreach (AudioClip clip in sfxClips)
        {
            if (clip != null && !audioClipDictionary.ContainsKey(clip.name))
            {
                audioClipDictionary.Add(clip.name, clip);
            }
        }

        // Добавляем UI клипы
        foreach (AudioClip clip in uiClips)
        {
            if (clip != null && !audioClipDictionary.ContainsKey(clip.name))
            {
                audioClipDictionary.Add(clip.name, clip);
            }
        }
    }

    #region Volume Control

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        // Пробуем использовать Audio Mixer, если настроен
        if (audioMixer != null)
        {
            try
            {
                float dbValue = masterVolume > 0 ? Mathf.Log10(masterVolume) * 20 : -80f;
                audioMixer.SetFloat("MasterVolume", dbValue);
            }
            catch (System.Exception)
            {
                // Если параметр не найден, управляем громкостью напрямую
                UpdateDirectVolume();
            }
        }
        else
        {
            UpdateDirectVolume();
        }

        SaveVolumeSettings();

        // Синхронизируем с другими игроками через Photon
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("SyncVolumeSettings", RpcTarget.Others, masterVolume, musicVolume, sfxVolume, uiVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            try
            {
                float dbValue = musicVolume > 0 ? Mathf.Log10(musicVolume) * 20 : -80f;
                audioMixer.SetFloat("MusicVolume", dbValue);
            }
            catch (System.Exception)
            {
                UpdateDirectVolume();
            }
        }
        else
        {
            UpdateDirectVolume();
        }

        SaveVolumeSettings();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            try
            {
                float dbValue = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f;
                audioMixer.SetFloat("SfxVolume", dbValue);
            }
            catch (System.Exception)
            {
                UpdateDirectVolume();
            }
        }
        else
        {
            UpdateDirectVolume();
        }

        SaveVolumeSettings();
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            try
            {
                float dbValue = uiVolume > 0 ? Mathf.Log10(uiVolume) * 20 : -80f;
                audioMixer.SetFloat("UIVolume", dbValue);
            }
            catch (System.Exception)
            {
                UpdateDirectVolume();
            }
        }
        else
        {
            UpdateDirectVolume();
        }

        SaveVolumeSettings();
    }

    private void UpdateDirectVolume()
    {
        // Управляем громкостью напрямую через AudioSource
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;

        if (uiSource != null)
            uiSource.volume = masterVolume * uiVolume;
    }

    #endregion

    #region Music Control

    public void PlayMusic(string clipName, bool loop = true)
    {
        // Проверяем наличие musicSource
        if (musicSource == null)
        {
            Debug.LogError("musicSource не инициализирован в AudioManager!");
            return;
        }

        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();

            // Уведомляем других игроков
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("PlayMusicRPC", RpcTarget.Others, clipName, loop);
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip '{clipName}' not found!");
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();

        if (photonView.IsMine)
        {
            photonView.RPC("StopMusicRPC", RpcTarget.Others);
        }
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    #endregion

    #region SFX Control

    // Замените существующий метод PlaySFX на этот улучшенный вариант:
    public void PlaySFX(string clipName, float volume = 1f, bool loop = false)
    {
        if (sfxSource == null)
        {
            Debug.LogError("sfxSource не инициализирован в AudioManager!");
            return;
        }

        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            if (loop)
            {
                // Для зацикленных звуков создаем отдельный AudioSource
                GameObject sfxObject = new GameObject($"SFX_{clipName}");
                sfxObject.transform.SetParent(transform);
                AudioSource loopSource = sfxObject.AddComponent<AudioSource>();

                // Копируем настройки из основного SFX источника
                loopSource.clip = clip;
                loopSource.volume = volume * masterVolume * sfxVolume;
                loopSource.loop = true;
                loopSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;

                loopSource.Play();

                // Сохраняем ссылку для возможности остановки
                if (activeSFXSources.ContainsKey(clipName))
                {
                    StopSpecificSFX(clipName);
                }
                activeSFXSources[clipName] = loopSource;
            }
            else
            {
                // Обычное воспроизведение
                sfxSource.PlayOneShot(clip, volume);
            }

            // Синхронизируем SFX с другими игроками
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("PlaySFXRPC", RpcTarget.Others, clipName, volume, loop);
            }
        }
        else
        {
            Debug.LogWarning($"SFX clip '{clipName}' not found!");
        }
    }

    // Новый метод для остановки конкретного SFX звука:
    public void StopSpecificSFX(string clipName)
    {
        if (activeSFXSources.TryGetValue(clipName, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
                if (source.gameObject != null)
                {
                    Destroy(source.gameObject);
                }
            }
            activeSFXSources.Remove(clipName);
        }

        // Останавливаем корутину, если она есть
        if (loopingSFXCoroutines.TryGetValue(clipName, out Coroutine coroutine))
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            loopingSFXCoroutines.Remove(clipName);
        }

        // Синхронизируем с другими игроками
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("StopSpecificSFXRPC", RpcTarget.Others, clipName);
        }
    }

    // Метод для проверки, играет ли конкретный SFX:
    public bool IsSFXPlaying(string clipName)
    {
        if (activeSFXSources.TryGetValue(clipName, out AudioSource source))
        {
            return source != null && source.isPlaying;
        }
        return false;
    }

    // Обновите существующий метод StopSFX для очистки всех активных источников:
    public void StopSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        // Останавливаем все активные зацикленные SFX
        foreach (var kvp in activeSFXSources.ToList())
        {
            StopSpecificSFX(kvp.Key);
        }

        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC("StopSFXRPC", RpcTarget.Others);
        }
    }

    public void PlaySFXAtPosition(string clipName, Vector3 position, float volume = 1f)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);

            if (photonView.IsMine)
            {
                photonView.RPC("PlaySFXAtPositionRPC", RpcTarget.Others, clipName, position.x, position.y, position.z, volume);
            }
        }
    }

    #endregion

    #region UI Sounds

    public void PlayUISound(string clipName)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            uiSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Mute Controls

    public void ToggleMusicMute()
    {
        isMusicMuted = !isMusicMuted;
        if (audioMixer != null)
        {
            try
            {
                audioMixer.SetFloat("MusicVolume", isMusicMuted ? -80f : (musicVolume > 0 ? Mathf.Log10(musicVolume) * 20 : -80f));
            }
            catch (System.Exception)
            {
                if (musicSource != null)
                    musicSource.volume = isMusicMuted ? 0f : masterVolume * musicVolume;
            }
        }
        else
        {
            if (musicSource != null)
                musicSource.volume = isMusicMuted ? 0f : masterVolume * musicVolume;
        }
    }

    public void ToggleSfxMute()
    {
        isSfxMuted = !isSfxMuted;
        if (audioMixer != null)
        {
            try
            {
                audioMixer.SetFloat("SfxVolume", isSfxMuted ? -80f : (sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f));
            }
            catch (System.Exception)
            {
                if (sfxSource != null)
                    sfxSource.volume = isSfxMuted ? 0f : masterVolume * sfxVolume;
            }
        }
        else
        {
            if (sfxSource != null)
                sfxSource.volume = isSfxMuted ? 0f : masterVolume * sfxVolume;
        }
    }

    public void ToggleUIMute()
    {
        isUiMuted = !isUiMuted;
        if (audioMixer != null)
        {
            try
            {
                audioMixer.SetFloat("UIVolume", isUiMuted ? -80f : (uiVolume > 0 ? Mathf.Log10(uiVolume) * 20 : -80f));
            }
            catch (System.Exception)
            {
                if (uiSource != null)
                    uiSource.volume = isUiMuted ? 0f : masterVolume * uiVolume;
            }
        }
        else
        {
            if (uiSource != null)
                uiSource.volume = isUiMuted ? 0f : masterVolume * uiVolume;
        }
    }

    #endregion

    #region Photon RPCs

    [PunRPC]
    void PlayMusicRPC(string clipName, bool loop)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    [PunRPC]
    void StopMusicRPC()
    {
        musicSource.Stop();
    }

    // Добавьте эти RPC методы в секцию #region Photon RPCs:
    [PunRPC]
    void PlaySFXRPC(string clipName, float volume, bool loop)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            if (loop)
            {
                GameObject sfxObject = new GameObject($"SFX_{clipName}");
                sfxObject.transform.SetParent(transform);
                AudioSource loopSource = sfxObject.AddComponent<AudioSource>();

                loopSource.clip = clip;
                loopSource.volume = volume * masterVolume * sfxVolume;
                loopSource.loop = true;
                loopSource.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;

                loopSource.Play();

                if (activeSFXSources.ContainsKey(clipName))
                {
                    StopSpecificSFX(clipName);
                }
                activeSFXSources[clipName] = loopSource;
            }
            else
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }

    [PunRPC]
    public void StopTimerSoundRPC()
    {
        StopSpecificSFX("timer");
    }

    [PunRPC]
    void StopSpecificSFXRPC(string clipName)
    {
        if (activeSFXSources.TryGetValue(clipName, out AudioSource source))
        {
            if (source != null)
            {
                source.Stop();
                if (source.gameObject != null)
                {
                    Destroy(source.gameObject);
                }
            }
            activeSFXSources.Remove(clipName);
        }
    }

    [PunRPC]
    void PlaySFXAtPositionRPC(string clipName, float x, float y, float z, float volume)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            Vector3 position = new Vector3(x, y, z);
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }

    [PunRPC]
    void SyncVolumeSettings(float master, float music, float sfx, float ui)
    {
        masterVolume = master;
        musicVolume = music;
        sfxVolume = sfx;
        uiVolume = ui;

        audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
        audioMixer.SetFloat("SfxVolume", Mathf.Log10(sfxVolume) * 20);
        audioMixer.SetFloat("UIVolume", Mathf.Log10(uiVolume) * 20);
    }

    #endregion

    #region Save/Load Settings

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);

        // Применяем загруженные настройки
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
        SetUIVolume(uiVolume);
    }

    #endregion

    #region IPunObservable Implementation

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправляем данные о текущем состоянии музыки
            stream.SendNext(musicSource.isPlaying);
            stream.SendNext(musicSource.time);
            if (musicSource.clip != null)
            {
                stream.SendNext(musicSource.clip.name);
            }
            else
            {
                stream.SendNext("");
            }
        }
        else
        {
            // Получаем данные о состоянии музыки от других игроков
            bool isPlaying = (bool)stream.ReceiveNext();
            float time = (float)stream.ReceiveNext();
            string clipName = (string)stream.ReceiveNext();

            // Синхронизируем воспроизведение музыки
            if (!string.IsNullOrEmpty(clipName) && isPlaying)
            {
                if (musicSource.clip == null || musicSource.clip.name != clipName)
                {
                    PlayMusic(clipName);
                }

                // Синхронизируем время воспроизведения
                if (Mathf.Abs(musicSource.time - time) > 1f)
                {
                    musicSource.time = time;
                }
            }
        }
    }

    #endregion

    #region Utility Methods

    public bool IsMusicPlaying()
    {
        return musicSource.isPlaying;
    }

    public void PlayUISoundByIndex(int index)
    {
        if (index >= 0 && index < uiClips.Length && uiClips[index] != null)
        {
            uiSource.PlayOneShot(uiClips[index]);
        }
        else
        {
            Debug.LogWarning($"UI clip at index {index} not found or null!");
        }
    }

    public float GetMusicProgress()
    {
        if (musicSource.clip != null)
        {
            return musicSource.time / musicSource.clip.length;
        }
        return 0f;
    }

    public void FadeMusic(float targetVolume, float duration)
    {
        StartCoroutine(FadeMusicCoroutine(targetVolume, duration));
    }

    private IEnumerator FadeMusicCoroutine(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, progress);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    #endregion
}