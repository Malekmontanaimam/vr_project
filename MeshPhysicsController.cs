using UnityEngine;
using System.Collections.Generic;
using Physics;

public class SoftBodyController : MonoBehaviour
{
    [Header("Physics Properties")]
    public float objectMass = 4f;
    public Physics.Material objectMaterial;
    
    [Header("Wind Properties")]
    public Vector3 windVector = Vector3.right;
    public float windPower = 0f;
    public float windPowerMax = 10f;
    
    [Header("Collision Properties")]
    public bool collisionActive = true;
    public float collisionDistance = 0.05f;
    public bool groundCollisionActive = true;
    public float groundLevel = 0f;
    
    public float stiffness = 0.4f;
    public Vector3 initialSpeed = Vector3.zero;

    [Header("Simulation Settings")]
    public float updateStep = 0.016f;
    public int skipFrames = 2;
    private int frameCounter = 0;

    [Header("Debug & Visualization")]
    public bool debugMode = true;
    public bool springTrail = false;
    public int springTrailLength = 100;
    private List<Vector3> centerTrailPoints = new List<Vector3>();

    [Header("Soft Body Reference")]
    public SoftBodyObject deformableBody;
    private MeshFilter meshComp;
    private Mesh meshData;
    private float lastStepTime = 0f;
    public SoftBodyController otherSoftBody;
    
    private Vector3 bodyCenter;
    private Vector3 bodyStartPosition;

    void Awake()
    {
        meshComp = GetComponent<MeshFilter>();
        if (meshComp == null)
        {
            Debug.LogError("No MeshFilter found on the object!");
            enabled = false;
            return;
        }

        meshData = meshComp.mesh;
        if (meshData == null)
        {
            Debug.LogError("No Mesh found on the MeshFilter!");
            enabled = false;
            return;
        }

        if (objectMaterial == null)
        {
            objectMaterial = Physics.Material.Wood;
        }

        try
        {
            Vector3[] worldVerts = new Vector3[meshData.vertexCount];
            for (int i = 0; i < meshData.vertexCount; i++)
            {
                worldVerts[i] = transform.TransformPoint(meshData.vertices[i]);
            }
            deformableBody = new SoftBodyObject(
                worldVerts,
                meshData.triangles,
                objectMass,
                objectMaterial ?? Physics.Material.Wood,
                false
            );

            if (deformableBody != null && deformableBody.points != null)
            {
                foreach (var pt in deformableBody.points)
                {
                    pt.velocity = initialSpeed;
                }
            }

            deformableBody.physics.shapeRetentionStiffness = stiffness;
            Debug.Log($"SoftBody created with {deformableBody.points.Count} points and {deformableBody.springs.Count} springs");
            bodyCenter = deformableBody.center;
            bodyStartPosition = transform.position;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing SoftBodyObject: {e.Message}\n{e.StackTrace}");
            enabled = false;
            return;
        }
    }

    void FixedUpdate()
    {
        frameCounter++;
        if (frameCounter < skipFrames)
            return;
        frameCounter = 0;

        if (deformableBody == null || deformableBody.physics == null)
        {
            return;
        }

        if (collisionActive)
        {
            if (otherSoftBody != null && otherSoftBody.deformableBody != null)
            {
                Physics.XPBDCollision.ResolveCollision(deformableBody, otherSoftBody.deformableBody, collisionDistance);
            }
            if (groundCollisionActive)
            {
                float restitution = 0.5f;
                foreach (var pt in deformableBody.points)
                {
                    if (pt.position.y < groundLevel)
                    {
                        pt.position.y = groundLevel;
                        if (pt.velocity.y < 0)
                            pt.velocity.y = -pt.velocity.y * restitution;
                    }
                }
            }
        }

        deformableBody.physics.wind = windVector;
        deformableBody.physics.windStrength = windPower;
        deformableBody.UpdatePhysics(Time.fixedDeltaTime * skipFrames);

        Vector3 centerOffset = deformableBody.center - bodyCenter;
        transform.position = bodyStartPosition + centerOffset;

        if (springTrail)
        {
            centerTrailPoints.Add(deformableBody.center);
            if (centerTrailPoints.Count > springTrailLength)
                centerTrailPoints.RemoveAt(0);
        }
        else
        {
            centerTrailPoints.Clear();
        }
    }

    void Update()
    {
        if (Time.time - lastStepTime < updateStep)
            return;
        lastStepTime = Time.time;

        if (deformableBody != null && meshData != null)
        {
            Vector3[] newVerts = new Vector3[deformableBody.points.Count];
            for (int i = 0; i < deformableBody.points.Count; i++)
            {
                newVerts[i] = transform.InverseTransformPoint(deformableBody.points[i].position);
            }
            meshData.vertices = newVerts;
            meshData.RecalculateNormals();
            meshData.RecalculateBounds();
        }

        if (deformableBody != null && debugMode)
        {
            deformableBody.DrawDebugPoints();
            Debug.Log($"SoftBody Physics - Linear Velocity: {deformableBody.GetAverageVelocity().magnitude:F2} m/s");
        }
    }

    public void SetSimulationStep(float step)
    {
        updateStep = Mathf.Clamp(step, 0.001f, 0.1f);
    }

    public int GetActivePointCount()
    {
        if (deformableBody?.physics == null) return 0;
        int activeCount = 0;
        for (int i = 0; i < deformableBody.points.Count; i++)
        {
            if (deformableBody.points[i] != null)
            {
                activeCount++;
            }
        }
        return activeCount;
    }

    public int GetActiveSpringCount()
    {
        if (deformableBody?.springs == null) return 0;
        int activeCount = 0;
        foreach (var spring in deformableBody.springs)
        {
            if (spring != null && spring.isActive)
            {
                activeCount++;
            }
        }
        return activeCount;
    }
    void OnValidate()
{
    // مثال: إعادة ضبط متغير إذا تغيرت قيمة أخرى
    
    // أو يمكنك إعادة تهيئة الجسم الفيزيائي هنا إذا لزم الأمر
}
    public float GetBodyAverageVelocity()
    {
        if (deformableBody != null)
        {
            return deformableBody.GetAverageVelocity().magnitude;
        }
        return 0f;
    }

    

    public void SetCollisionDistance(float distance)
    {
        collisionDistance = Mathf.Clamp(distance, 0.01f, 0.5f);
    }

    public void ToggleCollisionActive()
    {
        collisionActive = !collisionActive;
    }

    public void ToggleGroundCollisionActive()
    {
        groundCollisionActive = !groundCollisionActive;
    }

    public void SetGroundLevel(float y)
    {
        groundLevel = y;
    }

    public string GetCollisionStatus()
    {
        return $"Collision: {(collisionActive ? "Enabled" : "Disabled")}\n" +
               $"Threshold: {collisionDistance:F3}\n" +
               $"Ground Collision: {(groundCollisionActive ? "Enabled" : "Disabled")}";
    }

    void OnDrawGizmos()
    {
        if (deformableBody != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(deformableBody.center, 0.1f);

            if (springTrail && deformableBody.springs != null)
            {
                foreach (var spring in deformableBody.springs)
                {
                    if (spring == null) continue;
                    float currentLength = (spring.p1.position - spring.p2.position).magnitude;
                    float stretch = Mathf.Abs(currentLength - spring.restLength) / spring.restLength;
                    Color springColor = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(stretch * 2f));
                    Gizmos.color = springColor;
                    Gizmos.DrawLine(spring.p1.position, spring.p2.position);
                }
            }

            if (groundCollisionActive)
            {
                foreach (var pt in deformableBody.points)
                {
                    if (pt.position.y < groundLevel)
                    {
                        //Gizmos.color = Color.red;
                        //Gizmos.DrawSphere(pt.position, 0.04f);
                    }
                }
            }

            if (collisionActive && otherSoftBody != null && otherSoftBody.deformableBody != null)
            {
                foreach (var ptA in deformableBody.points)
                {
                    foreach (var ptB in otherSoftBody.deformableBody.points)
                    {
                        float distance = Vector3.Distance(ptA.position, ptB.position);
                        if (distance < collisionDistance && distance > 0.001f)
                        {
                            //Gizmos.color = Color.magenta;
                            //Gizmos.DrawSphere(ptA.position, 0.04f);
                            //Gizmos.DrawSphere(ptB.position, 0.04f);
                        }
                    }
                }
            }
            if (windPower > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(deformableBody.center, windVector * windPower * 0.5f);
            }
            if (groundCollisionActive)
            {
                Gizmos.color = Color.red;
                Vector3 groundPos = new Vector3(deformableBody.center.x, groundLevel, deformableBody.center.z);
                Gizmos.DrawWireCube(groundPos, new Vector3(2f, 0.01f, 2f));
            }
            if (springTrail && centerTrailPoints.Count > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 1; i < centerTrailPoints.Count; i++)
                {
                    Gizmos.DrawLine(centerTrailPoints[i - 1], centerTrailPoints[i]);
                }
            }
        }
    }

    public enum MaterialTypeChoice
    {
        Metal,
        Wood,
        Plastic,
        Rubber
    }

    [Header("Material Selection")]
    public MaterialTypeChoice selectedMaterial = MaterialTypeChoice.Wood;

    public void ApplySelectedMaterial()
    {
        switch (selectedMaterial)
        {
            case MaterialTypeChoice.Metal:
                objectMaterial = Physics.Material.Metal;
                break;
            case MaterialTypeChoice.Wood:
                objectMaterial = Physics.Material.Wood;
                break;
            case MaterialTypeChoice.Plastic:
                objectMaterial = Physics.Material.Plastic;
                break;
            case MaterialTypeChoice.Rubber:
                objectMaterial = Physics.Material.Rubber;
                break;
        }
    }
} 
