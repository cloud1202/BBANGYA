using UnityEngine;
using UnityEngine.Events;

[ManagerOrder(6)]
public class SoundManager : ReferenceManager<SoundManager, SoundData>, IManager
{
    private const float DEFAULT_SOUND_VOLUM = 0.5f;

    private UnityAction<float> _updateSoundVolumEvent = null;
    private UnityAction<float> _updateBGMVolumEvent = null;
    private UnityAction<float> _updateSFXVolumEvent = null;

    [SerializeField] private AudioSource _bgmAudio;
    [SerializeField] private AudioSource _sfxAudio;

    private float _soundVolumPer;
    public float SoundVolumPer
    {
        get { return _soundVolumPer; }
        set
        {
            if (_soundVolumPer == value)
                return;

            _soundVolumPer = value;
            _updateSoundVolumEvent?.Invoke(value);
        }
    }

    private float _bgmVolumPer;
    public float BGMVolumPer
    {
        get { return _bgmVolumPer; }
        set
        {
            if (_bgmVolumPer == value)
                return;

            _bgmVolumPer = value;
            _updateBGMVolumEvent?.Invoke(value);
        }
    }

    private float _sfxVolumPer;
    public float SFXVolumPer
    {
        get { return _sfxVolumPer; }
        set
        {
            if (_sfxVolumPer == value)
                return;

            _sfxVolumPer = value;
            _updateSFXVolumEvent?.Invoke(value);
        }
    }

    public override void Init()
    {
        base.Init();
        CreateBGMAudio();
        CreateSFXAudio();

        _updateSoundVolumEvent += UpdateVolum;
        _updateBGMVolumEvent += UpdateBGMVolum;
        _updateSFXVolumEvent += UpdateSFXVolum;
        SoundVolumPer = 0.5f;
    }

    private void CreateBGMAudio()
    {
        var go = new GameObject("BGM");
        _bgmAudio = go.AddComponent<AudioSource>();
        _bgmAudio.playOnAwake = false;
        _bgmAudio.loop = true;
        go.transform.SetParent(this.transform);
    }

    private void CreateSFXAudio()
    {
        var go = new GameObject("SFX");
        _sfxAudio = go.AddComponent<AudioSource>();
        _sfxAudio.playOnAwake = false;
        go.transform.SetParent(this.transform);
    }

    private void UpdateVolum(float volumPer)
    {
        _bgmAudio.volume = DEFAULT_SOUND_VOLUM * _bgmVolumPer * volumPer;
        _sfxAudio.volume = DEFAULT_SOUND_VOLUM * _sfxVolumPer * volumPer;
    }

    private void UpdateBGMVolum(float volumPer)
    {
        _bgmAudio.volume = DEFAULT_SOUND_VOLUM  * volumPer;
    }

    private void UpdateSFXVolum(float volumPer)
    {
        _sfxAudio.volume = DEFAULT_SOUND_VOLUM  * volumPer;
    }

    public void PlayBGM(SoundData sound)
    {
    }

    public void PlaySFX(SoundData sound)
    {
    }

    public void SubscribeToSoundHandler(UnityAction<float> subscribeEvent)
    {
        if (subscribeEvent == null)
            return;

        _updateSoundVolumEvent += subscribeEvent;
        subscribeEvent(SoundVolumPer);
    }

    public void UnsubscribeToSoundHandler(UnityAction<float> subscribeEvent)
    {
        _updateSoundVolumEvent -= subscribeEvent;
    }
}
