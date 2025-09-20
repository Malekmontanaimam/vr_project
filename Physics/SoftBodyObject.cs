using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Physics {
public class SoftBodyObject
{
    public Vector3 center;
    public List<SoftBodyPoint> points;
    public List<SoftBodyObject.OptimizedSpring> springs = new List<SoftBodyObject.OptimizedSpring>();
    public float springK = 3f;
    public float bendingSpringK = 0.1f;
    public Material material;
    public float deformation = 0f;
    public float maxDeformation = 1.0f;
    public float baseDamping = 0.9f;
    public float velocityDamping = 0.8f;
    public float shapeRetentionStiffness = 5f;
    private Vector3[] originalPositions;
    public SoftBodyPhysicsEngine physics;
    private float averageEdgeLength = 1f;
    public bool isRoundShape = false;
    public float boundingRadius = 0f;
    
    private Dictionary<int, List<int>> pointSpringMap;
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.016f;
    private int maxIterations = 3;
    private bool useSpatialHashing = true;
    private SpatialHash spatialHash;

    public SoftBodyObject(Vector3[] vertices, int[] triangles, float mass, Material mat = null, bool isRound = true)
    {
        material = mat ?? Material.Metal;
        isRoundShape = isRound;
        
        points = new List<SoftBodyPoint>();
        float pointMass = mass / vertices.Length;
        for (int i = 0; i < vertices.Length; i++)
        {
            points.Add(new SoftBodyPoint(vertices[i], pointMass));
        }

        CreateOptimizedSpringsFromMesh(triangles);
        CreatePointSpringMap();

        originalPositions = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
            originalPositions[i] = points[i].position;

        center = Vector3.zero;
        foreach (var point in points)
            center += point.position;
        center /= points.Count;

        boundingRadius = CalculateBoundingRadius(vertices);
        physics = new SoftBodyPhysicsEngine(points, mass, originalPositions, springs, pointSpringMap);
        if (useSpatialHashing)
        {
            spatialHash = new SpatialHash(2.0f);
        }
    }

    private void CreateOptimizedSpringsFromMesh(int[] triangles)
    {
        springs = new List<SoftBodyObject.OptimizedSpring>();
        var springEdges = new HashSet<(int, int)>();
        var edgeOppositeVerts = new Dictionary<(int, int), List<int>>();

        float totalEdgeLength = 0f;
        int edgeCount = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            AddOptimizedSpring(i1, i2, springEdges, ref totalEdgeLength, ref edgeCount);
            AddOptimizedSpring(i2, i3, springEdges, ref totalEdgeLength, ref edgeCount);
            AddOptimizedSpring(i3, i1, springEdges, ref totalEdgeLength, ref edgeCount);

            AddOppositeVertex(i1, i2, i3, edgeOppositeVerts);
            AddOppositeVertex(i2, i3, i1, edgeOppositeVerts);
            AddOppositeVertex(i3, i1, i2, edgeOppositeVerts);
        }

        averageEdgeLength = edgeCount > 0 ? totalEdgeLength / edgeCount : 1f;

        foreach (var edge in edgeOppositeVerts.Keys)
        {
            var oppositeVerts = edgeOppositeVerts[edge];
            if (oppositeVerts.Count == 2)
            {
                AddOptimizedBendingSpring(oppositeVerts[0], oppositeVerts[1], springEdges);
            }
        }
    }

    private void AddOptimizedSpring(int index1, int index2, HashSet<(int, int)> springEdges, 
                                   ref float totalEdgeLength, ref int edgeCount)
    {
        var key = index1 < index2 ? (index1, index2) : (index2, index1);
        if (!springEdges.Contains(key))
        {       
            springEdges.Add(key);
            SoftBodyPoint p1 = points[index1];
            SoftBodyPoint p2 = points[index2];
            float restLength = Vector3.Distance(p1.position, p2.position);
            
            totalEdgeLength += restLength;
            edgeCount++;
            
            springs.Add(new SoftBodyObject.OptimizedSpring(p1, p2, springK * material.elasticity, restLength, index1, index2));
        }
    }

    private void AddOptimizedBendingSpring(int index1, int index2, HashSet<(int, int)> springEdges)
    {
        var key = index1 < index2 ? (index1, index2) : (index2, index1);
        if (!springEdges.Contains(key))
        {
            springEdges.Add(key);
            SoftBodyPoint p1 = points[index1];
            SoftBodyPoint p2 = points[index2];
            float restLength = Vector3.Distance(p1.position, p2.position);
            springs.Add(new SoftBodyObject.OptimizedSpring(p1, p2, bendingSpringK * material.elasticity, restLength, index1, index2, true));
        }
    }

    private void CreatePointSpringMap()
    {
        pointSpringMap = new Dictionary<int, List<int>>();
        
        for (int i = 0; i < springs.Count; i++)
        {
            var spring = springs[i];
            if (!pointSpringMap.ContainsKey(spring.pointIndex1))
                pointSpringMap[spring.pointIndex1] = new List<int>();
            if (!pointSpringMap.ContainsKey(spring.pointIndex2))
                pointSpringMap[spring.pointIndex2] = new List<int>();
                
            pointSpringMap[spring.pointIndex1].Add(i);
            pointSpringMap[spring.pointIndex2].Add(i);
        }
    }

    private void AddOppositeVertex(int v1, int v2, int opposite, Dictionary<(int, int), List<int>> edgeMap)
    {
        var key = v1 < v2 ? (v1, v2) : (v2, v1);
        if (!edgeMap.ContainsKey(key))
        {
            edgeMap[key] = new List<int>();
        }
        edgeMap[key].Add(opposite);
    }

    private float CalculateBoundingRadius(Vector3[] vertices)
    {
        Vector3 center = Vector3.zero;
        foreach(Vector3 v in vertices) center += v;
        center /= vertices.Length;

        float maxDist = 0f;
        foreach(Vector3 v in vertices)
        {
            float dist = Vector3.Distance(v, center);
            if(dist > maxDist) maxDist = dist;
        }
        return maxDist;
    }

    public void UpdatePhysics(float deltaTime)
    {
        if (Time.time - lastUpdateTime < updateInterval)
            return;
            
        lastUpdateTime = Time.time;

        if (useSpatialHashing && spatialHash != null)
        {
            spatialHash.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                spatialHash.Insert(points[i].position, i);
            }
        }

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            physics.UpdatePhysicsOptimized(deltaTime / maxIterations);
        }

        UpdateCenter();
    }

    private void UpdateCenter()
    {
        if (points.Count == 0) return;
        
        Vector3 totalPosition = Vector3.zero;
        foreach (var point in points)
        {
            totalPosition += point.position;
        }
        center = totalPosition / points.Count;
    }

    public void ApplyDeformation(Vector3 impactPoint, float force)
    {
        if(force < 0.1f) return;
        
        float maxForce = 10f;
        float normalizedForce = Mathf.Clamp01(force / maxForce);
        deformation = Mathf.Min(deformation + normalizedForce * 0.1f, maxDeformation);
        
        float maxDeformationRadius = isRoundShape ? 
            boundingRadius * 0.8f : 
            averageEdgeLength * 3.0f;
        
        for(int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            float distance = Vector3.Distance(point.position, impactPoint);
            if(distance < maxDeformationRadius)
            {
                float t = 1 - (distance / maxDeformationRadius);
                float deformationFactor = Mathf.Pow(t, 2) * deformation;
                
                Vector3 deformationDir = (point.position - impactPoint).normalized;
                point.position = originalPositions[i] + 
                                deformationDir * 
                                deformationFactor * 
                                maxDeformationRadius * 0.5f;
            }
        }
    }

    public void ResetDeformation()
    {
        deformation = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            points[i].position = originalPositions[i];
        }
    }

    public Vector3 GetAverageVelocity()
    {
        if (points.Count == 0) return Vector3.zero;

        Vector3 v = Vector3.zero;
        foreach (var p in points)
            v += p.velocity;
        return v / points.Count;
    }

    public void DrawDebugPoints()
    {
        int step = Mathf.Max(1, springs.Count / 200);
        for (int i = 0; i < springs.Count; i += step)
        {
            var spring = springs[i];
            Color color = spring.isBendingSpring ? Color.cyan : Color.yellow;
            Debug.DrawLine(spring.p1.position, spring.p2.position, color);
        }
        
        step = Mathf.Max(1, points.Count / 50);
        for (int i = 0; i < points.Count; i += step)
        {
            var p = points[i];
            Debug.DrawLine(p.position, p.position + Vector3.up * 0.05f, Color.red);
        }
    }

    public Bounds GetAABB()
    {
        if (points == null || points.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);
        Vector3 min = points[0].position;
        Vector3 max = points[0].position;
        foreach (var p in points)
        {
            min = Vector3.Min(min, p.position);
            max = Vector3.Max(max, p.position);
        }
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    public List<Vector3> GetConvexHullPoints()
{
    List<Vector3> hull = new List<Vector3>();
    if (points == null || points.Count < 4)
    {
        foreach (var p in points) hull.Add(p.position);
        return hull;
    }
    Vector3 center = Vector3.zero;
    foreach (var p in points) center += p.position;
    center /= points.Count;
    float maxDist = 0f;
    foreach (var p in points)
    {
        float d = Vector3.Distance(p.position, center);
        if (d > maxDist) maxDist = d;
    }
    // خذ كل النقاط التي تبعد أكثر من 95% من المسافة القصوى
    foreach (var p in points)
    {
        float d = Vector3.Distance(p.position, center);
        if (d > 0.95f * maxDist)
            hull.Add(p.position);
    }
    return hull;
}

public class OptimizedSpring
{
    public SoftBodyPoint p1, p2;
    public float restLength;
    public float k;
    public float damping = 0.1f;
    public int pointIndex1, pointIndex2;
    public bool isBendingSpring = false;
    public bool isActive = true;

    public OptimizedSpring(SoftBodyPoint a, SoftBodyPoint b, float springK, float initialRestLength, 
                          int index1, int index2, bool bending = false)
    {
        p1 = a;
        p2 = b;
        restLength = initialRestLength; 
        k = springK;
        pointIndex1 = index1;
        pointIndex2 = index2;
        isBendingSpring = bending;
    }

    public void ApplyForceToPoint(SoftBodyPoint point)
    {
        if (!isActive) return;
        
        Vector3 dir = p2.position - p1.position;
        float dist = dir.magnitude;
        
        if (dist < 0.001f) return;
        
        float forceMag = k * (dist - restLength);
        Vector3 force = dir.normalized * forceMag;

        Vector3 relativeVelocity = p2.velocity - p1.velocity;
        Vector3 dampingForce = dir.normalized * Vector3.Dot(relativeVelocity, dir.normalized) * damping;

        if (point == p1)
        {
            point.force += force + dampingForce;
        } 
        else if (point == p2)
        {
            point.force -= force + dampingForce;
        }
    }
}

public class SpatialHash
{
    private Dictionary<Vector3Int, List<int>> grid;
    private float cellSize;

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        this.grid = new Dictionary<Vector3Int, List<int>>();
    }

    public void Clear()
    {
        grid.Clear();
    }

    public void Insert(Vector3 position, int pointIndex)
    {
        Vector3Int cell = GetCell(position);
        if (!grid.ContainsKey(cell))
        {
            grid[cell] = new List<int>();
        }
        grid[cell].Add(pointIndex);
    }

    public List<int> GetNearbyPoints(Vector3 position, float radius)
    {
        List<int> nearbyPoints = new List<int>();
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        Vector3Int centerCell = GetCell(position);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector3Int cell = centerCell + new Vector3Int(x, y, z);
                    if (grid.ContainsKey(cell))
                    {
                        nearbyPoints.AddRange(grid[cell]);
                    }
                }
            }
        }

        return nearbyPoints;
    }

    private Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
}
} }