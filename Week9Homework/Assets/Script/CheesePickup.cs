using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
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
    [SerializeField, HideInInspector] private Vector3 visualPivot;
    private Vector3 restPosition, restScale, pivotInParent;
    private Quaternion restRotation;
    private float phase;
    private bool captured;
    public bool IsCollected { get; private set; }

    private void Awake()
    {
        phase = Random.value * Mathf.PI * 2f;
        CaptureRestPose();
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

    private void LateUpdate() { ApplyAnimation(Time.time); }

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
        if (IsCollected || gameObject.scene.name != "2" || !other) return false;
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
        if (!captured || !visualTarget) return;
        visualTarget.localPosition = restPosition;
        visualTarget.localRotation = restRotation;
        visualTarget.localScale = restScale;
    }
}
