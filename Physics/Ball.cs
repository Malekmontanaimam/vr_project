using UnityEngine;
using System.Collections.Generic;
namespace Physics {

public class Ball
{
    public Vector3 center;
    public float radius;
    public int pointCount = 20;
    public BallPhysics physics;
    public List<Spring> springs = new List<Spring>();
    public float springK = 20f;
    
    // خصائص المادة
    public Material material;
    public float deformation = 0f;
    public float maxDeformation = 0.5f;
    private Vector3[] originalPositions;

    public Ball(Vector3 c, float r, float mass, Material mat = null, int pointsNum = 20)
    {
        center = c;
        radius = r;
        pointCount = pointsNum;
        material = mat ?? Material.Plastic; // استخدام البلاستيك كقيمة افتراضية
        
        var points = GeneratePointsOnCircle(center, radius, pointCount, mass / pointCount);
        physics = new BallPhysics(points, mass);
        
        // حفظ المواقع الأصلية للنقاط
        originalPositions = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
            originalPositions[i] = points[i].position;

        // اربط كل نقطة بجارتها بنابض (دائرة)
        for (int i = 0; i < pointCount; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % pointCount];
            springs.Add(new Spring(a, b, springK * material.elasticity));
        }
    }

    public void ApplyDeformation(Vector3 impactPoint, float force)
    {
        if (force < 0.1f) return; // تجاهل القوى الصغيرة
        
        float maxForce = 10f; // أقصى قوة للتشوه
        float normalizedForce = Mathf.Clamp01(force / maxForce);
        
        // تحديث درجة التشوه
        deformation = Mathf.Min(deformation + normalizedForce * 0.1f, maxDeformation);
        
        // تطبيق التشوه على النقاط
        for (int i = 0; i < physics.points.Count; i++)
        {
            var point = physics.points[i];
            float distance = Vector3.Distance(point.position, impactPoint);
            if (distance < radius * 2)
            {
                float deformationFactor = (1 - distance / (radius * 2)) * deformation;
                Vector3 deformationDir = (point.position - impactPoint).normalized;
                point.position = originalPositions[i] + deformationDir * deformationFactor * radius;
            }
        }
    }

    public void ResetDeformation()
    {
        deformation = 0f;
        for (int i = 0; i < physics.points.Count; i++)
        {
            physics.points[i].position = originalPositions[i];
        }
    }

    // توزيع النقاط على دائرة (للتبسيط)
    private List<BallPoint> GeneratePointsOnCircle(Vector3 center, float radius, int count, float pointMass)
    {
        List<BallPoint> pts = new List<BallPoint>();
        for (int i = 0; i < count; i++)
        {
            float angle = 2 * Mathf.PI * i / count;
            float x = center.x + radius * Mathf.Cos(angle);
            float y = center.y + radius * Mathf.Sin(angle);
            float z = center.z;
            pts.Add(new BallPoint(new Vector3(x, y, z), pointMass));
        }
        return pts;
    }

    public Vector3 GetAverageVelocity()
    {
        Vector3 v = Vector3.zero;
        foreach (var p in physics.points)
            v += p.velocity;
        return v / physics.points.Count;
    }

    public void DrawDebugPoints()
    {
        foreach (var p in physics.points)
            Debug.DrawLine(p.position, p.position + Vector3.up * 0.05f, Color.red);
        foreach (var s in springs)
            Debug.DrawLine(s.p1.position, s.p2.position, Color.yellow);
    }
}
}