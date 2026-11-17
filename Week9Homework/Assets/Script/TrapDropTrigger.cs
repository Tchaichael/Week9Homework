using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TrapDropTrigger : MonoBehaviour
{
    public Transform dropTarget;
    [Tooltip("下落终点的世界坐标 Y。")]
    public float targetY = -80f;
    [Tooltip("每秒下落的世界单位数。"), Min(.1f)]
    public float dropSpeed = 600f;
    public bool HasTriggered { get; private set; }
    public bool HasLanded { get; private set; }
    private Vector3 destination;

    private void OnTriggerEnter(Collider other) { TryActivate(other); }
    private void OnTriggerStay(Collider other) { TryActivate(other); }

    public bool TryActivate(Collider other)
    {
        if (HasTriggered || !dropTarget || !other) return false;
        Rigidbody body = other.attachedRigidbody;
        if (!body || (!body.GetComponent<BallForces>() && !body.GetComponent<CollisionSplit>())) return false;
        HasTriggered = true;
        destination = new Vector3(dropTarget.position.x, targetY, dropTarget.position.z);
        // Move the whole obstacle together, including its jointed pieces.
        foreach (var motion in dropTarget.GetComponentsInChildren<MovingObstacle>()) motion.enabled = false;
        foreach (var piece in dropTarget.GetComponentsInChildren<Rigidbody>())
        {
            if (!piece.isKinematic)
            {
                piece.linearVelocity = Vector3.zero;
                piece.angularVelocity = Vector3.zero;
            }
            piece.isKinematic = true;
            piece.useGravity = false;
        }
        return true;
    }

    private void FixedUpdate() { AdvanceDrop(Time.fixedDeltaTime); }

    public void AdvanceDrop(float deltaTime)
    {
        if (!HasTriggered || HasLanded || !dropTarget) return;
        dropTarget.position = Vector3.MoveTowards(dropTarget.position, destination,
            Mathf.Max(.1f, dropSpeed) * Mathf.Max(0f, deltaTime));
        Physics.SyncTransforms();
        HasLanded = dropTarget.position == destination;
    }
}
