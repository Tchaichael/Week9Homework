using UnityEngine;
using UnityEngine.InputSystem;

public class BallForces : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody rb;
    public float movementForce;
    public float jumpForce;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            rb.AddForce(Vector3.up*jumpForce,ForceMode.Impulse);
        }
    }
    void FixedUpdate()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            rb.AddForce(Vector3.forward * movementForce, ForceMode.Force );
        }
        if (Keyboard.current.sKey.isPressed)
        {
            rb.AddForce(Vector3.back*movementForce, ForceMode.Force);
        }
        if (Keyboard.current.aKey.isPressed)
        {
            rb.AddForce(Vector3.left * movementForce, ForceMode.Force);
        }
        if (Keyboard.current.dKey.isPressed)
        {
            rb.AddForce(Vector3.right * movementForce, ForceMode.Force);
        }

    }
}
