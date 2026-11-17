using UnityEngine;

// Neighbor joints form the chain; this restoring force preserves its mountain silhouette.
public class SpringMountain : MonoBehaviour
{
    [Tooltip("受撞后回到小山轮廓的力度。"), Min(0f)] public float shapeStrength = 30f;
    [Tooltip("回形阻尼，越大越快稳定。"), Min(0f)] public float shapeDamping = 8f;
    [SerializeField] private Rigidbody[] pieces;
    [SerializeField] private Vector3[] restPositions;

    public void CaptureShape(Rigidbody[] orderedPieces)
    {
        pieces = orderedPieces;
        restPositions = new Vector3[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
            restPositions[i] = transform.InverseTransformPoint(pieces[i].position);
    }

    private void FixedUpdate()
    {
        if (pieces == null || restPositions == null || pieces.Length != restPositions.Length) return;
        for (int i = 0; i < pieces.Length; i++)
        {
            Rigidbody piece = pieces[i];
            if (!piece || piece.isKinematic) continue;
            Vector3 acceleration = RestoreAcceleration(transform.TransformPoint(restPositions[i]) - piece.position,
                piece.linearVelocity, shapeStrength, shapeDamping);
            piece.AddForce(acceleration, ForceMode.Acceleration);
        }
    }

    public static Vector3 RestoreAcceleration(Vector3 displacement, Vector3 velocity, float strength, float damping)
    {
        Vector3 acceleration = displacement * strength - velocity * damping;
        acceleration.z = 0f;
        return acceleration;
    }
}
