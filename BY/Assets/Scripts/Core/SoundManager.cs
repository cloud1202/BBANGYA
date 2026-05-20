using UnityEngine;

public class SoundManager : SingletonInstance<SoundManager>
{
    private const float DEFAULT_SOUND_VOLUM = 1.0f;

    public delegate void UpdateVolumDelegate(float volumPer);

    private UpdateVolumDelegate _updateSoundVolumEvent = null;

    [SerializeField] private AudioSource _bgmAudio;
    [SerializeField] private AudioSource _clickAudio;
    [SerializeField] private AudioSource _missAudio;

    private float _soundVolumPer;
    public float SoundVolumPer
    {
        get { return _soundVolumPer; }
        set
        {
            if (_soundVolumPer == value)
                return;

            _soundVolumPer = value;
            _updateSoundVolumEvent(value);
        }
    }

    public override void Init()
    {
        base.Init();
        _updateSoundVolumEvent += UpdateVolum;
        SoundVolumPer = 0.5f;
    }

    private void UpdateVolum(float volumPer)
    {
        _bgmAudio.volume = DEFAULT_SOUND_VOLUM * volumPer;
        _clickAudio.volume = DEFAULT_SOUND_VOLUM * volumPer;
        _missAudio.volume = DEFAULT_SOUND_VOLUM * volumPer;
    }

    public void PlayClick()
    {
        _clickAudio.PlayOneShot(_clickAudio.clip);
    }

    public void PlayMiss()
    {
        _missAudio.PlayOneShot(_missAudio.clip);
    }

    public void SubscribeToSoundHandler(UpdateVolumDelegate subscribeEvent)
    {
        if (subscribeEvent == null)
            return;

        _updateSoundVolumEvent += subscribeEvent;
        subscribeEvent(SoundVolumPer);
    }
}
