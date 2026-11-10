using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.WSA;

public class spinner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody rb;
    public float spinForce;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if(Keyboard.current.tKey.wasPressedThisFrame )
        {
            rb.AddTorque(Vector3.up * spinForce, ForceMode.Impulse);

        }

        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            rb.AddTorque(Vector3.up * -spinForce, ForceMode.Impulse);
        }
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            rb.AddTorque(Vector3.forward * -spinForce, ForceMode.Impulse);
        }
    }
}
