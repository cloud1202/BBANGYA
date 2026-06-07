using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager : SingletonInstance<AddressableManager>
{
    private Dictionary<ContainLabel, List<AsyncOperationHandle>> _loadHandles = new Dictionary<ContainLabel, List<AsyncOperationHandle>>();
    private List<GameObject> _instantiateHandles = new List<GameObject>();

    public override void Init()
    {
        base.Init();
    }

    public async UniTask SetAddressable()
    {
        await Addressables.InitializeAsync();
        await LoadRemoteAddressable();
    }

    private async UniTask LoadRemoteAddressable()
    {
        long downloadSize = await Addressables.GetDownloadSizeAsync("Resources");
        if (downloadSize == 0)
        {
            Logging($"Not Found Download Addressable");
            return;
        }
        var downloadHandle = Addressables.DownloadDependenciesAsync("Resources", false);
        float progress = 0;

        while (downloadHandle.Status == AsyncOperationStatus.None)
        {
            float percentageComplete = downloadHandle.GetDownloadStatus().Percent;
            if (percentageComplete > progress * 1.1) // Report at most every 10% or so
            {
                progress = percentageComplete; // More accurate %
                Logging($"Progress :: {progress} Download Size :: {downloadHandle.GetDownloadStatus().DownloadedBytes} / {downloadSize}");
                //UpdateLoadGauage(progress);
            }

            await UniTask.NextFrame();
        }

        CompletionAddressableLoad(downloadHandle.Status == AsyncOperationStatus.Succeeded);
        downloadHandle.Release(); //Release the operation handle
    }

    private void UpdateLoadGauage(float progress)
    {
        Logging($"Progress :: {progress}");
    }

    private void CompletionAddressableLoad(bool complete)
    {
        Logging($"Complete :: {complete}");
    }

    public void AssetReleaseForLabel(ContainLabel label)
    {
        if (_loadHandles.TryGetValue(label, out List<AsyncOperationHandle> handles) == false)
            return;

        Logging($"Release {label} Load Handle Count : {handles.Count}");

        foreach (var handle in handles)
        {
            Logging($"{handle.DebugName}");
            handle.Release();
        }

        _loadHandles.Remove(label);

        Logging($"Clear {label} Load Handle Count : {handles.Count}");
    }

    public void InstantiateRelease()
    {
        Logging($"Release InstantiateRelease Handle Count : {_instantiateHandles.Count}");

        foreach (var handle in _instantiateHandles)
        {
            if (handle == null)
                continue;
            Logging($"{handle.name}");
            GameObject.Destroy(handle);
        }

        _instantiateHandles.Clear();

        Logging($"Clear InstantiateRelease Handle Count : {_instantiateHandles.Count}");
    }

    public async UniTask PreloadAssets(ContainLabel label, IAssetResource[] assetResources)
    {
        if (_loadHandles.ContainsKey(label) == false)
            _loadHandles.Add(label, new List<AsyncOperationHandle>());

        List<UniTask<UnityEngine.Object>> handles = new List<UniTask<UnityEngine.Object>>();

        Logging($"PreLoad Asset : {label}");
        int length = assetResources.Length;
        for (int index = 0; index < length; ++index)
        {
            if (assetResources[index].assetRef.OperationHandle.IsValid())
                continue;
            var loadHandle = assetResources[index].assetRef.LoadAssetAsync<UnityEngine.Object>();
            _loadHandles[label].Add(loadHandle);
            handles.Add(loadHandle.ToUniTask());
            loadHandle.Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    Logging($"loadHandle Asset : {handle.Result.ToString()}");
                }
                else
                {
                    Logging($"Error Load Asset : {handle.OperationException}");
                }
            };
        }

        await UniTask.WhenAll(handles);
        Logging($"Complete PreLoad Assets");
    }

    public async UniTask<T> Load<T>(AssetReference assetRef) where T : UnityEngine.Object
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
        {
            Warning($"AssetReference가 유효하지 않습니다.");
            return null;
        }

        if (assetRef.OperationHandle.IsValid() && assetRef.OperationHandle.IsDone &&
            assetRef.OperationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            T preloadedResult = assetRef.OperationHandle.Result as T;
            if (preloadedResult != null)
            {
                Logging("LoadPreloadResult");
                return preloadedResult;
            }
        }

        AsyncOperationHandle newLoadHandle = assetRef.LoadAssetAsync<T>();

        await newLoadHandle.ToUniTask();

        if (newLoadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Logging("newLoadHandle");
            T preloadedResult = newLoadHandle.Result as T;
            return preloadedResult;
        }

        Error($"Failed to load '{assetRef.RuntimeKey}': {newLoadHandle.OperationException}");
        return null;
    }

    public async UniTask<T> Instantiate<T>(IAssetResource assetResource, Transform parent, bool isProtected)
        where T : UnityEngine.Object
    {
        //await Load<GameObject>(assetRef);

        AsyncOperationHandle instantiateHandle = assetResource.assetRef.InstantiateAsync(parent);
        await instantiateHandle.ToUniTask();

        var go = instantiateHandle.Result as GameObject;
        var obj = go.AddComponent<InstantiateObject>();
        assetResource.instance = go;
        if (isProtected == false)
            _instantiateHandles.Add(go);

        obj.SetAssetReference(assetResource);

        if(typeof(T) == typeof(GameObject))
            return go as T;
        else
            return go.GetComponent<T>();
    }


    public async UniTask<T> LoadResourceData<T>(string name)
    {
        return await Addressables.LoadAssetAsync<T>($"Assets/AddressableAssets/ScriptableObject/{name}.asset");
    }
}
