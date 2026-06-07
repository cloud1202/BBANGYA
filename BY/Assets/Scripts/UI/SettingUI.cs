using Unity.Netcode;
using UnityEngine;

public class SettingUI : BaseUI
{
    public override void Init()
    {
        base.Init();
    }

    public async void OnClickLeaveBtn()
    {
        // 방 나가기 (로비 종료 + 네트워크 종료)
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient))
        {
            await NetworkGameManager.Instance.LeaveLobby();
        }

        OnClickCloseBtn();
    }

    public void OnClickBackBtn()
    {
        OnClickCloseBtn();
    }

    public void OnClickCloseBtn()
    {
        Destroy(this.gameObject);
    }
}
