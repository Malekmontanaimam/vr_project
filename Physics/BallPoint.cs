using UnityEngine;
namespace Physics {
public class BallPoint
{
    public Vector3 position;
    public float mass;
    public Vector3 velocity;
    public Vector3 force;

    public BallPoint(Vector3 pos, float m)
    {
        position = pos;
        mass = m;
        velocity = Vector3.zero;
        force = Vector3.zero;
    }
}
}