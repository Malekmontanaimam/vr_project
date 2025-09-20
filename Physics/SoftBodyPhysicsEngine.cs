using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Physics {
public class SoftBodyPhysicsEngine
{
    public float totalMass;
    public List<SoftBodyPoint> points;
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float airResistance = 0.05f;
    public float baseDamping = 0.9f;
    public float velocityDamping = 0.8f;
    public float shapeRetentionStiffness = 100f;
    public Vector3 wind = Vector3.zero; 
    public float windStrength = 0f;     
    private Vector3[] originalPositions;
    private List<SoftBodyObject.OptimizedSpring> springs;
    private Dictionary<int, List<int>> pointSpringMap;
    
   
    private Vector3[] forces; 
    private bool[] activePoints; 
    private float lastStabilityCheck = 0f;
    private float stabilityCheckInterval = 0.1f;
    private float velocityThreshold = 0.01f;  

    public SoftBodyPhysicsEngine(List<SoftBodyPoint> pts, float mass, Vector3[] originalPos, 
                               List<SoftBodyObject.OptimizedSpring> objectSprings, Dictionary<int, List<int>> springMap)
    {
        points = pts;
        totalMass = mass;
        originalPositions = originalPos;
        springs = objectSprings;
        pointSpringMap = springMap;
        
        
        forces = new Vector3[points.Count];
        activePoints = new bool[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            activePoints[i] = true;
        }
    }

    public void UpdatePhysicsOptimized(float deltaTime)
    {
        if (points == null || points.Count == 0) return;

        
        if (Time.time - lastStabilityCheck > stabilityCheckInterval)
        {
            UpdateActivePoints();
            lastStabilityCheck = Time.time;
        }

        
        ApplyForcesOptimized(deltaTime);
        
        
        UpdatePositionsAndVelocities(deltaTime);
    }

    private void UpdateActivePoints()
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null) continue;
            
            
            float velocityMagnitude = points[i].velocity.magnitude;
            float distanceFromOriginal = Vector3.Distance(points[i].position, originalPositions[i]);
            
            bool isStable = velocityMagnitude < velocityThreshold && 
                           distanceFromOriginal < 0.1f;
            
            activePoints[i] = !isStable;
        }
    }

    private void ApplyForcesOptimized(float deltaTime)
    {
        
        for (int i = 0; i < forces.Length; i++)
        {
            forces[i] = Vector3.zero;
        }

        
        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            if (!activePoints[pointIndex] || points[pointIndex] == null) continue;
                 if (windStrength > 0.1f)
         Debug.Log($"Wind applied to point {pointIndex}: {  windStrength}");
            
            if (pointSpringMap.ContainsKey(pointIndex))
            {
                var springIndices = pointSpringMap[pointIndex];
                foreach (int springIndex in springIndices)
                {
                    if (springIndex < springs.Count && springs[springIndex] != null)
                    {
                        var spring = springs[springIndex];
                        if (spring.isActive)
                        {
                            
                            ApplySpringForceOptimized(spring, pointIndex, ref forces[pointIndex]);
                        }
                    }
                }
            }
             
            forces[pointIndex] += wind.normalized * windStrength * points[pointIndex].mass;
        
            
            Vector3 shapeRetentionForce = (originalPositions[pointIndex] - points[pointIndex].position) * shapeRetentionStiffness;
            forces[pointIndex] += shapeRetentionForce;

            
            forces[pointIndex] += gravity * points[pointIndex].mass;

            
            forces[pointIndex] -= points[pointIndex].velocity * airResistance * points[pointIndex].mass;

        }
    }

    private void ApplySpringForceOptimized(SoftBodyObject.OptimizedSpring spring, int pointIndex, ref Vector3 force)
    {
        if (spring.p1 == null || spring.p2 == null) return;

        Vector3 dir = spring.p2.position - spring.p1.position;
        float dist = dir.magnitude;
        
        if (dist < 0.001f) return; 
        
        float forceMag = spring.k * (dist - spring.restLength);
        Vector3 springForce = dir.normalized * forceMag;

        
        Vector3 relativeVelocity = spring.p2.velocity - spring.p1.velocity;
        Vector3 dampingForce = dir.normalized * Vector3.Dot(relativeVelocity, dir.normalized) * spring.damping;

        
        if (pointIndex == spring.pointIndex1)
        {
            force += springForce + dampingForce;
        }
        else if (pointIndex == spring.pointIndex2)
        {
            force -= springForce + dampingForce;
        }
    }

    private void UpdatePositionsAndVelocities(float deltaTime)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (!activePoints[i] || points[i] == null) continue;

            var p = points[i];
            
            
            Vector3 acceleration = forces[i] / p.mass;
            
            
            p.velocity += acceleration * deltaTime;
            
           
            float currentDamping = Mathf.Lerp(baseDamping, velocityDamping, 
                Mathf.Clamp01(p.velocity.magnitude / 10f));
            p.velocity *= currentDamping;
            
            
            p.position += p.velocity * deltaTime;

            
            if (float.IsNaN(p.position.x) || float.IsNaN(p.position.y) || float.IsNaN(p.position.z) ||
                float.IsInfinity(p.position.x) || float.IsInfinity(p.position.y) || float.IsInfinity(p.position.z) ||
                Mathf.Abs(p.position.x) > 1000 || Mathf.Abs(p.position.y) > 1000 || Mathf.Abs(p.position.z) > 1000)
            {
                Debug.LogWarning($"Point {i} has invalid position: {p.position}");
                p.position = originalPositions[i];
                p.velocity = Vector3.zero;
                activePoints[i] = false;
            }
        }
    }

}
} 