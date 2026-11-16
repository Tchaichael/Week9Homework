using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windForce = 5f;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(
                windDirection.normalized * windForce,
                ForceMode.Force
            );
        }
    }
}