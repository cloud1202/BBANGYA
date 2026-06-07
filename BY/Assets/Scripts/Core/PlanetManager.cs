using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetManager : SingletonInstance<PlanetManager>, IManager
{
    private List<IPlanet> _planets = new List<IPlanet>();
    public BoxCollider2D bounding { get; private set; }

    public override void Init() 
    {
        base.Init();
    }

    async public UniTask SpawnPlanets()
    {
        var planets = await PrefabManager.Instance.InstantiateObject<InstantiateObject>(PrefabData.Ground);
        _planets = planets.GetComponentsInChildren<IPlanet>().ToList();
        bounding = planets.GetComponentInChildren<BoxCollider2D>();
    }

    public void RegisterPlanet(IPlanet planet) 
    {
        _planets.Add(planet);
    }

    public Vector2 GetTotalForce(Vector3 target)
    {
        Vector2 totalForce = Vector2.zero;

        foreach (var planet in _planets)
        {

            totalForce += planet.GetForce(target);
        }

        return totalForce;
    }
}
