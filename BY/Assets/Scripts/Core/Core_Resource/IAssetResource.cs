using UnityEngine.AddressableAssets;

public interface IAssetResource
{
    public AssetReference assetRef { get; }
    public UnityEngine.Object instance { get; set; }
    public bool isInstance { get; }
}
