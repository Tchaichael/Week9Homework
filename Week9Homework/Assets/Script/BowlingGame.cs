using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BowlingController : MonoBehaviour
{
    [Header("Ball")]
    public Rigidbody ball;

    [Header("Ball Reset Position")]
    public Vector3 resetPosition = new Vector3(0.23f, 0.9f, -12.68f);

    [Header("Game")]
    public int maxRolls = 3;

    [Header("Prediction Line")]
    public LineRenderer predictionLine;

    public float predictionLength = 20f;


    private int rollCount = 0;

    // true = 等待第一次左键归位
    // false = 等待 Bowl 发射
    private bool waitingForReset = true;

    private bool waitingForRoll = false;

    private bool ballWasMoving = false;


    void Start()
    {
        if (predictionLine != null)
        {
            predictionLine.enabled = false;
        }
    }


    void Update()
    {
        CheckBall();

        HandleLeftClick();

        UpdatePredictionLine();
    }


    // =====================================================
    // 检测球的状态
    // =====================================================

    void CheckBall()
    {
        bool ballIsMoving =
            ball.linearVelocity.sqrMagnitude > 0.0025f ||
            ball.angularVelocity.sqrMagnitude > 0.0025f;


        // 球刚刚停止
        if (ballWasMoving && !ballIsMoving)
        {
            // 如果这是最后一次发射
            if (rollCount >= maxRolls)
            {
                GameOver();
                return;
            }

            // 下一步等待玩家左键归位
            waitingForReset = true;
            waitingForRoll = false;
        }


        ballWasMoving = ballIsMoving;
    }


    // =====================================================
    // 左键控制
    // =====================================================

    void HandleLeftClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }


        // ---------------------------------------------
        // 球已经发射完
        // 第一次左键 → 归位
        // ---------------------------------------------

        if (waitingForReset)
        {
            ResetBall();

            waitingForReset = false;
            waitingForRoll = true;

            return;
        }


        // ---------------------------------------------
        // 已经归位
        // 第二次左键不负责发射
        //
        // 这里实际上等待的是 Bowl
        // ---------------------------------------------
    }


    // =====================================================
    // 归位
    // =====================================================

    void ResetBall()
    {
        ball.isKinematic = true;

        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;

        ball.transform.position = resetPosition;

        ball.isKinematic = false;

        Debug.Log("Ball reset.");
    }


    // =====================================================
    // 预测线
    // =====================================================

    void UpdatePredictionLine()
    {
        if (predictionLine == null)
        {
            return;
        }


        // 球正在运动
        if (ballWasMoving)
        {
            predictionLine.enabled = false;
            return;
        }


        // 没有等待发射
        if (!waitingForRoll)
        {
            predictionLine.enabled = false;
            return;
        }


        // 已经用完三次
        if (rollCount >= maxRolls)
        {
            predictionLine.enabled = false;
            return;
        }


        predictionLine.enabled = true;

        Vector3 direction = CalculateDirection();

        predictionLine.positionCount = 2;

        predictionLine.SetPosition(
            0,
            ball.transform.position
        );

        predictionLine.SetPosition(
            1,
            ball.transform.position +
            direction * predictionLength
        );
    }


    // =====================================================
    // 计算发射方向
    // =====================================================

    Vector3 CalculateDirection()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        Ray ray =
            Camera.main.ScreenPointToRay(mousePosition);


        // 球所在的水平面
        Plane ballPlane =
            new Plane(
                Vector3.up,
                new Vector3(
                    0f,
                    ball.transform.position.y,
                    0f
                )
            );


        if (ballPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPosition =
                ray.GetPoint(distance);


            // 关键：
            // 只使用 X
            // Y = 0
            // Z = 1
            Vector3 direction = new Vector3(
                mouseWorldPosition.x -
                ball.transform.position.x,

                0f,

                1f
            );


            return direction.normalized;
        }


        return Vector3.forward;
    }


    // =====================================================
    // Game Over
    // =====================================================

    void GameOver()
    {
        predictionLine.enabled = false;

        SceneManager.LoadScene("GameOver");
    }
}