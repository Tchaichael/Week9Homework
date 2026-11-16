using UnityEngine;

// A visible connection follows the real physics anchors, in edit mode and play mode.
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class JointLinkVisual : MonoBehaviour
{
    public Joint joint;
    public bool spring;
    private readonly Vector3[] points = new Vector3[17];

    private void LateUpdate()
    {
        if (joint == null) return;
        Vector3 start = joint.transform.TransformPoint(joint.anchor);
        Vector3 end = joint.connectedBody == null ? joint.connectedAnchor :
            joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
        Vector3 across = Vector3.Cross((end - start).normalized, Vector3.forward);
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            float wave = spring && i > 0 && i < points.Length - 1 ?
                (i % 2 == 0 ? 1f : -1f) * 1.3f : 0f;
            points[i] = Vector3.Lerp(start, end, t) + across * wave + Vector3.back * 3f;
        }
        LineRenderer line = GetComponent<LineRenderer>();
        line.positionCount = points.Length;
        line.SetPositions(points);
    }
}
