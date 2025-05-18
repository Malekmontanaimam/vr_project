using UnityEngine;
using Physics;

public class BallController : MonoBehaviour
{
    public float mass = 4f;
    public float radius = 0.5f;
    public int pointsCount = 20;

    public Ball myBall;
    
    
    public Vector3 windDirection = Vector3.right;
    public float windStrength = 0f;
    public float maxWindStrength = 10f;
    public bool showDebugInfo = true;
    public bool isDragging = false;

    void Start()
    {
        myBall = new Ball(transform.position, radius, mass, Physics.Material.Wood, pointsCount);
    }

    void Update()
    {
        if (isDragging) return;
        // تحديث قوة الرياح
        Vector3 windForce = windDirection.normalized * windStrength;
        myBall.physics.UpdateWindForce(windForce);

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

        // تطبيق الدوران على الكرة
        transform.Rotate(myBall.physics.angularVelocity * Time.deltaTime);

        // رسم النقاط والنوابض
        myBall.DrawDebugPoints();

        // عرض معلومات التصحيح
        if (showDebugInfo)
        {
            Debug.Log($"Wind Force: {windForce.magnitude:F2} N");
            Debug.Log($"Angular Velocity: {myBall.physics.angularVelocity.magnitude:F2} rad/s");
            Debug.Log($"Linear Velocity: {myBall.GetAverageVelocity().magnitude:F2} m/s");
        }
    }

    // إضافة قوة خارجية (يمكن استدعاؤها من أي مكان)
    public void ApplyForce(Vector3 force)
    {
        myBall.physics.AddExternalForce(force);
    }

    // إزالة قوة خارجية
    public void RemoveForce(Vector3 force)
    {
        myBall.physics.RemoveExternalForce(force);
    }
}