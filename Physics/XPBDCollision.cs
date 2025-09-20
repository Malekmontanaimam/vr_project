using UnityEngine;
using System.Collections.Generic;

namespace Physics
{
    public static class XPBDCollision
    {
        public static void ResolveCollision(
            SoftBodyObject objA,
            SoftBodyObject objB,
            float threshold)
        {
            Bounds aabbA = objA.GetAABB();
            Bounds aabbB = objB.GetAABB();
            if (!aabbA.Intersects(aabbB))
            {
                Debug.Log("[AABB] No overlap between objects.");
                return;
            }
            Debug.Log("[AABB] Overlap detected between objects.");
            if (!GJKCollision(objA, objB))
            {
                Debug.Log("[GJK] No actual collision detected between objects.");
                return;
            }
            Debug.Log("[GJK] Actual collision detected between objects!");
            // هنا فقط نكشف التصادم ولا نقوم بأي حل فيزيائي
            float restitution = 0.5f;
            foreach (var pointA in objA.points)
            {
                foreach (var pointB in objB.points)
                {
                    Vector3 delta = pointB.position - pointA.position;
                    float dist = delta.magnitude;
                    if (dist < threshold && dist > 0.001f)
                    {
                        Vector3 normal = delta.normalized;
                        float penetration = threshold - dist;
                        // ادفع النقاط بعيد عن بعض
                        pointA.position -= normal * (penetration / 2f);
                        pointB.position += normal * (penetration / 2f);
                        // اعكس جزء من السرعة (ارتداد)
                        float relVel = Vector3.Dot(pointA.velocity - pointB.velocity, normal);
                        if (relVel < 0)
                        {
                            Vector3 impulse = normal * relVel * restitution / 2f;
                            pointA.velocity -= impulse;
                            pointB.velocity += impulse;
                        }
                    }
                }
            }
        }

        private static void FindCollisionPoints(
            SoftBodyObject objA,
            SoftBodyObject objB,
            List<CollisionConstraint> constraints,
            float threshold,
            float compliance,
            float damping)
        {
            bool collisionDetected = false;
            foreach (var pointA in objA.points)
            {
                foreach (var pointB in objB.points)
                {
                    Vector3 delta = pointB.position - pointA.position;
                    float distance = delta.magnitude;
                    if (distance < threshold && distance > 0.001f)
                    {
                        Vector3 normal = delta.normalized;
                        var constraint = new CollisionConstraint
                        {
                            pointA = pointA,
                            pointB = pointB,
                            normal = normal,
                            restDistance = threshold,
                            compliance = compliance,
                            damping = damping,
                            lambda = 0f
                        };
                        constraints.Add(constraint);
                        if (!collisionDetected)
                        {
                            Debug.Log("[XPBDCollision] Collision detected between objects!");
                            collisionDetected = true;
                        }
                        float force = (pointA.velocity - pointB.velocity).magnitude;
                        objA.ApplyDeformation(pointA.position, force);
                        objB.ApplyDeformation(pointB.position, force);
                    }
                }
            }
        }

        private static bool GJKCollision(SoftBodyObject objA, SoftBodyObject objB)
        {
            // استخدم نقاط Convex Hull بدلاً من جميع النقاط
            List<Vector3> vertsA = objA.GetConvexHullPoints();
            List<Vector3> vertsB = objB.GetConvexHullPoints();

            Vector3 direction = vertsA[0] - vertsB[0];
            if (direction == Vector3.zero) direction = Vector3.right;

            List<Vector3> simplex = new List<Vector3>();
            simplex.Add(Support(vertsA, vertsB, direction));
            direction = -simplex[0];

            int maxIters = 20;
            for (int i = 0; i < maxIters; i++)
            {
                Vector3 A = Support(vertsA, vertsB, direction);
                if (Vector3.Dot(A, direction) < 0)
                    return false;
                simplex.Add(A);
                if (HandleSimplex(ref simplex, ref direction))
                    return true;
            }
            return false;
        }

        private static Vector3 Support(List<Vector3> vertsA, List<Vector3> vertsB, Vector3 dir)
        {
            Vector3 pA = FarthestPointInDirection(vertsA, dir);
            Vector3 pB = FarthestPointInDirection(vertsB, -dir);
            return pA - pB;
        }
        private static Vector3 FarthestPointInDirection(List<Vector3> verts, Vector3 dir)
        {
            float maxDot = float.NegativeInfinity;
            Vector3 farthest = verts[0];
            foreach (var v in verts)
            {
                float dot = Vector3.Dot(v, dir);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    farthest = v;
                }
            }
            return farthest;
        }
        private static bool HandleSimplex(ref List<Vector3> simplex, ref Vector3 direction)
        {
            if (simplex.Count == 2)
            {
                Vector3 A = simplex[1];
                Vector3 B = simplex[0];
                Vector3 AB = B - A;
                Vector3 AO = -A;
                direction = Vector3.Cross(Vector3.Cross(AB, AO), AB);
            }
            else if (simplex.Count == 3)
            {
                Vector3 A = simplex[2];
                Vector3 B = simplex[1];
                Vector3 C = simplex[0];
                Vector3 AB = B - A;
                Vector3 AC = C - A;
                Vector3 AO = -A;
                Vector3 ABC = Vector3.Cross(AB, AC);
                Vector3 ABC_AC = Vector3.Cross(AC, ABC);
                if (Vector3.Dot(ABC_AC, AO) > 0)
                {
                    simplex.RemoveAt(1);
                    direction = Vector3.Cross(AC, AO);
                }
                else
                {
                    Vector3 AB_ABC = Vector3.Cross(ABC, AB);
                    if (Vector3.Dot(AB_ABC, AO) > 0)
                    {
                        simplex.RemoveAt(0);
                        direction = Vector3.Cross(AB, AO);
                    }
                    else
                    {
                        if (Vector3.Dot(ABC, AO) > 0)
                        {
                            direction = ABC;
                        }
                        else
                        {
                            Vector3 temp = simplex[0];
                            simplex[0] = simplex[1];
                            simplex[1] = temp;
                            direction = -ABC;
                        }
                    }
                }
            }
            else if (simplex.Count == 4)
            {
                Vector3 A = simplex[3];
                Vector3 B = simplex[2];
                Vector3 C = simplex[1];
                Vector3 D = simplex[0];
                if (TetrahedronContainsOrigin(A, B, C, D, ref direction))
                    return true;
                simplex.RemoveAt(0);
            }
            return false;
        }
        private static bool TetrahedronContainsOrigin(Vector3 A, Vector3 B, Vector3 C, Vector3 D, ref Vector3 direction)
        {
            Vector3 AO = -A;
            Vector3 AB = B - A;
            Vector3 AC = C - A;
            Vector3 AD = D - A;
            Vector3 ABC = Vector3.Cross(AB, AC);
            Vector3 ACD = Vector3.Cross(AC, AD);
            Vector3 ADB = Vector3.Cross(AD, AB);
            if (Vector3.Dot(ABC, AO) > 0)
            {
                direction = ABC;
                return false;
            }
            if (Vector3.Dot(ACD, AO) > 0)
            {
                direction = ACD;
                return false;
            }
            if (Vector3.Dot(ADB, AO) > 0)
            {
                direction = ADB;
                return false;
            }
            return true;
        }

        public class CollisionConstraint
        {
            public SoftBodyPoint pointA;
            public SoftBodyPoint pointB;
            public Vector3 normal;
            public float restDistance;
            public float compliance;
            public float damping;
            public float lambda = 0f;
        }

        public class GroundCollisionConstraint
        {
            public SoftBodyPoint point;
            public float groundY;
            public float compliance;
            public float damping;
            public float lambda = 0f;
        }
    }
} 