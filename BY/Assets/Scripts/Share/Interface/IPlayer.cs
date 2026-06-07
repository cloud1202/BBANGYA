using UnityEngine;

public interface IPlayer
{
    string Name { get; }
    bool isAction { get; set; }
    bool isGround { get; set; }
    bool isDummy { get; set; }

    void SetPlayerInfo(string name);
    void SetMoveDir(Vector2 moveDir);
    void UpdateMove();
    void SetCursorPoint(Vector2 pos);
    void DoAttack();
    void DoHolding();
    void DoHoldingCancel();
    void DoSprint();
    void DoJump();
    void OnDead();
}
