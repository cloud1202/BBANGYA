using UnityEngine;

public class Planet : MonoBehaviour, IPlanet
{
    private float _gravityStrength = 150f;
    public Vector2 surface { get; private set; } = Vector2.zero;


    private void Awake()
    {
        PlanetManager.Instance.RegisterPlanet(this);
    }

    public Vector2 GetForce(Vector3 target)
    {
        Vector2 dir = transform.position - target;

        float distance = dir.magnitude;
        if (distance > 10)
            return Vector2.zero;
        float force = _gravityStrength / (distance * distance);
        surface = new Vector2(-dir.y, dir.x);
        return dir.normalized * force;
    }
}
