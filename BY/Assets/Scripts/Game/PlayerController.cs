using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IPlayer
{
    private const float CHARACTER_SPEED = 5;
    [SerializeField] private float _force = 5;
    [SerializeField] private SPUM_Prefabs _prefabs;
    [SerializeField] private Transform _revoler;
    [SerializeField] private Transform _muzzle;
    private PlayerState _currentState;
    public PlayerState currentState { get => _currentState; }

    private Vector3 _goalPos;
    private Vector3 _lastDirection;
    public bool isAction { get; set; } = false;
    public bool isGround { get; set; } = false;

    public string Name => gameObject.name;

    public Dictionary<PlayerState, int> IndexPair = new();

    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private Rigidbody2D _rb2D;
    [SerializeField] private Bullet _bullet;
    [SerializeField] private Queue<Bullet> _bullets = new Queue<Bullet>();
    private Vector2 _cursorDir;

    public NetworkVariable<bool> IsDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsFacingRight = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<float> AimPosY = new NetworkVariable<float>(
        0.5f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<float> IsForward = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // ─────────────────────────────────────────
    // Spawn 시 초기화
    // ─────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitVisual();

        IsFacingRight.OnValueChanged += OnFacingChanged;
        AimPosY.OnValueChanged += OnAimPosYChanged;
        IsForward.OnValueChanged += OnIsForwardChanged;

        if (IsOwner)
        {
            InitOwner();
            gameObject.name = NetworkManager.Singleton.LocalClientId.ToString();
            PlayerManager.Instance.RegisterLocalPlayer(this);
            GameManager.Instance.RegistPlayer(this);
            _camera.enabled = true;
            Debug.Log($"[PlayerController] 내 캐릭터 스폰 완료 | clientId: {NetworkManager.Singleton.LocalClientId}");
        }
        else
        {
            _camera.enabled = false;
            if (_rb2D != null) _rb2D.bodyType = RigidbodyType2D.Kinematic;

            ApplyFacing(IsFacingRight.Value);
            ApplyAimPosY(AimPosY.Value);
            ApplyIsForward(IsForward.Value);
            Debug.Log($"[PlayerController] 상대 캐릭터 스폰 완료");
        }
    }

    public override void OnNetworkDespawn()
    {
        IsFacingRight.OnValueChanged -= OnFacingChanged;
        AimPosY.OnValueChanged -= OnAimPosYChanged;
        IsForward.OnValueChanged -= OnIsForwardChanged;
        base.OnNetworkDespawn();
    }

    private void OnFacingChanged(bool oldVal, bool newVal) { if (!IsOwner) ApplyFacing(newVal); }
    private void OnAimPosYChanged(float oldVal, float newVal) { if (!IsOwner) ApplyAimPosY(newVal); }
    private void OnIsForwardChanged(float oldVal, float newVal) { if (!IsOwner) ApplyIsForward(newVal); }

    private void ApplyFacing(bool facingRight)
    {
        _prefabs.transform.localScale = facingRight ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
    }

    private void ApplyAimPosY(float posY) => _prefabs._anim.SetFloat("PosY", posY);
    private void ApplyIsForward(float value) => _prefabs._anim.SetFloat("IsForward", value);

    private void InitVisual()
    {
        if (_prefabs == null)
            _prefabs = transform.GetChild(0).GetComponent<SPUM_Prefabs>();

        if (!_prefabs.allListsHaveItemsExist())
            _prefabs.PopulateAnimationLists();

        _prefabs.OverrideControllerInit();

        foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
            IndexPair[state] = 0;
    }

    private void InitOwner()
    {
        for (int i = 0; i < 10; ++i)
        {
            Bullet bullet = Instantiate(_bullet, this.transform);
            bullet.Init(gameObject.name, Reload);
            _bullets.Enqueue(bullet);
        }
    }

    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            InitVisual();
            InitOwner();
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner && (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)) return;
        if (isAction) return;

        transform.position = new Vector3(transform.position.x, transform.position.y, transform.localPosition.y * 0.01f);

        switch (currentState)
        {
            case PlayerState.IDLE: break;
            case PlayerState.MOVE: DoMove(); break;
        }
        PlayStateAnimation(currentState);
    }

    // ─────────────────────────────────────────
    // 애니메이션 동기화
    // ─────────────────────────────────────────
    [ServerRpc]
    private void PlayAnimationServerRpc(int stateInt, int index)
    {
        PlayAnimationClientRpc(stateInt, index);
    }

    [ClientRpc]
    private void PlayAnimationClientRpc(int stateInt, int index)
    {
        if (IsOwner) return;
        PlayerState state = (PlayerState)stateInt;
        if (_prefabs != null)
            _prefabs.PlayAnimation(state, index);
    }

    private float PlayStateAnimation(PlayerState state)
    {
        float len = _prefabs.PlayAnimation(state, IndexPair[state]);

        if (IsOwner && IsSpawned)
        {
            if (IsHost)
                PlayAnimationClientRpc((int)state, IndexPair[state]);
            else
                PlayAnimationServerRpc((int)state, IndexPair[state]);
        }

        switch (state)
        {
            //case PlayerState.HOLDING: _currentState = PlayerState.HOLDING; break;
            case PlayerState.ATTACK:
                Shooting().Forget();
                _currentState = PlayerState.ATTACK;
                break;
            case PlayerState.JUMP: _currentState = PlayerState.JUMP; break;
        }
        return len;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        if (IsDead.Value) return;
        IsDead.Value = true;
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc() => OnDead();

    public void SetPlayerInfo(string name)
    {
        if (!IsOwner) return;
        gameObject.name = name;
    }

    void DoMove()
    {
        Vector3 _dirVec = _goalPos - transform.position;
        if (_dirVec.sqrMagnitude < 0.1f) { _currentState = PlayerState.IDLE; return; }

        Vector3 _dirMVec = _dirVec.normalized;
        if (_dirMVec.x == 0) { _currentState = PlayerState.IDLE; return; }

        _dirMVec = new Vector3(_dirMVec.x, 0, 0);
        transform.position += _dirMVec * CHARACTER_SPEED * Time.deltaTime;

        float isForwardVal = (_dirMVec.x > 0 ^ _cursorDir.x > 0) ? 0f : 1f;
        if (Mathf.Abs(IsForward.Value - isForwardVal) > 0.01f)
            IsForward.Value = isForwardVal;
        ApplyIsForward(isForwardVal);
    }

    public void DoAttack()
    {
        if (!IsOwner) return;
        if (isAction && currentState != PlayerState.HOLDING && currentState != PlayerState.JUMP) return;

        int index = currentState == PlayerState.HOLDING ? 1 : 0;
        isAction = true;
        SetStateAnimationIndex(PlayerState.ATTACK, index);
        EndPlayerAnimation(PlayStateAnimation(PlayerState.ATTACK)).Forget();
    }

    public void DoHolding()
    {
        if (!IsOwner || isAction) return;
        isAction = true;
        SetStateAnimationIndex(PlayerState.HOLDING, 0);
        PlayStateAnimation(PlayerState.HOLDING);
    }

    public void DoHoldingCancel()
    {
        if (!IsOwner || currentState != PlayerState.HOLDING) return;
        isAction = false;
        SetStateAnimationIndex(PlayerState.HOLDING, 0);
        PlayStateAnimation(PlayerState.HOLDING);
    }

    public void DoSprint()
    {
        if (!IsOwner) return;
        isAction = true;
        _prefabs._anim.Rebind();
        SetStateAnimationIndex(PlayerState.SPRINT, 0);
        float animTime = PlayStateAnimation(PlayerState.SPRINT);
        transform.DOMove(transform.position + (_lastDirection * 2f), animTime).SetEase(Ease.InOutSine);
        EndPlayerAnimation(animTime).Forget();
    }

    public void DoJump()
    {
        if (!IsOwner || isAction || !isGround) return;
        isAction = true;
        _prefabs._anim.Rebind();
        SetStateAnimationIndex(PlayerState.JUMP, 0);
        float animTime = PlayStateAnimation(PlayerState.JUMP);
        _rb2D.AddForce(((_goalPos - transform.position).normalized + Vector3.up) * CHARACTER_SPEED, ForceMode2D.Impulse);
        LandingPlayerGround().Forget();
    }

    public void SetMoveDir(Vector2 moveDir)
    {
        if (!IsOwner || isAction) return;
        _lastDirection = moveDir;
        _goalPos = transform.position + _lastDirection;
        if (_lastDirection == Vector3.zero)
            _lastDirection = new Vector2(Mathf.Sign(_prefabs.transform.localScale.x) * -1f, 0);
        _currentState = PlayerState.MOVE;
    }

    public void UpdateMove()
    {
        if (!IsOwner || isAction) return;
        _goalPos = transform.position + _lastDirection;
        if (_lastDirection == Vector3.zero)
            _lastDirection = new Vector2(Mathf.Sign(_prefabs.transform.localScale.x) * -1f, 0);
        _currentState = PlayerState.MOVE;
    }

    public void SetCursorPoint(Vector2 pos)
    {
        if (!IsOwner) return;
        _cursorDir = (Camera.main.ScreenToWorldPoint(pos) - transform.position).normalized;

        bool facingRight = _cursorDir.x > 0;
        if (IsFacingRight.Value != facingRight) IsFacingRight.Value = facingRight;
        ApplyFacing(facingRight);

        float posY = Mathf.InverseLerp(0, Screen.height, pos.y);
        if (Mathf.Abs(AimPosY.Value - posY) > 0.01f) AimPosY.Value = posY;
        ApplyAimPosY(posY);
    }

    private void SetStateAnimationIndex(PlayerState state, int index = 0) => IndexPair[state] = index;

    private async UniTask EndPlayerAnimation(float length)
    {
        await UniTask.WaitForSeconds(length);
        isAction = false;
    }

    private async UniTask LandingPlayerGround()
    {
        isGround = false;
        await UniTask.WaitUntil(() => isGround);
        isAction = false;
    }

    async private UniTask Shooting()
    {
        if (!IsOwner) return;

        int delay = 0;
        if (currentState != PlayerState.IDLE && currentState != PlayerState.HOLDING && currentState != PlayerState.JUMP) return;
        if (currentState == PlayerState.IDLE || currentState == PlayerState.JUMP) delay = 5;

        await UniTask.DelayFrame(delay);

        Bullet bullet = _bullets.Count == 0 ? Instantiate(_bullet, this.transform) : _bullets.Dequeue();
        if (_bullets.Count == 0) bullet.Init(gameObject.name, Reload);

        bullet.Firing(_muzzle, (_muzzle.position - _revoler.position).normalized);

        if (delay > 0)
            _rb2D.AddForce((_revoler.position - _muzzle.position).normalized * _force, ForceMode2D.Impulse);
    }

    private void Reload(Bullet bullet) => _bullets.Enqueue(bullet);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
        isGround = true;
    }

    public void OnDead()
    {
        if (IsServer) GameManager.Instance.UnregistPlayer(this);
        if (IsOwner) GetComponent<NetworkObject>().Despawn();
    }
}
