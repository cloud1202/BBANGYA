using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시스템 설정 UI
/// - 해상도 드롭다운: 선택 즉시 화면 변경
/// - 언어 드롭다운: 변경 시 경고 팝업 → Yes면 언어 변경 후 로비로 이동
/// </summary>
public class SystemSettingUI : MonoBehaviour
{
    [Header("해상도")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;

    [Header("언어")]
    [SerializeField] private TMP_Dropdown _languageDropdown;

    [Header("언어 변경 경고 팝업")]
    [SerializeField] private GameObject _languagePopup;
    [SerializeField] private Button _popupYesButton;
    [SerializeField] private Button _popupNoButton;
    [SerializeField] private TextMeshProUGUI _popupMessageText;

    // 지원 해상도 목록
    private readonly List<Resolution> _supportedResolutions = new List<Resolution>();
    private int _pendingLanguageIndex = -1;
    private int _currentLanguageIndex = 0;

    // 지원 언어 목록 (필요에 따라 추가)
    private readonly List<string> _languages = new List<string> { "한국어", "English", "日本語" };

    void Start()
    {
        InitResolutionDropdown();
        InitLanguageDropdown();

        _popupYesButton.onClick.AddListener(OnPopupYes);
        _popupNoButton.onClick.AddListener(OnPopupNo);

        _languagePopup.SetActive(false);
    }

    // ─────────────────────────────────────────
    // 해상도 초기화
    // ─────────────────────────────────────────
    private void InitResolutionDropdown()
    {
        _supportedResolutions.Clear();
        _resolutionDropdown.ClearOptions();

        // 중복 제거하여 해상도 목록 구성
        var seen = new HashSet<string>();
        var options = new List<string>();
        int currentIndex = 0;

        Resolution[] resolutions = Screen.resolutions;
        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = resolutions[i];
            string key = $"{res.width}x{res.height}";
            if (seen.Contains(key)) continue;
            seen.Add(key);

            _supportedResolutions.Add(res);
            options.Add(key);

            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
                currentIndex = options.Count - 1;
        }

        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.value = currentIndex;
        _resolutionDropdown.RefreshShownValue();

        _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    // ─────────────────────────────────────────
    // 언어 초기화
    // ─────────────────────────────────────────
    private void InitLanguageDropdown()
    {
        _languageDropdown.ClearOptions();
        _languageDropdown.AddOptions(_languages);

        // 저장된 언어 불러오기
        _currentLanguageIndex = PlayerPrefs.GetInt("LanguageIndex", 0);
        _languageDropdown.value = _currentLanguageIndex;
        _languageDropdown.RefreshShownValue();

        _languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    // ─────────────────────────────────────────
    // 해상도 변경 - 즉시 적용
    // ─────────────────────────────────────────
    private void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _supportedResolutions.Count) return;

        Resolution res = _supportedResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        PlayerPrefs.SetInt("ResolutionWidth", res.width);
        PlayerPrefs.SetInt("ResolutionHeight", res.height);
        PlayerPrefs.Save();

        Debug.Log($"[SystemSettingUI] 해상도 변경: {res.width}x{res.height}");
    }

    // ─────────────────────────────────────────
    // 언어 변경 - 팝업 표시
    // ─────────────────────────────────────────
    private void OnLanguageDropdownChanged(int index)
    {
        if (index == _currentLanguageIndex) return;

        _pendingLanguageIndex = index;
        _popupMessageText.text = $"언어를 <b>{_languages[index]}</b>(으)로 변경하면\n로비 화면으로 이동합니다.\n계속하시겠습니까?";
        _languagePopup.SetActive(true);
    }

    // ─────────────────────────────────────────
    // 팝업 Yes - 언어 변경 후 로비로 이동
    // ─────────────────────────────────────────
    private void OnPopupYes()
    {
        _languagePopup.SetActive(false);

        if (_pendingLanguageIndex < 0) return;

        _currentLanguageIndex = _pendingLanguageIndex;
        PlayerPrefs.SetInt("LanguageIndex", _currentLanguageIndex);
        PlayerPrefs.Save();

        Debug.Log($"[SystemSettingUI] 언어 변경: {_languages[_currentLanguageIndex]}");

        // 로비로 이동 (NetworkManager 종료 후 씬 로드)
        GoToLobby();
    }

    // ─────────────────────────────────────────
    // 팝업 No - 팝업 닫고 드롭다운 원복
    // ─────────────────────────────────────────
    private void OnPopupNo()
    {
        _languagePopup.SetActive(false);

        // 드롭다운을 기존 언어로 원복
        _languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
        _languageDropdown.value = _currentLanguageIndex;
        _languageDropdown.RefreshShownValue();
        _languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);

        _pendingLanguageIndex = -1;
    }

    // ─────────────────────────────────────────
    // 로비로 이동
    // ─────────────────────────────────────────
    private void GoToLobby()
    {
        // 네트워크 연결 중이면 종료
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            (Unity.Netcode.NetworkManager.Singleton.IsHost ||
             Unity.Netcode.NetworkManager.Singleton.IsClient))
        {
            NetworkGameManager.Instance.LeaveLobby().ContinueWith(_ =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            });
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    // ─────────────────────────────────────────
    // 저장된 설정 적용 (앱 시작 시 호출)
    // ─────────────────────────────────────────
    public static void ApplySavedSettings()
    {
        int w = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);
        Screen.SetResolution(w, h, Screen.fullScreen);
    }
}
