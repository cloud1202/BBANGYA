using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerController : NetworkBehaviour, IPlayer
{
    [SerializeField] private  float CHARACTER_SPEED = 1.5f;
    [SerializeField] private float _force = 2.5f;
    [SerializeField] private float _damping = 1.5f;
    [SerializeField] private SPUM_Prefabs _prefabs;
    [SerializeField] private Transform _revolver;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Transform _weaponeTr;
    [SerializeField] private Transform _bodyTr;

    private PlayerState _currentState;
    public PlayerState currentState { get => _currentState; }

    private Vector3 _goalPos;
    private Vector3 _lastDirection;
    public bool isAction { get; set; } = false;
    public bool isGround { get; set; } = false;
    // 더미 여부
    private bool _isDummy = false;
    public bool isDummy 
    {
        get => _isDummy; 
        set {
            if (_isDummy == value)
                return;

            _isDummy = value;
            if(_isDummy)
                gameObject.AddComponent<DummyController>();
        }
    }

    public string Name => gameObject.name;

    public Dictionary<PlayerState, int> IndexPair = new();

    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private CinemachineConfiner2D _bounding;
    [SerializeField] private Light2D _light;
    [SerializeField] private Rigidbody2D _rb2D;
    [SerializeField] private Bullet _bullet;
    [SerializeField] private Queue<Bullet> _bullets = new Queue<Bullet>();
    private Vector2 _cursorDir;
    private float _currentWeaponAngle = 0f;

    private Tween _floatTween;
    private Vector3 _floatOriginPos;

    private float _recoilAngleOffset = 0f;
    private bool _isRecoiling = false;


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

    public NetworkVariable<float> IsForward = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<float> WeaponAngle = new NetworkVariable<float>(
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
        IsForward.OnValueChanged += OnIsForwardChanged;
        WeaponAngle.OnValueChanged += OnWeaponAngleChanged;

        if (IsOwner)
        {
            gameObject.name = NetworkManager.Singleton.LocalClientId.ToString();
            InitOwner();

            if (!_isDummy)
            {
                _bounding.BoundingShape2D = PlanetManager.Instance.bounding;
                PlayerManager.Instance.RegisterLocalPlayer(this);
                GameManager.Instance.RegistPlayer(this);
                _camera.enabled = true;
            }
            else
            {
                gameObject.name = $"Dummy_{NetworkManager.Singleton.LocalClientId.ToString()}";
                // 더미는 카메라 비활성화
                if (_camera != null) _camera.enabled = false;
                if (_light != null) _light.enabled = false;
                Debug.Log("[PlayerController] 더미 캐릭터 스폰 완료");
            }
        }
        else
        {
            if (_camera != null) _camera.enabled = false;
            if (_light != null) _light.enabled = false;
            if (_rb2D != null) _rb2D.bodyType = RigidbodyType2D.Kinematic;

            ApplyFacing(IsFacingRight.Value);
            ApplyIsForward(IsForward.Value);
            _currentWeaponAngle = WeaponAngle.Value;
        }
    }

    public override void OnNetworkDespawn()
    {
        IsFacingRight.OnValueChanged -= OnFacingChanged;
        IsForward.OnValueChanged -= OnIsForwardChanged;
        WeaponAngle.OnValueChanged -= OnWeaponAngleChanged;
        _floatTween?.Kill();
        PlayerManager.Instance.RegisterLocalPlayer(null);
        base.OnNetworkDespawn();
    }

    private void OnFacingChanged(bool oldVal, bool newVal) { if (!IsOwner) ApplyFacing(newVal); }
    private void OnIsForwardChanged(float oldVal, float newVal) { if (!IsOwner) ApplyIsForward(newVal); }
    private void OnWeaponAngleChanged(float oldVal, float newVal) { if (!IsOwner) _currentWeaponAngle = newVal; }

    private void ApplyFacing(bool facingRight)
    {
        _prefabs.transform.localScale = facingRight ? new Vector3(-1, 1, 1) : new Vector3(1, 1, 1);
    }

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
        if (_rb2D != null)
        {
            _rb2D.gravityScale = 0f;
            _rb2D.linearDamping = _damping;
            _rb2D.angularDamping = 5f;
        }

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
        var force = PlanetManager.Instance.GetTotalForce(transform.position);
        _rb2D.AddForce(force);

        var dir = (force - (Vector2)_prefabs.transform.up).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _prefabs.transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

        switch (currentState)
        {
            // MOVE에서 바꾸는중 IDLE로 들오는 일이 없음
            case PlayerState.IDLE:
                IndexPair[currentState] = isGround ? 0 : 1;
                break;
            case PlayerState.MOVE: DoMove(); break;
            case PlayerState.ATTACK: _currentState = PlayerState.IDLE; break;
        }
        PlayStateAnimation(currentState);
    }

    void LateUpdate()
    {
        if (_weaponeTr == null) return;
        float finalAngle = _currentWeaponAngle + _recoilAngleOffset;
        _weaponeTr.rotation = Quaternion.Euler(0f, 0f, finalAngle);
        _light.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle + 180f);
    }

    // ─────────────────────────────────────────
    // 애니메이션 동기화
    // ─────────────────────────────────────────
    [ServerRpc]
    private void PlayAnimationServerRpc(PlayerState state, int index)
    {
        PlayAnimationClientRpc(state, index);
    }

    [ClientRpc]
    private void PlayAnimationClientRpc(PlayerState state, int index)
    {
        if (IsOwner) return;
        if (_prefabs == null) return;
        PlayAnimation(state, index);
    }

    private void PlayStateAnimation(PlayerState state)
    {
        PlayAnimation(state, IndexPair[state]);
        if (IsOwner && IsSpawned)
        {
            if (IsHost)
                PlayAnimationClientRpc(state, IndexPair[state]);
            else
                PlayAnimationServerRpc(state, IndexPair[state]);
        }
    }

    private void PlayAnimation(PlayerState state, int index)
    {
        float len = _prefabs.PlayAnimation(state, index);
        switch (state)
        {
            case PlayerState.ATTACK:
                _currentState = PlayerState.ATTACK;
                Shooting();
                EndPlayerAnimation(len).Forget();
                break;
            case PlayerState.SPRINT:
                EndPlayerAnimation(len).Forget();
                break;
            case PlayerState.JUMP:
                _currentState = PlayerState.JUMP;
                break;
        }
    }

    [ServerRpc]
    public void TakeDamageServerRpc()
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

        _dirMVec = -Vector2.Perpendicular(_prefabs.transform.up) * _dirMVec.x;
        transform.position += _dirMVec * CHARACTER_SPEED * Time.deltaTime;

        float isForwardVal = (_dirMVec.x > 0 ^ _cursorDir.x > 0) ? 0f : 1f;
        if (Mathf.Abs(IsForward.Value - isForwardVal) > 0.01f)
            IsForward.Value = isForwardVal;
        ApplyIsForward(isForwardVal);
    }

    public void DoAttack()
    {
        if (!IsOwner) return;
        if (isAction && currentState != PlayerState.JUMP) return;

        isAction = true;
        SetStateAnimationIndex(PlayerState.ATTACK, 0);
        PlayStateAnimation(PlayerState.ATTACK);
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
        PlayStateAnimation(PlayerState.SPRINT);
        _rb2D.AddForce((Vector2)_lastDirection.normalized * _force * 3f, ForceMode2D.Impulse);
    }

    public void DoJump()
    {
        if (!IsOwner) return;
        if (!isGround) return;
        _rb2D.AddForce(_prefabs.transform.up * _force * 2f, ForceMode2D.Impulse);
    }

    public void SetMoveDir(Vector2 moveDir)
    {
        if (!IsOwner || isAction) return;
        if (!isGround) return;
        _lastDirection = moveDir;
        _goalPos = transform.position + _lastDirection;
        if (_lastDirection == Vector3.zero)
            _lastDirection = new Vector2(Mathf.Sign(_prefabs.transform.localScale.x) * -1f, 0);
        _currentState = PlayerState.MOVE;
    }

    // ─────────────────────────────────────────
    // 더미 전용 이동 (isGround 체크 없음)
    // ─────────────────────────────────────────
    public void SetDummyMoveDir(Vector2 moveDir)
    {
        if (!IsOwner) return;
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

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(pos);
        worldPos.z = 0f;

        Vector2 dir = (worldPos - _weaponeTr.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (_prefabs.transform.localScale.x < 0)
            angle = angle + 115f;
        else
            angle = angle + 65f;

        _currentWeaponAngle = angle;

        if (Mathf.Abs(Mathf.DeltaAngle(WeaponAngle.Value, angle)) > 0.5f)
            WeaponAngle.Value = angle;

        _cursorDir = dir;
        float dot = Vector2.Dot(dir, Vector2.Perpendicular(_prefabs.transform.up));
        bool facingRight = dot < 0;
        if (IsFacingRight.Value != facingRight) IsFacingRight.Value = facingRight;
        ApplyFacing(facingRight);
    }

    private void SetStateAnimationIndex(PlayerState state, int index = 0) => IndexPair[state] = index;

    private async UniTask EndPlayerAnimation(float length)
    {
        await UniTask.WaitForSeconds(length);
        isAction = false;
    }

    private void Shooting()
    {
        if (!IsOwner) return;
        if (currentState != PlayerState.IDLE && currentState != PlayerState.ATTACK) return;

        Bullet bullet = _bullets.Count == 0 ? Instantiate(_bullet, this.transform) : _bullets.Dequeue();
        if (_bullets.Count == 0) bullet.Init(gameObject.name, Reload);

        bullet.Firing(_muzzle, (_muzzle.position - _revolver.position).normalized);
        _rb2D.AddForce((_revolver.position - _muzzle.position).normalized * _force, ForceMode2D.Impulse);

        PlayRecoilEffect().Forget();
    }

    private async UniTaskVoid PlayRecoilEffect()
    {
        if (_isRecoiling) return;
        _isRecoiling = true;

        DOTween.To(() => _recoilAngleOffset, x => _recoilAngleOffset = x, 25f, 0.05f).SetEase(Ease.OutQuad);
        await UniTask.WaitForSeconds(0.05f);

        DOTween.To(() => _recoilAngleOffset, x => _recoilAngleOffset = x, 0f, 0.2f).SetEase(Ease.OutElastic);
        await UniTask.WaitForSeconds(0.2f);

        _isRecoiling = false;
    }

    private void Reload(Bullet bullet) => _bullets.Enqueue(bullet);

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Wall") == false) return;
        var dir = (collision.transform.position - transform.position).normalized;
        _rb2D.AddForce(dir * _force * 2f, ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
        isGround = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;
        isGround = false;
    }

    public void OnDead()
    {
        Debug.Log($"Dead Player : {Name}");
        if (IsServer) GameManager.Instance.UnregistPlayer(this);
        if (IsOwner) GetComponent<NetworkObject>().Despawn();
    }
}
