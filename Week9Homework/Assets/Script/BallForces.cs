using UnityEngine;
using UnityEngine.InputSystem;

public class BallForces : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody rb;
    public float movementForce;
    public float jumpForce;
    public float clickForce;
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
        if (Mouse .current .leftButton .wasPressedThisFrame)
        {
            Ray mouseCameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit mouseRaycastHit;

            if (Physics .Raycast (mouseCameraRay, out mouseRaycastHit))
            {
                if(mouseRaycastHit .collider .gameObject ==this.gameObject )
                {
                 
                    Vector3 forceDirection = transform.position - mouseRaycastHit.point;
                    forceDirection.Normalize();
                    rb.AddForceAtPosition(forceDirection * clickForce,
                   mouseRaycastHit.point, ForceMode.Impulse);
                }
            }
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
