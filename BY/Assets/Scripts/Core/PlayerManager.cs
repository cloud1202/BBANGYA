using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using static PrefabManager;

public class PlayerManager : SingletonInstance<PlayerManager>, IManager
{
    private Vector2 _direction;
    private PlayerController _player;

    public override void Init() 
    {
        base.Init();
        LoadPlayer().Forget();
    }

    private async UniTask LoadPlayer()
    {
        _player = await PrefabManager.Instance.InstantiateObject<PlayerController>(Prefabs_Data.Player, this.transform);
    }

    void Start()
    {
        InputManager.Instance.SubscribeToPlayerMove(OnMove, true, true, true);
        InputManager.Instance.SubscribeToPlayerInteract(OnInteracted, true, false, false);
        InputManager.Instance.SubscribeToPlayerRightAttack(OnRightAttack, true, false, false);
        InputManager.Instance.SubscribeToPlayerLeftAttack(OnLeftAttack, true, false, false);
        InputManager.Instance.SubscribeToPlayerSprint(OnSprint, true, false, false);
    }

    private bool IsDisableAction => _player == null || _player.isAction;

    public void OnMove(InputAction.CallbackContext context)
    {
        _direction = context.ReadValue<Vector2>();
    }

    public void OnInteracted(InputAction.CallbackContext context)
    {
        if (IsDisableAction)
            return;

        _player = null;
    }

    public void OnRightAttack(InputAction.CallbackContext context)
    {
        if (IsDisableAction)
            return;

        _player.isAction = true;
        _player._prefabs._anim.Rebind();
        _player.SetStateAnimationIndex(PlayerState.DEFENCE, 0);
        EndPlayerAnimation(_player.PlayStateAnimation(PlayerState.DEFENCE)).Forget();
    }

    public void OnLeftAttack(InputAction.CallbackContext context)
    {
        if (IsDisableAction)
            return;

        _player.isAction = true;
        _player._prefabs._anim.Rebind();
        _player.SetStateAnimationIndex(PlayerState.ATTACK, 0);
        EndPlayerAnimation(_player.PlayStateAnimation(PlayerState.ATTACK)).Forget();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (IsDisableAction)
            return;

        _player.DoSprint();
    }

    private async UniTask EndPlayerAnimation(float length)
    {
        await UniTask.WaitForSeconds(length);

        _player.isAction = false;
    }

    private void FixedUpdate()
    {
        OnMoveHandle();
    }

    private void OnMoveHandle()
    {
        if (IsDisableAction)
            return;

        var goalPos = _player.SetMovePos(_direction);
    }
}
