using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject waitingPanel;

    [Header("Main Panel")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("Waiting Panel")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button leaveButton;

    void Start()
    {
        createRoomButton.onClick.AddListener(OnCreateRoom);
        quickJoinButton.onClick.AddListener(OnQuickJoin);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        leaveButton.onClick.AddListener(OnLeave);

        NetworkGameManager.Instance.OnLobbyCreated.AddListener(OnLobbyCreated);
        NetworkGameManager.Instance.OnLobbyJoined.AddListener(OnLobbyJoined);
        NetworkGameManager.Instance.OnError.AddListener(OnError);

        ShowMainPanel();
    }

    private async void OnCreateRoom()
    {
        SetButtonsInteractable(false);
        SetStatus("방 만드는 중...");
        await NetworkGameManager.Instance.CreateLobbyAsHost();
    }

    private async void OnQuickJoin()
    {
        SetButtonsInteractable(false);
        SetStatus("빈 방 찾는 중...");
        await NetworkGameManager.Instance.QuickJoin();
    }

    private async void OnJoinRoom()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("방 코드를 입력해 주세요!");
            return;
        }
        SetButtonsInteractable(false);
        SetStatus("참가 중...");
        await NetworkGameManager.Instance.JoinLobbyByCode(code);
    }

    private async void OnLeave()
    {
        await NetworkGameManager.Instance.LeaveLobby();
        ShowMainPanel();
    }

    private void OnLobbyCreated()
    {
        ShowWaitingPanel();
        string code = NetworkGameManager.Instance.GetLobbyCode();
        SetStatus("상대방 기다리는 중...");
    }

    private void OnLobbyJoined()
    {
        ShowWaitingPanel();
        SetStatus("게임 시작 중...");
    }

    private void OnError(string message)
    {
        SetButtonsInteractable(true);
        ShowError(message);
    }

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        waitingPanel.SetActive(false);
        SetButtonsInteractable(true);
        SetStatus("");
    }

    private void ShowWaitingPanel()
    {
        mainPanel.SetActive(false);
        waitingPanel.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        createRoomButton.interactable = value;
        quickJoinButton.interactable = value;
        joinRoomButton.interactable = value;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ShowError(string message)
    {
        SetStatus($"<color=red>오류: {message}</color>");
        StartCoroutine(ClearStatusAfterDelay(3f));
    }

    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetStatus("");
    }
}
