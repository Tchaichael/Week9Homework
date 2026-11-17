//using UnityEngine;
//using UnityEngine.InputSystem;

//public class BallForces : MonoBehaviour
//{

//    private Rigidbody rb;
//    public float movementForce;
//    public float jumpForce;
//    public float clickForce;
//    void Start()
//    {
//        rb = GetComponent<Rigidbody>();
//    }


//    void Update()
//    {
//        if (Keyboard.current.spaceKey.wasPressedThisFrame)
//        {
//            rb.AddForce(Vector3.up*jumpForce,ForceMode.Impulse);
//        }
//        if (Mouse .current .leftButton .wasPressedThisFrame)
//        {
//            Ray mouseCameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
//            RaycastHit mouseRaycastHit;

//            if (Physics .Raycast (mouseCameraRay, out mouseRaycastHit))
//            {
//                if(mouseRaycastHit .collider .gameObject ==this.gameObject )
//                {

//                    Vector3 forceDirection = transform.position - mouseRaycastHit.point;
//                    forceDirection.Normalize();
//                    rb.AddForceAtPosition(forceDirection * clickForce,
//                   mouseRaycastHit.point, ForceMode.Impulse);
//                }
//            }
//        }
//    }
//    void FixedUpdate()
//    {
//        if (Keyboard.current.wKey.isPressed)
//        {
//            rb.AddForce(Vector3.forward * movementForce, ForceMode.Force );
//        }
//        if (Keyboard.current.sKey.isPressed)
//        {
//            rb.AddForce(Vector3.back*movementForce, ForceMode.Force);
//        }
//        if (Keyboard.current.aKey.isPressed)
//        {
//            rb.AddForce(Vector3.left * movementForce, ForceMode.Force);
//        }
//        if (Keyboard.current.dKey.isPressed)
//        {
//            rb.AddForce(Vector3.right * movementForce, ForceMode.Force);
//        }

//    }
//}


using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BallForces : MonoBehaviour
{
    [Header("Drag Settings")]

    public float minX = -237.5f;
    public float maxX = 385f;

    public GameObject retryButton;

    public float minY = 213f;
    public float maxY = 372f;

    // 鼠标最多可以从初始位置拖动多少像素
    public float maxMouseDrag = 300f;

    // 鼠标移动到世界坐标时的灵敏度
    public float dragSensitivity = 0.01f;
    [Tooltip("以进入场景时的小猫位置为中心设置拖拽范围，适合移动了出生点的关卡。")]
    public bool dragBoundsFollowSpawn;

    [Header("Sound")]
    public AudioClip hitAudioClip; 
    private AudioSource audioSource;

    [Header("Launch Settings")]

    // 最小发射力度
    public float minLaunchForce = 10f;

    // 最大发射力度
    public float maxLaunchForce = 100f;


    [Header("Physics Settings")]

    // 是否在发射后使用重力
    public bool useGravity = true;


    private Rigidbody rb;

    // 白子开始拖动时的位置
    private Vector3 startPosition;

    // 开始拖动时鼠标的位置
    private Vector2 mouseStartPosition;

    // 当前是否正在拖动
    private bool isDragging = false;
    private bool hasCompletedLevel;


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = useGravity;

        // 游戏开始时，白子固定在原位置
        rb.isKinematic = true;

        startPosition = transform.position;
        if (dragBoundsFollowSpawn)
        {
            float range = Mathf.Abs(maxMouseDrag * dragSensitivity);
            minX = startPosition.x - range;
            maxX = startPosition.x + range;
            minY = startPosition.y - range;
            maxY = startPosition.y + range;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }


    void Update()
    {
        // Scenes with click-dash switch permanently from aiming after launch.
        var clickDash = GetComponent<CatClickDash>();
        if (clickDash != null && clickDash.IsArmed) return;
        if (Mouse.current == null) return;
        var splitter = GetComponent<CollisionSplit>();
        if (splitter != null && splitter.HasSplit) return;
        // =========================
        // 鼠标按下
        // =========================

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDragging();
        }


        // =========================
        // 鼠标拖动
        // =========================

        if (
            isDragging &&
            Mouse.current.leftButton.isPressed
        )
        {
            DragBall();
        }


        // =========================
        // 鼠标松开
        // =========================

        if (
            isDragging &&
            Mouse.current.leftButton.wasReleasedThisFrame
        )
        {
            LaunchBall();
        }
    }


    // =====================================================
    // 检查玩家有没有点到白子
    // =====================================================

    void TryStartDragging()
    {
        Ray ray = Camera.main.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                StartDragging();
            }
        }
    }


    // =====================================================
    // 开始拖动
    // =====================================================

    void StartDragging()
    {
        GetComponent<CatLaunchSpin>()?.StopAndReset();
        isDragging = true;

        // 记录白子开始拖动的位置
        startPosition = transform.position;

        // 记录鼠标开始拖动的位置
        mouseStartPosition =
            Mouse.current.position.ReadValue();

        // 拖动期间不让 Rigidbody 干扰白子位置
        rb.isKinematic = true;
    }


    // =====================================================
    // 拖动白子
    // =====================================================

    void DragBall()
    {
        // 当前鼠标位置
        Vector2 currentMousePosition =
            Mouse.current.position.ReadValue();

        // 鼠标从开始位置移动了多少
        Vector2 mouseDelta =
            currentMousePosition - mouseStartPosition;

        transform.position = GetDragPosition(mouseDelta);
    }

    public Vector3 GetDragPosition(Vector2 mouseDelta)
    {


        // 限制鼠标移动范围
        mouseDelta.x = Mathf.Clamp(
            mouseDelta.x,
            -maxMouseDrag,
            maxMouseDrag
        );

        mouseDelta.y = Mathf.Clamp(
            mouseDelta.y,
            -maxMouseDrag,
            maxMouseDrag
        );


        // 鼠标移动 → Unity 世界坐标移动
        float newX =
            startPosition.x +
            mouseDelta.x * dragSensitivity;

        float newY =
            startPosition.y +
            mouseDelta.y * dragSensitivity;

        newX = Mathf.Clamp(newX, minX, maxX);
        newY = Mathf.Clamp(newY, minY, maxY);

        // Z 永远保持原来的位置
        float newZ =
            startPosition.z;


        // 设置白子位置
        return new Vector3(
            newX,
            newY,
            newZ
        );
        
        
    }


    // =====================================================
    // 松开鼠标 → 发射
    // =====================================================

    void LaunchBall()
    {
        isDragging = false;


        // ==========================================
        // 计算拖动距离
        // ==========================================

        Vector3 dragVector =
            transform.position - startPosition;

        float dragDistance =
            dragVector.magnitude;


        // 如果基本没拖动
        // 就不发射
        if (dragDistance < 0.01f)
        {
            transform.position = startPosition;

            rb.isKinematic = false;

            return;
        }


        // ==========================================
        // 计算发射方向
        // ==========================================

        // 拖动方向的反方向
        Vector3 launchDirection =
            -dragVector.normalized;


        // ==========================================
        // 计算发射力度
        // ==========================================

        // 0 = 完全没有拉
        // 1 = 拉到最大范围

        float dragPercent =
            Mathf.Clamp01(
                dragDistance /
                (maxMouseDrag * dragSensitivity)
            );


        // 根据拖动距离计算力量

        float launchForce =
            Mathf.Lerp(
                minLaunchForce,
                maxLaunchForce,
                dragPercent
            );


        // ==========================================
        // 恢复 Rigidbody Physics
        // ==========================================

        rb.isKinematic = false;


        // ==========================================
        // 发射
        // ==========================================

        rb.isKinematic = false;

        if (retryButton != null) retryButton.SetActive(true);

        var splitter = GetComponent<CollisionSplit>();
        if (splitter != null) splitter.NotifyLaunched();


        rb.AddForce(
            launchDirection * launchForce,
            ForceMode.Impulse
        );
        GetComponent<CatClickDash>()?.Arm(rb);
        GetComponent<CatLaunchSpin>()?.BeginSpin();
        BallGameManager gm = GetComponent<BallGameManager>();
        if (gm != null)
        {
            gm.TriggerLaunch();
        }
    }
    private void OnCollisionEnter(Collision collision)
{
    Debug.Log("Ball hit: " + collision.gameObject.name);

        if (hitAudioClip != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(hitAudioClip);
        }

        TryCompleteLevel(collision.collider);

       
    }
 
    public bool TryCompleteLevel(Collider other)
    {
        Transform target = other.transform;
        while (target != null && target.name != "wincondition") target = target.parent;
        if (target == null) return false;
        if (hasCompletedLevel) return true;
        hasCompletedLevel = true;
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextScene < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(nextScene);
        return true;
    }

    private void OnTriggerEnter(Collider other) { TryCompleteLevel(other); }
}
