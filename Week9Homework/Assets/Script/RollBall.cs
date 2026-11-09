using System.Reflection.Metadata.Ecma335;
using UnityEngine;
using UnityEngine.InputSystem;

public class RollBall : MonoBehaviour
{

    InputAction bowlButt;
    Rigidbody rigidbody  ;
    bool holdingButt;
    bool roll;
    public float  RollPower;
    public LayerMask personLayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody  = GetComponent<Rigidbody>();
        bowlButt = InputSystem.actions.FindAction("Bowl");
    }

    // Update is called once per frame
    void Update()
    {
        if(bowlButt.WasPressedThisFrame())
        {
            holdingButt = true;
            //  StartRoll();
         
        }else if(bowlButt.WasReleasedThisFrame())
        {
            holdingButt = false;
            roll = true;
        }
            
    }


    void FixedUpdate()
    {
        if(holdingButt && rigidbody.linearVelocity == Vector3.zero)
        {
            Debug.DrawRay(transform.position,FindDirection()*1000f,Color.red);
        }
        if (roll)
        {
            StartRoll();
        }
                
    }





    void StartRoll()
    {
        if (Physics.Raycast(transform.position, FindDirection(), 1000f, personLayer)==false)
        {
            rigidbody.isKinematic = false;
            // Vector3 poinTo = FindDirection();
            rigidbody.AddForce(RollPower * transform.TransformDirection(FindDirection()), ForceMode.Impulse);
            roll = false;
        }
    }
    Vector3  FindDirection()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        mousePosition.z = 10f;
        Vector3 direction = Camera.main. ScreenToWorldPoint(mousePosition);
        direction.z = 10f;
        Debug.Log(direction);
        return direction.normalized;
    }
    int CallculateGrade(int a, int b)
    {
   return a+b;
    }
}
