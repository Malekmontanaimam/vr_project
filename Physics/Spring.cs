using UnityEngine;
namespace Physics {
public class Spring
{
    public BallPoint p1, p2;
    public float restLength;
    public float k; // ثابت النابض

    public Spring(BallPoint a, BallPoint b, float springK)
    {
        p1 = a;
        p2 = b;
        restLength = Vector3.Distance(a.position, b.position);
        k = springK;
    }

    public void ApplyForce()
    {
        Vector3 dir = p2.position - p1.position;
        float dist = dir.magnitude;
        float forceMag = k * (dist - restLength);
        Vector3 force = dir.normalized * forceMag;

        // طبق القوة على النقاط (نابض)
        p1.force += force;
        p2.force -= force;
    }
}
}