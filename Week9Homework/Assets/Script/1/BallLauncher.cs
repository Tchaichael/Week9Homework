using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class BallGameManager : MonoBehaviour
{
    [Header("绑定组件")]
    public Transform ballTransform;
    private Rigidbody ballRb;
    private LineRenderer lineRenderer;

    [Header("UI 设置")]
    [Tooltip("把场景里的倒计时 TextMeshPro 拖到这里")]
    public TextMeshProUGUI countdownText;

    [Header("轨迹预测设置")]
    public int trajectorySteps = 30;
    public float timeStep = 0.05f;

    [Header("静止与超时设置")]
    [Tooltip("白子完全静止多少秒后开始倒计时（秒）")]
    public float waitBeforeCountdown = 1.0f; // 停下后等待几秒开始读秒

    [Tooltip("倒计时本身的秒数（UI 显示并重新加载场景的时间）")]
    public float countdownDuration = 4.0f;

    private Vector3 initialPosition;
    private bool hasFired = false;
    private bool isBallStopped = false; // 白子是否已经停稳

    private Vector3 lastPosition;       // 用于检测位置变化的上一帧位置
    private float stopTimer = 0f;       // 停稳后的缓冲计时器
    private float countdownTimer = 0f;  // 倒计时计时器

    void Start()
    {
        if (ballTransform == null)
        {
            ballTransform = transform;
        }

        ballRb = ballTransform.GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();

        initialPosition = ballTransform.position;
        lineRenderer.enabled = false;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 1. 如果已经发射出去了
        if (hasFired)
        {
            // 如果白子还没完全停稳，先检测它是不是静止了
            if (!isBallStopped)
            {
                CheckIfPositionStopped();
            }
            else
            {
                // 已经停稳了，开始进行倒计时！
                countdownTimer -= Time.deltaTime;
                float remainingTime = Mathf.Max(0, countdownTimer);

                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = "Reload Time: " + remainingTime.ToString("F1") + "s";
                }

                // 倒计时结束，重新加载场景
                if (countdownTimer <= 0f)
                {
                    Debug.Log("倒计时结束，重新加载场景！");
                    ReloadCurrentScene();
                }
            }
            return;
        }

        // 2. 检查旧脚本是不是正在拖拽白子（画预测线）
        Vector3 currentPos = ballTransform.position;
        float dragDistance = Vector3.Distance(currentPos, initialPosition);

        if (dragDistance > 0.05f)
        {
            Vector3 dragVector = currentPos - initialPosition;
            Vector3 launchDir = -dragVector.normalized;

            float dragPercent = Mathf.Clamp01(dragVector.magnitude / 3f);
            float currentForce = Mathf.Lerp(10f, 100f, dragPercent);

            Vector3 estimatedVelocity = launchDir * currentForce;

            DrawTrajectory(currentPos, estimatedVelocity);
            lineRenderer.enabled = true;
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    // 核心检测：判断 X 和 Y 轴位置是否静止
    void CheckIfPositionStopped()
    {
        // 计算当前位置与上一帧位置的距离（只比较 X 和 Y 轴）
        Vector2 currentXY = new Vector2(ballTransform.position.x, ballTransform.position.y);
        Vector2 lastXY = new Vector2(lastPosition.x, lastPosition.y);
        float moveDelta = Vector2.Distance(currentXY, lastXY);

        // 如果这一帧和上一帧移动的距离极小（几乎没动），或者刚体速度很低
        if (moveDelta < 0.001f && ballRb.linearVelocity.magnitude < 0.1f)
        {
            stopTimer += Time.deltaTime;

            // 连续静止达到设定的缓冲时间（例如 1 秒），认为它真正停稳了
            if (stopTimer >= waitBeforeCountdown)
            {
                isBallStopped = true;
                countdownTimer = countdownDuration; // 开启倒计时
                Debug.Log("白子 X/Y 轴已完全静止，开始倒计时！");
            }
        }
        else
        {
            // 如果还在动，重置静止计时器
            stopTimer = 0f;
        }

        // 更新上一帧的位置
        lastPosition = ballTransform.position;
    }

    // 绘制抛物线
    void DrawTrajectory(Vector3 origin, Vector3 initialVelocity)
    {
        lineRenderer.positionCount = trajectorySteps;
        Vector3[] points = new Vector3[trajectorySteps];
        Vector3 gravity = Physics.gravity;

        for (int i = 0; i < trajectorySteps; i++)
        {
            float t = i * timeStep;
            Vector3 pos = origin + initialVelocity * t + 0.5f * gravity * t * t;
            points[i] = pos;
        }

        lineRenderer.SetPositions(points);
    }

    // 由旧脚本在发射时调用
    public void TriggerLaunch()
    {
        hasFired = true;
        isBallStopped = false;
        stopTimer = 0f;
        lastPosition = ballTransform.position;
        lineRenderer.enabled = false;
        Debug.Log("收到发射信号，正在监控白子运动状态...");
    }

    // 重新加载当前场景
    void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    void OnEliminated()
    {
        Debug.Log("【围棋消除】黑子四面被包围，触发 Destroy！过关！");

        // 销毁黑子
        Destroy(gameObject);

        // 延迟 0.5 秒或者直接进入下一关
        Invoke("LoadNextScene", 0.5f);
    }

    void LoadNextScene()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("已经是最后一关啦！");
        }
    }
}