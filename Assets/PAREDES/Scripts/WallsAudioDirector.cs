using UnityEngine;
using UnityEngine.Audio;

public class WallsAudioDirector : MonoBehaviour
{
    public static WallsAudioDirector Instance { get; private set; }

    public bool HasMixer => mainMixer != null;

    [SerializeField] AudioMixer mainMixer;
    [SerializeField] AudioMixerGroup musicOutputGroup;
    [SerializeField] string musicVolumeParameter = "MusicVolume";
    [SerializeField] string sfxVolumeParameter = "SFXVolume";
    [Tooltip("Volume da música durante game over (0 = silêncio, 1 = igual ao utilizador).")]
    [Range(0f, 1f)]
    [SerializeField] float musicDuckLinear = 0.12f;
    [Tooltip("Volume relativo da música no menu principal (antes de Começar).")]
    [Range(0.05f, 1f)]
    [SerializeField] float menuMusicAttenuation = 0.22f;

    AudioSource _musicSource;
    float _musicDuckMultiplier = 1f;
    bool _menuMusicAttenuationActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null)
            _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = false;
        if (musicOutputGroup != null)
            _musicSource.outputAudioMixerGroup = musicOutputGroup;
        RefreshVolumes();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshVolumes()
    {
        float menuMul = _menuMusicAttenuationActive ? Mathf.Clamp01(menuMusicAttenuation) : 1f;
        float musicLin = WallsAudioPrefs.MusicLinear * _musicDuckMultiplier * menuMul;
        float sfxLin = WallsAudioPrefs.SfxLinear;

        if (mainMixer != null)
        {
            mainMixer.SetFloat(musicVolumeParameter, LinearToDecibels(musicLin));
            mainMixer.SetFloat(sfxVolumeParameter, LinearToDecibels(sfxLin));
            if (_musicSource != null)
                _musicSource.volume = 1f;
        }
        else if (_musicSource != null)
        {
            _musicSource.volume = musicLin;
        }
    }

    public float GetSfxLinear()
    {
        return WallsAudioPrefs.SfxLinear;
    }

    public void ApplyMusicDuck()
    {
        _musicDuckMultiplier = Mathf.Clamp01(musicDuckLinear);
        RefreshVolumes();
    }

    public void ClearMusicDuck()
    {
        _musicDuckMultiplier = 1f;
        RefreshVolumes();
    }

    public void SetMenuMusicAttenuation(bool attenuate)
    {
        _menuMusicAttenuationActive = attenuate;
        RefreshVolumes();
    }

    /// <summary>Música ao volume normal do utilizador (após premir Começar).</summary>
    public void ClearMenuMusicAttenuation()
    {
        SetMenuMusicAttenuation(false);
    }

    public static float LinearToDecibels(float linear)
    {
        linear = Mathf.Clamp01(linear);
        if (linear <= 0.0001f)
            return -80f;
        return Mathf.Log10(linear) * 20f;
    }
}
