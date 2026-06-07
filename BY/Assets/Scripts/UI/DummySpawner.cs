using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 에디터 테스트용 더미 캐릭터 스포너
/// 호스트 상태에서 더미 클라이언트처럼 동작하는 캐릭터를 스폰
/// </summary>
public class DummySpawner : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _spawnButton;
    [SerializeField] private Button _despawnButton;
    [SerializeField] private TextMeshProUGUI _statusText;

    private GameObject _dummyObj;
    private NetworkObject _dummyNetObj;

    private static readonly Vector3 DummySpawnPos = new Vector3(5f, 0f, 0f);

    void Start()
    {
        _spawnButton.onClick.AddListener(OnSpawnDummy);
        _despawnButton.onClick.AddListener(OnDespawnDummy);
        _despawnButton.interactable = false;
        SetStatus("대기 중");
    }

    // ─────────────────────────────────────────
    // 더미 스폰
    // ─────────────────────────────────────────
    private async void OnSpawnDummy()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            SetStatus("호스트만 더미 스폰 가능!");
            return;
        }

        if (_dummyObj != null)
        {
            SetStatus("이미 더미가 존재해요!");
            return;
        }

        SetStatus("더미 스폰 중...");
        _spawnButton.interactable = false;

        GameObject playerObj = await PrefabManager.Instance.InstantiateObject<GameObject>(PrefabData.Player, null);

        if (playerObj == null)
        {
            SetStatus("프리팹 로드 실패!");
            _spawnButton.interactable = true;
            return;
        }

        playerObj.transform.position = DummySpawnPos;

        // 더미 컴포넌트 추가 (입력 없이 동작)
        IPlayer dummy = playerObj.gameObject.GetComponent<IPlayer>();
        dummy.isDummy = true;

        // 서버 오너십으로 스폰 (클라이언트 없이 서버가 소유)
        _dummyNetObj = playerObj.GetComponent<NetworkObject>();
        _dummyNetObj.Spawn(true);

        _dummyObj = playerObj;

        _despawnButton.interactable = true;
        _spawnButton.interactable = false;
        SetStatus("더미 스폰 완료!");
    }

    // ─────────────────────────────────────────
    // 더미 디스폰
    // ─────────────────────────────────────────
    private void OnDespawnDummy()
    {
        if (_dummyNetObj != null && _dummyNetObj.IsSpawned)
            _dummyNetObj.Despawn(true);

        _dummyObj = null;
        _dummyNetObj = null;

        _spawnButton.interactable = true;
        _despawnButton.interactable = false;
        SetStatus("더미 제거 완료");
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null)
            _statusText.text = msg;
    }
}
