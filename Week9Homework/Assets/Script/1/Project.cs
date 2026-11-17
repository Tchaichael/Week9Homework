using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SplitBall : MonoBehaviour
{
    [Header("Split Settings")]
    public GameObject ballPrefab;

    public float splitForce = 5f;

    private Rigidbody rb;
    private bool hasSplit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Unity sends collision callbacks to disabled MonoBehaviours too.
        // Splitting belongs only to scene 2; its newer component takes priority.
        if (!isActiveAndEnabled || gameObject.scene.name != "2" ||
            GetComponent<CollisionSplit>() != null)
            return;

        if (CatPortal.IsPortalCollider(collision.collider) || CatPortal.IsTouchingPortal(rb)) return;

        // 防止一次碰撞产生很多次分裂
        if (hasSplit || ballPrefab == null)
            return;

        hasSplit = true;

        Split();
    }

    void Split()
    {
        Vector3 splitPosition = transform.position;

        // 创建两个新的棋子
        GameObject ball1 =
            Instantiate(ballPrefab, splitPosition, transform.rotation);

        GameObject ball2 =
            Instantiate(ballPrefab, splitPosition, transform.rotation);

        Rigidbody rb1 = ball1.GetComponent<Rigidbody>();
        Rigidbody rb2 = ball2.GetComponent<Rigidbody>();
        if (rb1 == null) rb1 = ball1.AddComponent<Rigidbody>();
        if (rb2 == null) rb2 = ball2.AddComponent<Rigidbody>();
        rb1.isKinematic = false;
        rb2.isKinematic = false;
        rb1.constraints = rb.constraints;
        rb2.constraints = rb.constraints;

        // 保存原来棋子的速度
        Vector3 originalVelocity = rb.linearVelocity;

        // 让两个新棋子继承原来的速度
        rb1.linearVelocity = originalVelocity;
        rb2.linearVelocity = originalVelocity;

        // 在原来的运动基础上，让两个棋子向不同方向分开
        rb1.AddForce(Vector3.up * splitForce, ForceMode.Impulse);
        rb2.AddForce(Vector3.down * splitForce, ForceMode.Impulse);

        // 删除原来的棋子
        Destroy(gameObject);
    }
}
