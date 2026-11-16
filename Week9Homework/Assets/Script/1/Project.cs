using UnityEngine;

public class SplitBall : MonoBehaviour
{
    [Header("Split Settings")]
    public GameObject ballPrefab;

    public float splitForce = 5f;

    private Rigidbody rb;
    private bool hasSplit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 防止一次碰撞产生很多次分裂
        if (hasSplit)
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