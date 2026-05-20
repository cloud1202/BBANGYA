using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private const float CHARACTER_SPEED = 5;
    public SPUM_Prefabs _prefabs;
    private PlayerState _currentState;

    private Vector3 _goalPos;
    private Vector3 _lastDirection;
    public bool isAction { get; set; } = false;
    public Dictionary<PlayerState, int> IndexPair = new();


    void Start()
    {
        if (_prefabs == null)
        {
            _prefabs = transform.GetChild(0).GetComponent<SPUM_Prefabs>();
            if (!_prefabs.allListsHaveItemsExist())
            {
                _prefabs.PopulateAnimationLists();
            }
        }
        _prefabs.OverrideControllerInit();
        foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
        {
            IndexPair[state] = 0;
        }
    }
    public void SetStateAnimationIndex(PlayerState state, int index = 0)
    {

        IndexPair[state] = index;
    }
    public float PlayStateAnimation(PlayerState state)
    {
        return _prefabs.PlayAnimation(state, IndexPair[state]);
    }
    void FixedUpdate()
    {
        if (isAction) return;

        transform.position = new Vector3(transform.position.x, transform.position.y, transform.localPosition.y * 0.01f);
        switch (_currentState)
        {
            case PlayerState.IDLE:

                break;

            case PlayerState.MOVE:
                DoMove();
                break;
        }
        PlayStateAnimation(_currentState);

    }

    void DoMove()
    {
        Vector3 _dirVec = _goalPos - transform.position;
        Vector3 _disVec = (Vector2)_goalPos - (Vector2)transform.position;
        if (_disVec.sqrMagnitude < 0.1f)
        {
            _currentState = PlayerState.IDLE;
            return;
        }
        Vector3 _dirMVec = _dirVec.normalized;
        transform.position += _dirMVec * CHARACTER_SPEED * Time.deltaTime;

        if (_dirMVec.x > 0) _prefabs.transform.localScale = new Vector3(-1, 1, 1);
        else if (_dirMVec.x < 0) _prefabs.transform.localScale = new Vector3(1, 1, 1);
    }

    public void DoSprint()
    {
        isAction = true;
        _prefabs._anim.Rebind();
        SetStateAnimationIndex(PlayerState.SPRINT, 0);
        float animTime = PlayStateAnimation(PlayerState.SPRINT);
        transform.DOMove(transform.position + (_lastDirection * 2f), animTime).SetEase(Ease.InOutSine);
        EndPlayerAnimation(animTime).Forget();
    }

    public Vector2 SetMovePos(Vector2 pos)
    {
        isAction = false;
        _lastDirection = pos;
        _goalPos = transform.position + _lastDirection;

        if (pos == Vector2.zero)
            _lastDirection = new Vector2(Mathf.Sign(_prefabs.transform.localScale.x) * -1f, 0);
        _currentState = PlayerState.MOVE;
        return _goalPos;
    }

    private async UniTask EndPlayerAnimation(float length)
    {
        await UniTask.WaitForSeconds(length);

        isAction = false;
    }

}
