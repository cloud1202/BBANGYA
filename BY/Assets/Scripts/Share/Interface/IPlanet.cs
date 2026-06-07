using UnityEngine;
public interface IPlanet
{
    public Vector2 surface { get;}
    public Vector2 GetForce(Vector3 target);
}
