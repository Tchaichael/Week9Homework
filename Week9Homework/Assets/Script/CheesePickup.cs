using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
[ExecuteAlways]
public class CheesePickup : MonoBehaviour
{
    [Header("拾取音效（拖入你自己的音频）")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 1f;
    [Header("奶酪轻微动效")]
    public Transform visualTarget;
    [Range(0f, .2f)] public float pulseAmount = .04f;
    [Min(.1f)] public float pulsePeriod = 2.5f;
    public float rotationSpeed = 18f;
    [Header("拾取范围自动对齐")]
    [Tooltip("自动把拾取触发器对齐可见奶酪，移动 Cheese Visual 后也会更新。")]
    public bool autoFitTrigger = true;
    [Range(0f, .5f)] public float triggerPadding = .08f;
    public float gameplayDepth = -79f;
    [Min(.1f)] public float gameplayDepthPadding = 4f;
    [SerializeField, HideInInspector] private Vector3 visualPivot;
    private Vector3 restPosition, restScale, pivotInParent;
    private Quaternion restRotation;
    private float phase;
    private bool captured;
    private Renderer[] visualRenderers;
    private Transform cachedVisual;
    public bool IsCollected { get; private set; }

    private void Awake()
    {
        phase = Random.value * Mathf.PI * 2f;
        CaptureRestPose();
        RefreshPickupBounds();
        if (Application.isPlaying)
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
                if (collider.gameObject != gameObject) collider.enabled = false;
        }
    }

    public void SetVisualPivot(Vector3 worldCenter)
    {
        if (visualTarget) visualPivot = visualTarget.InverseTransformPoint(worldCenter);
        CaptureRestPose();
    }

    public void CaptureRestPose()
    {
        if (!visualTarget) return;
        restPosition = visualTarget.localPosition;
        restScale = visualTarget.localScale;
        restRotation = visualTarget.localRotation;
        pivotInParent = restPosition + restRotation * Vector3.Scale(restScale, visualPivot);
        captured = true;
    }

    private void LateUpdate()
    {
        if (Application.isPlaying) ApplyAnimation(Time.time);
        RefreshPickupBounds();
    }

    private void FixedUpdate() { RefreshPickupBounds(); }

    private void OnValidate() { RefreshPickupBounds(); }

    public void RefreshPickupBounds()
    {
        if (!autoFitTrigger || IsCollected || !visualTarget) return;
        if (visualRenderers == null || cachedVisual != visualTarget)
        {
            cachedVisual = visualTarget;
            visualRenderers = visualTarget.GetComponentsInChildren<Renderer>(true);
        }
        var collider = GetComponent<BoxCollider>();
        if (!collider) return;
        bool found = false;
        Bounds localBounds = default;
        foreach (var renderer in visualRenderers)
        {
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Bounds bounds = renderer.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                Vector3 local = transform.InverseTransformPoint(world);
                if (!found) { localBounds = new Bounds(local, Vector3.zero); found = true; }
                else localBounds.Encapsulate(local);
                // Keep the pickup volume crossing the cat's XY gameplay plane.
                world.z = gameplayDepth - gameplayDepthPadding;
                localBounds.Encapsulate(transform.InverseTransformPoint(world));
                world.z = gameplayDepth + gameplayDepthPadding;
                localBounds.Encapsulate(transform.InverseTransformPoint(world));
            }
        }
        if (!found) return;
        Vector3 size = localBounds.size * (1f + Mathf.Max(0f, triggerPadding));
        size = Vector3.Max(size, Vector3.one * .01f);
        if (collider.center != localBounds.center) collider.center = localBounds.center;
        if (collider.size != size) collider.size = size;
        collider.isTrigger = true;
    }

    public void ApplyAnimation(float time)
    {
        if (IsCollected || !visualTarget) return;
        if (!captured) CaptureRestPose();
        float scale = 1f + Mathf.Sin(time * Mathf.PI * 2f / Mathf.Max(.1f, pulsePeriod) + phase) * pulseAmount;
        visualTarget.localScale = restScale * scale;
        visualTarget.localRotation = Quaternion.AngleAxis(time * rotationSpeed, Vector3.up) * restRotation;
        // Rotate around the visible cheese center even when the imported mesh pivot is offset.
        visualTarget.localPosition = pivotInParent -
            visualTarget.localRotation * Vector3.Scale(visualTarget.localScale, visualPivot);
    }

    private void OnTriggerEnter(Collider other) { TryCollect(other); }
    private void OnTriggerStay(Collider other) { TryCollect(other); }

    public bool TryCollect(Collider other)
    {
        if (IsCollected || !other) return false;
        Rigidbody cat = other.attachedRigidbody;
        if (!cat || (!cat.GetComponent<BallForces>() && !cat.GetComponent<CollisionSplit>())) return false;
        IsCollected = true;
        foreach (Collider collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        if (pickupSound)
        {
            // A separate 2D source survives the cheese disappearing and stays audible
            // despite the game's large world-space distance from the camera.
            var sound = new GameObject("Cheese Pickup Sound");
            SceneManager.MoveGameObjectToScene(sound, gameObject.scene);
            var source = sound.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.clip = pickupSound;
            source.volume = soundVolume;
            source.Play();
            Destroy(sound, Mathf.Max(.1f, pickupSound.length) + .1f);
        }
        gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject);
        return true;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (!captured || !visualTarget) return;
        visualTarget.localPosition = restPosition;
        visualTarget.localRotation = restRotation;
        visualTarget.localScale = restScale;
    }
}
