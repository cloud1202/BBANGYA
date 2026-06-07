using Cysharp.Threading.Tasks;
using UnityEngine;
using static SoundManager;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    [SerializeField] private SoundData _soundType;
    private AudioSource _audioSource;
    private float _initVolum;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _initVolum = _audioSource.volume;
        SetAudioClip().Forget();
        SoundManager.Instance.SubscribeToSoundHandler(UpdateVolum);
    }

    async private UniTask SetAudioClip()
    {
        //_audioSource.clip = await SoundManager.Instance.LoadAudioClip(_soundType);
    }

    private void OnDestroy()
    {
        if (SoundManager.IsCreatedInstance() == false)
            return;

        SoundManager.Instance.UnsubscribeToSoundHandler(UpdateVolum);
    }

    private void UpdateVolum(float volumPer)
    {
        _audioSource.volume = _initVolum * volumPer;
    }

    public void PlaySound()
    {
        _audioSource.Play();
    }

    public void FadeSound(float value, float duration)
    {
        //SoundManager.Instance.FadeSound(_audioSource, value, duration);
    }
}
