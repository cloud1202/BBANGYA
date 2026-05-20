using UnityEngine;
using UnityEngine.AddressableAssets;

public class InstantiateObject : MonoBehaviour
{
    private AssetReference m_assetRef;

    public void SetAssetReference(AssetReference assetRef)
    {
        m_assetRef = assetRef;
    }
    private void OnDestroy()
    {
        m_assetRef.ReleaseAsset();
        m_assetRef.ReleaseInstance(gameObject);
    }
}
