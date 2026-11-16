using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    public Vector3 moveDirection = new Vector3(0, 5f, 0); // 移动方向和距离
    public float speed = 2f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 往返运动
        float t = Mathf.PingPong(Time.time * speed, 1f);
        transform.position = startPos + moveDirection * t;
    }
}