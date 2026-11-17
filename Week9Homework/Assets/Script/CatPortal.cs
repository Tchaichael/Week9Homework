using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CatPortal : MonoBehaviour
{
    public BoxCollider exitPortal;
    [Tooltip("沿出口长边的垂线飞出；勾选后改为另一侧。")]
    public bool reverseExit;
    [Min(0.01f)] public float exitClearance = 0.5f;
    [Header("出口减速与加速")]
    [Tooltip("刚出门时的速度 / 入门速度。"), Range(0.05f, 1f)]
    public float initialExitSpeedRatio = 0.3f;
    [Tooltip("最终恢复的速度 / 入门速度。"), Min(0.05f)]
    public float targetExitSpeedRatio = 1f;
    [Tooltip("从初始速度平滑加速到目标速度的秒数。"), Min(0.01f)]
    public float exitAccelerationDuration = 1.2f;
    private readonly Dictionary<Rigidbody, float> lastTeleport = new Dictionary<Rigidbody, float>();

    private void OnTriggerEnter(Collider other) { TryTeleport(other); }
    private void OnTriggerStay(Collider other) { TryTeleport(other); }

    public static bool IsPortalCollider(Collider other)
    {
        if (!other) return false;
        if (other.GetComponentInParent<CatPortal>()) return true;
        // The one-way exit has no CatPortal component of its own.
        foreach (CatPortal portal in Object.FindObjectsByType<CatPortal>())
        {
            if (portal.exitPortal &&
                other.transform.IsChildOf(portal.exitPortal.transform)) return true;
        }
        return false;
    }

    public static bool IsTouchingPortal(Rigidbody body)
    {
        foreach (CatPortal portal in Object.FindObjectsByType<CatPortal>())
        {
            if (!portal.isActiveAndEnabled) continue;
            foreach (Collider cat in body.GetComponentsInChildren<Collider>())
            {
                if (!cat.enabled || cat.isTrigger || cat.attachedRigidbody != body) continue;
                if (Overlaps(cat, portal.GetComponent<BoxCollider>()) ||
                    Overlaps(cat, portal.exitPortal)) return true;
            }
        }
        return false;
    }

    private static bool Overlaps(Collider cat, Collider door)
    {
        return door && door.enabled && door.gameObject.activeInHierarchy &&
            Physics.ComputePenetration(cat, cat.transform.position, cat.transform.rotation,
                door, door.transform.position, door.transform.rotation, out _, out _);
    }

    public bool TryTeleport(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (!exitPortal || !exitPortal.enabled || !exitPortal.gameObject.activeInHierarchy ||
            !body || body.isKinematic || body.linearVelocity.sqrMagnitude < 0.0001f) return false;
        // Only the cat player and its split pieces can travel through this portal.
        if (!body.GetComponent<BallForces>() && !body.GetComponent<CollisionSplit>()) return false;
        if (lastTeleport.TryGetValue(body, out float time) && Time.time - time < 0.25f) return false;

        // Gameplay is constrained to XY. The model's long local Y axis is the door edge.
        Vector3 tangent = exitPortal.transform.up;
        tangent.z = 0f;
        if (tangent.sqrMagnitude < 0.0001f) return false;
        Vector3 normal = Vector3.Cross(Vector3.forward, tangent.normalized);
        if (Vector3.Dot(normal, exitPortal.transform.right) < 0f) normal = -normal;
        if (reverseExit) normal = -normal;
        float speed = body.linearVelocity.magnitude;
        Vector3 center = exitPortal.transform.TransformPoint(exitPortal.center);
        center.z = body.position.z;
        // Place the entire cat beyond the exit, even when it has several colliders.
        float offset = ProjectExtent(exitPortal.bounds.extents, normal) + exitClearance;
        float catExtent = 0f;
        foreach (Collider collider in body.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger || collider.attachedRigidbody != body) continue;
            catExtent = Mathf.Max(catExtent, ProjectExtent(collider.bounds.extents, normal) -
                Vector3.Dot(collider.bounds.center - body.position, normal));
        }
        lastTeleport[body] = Time.time;
        body.GetComponent<CollisionSplit>()?.ProtectPortalTransit();
        body.position = center + normal * (offset + catExtent);
        var acceleration = body.GetComponent<CatPortalExitAcceleration>();
        if (!acceleration) acceleration = body.gameObject.AddComponent<CatPortalExitAcceleration>();
        acceleration.Begin(normal, speed * initialExitSpeedRatio,
            speed * Mathf.Max(initialExitSpeedRatio, targetExitSpeedRatio), exitAccelerationDuration);
        body.WakeUp();
        foreach (TrailRenderer trail in body.GetComponentsInChildren<TrailRenderer>()) trail.Clear();
        body.GetComponent<CatStarTrail>()?.ResetAfterTeleport();
        return true;
    }

    private static float ProjectExtent(Vector3 extents, Vector3 direction)
    {
        return Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.y) * extents.y +
            Mathf.Abs(direction.z) * extents.z;
    }
}
