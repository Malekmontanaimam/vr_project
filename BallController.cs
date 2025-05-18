using UnityEngine;
using Physics;

public class BallController : MonoBehaviour
{
    public float mass = 4f;
    public float radius = 0.5f;
    public int pointsCount = 20;

    public Ball myBall;

    void Start()
    {
        myBall = new Ball(transform.position, radius, mass, Physics.Material.Wood, pointsCount);
    }

    void Update()
    {
        // تحديث فيزياء النقاط والنوابض
        myBall.physics.UpdatePhysics(Time.deltaTime, myBall.springs);

        // تصادم النقاط مع الأرضية
        foreach (var p in myBall.physics.points)
        {
            if (p.position.y < radius)
            {
                p.position.y = radius;
                if (p.velocity.y < 0)
                    p.velocity.y *= -0.7f;
            }
        }

        // حساب مركز الكرة الجديد (متوسط مواقع النقاط)
        Vector3 newCenter = Vector3.zero;
        foreach (var p in myBall.physics.points)
            newCenter += p.position;
        if (myBall.physics.points.Count > 0)
            newCenter /= myBall.physics.points.Count;
        // حماية من NaN
        if (float.IsNaN(newCenter.x) || float.IsNaN(newCenter.y) || float.IsNaN(newCenter.z))
            return;

        myBall.center = newCenter;
        transform.position = newCenter;

        // رسم النقاط والنوابض
        myBall.DrawDebugPoints();
    }
}