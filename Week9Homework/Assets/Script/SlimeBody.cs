using UnityEngine;

// Keeps existing rigidbodies, scripts and goal objects intact when pieces fuse.
public class SlimeBody : MonoBehaviour
{
    public SlimeStage stage;
    public bool white;

    private void OnCollisionEnter(Collision collision)
    {
        SlimeBody other = collision.collider.GetComponentInParent<SlimeBody>();
        if (other != null && other != this && stage != null && stage == other.stage && white == other.white)
            stage.Stick(this, other);
    }
}
