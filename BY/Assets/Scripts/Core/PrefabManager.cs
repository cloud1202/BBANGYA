using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using static AssetReferenceBase<PrefabManager.Prefabs_Data, UnityEngine.GameObject>;


[ManagerOrder(3)]
public class PrefabManager : SingletonInstance<PrefabManager>, IManager
{
    private PrefabAssetReference _assetReference;
    private Dictionary<Prefabs_Data, AssetResource> _objectAssetMap = new Dictionary<Prefabs_Data, AssetResource>();
    public enum Prefabs_Data
    {
        Player,
        Ground,
        LobbyCanvas,
    }

    public override void Init()
    {
        base.Init();
    }

    async public UniTask LoadAssetReference()
    {
        _assetReference = await AddressableManager.Instance.LoadResourceData<PrefabAssetReference>(nameof(PrefabAssetReference));
        AssetReferenceMapping();
        await PreloadAssets(ContainLabel.Common);
    }

    private void AssetReferenceMapping()
    {
        foreach (var obj in _assetReference.assetDatas)
        {
            if (!_objectAssetMap.ContainsKey(obj.id))
            {
                _objectAssetMap.Add(obj.id, obj);
            }
        }
    }

    public async UniTask PreloadAssets(ContainLabel label)
    {
        List<AssetResource> assets = new List<AssetResource>();

        foreach (var obj in _assetReference.assetDatas)
        {
            if ((obj.containLabel & label) > 0)
            {
                assets.Add(obj);
            }
        }
        Debug.Log($"{assets.Count}");
        await AddressableManager.Instance.PreloadAssets(label, assets.ToArray());
    }

    public async UniTask<T> InstantiateObject<T>(Prefabs_Data data, Transform parent = null, bool isProtected = false) where T : Object
    {
        if (_objectAssetMap.TryGetValue(data, out var obj) == false)
        {
            return default;
        }

        if (parent == null)
            parent = this.transform;

        return await AddressableManager.Instance.Instantiate<T>(obj, parent, isProtected);
    }
}
