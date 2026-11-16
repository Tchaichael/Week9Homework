using UnityEngine;

public class RotatingObstacle : MonoBehaviour
{
    public float rotationSpeed = 50f; // 旋转速度

    void Update()
    {
        // 绕 Z 轴或 Y 轴持续旋转
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}