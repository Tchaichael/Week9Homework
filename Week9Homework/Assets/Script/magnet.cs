using UnityEngine;

public class magnet : MonoBehaviour
{
    public float pullForce;

    private void OnTriggerStay(Collider other)
    {
        BallForces myballForces = other.GetComponent<BallForces>();
        if(myballForces != null)
        {
            Vector3 forceDirection = transform.position - other.transform.position;
            other.GetComponent<Rigidbody>().AddForce(forceDirection * -pullForce, ForceMode.Force);
        }
    }
}
