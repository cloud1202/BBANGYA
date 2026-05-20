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
        Player_Jump,
    }
    private PlayerInput _inputHandler;
    public override void Init()
    {
        base.Init();
        _inputHandler = new PlayerInput();
        _inputHandler.Player.Enable();
    }

    private void SubscribeToInputHandler(InputType type, Action<CallbackContext> start, Action<CallbackContext> perform, Action<CallbackContext> cancel)
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
                input = _inputHandler.Player.Sprint;
                break;
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

    public void SubscribeToPlayerMove(Action<CallbackContext> start = null, Action<CallbackContext> perform = null, Action<CallbackContext> cancel = null)
    {
        SubscribeToInputHandler(InputType.Player_Move, start, perform, cancel);
    }

    public void SubscribeToPlayerInteract(Action<CallbackContext> start = null, Action<CallbackContext> perform = null, Action<CallbackContext> cancel = null)
    {
        SubscribeToInputHandler(InputType.Player_Interact, start, perform, cancel);
    }

    public void SubscribeToPlayerRightAttack(Action<CallbackContext> start = null, Action<CallbackContext> perform = null, Action<CallbackContext> cancel = null)
    {
        SubscribeToInputHandler(InputType.Player_RightAttack, start, perform, cancel);
    }

    public void SubscribeToPlayerLeftAttack(Action<CallbackContext> start = null, Action<CallbackContext> perform = null, Action<CallbackContext> cancel = null)
    {
        SubscribeToInputHandler(InputType.Player_LeftAttack, start, perform, cancel);
    }

    public void SubscribeToPlayerSprint(Action<CallbackContext> start = null, Action<CallbackContext> perform = null, Action<CallbackContext> cancel = null)
    {
        SubscribeToInputHandler(InputType.Player_Sprint, start, perform, cancel);
    }

    public void SubscribeToPlayerJump(Action<CallbackContext> start = null, Action<CallbackContext> perform = null, Action<CallbackContext> cancel = null)
    {
        SubscribeToInputHandler(InputType.Player_Jump, start, perform, cancel);
    }
}
