using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class CollisionSplit : MonoBehaviour
{
    [Header("Scene 2 - Split on obstacle contact")]
    [Min(0f)] public float splitForce = 5f;
    [Range(0.4f, 1f)] public float pieceScale = 0.7f;
    public LayerMask obstacleLayers = ~0;
    public bool HasSplit { get; private set; }
    private bool launched;
    private Rigidbody body;
    private BallForces owner;
    private Vector3 incomingVelocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        owner = GetComponent<BallForces>();
    }

    public void NotifyLaunched() { launched = true; }

    private void FixedUpdate()
    {
        if (!body.isKinematic) incomingVelocity = body.linearVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameObject.scene.name != "2") return;
        if (owner != null && owner.TryCompleteLevel(collision.collider)) return;
        if (!launched || HasSplit || body.isKinematic || collision.contactCount == 0) return;
        if ((obstacleLayers.value & (1 << collision.gameObject.layer)) == 0) return;
        if (collision.collider.GetComponentInParent<CollisionSplit>() != null) return;
        Split(collision.GetContact(0).normal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameObject.scene.name == "2" && owner != null) owner.TryCompleteLevel(other);
    }

    private void Split(Vector3 contactNormal)
    {
        // Keep one half on the original object to retain camera and UI references.
        HasSplit = true;
        Vector3 normal = new Vector3(contactNormal.x, contactNormal.y, 0f).normalized;
        if (normal.sqrMagnitude < 0.01f) normal = Vector3.up;
        Vector3 tangent = new Vector3(-normal.y, normal.x, 0f);
        Vector3 velocity = incomingVelocity;
        if (Vector3.Dot(velocity, normal) < 0f) velocity = Vector3.Reflect(velocity, normal);
        velocity.z = 0f;
        transform.localScale *= pieceScale;
        body.mass *= 0.5f;
        GameObject second = CreateSecondPiece();
        Rigidbody secondBody = second.GetComponent<Rigidbody>();
        CollisionSplit secondSplit = second.AddComponent<CollisionSplit>();
        secondSplit.owner = owner;
        secondSplit.HasSplit = true;
        secondSplit.launched = true;
        Physics.SyncTransforms();
        Collider firstCollider = GetComponent<Collider>();
        Collider secondCollider = second.GetComponent<Collider>();
        float radius = Mathf.Max(firstCollider.bounds.extents.x, firstCollider.bounds.extents.y);
        Vector3 center = body.position + normal * (radius * 0.5f + 0.1f);
        body.position = center + tangent * (radius + 0.15f);
        secondBody.position = center - tangent * (radius + 0.15f);
        Physics.IgnoreCollision(firstCollider, secondCollider);
        ResolveOverlap(firstCollider);
        ResolveOverlap(secondCollider);
        body.linearVelocity = velocity + tangent * splitForce;
        secondBody.linearVelocity = velocity - tangent * splitForce;
        secondBody.angularVelocity = body.angularVelocity;
        var manager = GetComponent<BallGameManager>();
        if (manager != null) manager.RegisterSplitPartner(secondBody);
    }

    private GameObject CreateSecondPiece()
    {
        // Copy appearance and physics, without duplicating input, countdown or camera scripts.
        var second = new GameObject(gameObject.name + " - Split Piece");
        SceneManager.MoveGameObjectToScene(second, gameObject.scene);
        second.layer = gameObject.layer;
        second.transform.SetPositionAndRotation(transform.position, transform.rotation);
        second.transform.localScale = transform.lossyScale;
        second.AddComponent<MeshFilter>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();
        MeshRenderer renderer = second.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = sourceRenderer.sharedMaterials;
        renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
        renderer.receiveShadows = sourceRenderer.receiveShadows;
        Collider source = GetComponent<Collider>();
        Collider copy;
        if (source is SphereCollider sphere)
        {
            var col = second.AddComponent<SphereCollider>();
            col.center = sphere.center;
            col.radius = sphere.radius;
            copy = col;
        }
        else if (source is CapsuleCollider capsule)
        {
            var col = second.AddComponent<CapsuleCollider>();
            col.center = capsule.center;
            col.radius = capsule.radius;
            col.height = capsule.height;
            col.direction = capsule.direction;
            copy = col;
        }
        else
        {
            var col = second.AddComponent<BoxCollider>();
            var box = source as BoxCollider;
            col.center = box != null ? box.center : Vector3.zero;
            col.size = box != null ? box.size : Vector3.one;
            copy = col;
        }
        copy.sharedMaterial = source.sharedMaterial;
        Rigidbody rb = second.AddComponent<Rigidbody>();
        rb.mass = body.mass;
        rb.useGravity = body.useGravity;
        rb.linearDamping = body.linearDamping;
        rb.angularDamping = body.angularDamping;
        rb.constraints = body.constraints;
        rb.interpolation = body.interpolation;
        rb.collisionDetectionMode = body.collisionDetectionMode;
        return second;
    }

    private static void ResolveOverlap(Collider collider)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Physics.SyncTransforms();
            bool moved = false;
            foreach (Collider other in Physics.OverlapBox(collider.bounds.center,
                collider.bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (other == collider || other.GetComponentInParent<CollisionSplit>() != null) continue;
                if (!Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out Vector3 direction, out float distance)) continue;
                direction.z = 0f;
                if (direction.sqrMagnitude < 0.001f) continue;
                collider.attachedRigidbody.position += direction.normalized * (distance + 0.02f);
                moved = true;
            }
            if (!moved) break;
        }
    }
}
