using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사운드 설정 UI
/// - 전체(All), BGM, SFX 볼륨 슬라이더
/// - SoundManager와 연동
/// </summary>
public class SoundSettingUI : MonoBehaviour
{
    [Header("전체 볼륨")]
    [SerializeField] private Slider _allSlider;
    [SerializeField] private TextMeshProUGUI _allValueText;

    [Header("BGM 볼륨")]
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private TextMeshProUGUI _bgmValueText;

    [Header("SFX 볼륨")]
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TextMeshProUGUI _sfxValueText;

    void Start()
    {
        // 저장된 값 불러오기
        float allVol = PlayerPrefs.GetFloat("VolumeAll", SoundManager.Instance.SoundVolumPer);
        float bgmVol = PlayerPrefs.GetFloat("VolumeBGM", SoundManager.Instance.BGMVolumPer);
        float sfxVol = PlayerPrefs.GetFloat("VolumeSFX", SoundManager.Instance.SFXVolumPer);

        // 슬라이더 초기값 설정 (이벤트 없이)
        _allSlider.SetValueWithoutNotify(allVol);
        _bgmSlider.SetValueWithoutNotify(bgmVol);
        _sfxSlider.SetValueWithoutNotify(sfxVol);

        UpdateValueText(_allValueText, allVol);
        UpdateValueText(_bgmValueText, bgmVol);
        UpdateValueText(_sfxValueText, sfxVol);

        // 이벤트 등록
        _allSlider.onValueChanged.AddListener(OnAllVolumeChanged);
        _bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        _sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // SoundManager에 적용
        ApplyAll(allVol);
        ApplyBGM(bgmVol);
        ApplySFX(sfxVol);
    }

    // ─────────────────────────────────────────
    // 슬라이더 이벤트
    // ─────────────────────────────────────────
    private void OnAllVolumeChanged(float value)
    {
        UpdateValueText(_allValueText, value);
        ApplyAll(value);
        PlayerPrefs.SetFloat("VolumeAll", value);
        PlayerPrefs.Save();
    }

    private void OnBGMVolumeChanged(float value)
    {
        UpdateValueText(_bgmValueText, value);
        ApplyBGM(value);
        PlayerPrefs.SetFloat("VolumeBGM", value);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(float value)
    {
        UpdateValueText(_sfxValueText, value);
        ApplySFX(value);
        PlayerPrefs.SetFloat("VolumeSFX", value);
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────
    // SoundManager 연동
    // ─────────────────────────────────────────
    private void ApplyAll(float value)
    {
        AudioListener.volume = value;
        //SoundManager.Instance.SoundVolumPer = value;
    }

    private void ApplyBGM(float value)
    {
        SoundManager.Instance.BGMVolumPer = value;
    }

    private void ApplySFX(float value)
    {
        SoundManager.Instance.SFXVolumPer = value;
    }

    private void UpdateValueText(TextMeshProUGUI text, float value)
    {
        if (text != null)
            text.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // ─────────────────────────────────────────
    // 저장된 설정 적용 (앱 시작 시 호출)
    // ─────────────────────────────────────────
    public static void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("VolumeAll", 1f);
    }
}
