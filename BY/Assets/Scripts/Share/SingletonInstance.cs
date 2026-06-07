using UnityEngine;

public class SingletonInstance<T> : MonoBehaviour
    where T : Component
{
    private static T m_instance;
    public static T Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindAnyObjectByType<T>();

                if (m_instance == null)
                {
                    Debug.Log($"Created instance Manager of: {typeof(T)}");
                    GameObject go = new GameObject();
                    go.name = $"{typeof(T)}(Singletone)";
                    m_instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }


            return m_instance;
        }
    }
    
    public static bool IsCreatedInstance() => m_instance != null;

    private void Awake()
    {
        Init();
    }

    public virtual void Init()
    {
        if (m_instance == null)
        {
            m_instance = FindAnyObjectByType<T>();
            DontDestroyOnLoad(m_instance.gameObject);
        }
        else
            DestroyImmediate(gameObject);
    }
    protected void Logging(string log)
    {
        Debug.Log($"<color=yellow>[{typeof(T)}]</color> {log}");
    }
    protected void Warning(string log)
    {
        Debug.LogWarning($"[{typeof(T)}] {log}");
    }

    protected void Error(string log)
    {
        Debug.LogError($"[{typeof(T)}] {log}");
    }
}
