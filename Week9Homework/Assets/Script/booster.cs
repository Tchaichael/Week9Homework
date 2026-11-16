using UnityEngine;

public class BoostPad : MonoBehaviour
{
    public Vector3 boostDirection = Vector3.up;
    public float boostForce = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;

        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(
                boostDirection.normalized * boostForce,
                ForceMode.Impulse
            );
        }
    }
}