using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;

[ManagerOrder(2)]
public class NetworkGameManager : SingletonInstance<NetworkGameManager>, IManager
{
    private int maxPlayers = 2;

    public UnityEvent OnLobbyCreated = new UnityEvent();
    public UnityEvent OnLobbyJoined = new UnityEvent();
    public UnityEvent OnGameStarted = new UnityEvent();
    public UnityEvent<string> OnError = new UnityEvent<string>();

    private Lobby _currentLobby;
    private string _lobbyCode;
    private bool _isHost;
    private bool _isServiceReady = false;

    private float _heartbeatTimer;
    private const float HeartbeatInterval = 15f;

    public override void Init()
    {
        base.Init();
        InitializeServices().Forget();
    }

    // ─────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────
    private async UniTaskVoid InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _isServiceReady = true;
            Debug.Log($"[NGM] 로그인 완료 | PlayerId: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGM] 서비스 초기화 실패: {e.Message}");
            OnError?.Invoke(e.Message);
        }
    }

    private async UniTask WaitForServiceReady()
    {
        await UniTask.WaitUntil(() => _isServiceReady);
    }

    // ─────────────────────────────────────────
    // 호스트: 방 만들기
    // ─────────────────────────────────────────
    public async Task CreateLobbyAsHost(string lobbyName = "Game Room")
    {
        try
        {
            await WaitForServiceReady();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            _lobbyCode = _currentLobby.LobbyCode;
            _isHost = true;

            SetRelayHostData(allocation);
            NetworkManager.Singleton.StartHost();

            Debug.Log($"[NGM] 로비 생성 완료 | 코드: {_lobbyCode}");
            OnLobbyCreated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGM] 방 생성 실패: {e.Message}");
            OnError?.Invoke(e.Message);
        }
    }

    // ─────────────────────────────────────────
    // 클라이언트: 코드로 참가
    // ─────────────────────────────────────────
    public async Task JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            await WaitForServiceReady();

            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            _isHost = false;

            await ConnectAsClient(_currentLobby);

            Debug.Log($"[NGM] 코드로 참가 완료 | 코드: {lobbyCode}");
            OnLobbyJoined?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGM] 방 참가 실패: {e.Message}");
            OnError?.Invoke(e.Message);
        }
    }

    // ─────────────────────────────────────────
    // 빠른 참가: 빈 방 자동 검색 후 참가
    // ─────────────────────────────────────────
    public async Task QuickJoin()
    {
        try
        {
            await WaitForServiceReady();

            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0"
                    )
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(asc: true, field: QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            if (response.Results.Count > 0)
            {
                Debug.Log($"[NGM] 빈 방 발견! 자동 참가 중...");
                _currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(response.Results[0].Id);
                _isHost = false;

                await ConnectAsClient(_currentLobby);

                Debug.Log($"[NGM] 빠른 참가 완료!");
                OnLobbyJoined?.Invoke();
            }
            else
            {
                Debug.Log($"[NGM] 빈 방 없음 → 방 생성 후 대기");
                await CreateLobbyAsHost();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGM] 빠른 참가 실패: {e.Message}");
            OnError?.Invoke(e.Message);
        }
    }

    // ─────────────────────────────────────────
    // 클라이언트 연결 공통 로직
    // ─────────────────────────────────────────
    private async UniTask ConnectAsClient(Lobby lobby)
    {
        string relayJoinCode = lobby.Data["RelayJoinCode"].Value;
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        SetRelayClientData(joinAllocation);
        NetworkManager.Singleton.StartClient();

        await UniTask.WaitUntil(() => NetworkManager.Singleton.IsConnectedClient);
    }

    // ─────────────────────────────────────────
    // Relay 설정
    // ─────────────────────────────────────────
    private void SetRelayHostData(Allocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );
    }

    private void SetRelayClientData(JoinAllocation joinAllocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData
        );
    }

    // ─────────────────────────────────────────
    // 하트비트
    // ─────────────────────────────────────────
    void Update()
    {
        if (_isHost && _currentLobby != null)
        {
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HeartbeatInterval)
            {
                _heartbeatTimer = 0f;
                LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
        }
    }

    // ─────────────────────────────────────────
    // 종료
    // ─────────────────────────────────────────
    public async Task LeaveLobby()
    {
        try
        {
            if (_currentLobby != null)
            {
                if (_isHost)
                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                else
                    await LobbyService.Instance.RemovePlayerAsync(
                        _currentLobby.Id,
                        AuthenticationService.Instance.PlayerId
                    );

                _currentLobby = null;
            }

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
                NetworkManager.Singleton.Shutdown();
        }
        catch (Exception e)
        {
            Debug.LogError($"[NGM] 로비 나가기 실패: {e.Message}");
        }
    }

    public string GetLobbyCode() => _lobbyCode;
    public bool IsHost() => _isHost;

    async void OnApplicationQuit()
    {
        await LeaveLobby();
    }
}
