using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

[ManagerOrder(3)]
public class InputManager : SingletonInstance<InputManager>, IManager
{
    private enum InputType
    { 
        // Player
        Player_Move,
        Player_Interact,
        Player_RightAttack,
        Player_LeftAttack,
        Player_Sprint,
    }
    private PlayerInput _inputHandler;
    public override void Init()
    {
        base.Init();
        _inputHandler = new PlayerInput();
        _inputHandler.Player.Enable();
    }

    private void SubscribeToInputHandler(InputType type, Action<CallbackContext> action, bool isStart, bool isPerform, bool isCancel)
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
        }

        if (input == null)
            return;

        if (isStart)
            input.started += action;

        if (isPerform)
            input.performed += action;

        if (isCancel)
            input.canceled += action;
    }

    public void SubscribeToPlayerMove(Action<CallbackContext> action, bool isStart, bool isPerform, bool isCancel)
    {
        SubscribeToInputHandler(InputType.Player_Move, action, isStart, isPerform, isCancel);
    }

    public void SubscribeToPlayerInteract(Action<CallbackContext> action, bool isStart, bool isPerform, bool isCancel)
    {
        SubscribeToInputHandler(InputType.Player_Interact, action, isStart, isPerform, isCancel);
    }

    public void SubscribeToPlayerRightAttack(Action<CallbackContext> action, bool isStart, bool isPerform, bool isCancel)
    {
        SubscribeToInputHandler(InputType.Player_RightAttack, action, isStart, isPerform, isCancel);
    }

    public void SubscribeToPlayerLeftAttack(Action<CallbackContext> action, bool isStart, bool isPerform, bool isCancel)
    {
        SubscribeToInputHandler(InputType.Player_LeftAttack, action, isStart, isPerform, isCancel);
    }

    public void SubscribeToPlayerSprint(Action<CallbackContext> action, bool isStart, bool isPerform, bool isCancel)
    {
        SubscribeToInputHandler(InputType.Player_Sprint, action, isStart, isPerform, isCancel);
    }
}
