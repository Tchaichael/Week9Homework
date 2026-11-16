using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windForce = 5f;

    [Header("Wind Lines")]
    public bool showWindLines = true;
    [Range(1, 150)] public int lineCount = 45;
    [Min(0.01f)] public float lineLength = 3f;
    [Min(0.01f)] public float lineWidth = 0.36f;
    [Min(0f)] public float lineSpeed = 12f;
    public Color lineColor = new Color(0.75f, 0.94f, 1f, 0.5f);

    private BoxCollider windBounds;
    private Material lineMaterial;
    private LineRenderer[] lines;
    private Vector3[] positions;
    private readonly Vector3[] points = new Vector3[3];

    private void OnEnable()
    {
        windBounds = GetComponent<BoxCollider>();
        if (windBounds == null) return;

        Shader shader = Resources.Load<Shader>("WindLines");
        if (shader == null) return;
        lineMaterial = new Material(shader);
        int count = Mathf.Clamp(lineCount, 1, 150);
        lines = new LineRenderer[count];
        positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            GameObject child = new GameObject("Wind Line");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.numCapVertices = 2;
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.65f, 1f), new Keyframe(1f, 0f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            lines[i] = line;
            positions[i] = RandomPosition();
        }
    }

    private Vector3 RandomPosition()
    {
        Vector3 half = windBounds.size * 0.5f;
        return windBounds.center + new Vector3(
            Random.Range(-half.x, half.x), Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z));
    }

    private void LateUpdate()
    {
        if (lines == null || windBounds == null) return;
        Vector3 direction = windDirection.normalized * Mathf.Sign(windForce);
        bool visible = showWindLines && windBounds.enabled &&
            direction.sqrMagnitude > 0f && !Mathf.Approximately(windForce, 0f);
        Vector3 localDirection = transform.InverseTransformVector(direction);
        Bounds bounds = new Bounds(windBounds.center, windBounds.size);
        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer line = lines[i];
            line.enabled = visible;
            if (!visible) continue;
            positions[i] += localDirection * (Mathf.Max(0f, lineSpeed) * Time.deltaTime);
            if (!bounds.Contains(positions[i]))
                positions[i] = RandomPosition();

            // Clip both ends to the actual collider, including rotated/scaled zones.
            float ahead = DistanceToEdge(positions[i], localDirection, bounds);
            float behind = DistanceToEdge(positions[i], -localDirection, bounds);
            float halfLength = Mathf.Max(0.01f, lineLength) * 0.5f;
            Vector3 center = transform.TransformPoint(positions[i]);
            points[0] = center - direction * Mathf.Min(halfLength, behind);
            points[1] = center;
            points[2] = center + direction * Mathf.Min(halfLength, ahead);
            line.SetPositions(points);
            line.widthMultiplier = Mathf.Max(0.01f, lineWidth);
            Color color = lineColor;
            color.a *= Mathf.Clamp01(Mathf.Min(ahead, behind) / halfLength);
            line.startColor = color;
            line.endColor = color;
        }
    }

    private static float DistanceToEdge(Vector3 position, Vector3 direction, Bounds bounds)
    {
        float distance = float.PositiveInfinity;
        for (int axis = 0; axis < 3; axis++)
        {
            if (Mathf.Abs(direction[axis]) < 0.000001f) continue;
            float edge = direction[axis] > 0f ? bounds.max[axis] : bounds.min[axis];
            distance = Mathf.Min(distance, (edge - position[axis]) / direction[axis]);
        }
        return Mathf.Max(0f, distance);
    }

    private void OnDisable()
    {
        if (lines != null)
            foreach (LineRenderer line in lines)
                if (line != null) Destroy(line.gameObject);
        if (lineMaterial != null) Destroy(lineMaterial);
        lines = null;
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(
                windDirection.normalized * windForce,
                ForceMode.Force
            );
        }
    }
}
