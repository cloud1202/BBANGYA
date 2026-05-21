using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static PrefabManager;

[ManagerOrder(5)]
public class PlayerManager : SingletonInstance<PlayerManager>, IManager
{
    private Vector2 _direction;
    private IPlayer _player;

    public override void Init()
    {
        base.Init();
    }

    void Start()
    {
        InputManager.Instance.SubscribeToPlayerMove(OnMove, OnMove, OnMove);
        InputManager.Instance.SubscribeToPlayerRightAttack(OnRightAttack, null, OnRightAttackCancel);
        InputManager.Instance.SubscribeToPlayerLeftAttack(OnLeftAttack);
        InputManager.Instance.SubscribeToPlayerSprint(OnSprint);
        InputManager.Instance.SubscribeToPlayerJump(OnJump);
        InputManager.Instance.SubscribeToPlayerAim(OnAim, OnAim, OnAim);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // ─────────────────────────────────────────
    // 서버 콜백: 클라이언트 접속 시 캐릭터 스폰
    // ─────────────────────────────────────────
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log($"[PlayerManager] OnClientConnected | clientId: {clientId}");
        SpawnPlayerOnServer(clientId).Forget();
    }

    // ─────────────────────────────────────────
    // 서버에서 플레이어 스폰
    // ─────────────────────────────────────────
    public async UniTask SpawnPlayerOnServer(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Vector3 spawnPos = (clientId == NetworkManager.Singleton.LocalClientId)
            ? new Vector3(-5f, 0f, 0f)
            : new Vector3(5f, 0f, 0f);

        InstantiateObject playerObj = await PrefabManager.Instance.InstantiateObject<InstantiateObject>(Prefabs_Data.Player, null);

        if (playerObj == null)
        {
            Debug.LogError("[PlayerManager] Player 프리팹 로드 실패!");
            return;
        }

        playerObj.transform.position = spawnPos;

        NetworkObject networkObj = playerObj.GetComponent<NetworkObject>();
        networkObj.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[PlayerManager] 스폰 완료 | clientId: {clientId} | pos: {spawnPos}");
    }

    // ─────────────────────────────────────────
    // 호스트: 게임 시작 시 본인 스폰
    // ─────────────────────────────────────────
    public async UniTask SpawnLocalPlayer()
    {
        await UniTask.WaitUntil(() => NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsConnectedClient);

        if (NetworkManager.Singleton.IsHost)
            await SpawnPlayerOnServer(NetworkManager.Singleton.LocalClientId);
    }

    // ─────────────────────────────────────────
    // 로컬 플레이어 참조 등록
    // ─────────────────────────────────────────
    public void RegisterLocalPlayer(IPlayer player)
    {
        _player = player;
        Debug.Log($"[PlayerManager] 로컬 플레이어 등록 완료");
    }

    // ─────────────────────────────────────────
    // 입력 핸들러
    // ─────────────────────────────────────────
    public void OnMove(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _direction = context.ReadValue<Vector2>();
    }

    public void OnRightAttack(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _player.DoHolding();
    }

    public void OnRightAttackCancel(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _player.DoHoldingCancel();
    }

    public void OnLeftAttack(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _player.DoAttack();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _player.DoSprint();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _player.DoJump();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (_player == null) return;
        _player.SetCursorPoint(context.ReadValue<Vector2>());
    }

    private void FixedUpdate()
    {
        if (_player == null) return;
        _player.SetMoveDir(_direction);
    }
}
