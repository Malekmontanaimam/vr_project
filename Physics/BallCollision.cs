using UnityEngine;
using System.Collections.Generic;
namespace Physics {

public static class BallCollision
{
    public static void ResolveBallCollision(Ball ballA, Ball ballB)
    {
        Vector3 delta = ballB.center - ballA.center;
        float dist = delta.magnitude;
        float minDist = ballA.radius + ballB.radius;

        if (dist < minDist)
        {
            Vector3 normal = delta.normalized;
            Vector3 velA = ballA.GetAverageVelocity();
            Vector3 velB = ballB.GetAverageVelocity();

            Vector3 relVel = velA - velB;
            float velAlongNormal = Vector3.Dot(relVel, normal);

            if (velAlongNormal > 0) return;

            // حساب معامل الارتداد المشترك
            float restitution = Mathf.Min(ballA.material.elasticity, ballB.material.elasticity);
            
            float invMassA = 1f / ballA.physics.totalMass;
            float invMassB = 1f / ballB.physics.totalMass;
            float j = -(1 + restitution) * velAlongNormal;
            j /= invMassA + invMassB;

            Vector3 impulse = j * normal;

            // تطبيق الدفع
            foreach (var p in ballA.physics.points)
                p.velocity += impulse * invMassA / ballA.physics.points.Count;
            foreach (var p in ballB.physics.points)
                p.velocity -= impulse * invMassB / ballB.physics.points.Count;

            // تصحيح التداخل
            float penetration = minDist - dist;
            Vector3 correction = normal * (penetration / 2f);
            foreach (var p in ballA.physics.points)
                p.position -= correction;
            foreach (var p in ballB.physics.points)
                p.position += correction;

            // حساب قوة التصادم
            float impactForce = Mathf.Abs(j) / Time.fixedDeltaTime;
            
            // تطبيق التشوه
            Vector3 impactPoint = ballA.center + normal * ballA.radius;
            ballA.ApplyDeformation(impactPoint, impactForce);
            ballB.ApplyDeformation(impactPoint, impactForce);

            // تطبيق الاحتكاك
            Vector3 tangent = relVel - normal * velAlongNormal;
            if (tangent.magnitude > 0.001f)
            {
                tangent.Normalize();
                float jt = -Vector3.Dot(relVel, tangent);
                jt /= invMassA + invMassB;

                // معامل الاحتكاك المشترك
                float mu = Mathf.Sqrt(ballA.material.friction * ballB.material.friction);
                Vector3 frictionImpulse;
                if (Mathf.Abs(jt) < j * mu)
                    frictionImpulse = jt * tangent;
                else
                    frictionImpulse = -j * tangent * mu;

                // تطبيق قوة الاحتكاك
                foreach (var p in ballA.physics.points)
                    p.velocity += frictionImpulse * invMassA / ballA.physics.points.Count;
                foreach (var p in ballB.physics.points)
                    p.velocity -= frictionImpulse * invMassB / ballB.physics.points.Count;
            }
        }
    }
}
}