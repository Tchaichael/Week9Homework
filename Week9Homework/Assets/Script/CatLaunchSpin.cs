using UnityEngine;

// Rotate the appearance only, leaving the player's colliders and launch physics intact.
public class CatLaunchSpin : MonoBehaviour
{
    public Transform visualTarget;
    [Min(0)] public float initialSpeed = 60f;
    [Min(0)] public float maximumSpeed = 1440f;
    [Min(0.01f)] public float accelerationTime = 1.5f;

    public float CurrentSpeed { get; private set; }
    public bool IsSpinning { get; private set; }
    Quaternion restingLocalRotation;
    Quaternion launchRotation;
    bool capturedRestPose;
    float elapsed;
    float angle;

    public void BeginSpin()
    {
        if (!visualTarget) return;
        CaptureRestPose();
        launchRotation = visualTarget.rotation;
        elapsed = angle = 0f;
        CurrentSpeed = initialSpeed;
        IsSpinning = true;
    }

    public void StopAndReset()
    {
        IsSpinning = false;
        CurrentSpeed = elapsed = angle = 0f;
        if (!visualTarget) return;
        CaptureRestPose();
        visualTarget.localRotation = restingLocalRotation;
    }

    void CaptureRestPose()
    {
        if (capturedRestPose) return;
        restingLocalRotation = visualTarget.localRotation;
        capturedRestPose = true;
    }

    void LateUpdate() => AdvanceSpin(Time.deltaTime);

    void AdvanceSpin(float deltaTime)
    {
        if (!IsSpinning || !visualTarget || deltaTime <= 0f) return;
        float duration = Mathf.Max(.01f, accelerationTime);
        float rampTime = Mathf.Min(deltaTime, Mathf.Max(0f, duration - elapsed));
        float nextSpeed = Mathf.Lerp(initialSpeed, maximumSpeed,
            Mathf.Clamp01((elapsed + rampTime) / duration));
        // Integrate the acceleration, including a frame crossing the speed limit.
        angle = Mathf.Repeat(angle + (CurrentSpeed + nextSpeed) * .5f * rampTime
            + maximumSpeed * (deltaTime - rampTime), 360f);
        elapsed += deltaTime;
        CurrentSpeed = nextSpeed;
        // Rotate around world Y only; add no X or Z rotation.
        visualTarget.rotation = Quaternion.Euler(0f, angle, 0f) * launchRotation;
    }

    void OnDisable() => StopAndReset();
}

