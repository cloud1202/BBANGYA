using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private const int DEFAULT_ORDER = 999;
    void Awake()
    {
        LoadAllManagers();
    }

    private void LoadAllManagers()
    {
        Type managerInterface = typeof(IManager);

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var allManagerTypes = new List<(Type Type, int Order)>();

        foreach (Assembly assembly in assemblies)
        {
            try
            {
                IEnumerable<Type> managerTypes = assembly.GetTypes()
                    .Where(t => managerInterface.IsAssignableFrom(t)
                             && t.IsClass                          
                             && !t.IsAbstract                      
                             && t.GetConstructor(Type.EmptyTypes) != null);
                foreach (Type type in managerTypes)
                {
                    ManagerOrderAttribute orderAttr = type.GetCustomAttribute<ManagerOrderAttribute>();
                    int order = orderAttr != null ? orderAttr.Order : DEFAULT_ORDER;

                    allManagerTypes.Add((type, order));
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Debug.LogError($"Error loading types from assembly {assembly.GetName().Name}: {ex.Message}");
                foreach (Exception loaderEx in ex.LoaderExceptions)
                {
                    Debug.LogError($"Loader Exception: {loaderEx.Message}");
                }
            }
        }

        var sortedManagerTypes = allManagerTypes.OrderBy(item => item.Order);

        foreach (var item in sortedManagerTypes)
        {
            Type type = item.Type;
            Debug.Log($"[{item.Order}] Created instance of: {type.Name}");

            GameObject go = new GameObject();
            go.name = $"{type.Name}(Singleton)";
            go.AddComponent(type);
        }

        GameManager.Instance.Bootstrap().Forget();
    }
}
