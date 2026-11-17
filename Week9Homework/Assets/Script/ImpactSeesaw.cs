using System.Collections.Generic;
using UnityEngine;

// Prince is moved by TrapDropTrigger, so detect its swept contact rather than
// relying on impulses from a transform-driven kinematic obstacle.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody))]
public class ImpactSeesaw : MonoBehaviour
{
    public TrapDropTrigger dropTrigger;
    public Transform fallingPrince;
    [Header("右侧下压，左侧抬起")]
    [Range(1f, 85f)] public float rotationAngle = 42f;
    [Min(.02f)] public float rotationDuration = .24f;
    [Header("小猫弹飞")]
    [Min(0f)] public float launchSpeed = 110f;
    public Vector2 launchDirection = new Vector2(.3f, 1f);
    [Min(0f)] public float contactPadding = 1f;
    public bool HasImpacted { get; private set; }
    public int LaunchCount { get; private set; }
    private Rigidbody body;
    private Quaternion initialRotation;
    private Collider[] princeColliders;
    private BoxCollider[] boardColliders;
    private Bounds[] previousPrinceBounds;
    private readonly HashSet<Rigidbody> launched = new HashSet<Rigidbody>();
    private float elapsed;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        initialRotation = body.rotation;
        boardColliders = GetComponentsInChildren<BoxCollider>();
        princeColliders = fallingPrince ? fallingPrince.GetComponentsInChildren<Collider>() : new Collider[0];
        previousPrinceBounds = new Bounds[princeColliders.Length];
        for (int i = 0; i < princeColliders.Length; i++) previousPrinceBounds[i] = princeColliders[i].bounds;
    }

    private void FixedUpdate()
    {
        if (!HasImpacted && dropTrigger && dropTrigger.HasTriggered)
        {
            for (int i = 0; i < princeColliders.Length; i++)
            {
                var c = princeColliders[i];
                if (!c || !c.enabled || c.isTrigger) continue;
                Bounds current = c.bounds;
                if (current.center.y < previousPrinceBounds[i].center.y - .001f)
                {
                    Bounds swept = current;
                    swept.Encapsulate(previousPrinceBounds[i]);
                    foreach (var plate in boardColliders)
                        if (plate.enabled && plate.bounds.center.x > body.position.x && swept.Intersects(plate.bounds))
                        { ActivateImpact(); break; }
                }
                previousPrinceBounds[i] = current;
            }
        }
        else if (!HasImpacted)
            for (int i = 0; i < princeColliders.Length; i++)
                if (princeColliders[i]) previousPrinceBounds[i] = princeColliders[i].bounds;

        if (!HasImpacted || elapsed >= rotationDuration) return;
        elapsed = Mathf.Min(rotationDuration, elapsed + Time.fixedDeltaTime);
        Quaternion next = Quaternion.AngleAxis(-rotationAngle * Mathf.SmoothStep(0f, 1f, elapsed / rotationDuration), Vector3.forward) * initialRotation;
        Quaternion delta = next * Quaternion.Inverse(body.rotation);
        foreach (var plate in boardColliders)
        {
            if (!plate.enabled || plate.bounds.center.x >= body.position.x) continue;
            Bounds swept = plate.bounds;
            // Bound the current and upcoming plate pose to catch fast upward hits.
            Vector3 center = swept.center, extents = swept.extents;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        swept.Encapsulate(body.position + delta * (center + Vector3.Scale(extents, new Vector3(x,y,z)) - body.position));
            swept.Expand(contactPadding * 2f);
            foreach (var hit in Physics.OverlapBox(swept.center, swept.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (hit.bounds.center.y >= center.y) TryLaunch(hit.attachedRigidbody);
        }
        body.MoveRotation(next);
    }

    public void ActivateImpact() { if (!HasImpacted) { HasImpacted = true; elapsed = 0f; } }

    private void OnCollisionEnter(Collision collision) { LaunchContact(collision); }
    private void OnCollisionStay(Collision collision) { LaunchContact(collision); }
    private void LaunchContact(Collision collision)
    {
        if (HasImpacted && elapsed < rotationDuration && collision.rigidbody && collision.rigidbody.position.x < body.position.x)
            TryLaunch(collision.rigidbody);
    }
    private void TryLaunch(Rigidbody cat)
    {
        if (!cat || cat.isKinematic || (!cat.GetComponent<BallForces>() && !cat.GetComponent<CollisionSplit>()) || !launched.Add(cat)) return;
        cat.GetComponent<CatPortalExitAcceleration>()?.Cancel();
        Vector2 direction = launchDirection.sqrMagnitude > .001f ? launchDirection.normalized : Vector2.up;
        cat.linearVelocity = new Vector3(direction.x, direction.y, 0f) * launchSpeed;
        cat.WakeUp();
        LaunchCount++;
    }
}
