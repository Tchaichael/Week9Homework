using UnityEngine;

public class WindZone : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windForce = 5f;
    [Tooltip("Disable to keep hinged gates and spring bumpers anchored in strong wind.")]
    public bool affectJointedBodies = true;

    [Header("Wind Lines")]
    public bool showWindLines = true;
    [Range(1, 150)] public int lineCount = 45;
    [Min(0.01f)] public float lineLength = 3f;
    [Min(0.01f)] public float lineWidth = 0.36f;
    [InspectorName("Line Speed（线条移动速度）")]
    [Tooltip("风线沿 Wind Direction 穿过风区的速度，单位为世界单位/秒；与 Wave Speed 波动速度独立。"), Min(0f)]
    public float lineSpeed = 12f;
    public Color lineColor = new Color(0.75f, 0.94f, 1f, 0.5f);
    [Header("柔和曲线风效")]
    public bool softCurves;
    [Min(0f)] public float curveAmplitude = 2.8f;
    [Min(.1f)] public float curveWavelength = 45f;
    [HideInInspector] public float lineLifetime = 3f;
    [HideInInspector] public float fadeFraction = .25f;
    [Tooltip("只在风区边缘淡入淡出，风线会完整穿过风区。"), Min(.01f)]
    public float boundaryFadeDistance = 3f;
    [Tooltip("每条风线离开后，各自随机等待的秒数（最小 / 最大）。")]
    public Vector2 respawnDelay = new Vector2(.1f, 1.8f);
    [Tooltip("曲线内部波浪流动的速度，与整体平移速度独立。"), Min(0f)]
    public float waveSpeed = 2.2f;
    [Tooltip("叠加第二层柔和波动，避免所有线条形状一致。"), Range(0f, 1f)]
    public float secondaryWaveStrength = .35f;
    [Tooltip("整条风线轻微左右摆动的幅度。"), Min(0f)]
    public float swayAmplitude = 1.3f;

    private BoxCollider windBounds;
    private Material lineMaterial;
    private LineRenderer[] lines;
    private Vector3[] positions;
    private Vector3[] points;
    private float[] ages, phases;
    private Vector3[] startPositions;
    private float[] lifetimes;
    private Vector3 previousDirection;
    private float previousSpeed;

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
        ages = new float[count];
        phases = new float[count];
        startPositions = new Vector3[count];
        lifetimes = new float[count];
        previousDirection = windDirection.normalized * Mathf.Sign(windForce);
        previousSpeed = lineSpeed;
        points = new Vector3[softCurves ? 25 : 3];
        for (int i = 0; i < count; i++)
        {
            GameObject child = new GameObject("Wind Line");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.positionCount = points.Length;
            line.numCapVertices = softCurves ? 8 : 2;
            line.numCornerVertices = softCurves ? 4 : 0;
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.65f, 1f), new Keyframe(1f, 0f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            lines[i] = line;
            positions[i] = RandomPosition();
            ages[i] = Random.value * Mathf.Max(.1f, lineLifetime);
            phases[i] = Random.value * Mathf.PI * 2f;
            if (softCurves) ResetFlow(i, transform.InverseTransformVector(previousDirection), true);
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
        AdvanceVisuals(Time.deltaTime, Time.time);
    }

    public void AdvanceVisuals(float deltaTime, float animationTime)
    {
        if (lines == null || windBounds == null) return;
        Vector3 direction = windDirection.normalized * Mathf.Sign(windForce);
        bool visible = showWindLines && windBounds.enabled &&
            direction.sqrMagnitude > 0f && !Mathf.Approximately(windForce, 0f);
        Vector3 localDirection = transform.InverseTransformVector(direction);
        Bounds bounds = new Bounds(windBounds.center, windBounds.size);
        lineMaterial.SetFloat("_ClipWind", softCurves ? 1f : 0f);
        if (softCurves)
        {
            lineMaterial.SetMatrix("_WindWorldToLocal", transform.worldToLocalMatrix);
            lineMaterial.SetVector("_WindBoundsMin", bounds.min);
            lineMaterial.SetVector("_WindBoundsMax", bounds.max);
            Vector3 scale = transform.lossyScale;
            lineMaterial.SetVector("_WindFeather", new Vector4(
                boundaryFadeDistance / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                boundaryFadeDistance / Mathf.Max(.001f, Mathf.Abs(scale.y)),
                boundaryFadeDistance / Mathf.Max(.001f, Mathf.Abs(scale.z)), 0f));
        }
        if (softCurves && (direction != previousDirection || !Mathf.Approximately(lineSpeed, previousSpeed)))
        {
            for (int i = 0; i < lines.Length; i++) ResetFlow(i, localDirection, true);
        }
        previousDirection = direction;
        previousSpeed = lineSpeed;
        int samples = softCurves ? 25 : 3;
        if (points == null || points.Length != samples) points = new Vector3[samples];
        for (int i = 0; i < lines.Length; i++)
        {
            LineRenderer line = lines[i];
            line.enabled = visible;
            if (!visible) continue;
            ages[i] += Mathf.Max(0f, deltaTime);
            if (softCurves)
            {
                if (ages[i] >= lifetimes[i]) ResetFlow(i, localDirection, false);
                // Advection is independent of the waves: always move along the actual wind.
                positions[i] = startPositions[i] + localDirection * (Mathf.Max(0f, lineSpeed) * Mathf.Max(0f, ages[i]));
                line.enabled = ages[i] >= 0f;
            }
            else
            {
                positions[i] += localDirection * (Mathf.Max(0f, lineSpeed) * deltaTime);
                if (!bounds.Contains(positions[i])) positions[i] = RandomPosition();
            }

            // Clip both ends to the actual collider, including rotated/scaled zones.
            float ahead = DistanceToEdge(positions[i], localDirection, bounds);
            float behind = DistanceToEdge(positions[i], -localDirection, bounds);
            float halfLength = Mathf.Max(0.01f, lineLength) * 0.5f;
            Vector3 center = transform.TransformPoint(positions[i]);
            if (softCurves)
            {
                Vector3 sideways = Vector3.Cross(Vector3.forward, direction).normalized;
                for (int sample = 0; sample < samples; sample++)
                {
                    float t = sample / (float)(samples - 1);
                    float along = Mathf.Lerp(-halfLength, halfLength, t);
                    float bend = SampleCurveOffset(along, t, animationTime, phases[i]);
                    // Keep the entire moving curve intact. The material clips/fades only
                    // the fragments outside the wind zone, so its tail can fully leave.
                    points[sample] = center + direction * along + sideways * bend;
                }
            }
            else
            {
                points[0] = center - direction * Mathf.Min(halfLength, behind);
                points[1] = center;
                points[2] = center + direction * Mathf.Min(halfLength, ahead);
            }
            line.positionCount = samples;
            line.SetPositions(points);
            line.widthMultiplier = Mathf.Max(0.01f, lineWidth);
            Color color = lineColor;
            if (!softCurves) color.a *= Mathf.Clamp01(Mathf.Min(ahead, behind) / halfLength);
            line.startColor = color;
            line.endColor = color;
        }
    }

    private void ResetFlow(int index, Vector3 localDirection, bool stagger)
    {
        Bounds bounds = new Bounds(windBounds.center, windBounds.size);
        Vector3 start = Vector3.Lerp(windBounds.center, RandomPosition(), .8f);
        // Trace one full chord across the box; spawn with the whole curve outside
        // the entry face, and recycle only after the entire tail clears the exit.
        float behind = DistanceToEdge(start, -localDirection, bounds);
        float ahead = DistanceToEdge(start, localDirection, bounds);
        float padding = Mathf.Max(.01f, lineLength) * .5f + curveAmplitude + swayAmplitude + lineWidth * 2f;
        if (!float.IsInfinity(behind)) start -= localDirection * (behind + padding);
        float travel = behind + ahead + 2f * padding;
        float duration = lineSpeed > .001f ? travel / lineSpeed : float.PositiveInfinity;
        lifetimes[index] = Mathf.Max(.01f, duration);
        startPositions[index] = start;
        // Scatter startup across a whole journey, not a short shared burst. Every
        // subsequent line also gets its own new random pause after fully exiting.
        float minDelay = Mathf.Max(0f, Mathf.Min(respawnDelay.x, respawnDelay.y));
        float maxDelay = Mathf.Max(minDelay, Mathf.Max(respawnDelay.x, respawnDelay.y));
        float startupWindow = float.IsInfinity(lifetimes[index]) ? maxDelay : lifetimes[index] + maxDelay;
        ages[index] = stagger ? -Random.Range(0f, startupWindow) : -Random.Range(minDelay, maxDelay);
        phases[index] = Random.value * Mathf.PI * 2f;
        positions[index] = start;
    }

    public float SampleCurveOffset(float along, float t, float time, float phase)
    {
        float wave = along / Mathf.Max(.1f, curveWavelength) * Mathf.PI * 2f;
        float clock = time * waveSpeed * (1f + .12f * Mathf.Sin(phase));
        float primary = Mathf.Sin(wave - clock + phase);
        float secondary = Mathf.Sin(wave * 1.7f + clock * .73f + phase * 1.93f);
        float breathing = .8f + .2f * Mathf.Sin(clock * .43f + phase);
        float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        float bend = (primary + secondary * secondaryWaveStrength) /
            (1f + secondaryWaveStrength) * curveAmplitude * breathing * envelope;
        // Slow side-to-side drift and the two traveling waves deform the line over time.
        return bend + Mathf.Sin(clock * .55f + phase) * swayAmplitude;
    }

    public static float FadeEnvelope(float age, float fraction)
    {
        float fade = Mathf.Clamp(fraction, .05f, .5f);
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / fade)) *
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - age) / fade));
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
                if (line != null)
                {
                    if (Application.isPlaying) Destroy(line.gameObject);
                    else DestroyImmediate(line.gameObject);
                }
        if (lineMaterial != null)
        {
            if (Application.isPlaying) Destroy(lineMaterial);
            else DestroyImmediate(lineMaterial);
        }
        lines = null;
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && !rb.isKinematic &&
            (affectJointedBodies || rb.GetComponent<Joint>() == null))
        {
            rb.AddForce(
                windDirection.normalized * windForce,
                ForceMode.Force
            );
        }
    }
}
