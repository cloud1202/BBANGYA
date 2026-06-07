using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ReferenceManager<T, E> : SingletonInstance<T>
    where T : MonoBehaviour
    where E : Enum
{
    private AssetReferenceBase<E, GameObject> _assetReference;
    protected Dictionary<E, IAssetResource> _assetMap = new Dictionary<E, IAssetResource>();
    public override void Init()
    {
        base.Init();
    }

    async public UniTask LoadAssetReference()
    {
        _assetReference = await AddressableManager.Instance.LoadResourceData<AssetReferenceBase<E,GameObject>>(nameof(PrefabAssetReference));
        AssetReferenceMapping();
        await PreloadAssets(ContainLabel.Common);
    }

    private void AssetReferenceMapping()
    {
        foreach (var obj in _assetReference.assetDatas)
        {
            if (!_assetMap.ContainsKey(obj.id))
            {
                _assetMap.Add(obj.id, obj);
            }
        }
    }

    public async UniTask PreloadAssets(ContainLabel label)
    {
        List<IAssetResource> assets = new List<IAssetResource>();

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

    public async UniTask<TI> InstantiateObject<TI>(E data, Transform parent = null, bool isProtected = false) where TI : UnityEngine.Object
    {
        if (_assetMap.TryGetValue(data, out var obj) == false)
        {
            return default;
        }

        if (parent == null)
            parent = this.transform;

        return await AddressableManager.Instance.Instantiate<TI>(obj, parent, isProtected);
    }
}
