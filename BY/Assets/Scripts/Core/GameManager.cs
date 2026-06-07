using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;

public class GameManager : SingletonInstance<GameManager>, IManager
{
    private Dictionary<string, IPlayer> _players = new Dictionary<string, IPlayer>();

    async public UniTask Bootstrap()
    {
        await AddressableManager.Instance.SetAddressable();
        await PrefabManager.Instance.LoadAssetReference();
        await PrefabManager.Instance.LoadCanvas();
        await PrefabManager.Instance.LoadLobbyUI();
        await PlanetManager.Instance.SpawnPlanets();
    }

    public async UniTask StartGame()
    {
        //await PlayerManager.Instance.SpawnLocalPlayer();
    }

    public async UniTask OnClientJoined()
    {
        await PlayerManager.Instance.SpawnLocalPlayer();
    }

    public void RegistPlayer(IPlayer player)
    {
        _players.Add(player.Name, player);
    }

    public void UnregistPlayer(IPlayer player)
    {
        _players.Remove(player.Name);
    }

    public void AttackPlayer(string uid)
    {
        _players[uid].OnDead();
    }
}
