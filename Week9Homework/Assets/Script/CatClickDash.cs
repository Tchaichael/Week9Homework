using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// One input controller owns both the original cat and its split partner.
[RequireComponent(typeof(Rigidbody))]
public class CatClickDash : MonoBehaviour
{
    [Header("发射后点击冲刺")]
    [Tooltip("小猫碰撞体外围额外可点击的像素范围，以 1080p 为基准。"), Min(0f)]
    public float extraClickRadius = 80f;
    [InspectorName("Dash Distance（点击冲刺距离）")]
    [Tooltip("每次点击的短冲刺距离，单位为世界单位；结束后仍以 Cruise Speed 移动。"), Min(0.01f)]
    public float dashDistance = 6f;
    [Tooltip("短冲刺后保留的移动速度。"), Min(0f)]
    public float cruiseSpeed = 20f;
    [Tooltip("从冲刺速度回落到普通移动速度的秒数。"), Min(0.01f)]
    public float dashDuration = 0.15f;
    [Min(0f)] public float dashCooldown = 0.2f;
    [SerializeField, HideInInspector] private bool armed;
    public bool IsArmed => armed;
    [SerializeField, HideInInspector] private List<Rigidbody> cats = new List<Rigidbody>();
    private float nextDashTime;
    private CinemachineCamera followCamera;

    public void Arm(Rigidbody cat)
    {
        RegisterPiece(cat);
        cat.GetComponent<CatLaunchSpin>()?.EnableClickControl();
        armed = true;
    }

    public void RegisterPiece(Rigidbody cat)
    {
        if (cat && !cats.Contains(cat)) cats.Add(cat);
    }

    private void Update()
    {
        if (!IsArmed) return;
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed) ReleaseAllSpins();
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
        Camera camera = Camera.main;
        if (!camera) return;
        Vector2 mouse = Mouse.current.position.ReadValue();
        Rigidbody selected = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Rigidbody cat in cats)
        {
            if (!cat || cat.isKinematic || !cat.gameObject.activeInHierarchy) continue;
            Collider collider = cat.GetComponent<Collider>();
            if (!collider || !collider.enabled) continue;
            Bounds bounds = collider.bounds;
            Vector3 screenCenter = camera.WorldToScreenPoint(bounds.center);
            if (screenCenter.z <= 0f) continue;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            Vector2 screenEdge = camera.WorldToScreenPoint(bounds.center + camera.transform.right * radius);
            float clickRadius = Vector2.Distance(screenCenter, screenEdge) +
                extraClickRadius * Screen.height / 1080f;
            float distance = Vector2.Distance(mouse, screenCenter);
            if (distance > clickRadius || distance >= closestDistance) continue;
            selected = cat;
            closestDistance = distance;
        }
        if (!selected) return;
        FollowClickedCat(selected);
        ReleaseAllSpins();
        selected.GetComponent<CatLaunchSpin>()?.PressSpin();
        // Every valid click spins immediately, even while the movement dash is cooling down.
        if (Time.time < nextDashTime) return;
        nextDashTime = Time.time + dashCooldown;
        // Intersect the mouse ray with the cat's XY gameplay plane, not scenery behind it.
        Plane plane = new Plane(Vector3.forward, selected.position);
        Ray ray = camera.ScreenPointToRay(mouse);
        if (!plane.Raycast(ray, out float enter)) return;
        if (!TryGetDashDirection(selected.position, ray.GetPoint(enter), out Vector3 direction)) return;
        var motion = selected.GetComponent<CatPortalExitAcceleration>();
        if (!motion) motion = selected.gameObject.AddComponent<CatPortalExitAcceleration>();
        motion.BeginDistance(direction, dashDistance, cruiseSpeed, dashDuration);
        selected.WakeUp();
        GetComponent<BallGameManager>()?.TriggerLaunch();
    }

    public void FollowClickedCat(Rigidbody selected)
    {
        if (gameObject.scene.name != "2" || cats.Count < 2 || !selected || !cats.Contains(selected)) return;
        if (!followCamera)
        {
            // Pick only the camera already following one of this player's cats.
            foreach (var candidate in Object.FindObjectsByType<CinemachineCamera>())
            {
                if (!candidate.isActiveAndEnabled || candidate.gameObject.scene != gameObject.scene) continue;
                foreach (Rigidbody cat in cats)
                {
                    if (!cat || candidate.Follow != cat.transform) continue;
                    followCamera = candidate;
                    break;
                }
                if (followCamera) break;
            }
        }
        if (!followCamera) return;
        Transform previous = followCamera.Follow;
        followCamera.Follow = selected.transform;
        if (followCamera.Target.CustomLookAtTarget && followCamera.Target.LookAtTarget == previous)
            followCamera.LookAt = selected.transform;
    }

    private void ReleaseAllSpins()
    {
        foreach (Rigidbody cat in cats)
            if (cat) cat.GetComponent<CatLaunchSpin>()?.ReleaseSpin();
    }

    private void OnApplicationFocus(bool focused) { if (!focused) ReleaseAllSpins(); }
    private void OnDisable() { ReleaseAllSpins(); }

    public static bool TryGetDashDirection(Vector3 catPosition, Vector3 mousePosition, out Vector3 direction)
    {
        direction = catPosition - mousePosition;
        direction.z = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;
        direction.Normalize();
        return true;
    }
}
