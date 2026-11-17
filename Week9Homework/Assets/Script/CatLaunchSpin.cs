using UnityEngine;

// Rotate the appearance only, leaving the player's colliders and launch physics intact.
public class CatLaunchSpin : MonoBehaviour
{
    public Transform visualTarget;
    [Min(0)] public float initialSpeed = 60f;
    [Min(0)] public float maximumSpeed = 1440f;
    [Min(0.01f)] public float accelerationTime = 1.5f;
    [Header("点击后旋转并快速停下")]
    public bool burstAndDecay;
    [Tooltip("按住小猫时高速旋转，松开后减速；发射本身不自动旋转。")]
    public bool clickControlled;
    [Tooltip("点击后从最高转速减到完全停止的秒数。"), Min(0.01f)]
    public float spinDownTime = 0.5f;

    public float CurrentSpeed { get; private set; }
    public bool IsSpinning { get; private set; }
    Quaternion restingLocalRotation;
    Quaternion launchRotation;
    bool capturedRestPose;
    float elapsed;
    float angle;
    private bool clickHeld;

    public void EnableClickControl()
    {
        clickControlled = true;
        clickHeld = false;
        // Keep the visual orientation and physics motion; only stop automatic spin.
        CurrentSpeed = 0f;
        IsSpinning = false;
    }

    public void PressSpin()
    {
        if (!visualTarget) return;
        clickControlled = true;
        if (!IsSpinning)
        {
            CaptureRestPose();
            launchRotation = visualTarget.rotation;
            angle = 0f;
        }
        clickHeld = true;
        CurrentSpeed = Mathf.Max(CurrentSpeed, maximumSpeed);
        IsSpinning = true;
    }

    public void ReleaseSpin() { clickHeld = false; }

    public void BeginSpin()
    {
        if (clickControlled) return;
        if (!visualTarget) return;
        CaptureRestPose();
        launchRotation = visualTarget.rotation;
        elapsed = angle = 0f;
        CurrentSpeed = burstAndDecay ? maximumSpeed : initialSpeed;
        IsSpinning = true;
    }

    public void CopyMotionTo(CatLaunchSpin copy)
    {
        copy.initialSpeed = initialSpeed;
        copy.maximumSpeed = maximumSpeed;
        copy.accelerationTime = accelerationTime;
        copy.burstAndDecay = burstAndDecay;
        copy.spinDownTime = spinDownTime;
        copy.clickControlled = clickControlled;
        copy.clickHeld = clickHeld;
        if (!IsSpinning || !copy.visualTarget) return;
        copy.CaptureRestPose();
        copy.launchRotation = copy.visualTarget.rotation;
        copy.angle = 0f;
        copy.elapsed = elapsed;
        copy.CurrentSpeed = CurrentSpeed;
        copy.IsSpinning = true;
    }

    public void StopAndReset()
    {
        IsSpinning = false;
        clickHeld = false;
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
        if (clickControlled)
        {
            float previousSpeed = CurrentSpeed;
            CurrentSpeed = clickHeld ? Mathf.Max(CurrentSpeed, maximumSpeed) :
                GetDecayedSpeed(CurrentSpeed, maximumSpeed, spinDownTime, deltaTime);
            float movingTime = deltaTime;
            if (!clickHeld && maximumSpeed > 0f)
                movingTime = Mathf.Min(deltaTime, previousSpeed * Mathf.Max(.01f, spinDownTime) / maximumSpeed);
            angle = Mathf.Repeat(angle + (previousSpeed + CurrentSpeed) * .5f * movingTime, 360f);
            visualTarget.rotation = Quaternion.Euler(0f, angle, 0f) * launchRotation;
            if (CurrentSpeed <= 0f) IsSpinning = false;
            return;
        }
        if (burstAndDecay)
        {
            float stopTime = Mathf.Max(.01f, spinDownTime);
            float movingTime = Mathf.Min(deltaTime, Mathf.Max(0f, stopTime - elapsed));
            elapsed += deltaTime;
            float decayedSpeed = elapsed >= stopTime ? 0f :
                GetDecayedSpeed(maximumSpeed, maximumSpeed, stopTime, elapsed);
            angle = Mathf.Repeat(angle + (CurrentSpeed + decayedSpeed) * .5f * movingTime, 360f);
            CurrentSpeed = decayedSpeed;
            visualTarget.rotation = Quaternion.Euler(0f, angle, 0f) * launchRotation;
            // Leave the cat at its final orientation; stopping must not snap it back.
            if (CurrentSpeed <= 0f) IsSpinning = false;
            return;
        }
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

    public static float GetDecayedSpeed(float speed, float peakSpeed, float stopTime, float deltaTime)
    {
        return Mathf.MoveTowards(Mathf.Max(0f, speed), 0f,
            Mathf.Max(0f, peakSpeed) / Mathf.Max(.01f, stopTime) * Mathf.Max(0f, deltaTime));
    }

    void OnDisable() => StopAndReset();
}

