using UnityEngine;
namespace Physics {
public class SoftBodyPoint
{
    public Vector3 position;
    public Vector3 previousPosition;
    public float mass;
    public Vector3 velocity;
    public Vector3 force;

    public SoftBodyPoint(Vector3 pos, float m)
    {
        position = pos;
        previousPosition = pos;
        mass = m;
        velocity = Vector3.zero;
        force = Vector3.zero;
    }
}
}