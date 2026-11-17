using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CatPortalExitAcceleration : MonoBehaviour
{
    private Rigidbody body;
    private Vector3 direction;
    private float initialSpeed, targetSpeed, duration, elapsed, previousSpeed;
    private bool accelerating;
    private bool distanceBased;

    public void BeginDistance(Vector3 dashDirection, float distance, float endSpeed, float seconds)
    {
        float time = Mathf.Max(0.01f, seconds);
        float average = Mathf.Max(0.01f, distance) / time;
        float cruise = Mathf.Clamp(endSpeed, 0f, average);
        Begin(dashDirection, 2f * average - cruise, cruise, time);
        distanceBased = true;
    }

    public void Begin(Vector3 exitDirection, float startSpeed, float endSpeed, float seconds)
    {
        body = GetComponent<Rigidbody>();
        distanceBased = false;
        direction = exitDirection.normalized;
        initialSpeed = Mathf.Max(0f, startSpeed);
        // Also used by click-dash to settle a short burst back to cruising speed.
        targetSpeed = Mathf.Max(0f, endSpeed);
        duration = Mathf.Max(0.01f, seconds);
        elapsed = 0f;
        previousSpeed = initialSpeed;
        body.linearVelocity = direction * initialSpeed;
        accelerating = !Mathf.Approximately(targetSpeed, initialSpeed);
    }

    private void FixedUpdate()
    {
        if (!accelerating) return;
        if (!body || body.isKinematic) { accelerating = false; return; }
        float nextTime = Mathf.Min(duration, elapsed + Time.fixedDeltaTime);
        float speed = distanceBased ? AverageRampSpeed(initialSpeed, targetSpeed, duration,
            elapsed, Time.fixedDeltaTime) : Mathf.SmoothStep(initialSpeed, targetSpeed, nextTime / duration);
        elapsed = nextTime;
        // Add only the portal's acceleration; keep gravity, wind and other forces.
        body.linearVelocity += direction * (speed - previousSpeed);
        previousSpeed = speed;
        // After the last averaged dash step, settle to cruise on the next physics step.
        if (elapsed >= duration && (!distanceBased || Mathf.Approximately(speed, targetSpeed))) accelerating = false;
    }

    public static float AverageRampSpeed(float start, float end, float seconds, float time, float step)
    {
        float duration = Mathf.Max(0.01f, seconds);
        float dt = Mathf.Max(0.00001f, step);
        float a = Mathf.Clamp01(time / duration);
        float b = Mathf.Clamp01((time + dt) / duration);
        // Integral of SmoothStep(t) = t^3 - t^4/2. Averaging avoids distance
        // changing when the fixed timestep does not divide the dash duration.
        float integralA = a * a * a * (1f - 0.5f * a);
        float integralB = b * b * b * (1f - 0.5f * b);
        float rampTime = (b - a) * duration;
        float displacement = start * rampTime + (end - start) * duration * (integralB - integralA);
        return (displacement + end * (dt - rampTime)) / dt;
    }

    private void OnCollisionEnter(Collision collision) { StopOnObstacle(collision); }
    private void OnCollisionStay(Collision collision) { StopOnObstacle(collision); }

    private void StopOnObstacle(Collision collision)
    {
        // Once the cat reaches an obstacle, normal collision/split physics take over.
        if (accelerating && !CatPortal.IsPortalCollider(collision.collider)) accelerating = false;
    }

    public void Cancel() { accelerating = false; }

    private void OnDisable() { Cancel(); }
}
