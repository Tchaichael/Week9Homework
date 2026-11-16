using UnityEngine;

public class Magnet : MonoBehaviour
{
    public float pullForce;

    private void OnTriggerStay(Collider other)
    {
        BallForces myBallForces = other.GetComponent<BallForces>();
        if (myBallForces != null)
        {
            Vector3 forceVelocity = transform.position - other.transform.position;
            other.GetComponent<Rigidbody>().AddForce(forceVelocity * pullForce, ForceMode.Force);
        }
    }
}
