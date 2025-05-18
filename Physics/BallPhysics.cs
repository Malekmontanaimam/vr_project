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

    
    public Vector3 windForce = Vector3.zero;
    public Vector3 angularVelocity = Vector3.zero;
    public float momentOfInertia = 1f;
    public List<Vector3> externalForces = new List<Vector3>();
    public float airDensity = 1.225f; // كثافة الهواء
    public float dragCoefficient = 0.47f; // معامل السحب للكرة

    public BallPhysics(List<BallPoint> pts, float mass)
    {
        points = pts;
        totalMass = mass;
        momentOfInertia = (2f/5f) * mass * Mathf.Pow(0.5f, 2); // لحظة القصور الذاتي للكرة
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
            
            // إضافة قوة الرياح
            p.force += windForce * p.mass;

            // إضافة القوى الخارجية
            foreach (var force in externalForces)
                p.force += force * p.mass;

            // تحسين مقاومة الهواء باستخدام معادلة السحب
            if (p.velocity.magnitude > 0.1f)
            {
                float crossSectionalArea = Mathf.PI * Mathf.Pow(0.5f, 2); // مساحة المقطع العرضي للكرة
                float dragForce = 0.5f * airDensity * p.velocity.sqrMagnitude * dragCoefficient * crossSectionalArea;
                Vector3 airResistanceForce = -p.velocity.normalized * dragForce;
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

            // تطبيق الدوران
            Vector3 rotationForce = Vector3.Cross(p.position - Vector3.zero, p.velocity);
            angularVelocity += rotationForce * deltaTime / momentOfInertia;
            angularVelocity *= 0.98f; // تخميد الدوران
            
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

    // إضافة قوة خارجية
    public void AddExternalForce(Vector3 force)
    {
        externalForces.Add(force);
    }

    // إزالة قوة خارجية
    public void RemoveExternalForce(Vector3 force)
    {
        externalForces.Remove(force);
    }

    // تحديث قوة الرياح
    public void UpdateWindForce(Vector3 newWindForce)
    {
        windForce = newWindForce;
    }
}
}