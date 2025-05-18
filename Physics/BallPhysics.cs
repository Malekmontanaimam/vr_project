using UnityEngine;
using System.Collections.Generic;
namespace Physics {

public class BallPhysics
{
    public float totalMass;
    public List<BallPoint> points;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float airResistance = 0.1f;
    public float baseDamping = 0.98f;
    public float velocityDamping = 0.95f;

    public BallPhysics(List<BallPoint> pts, float mass)
    {
        points = pts;
        totalMass = mass;
    }

    public void UpdatePhysics(float deltaTime, List<Spring> springs)
    {
        // 1. طبق قوى النوابض
        foreach (var s in springs)
            s.ApplyForce();

        // 2. تحديث القوى والحركة لكل نقطة
        foreach (var p in points)
        {
            // إضافة الجاذبية
            p.force += gravity * p.mass;
            
            // إضافة مقاومة الهواء
            if (p.velocity.magnitude > 0.1f)
            {
                Vector3 airResistanceForce = -p.velocity.normalized * 
                    p.velocity.sqrMagnitude * airResistance;
                p.force += airResistanceForce;
            }

            // حساب التسارع
            Vector3 acceleration = p.force / p.mass;
            
            // تحديث السرعة
            p.velocity += acceleration * deltaTime;
            
            // تخميد متغير حسب السرعة
            float currentDamping = Mathf.Lerp(baseDamping, velocityDamping, 
                p.velocity.magnitude / 10f);
            p.velocity *= currentDamping;
            
            // تحديث الموقع
            p.position += p.velocity * deltaTime;
            
            // حماية من NaN و Infinity
            if (float.IsNaN(p.position.x) || float.IsNaN(p.position.y) || float.IsNaN(p.position.z) ||
                float.IsInfinity(p.position.x) || float.IsInfinity(p.position.y) || float.IsInfinity(p.position.z) ||
                Mathf.Abs(p.position.x) > 1000 || Mathf.Abs(p.position.y) > 1000 || Mathf.Abs(p.position.z) > 1000)
            {
                Debug.LogWarning("BallPoint boom! " + p.position);
                p.position = Vector3.zero;
                p.velocity = Vector3.zero;
            }
            
            // تصفير القوة بعد التحديث
            p.force = Vector3.zero;
        }
    }
}
}