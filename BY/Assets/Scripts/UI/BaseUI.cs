using UnityEngine;

public class BaseUI : MonoBehaviour, IBaseUI
{
    private void Awake()
    {
        Init();
    }

    public virtual void Init()
    {

    }
}
