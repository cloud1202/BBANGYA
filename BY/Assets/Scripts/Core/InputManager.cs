using System;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[ManagerOrder(3)]
public class InputManager : SingletonInstance<InputManager>, IManager
{
    private PlayerInput _inputHandler;
    public override void Init()
    {
        base.Init();
        _inputHandler = new PlayerInput();
        _inputHandler.Player.Enable();
        _inputHandler.UI.Enable();
    }

    public void SubscribeToInputHandler(InputType type, 
        Action<CallbackContext> start = null, 
        Action<CallbackContext> perform = null, 
        Action<CallbackContext> cancel = null)
    {
        InputAction input = null;
        switch (type)
        {
            case InputType.Player_Move:
                input = _inputHandler.Player.Move;
                break;
            case InputType.Player_RightAttack:
                input = _inputHandler.Player.RightAttack;
                break;
            case InputType.Player_LeftAttack:
                input = _inputHandler.Player.LeftAttack;
                break;
            case InputType.Player_Interact:
                input = _inputHandler.Player.Interact;
                break;
            case InputType.Player_Sprint:
                input = _inputHandler.Player.Sprint;
                break;
            case InputType.Player_Jump:
                input = _inputHandler.Player.Jump;
                break;
            case InputType.Player_Aim:
                input = _inputHandler.Player.Aim;
                break;
            case InputType.UI_Setting:
                input = _inputHandler.UI.Setting;
                break;
            default:
                return;
        }

        if (input == null)
            return;

        if (start != null)
            input.started += start;

        if (perform != null)
            input.performed += perform;

        if (cancel != null)
            input.canceled += cancel;
    }
}
