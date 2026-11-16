using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SlimeStage : MonoBehaviour
{
    public Color whiteSlime = new Color(0.65f, 0.88f, 1f);
    public Color blackSlime = new Color(0.025f, 0.035f, 0.055f);
    [Range(0.1f, 0.9f)] public float blend = 0.6f;
    [Range(0.25f, 1f)] public float thickness = 0.65f;
    [Range(0f, 0.06f)] public float wobble = 0.018f;
    public bool stickyContact = true;

    private class Piece
    {
        public Renderer source;
        public Collider collider;
        public MeshRenderer surface;
        public bool white;
        public Vector3 center;
        public float radius;
        public MaterialPropertyBlock properties = new MaterialPropertyBlock();
    }
    private readonly List<Piece> pieces = new List<Piece>();
    private readonly HashSet<(SlimeBody, SlimeBody)> bonds = new HashSet<(SlimeBody, SlimeBody)>();
    private readonly List<FixedJoint> joints = new List<FixedJoint>();
    private readonly Vector4[] neighbors = new Vector4[24];
    private Material material;
    private Mesh cube;
    private float nextDiscovery;

    private void OnEnable()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded || gameObject.scene.name != "3") return;
        if (material != null) return;
        Shader shader = Resources.Load<Shader>("ChessSlime");
        if (shader == null) return;
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = template.GetComponent<MeshFilter>().sharedMesh;
        template.GetComponent<Collider>().enabled = false;
        template.SetActive(false);
        Release(template);
        Discover();
    }

    private void Discover()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        foreach (MeshFilter mesh in root.GetComponentsInChildren<MeshFilter>(false))
        {
            GameObject go = mesh.gameObject;
            if (!(go.name.StartsWith("model__159") || go.name == "wincondition" || go.GetComponent<BallForces>() != null)) continue;
            Renderer source = go.GetComponent<Renderer>();
            Collider col = go.GetComponent<Collider>();
            if (source == null || col == null || !col.enabled || col.isTrigger) continue;
            Piece existing = pieces.Find(p => p.source == source);
            if (existing != null)
            {
                EnsureInteraction(go, existing.white);
                continue;
            }
            // Remove transient surfaces left over from an editor domain reload.
            Transform previous = go.transform.Find("_SlimeSurface");
            if (previous != null) Release(previous.gameObject);
            bool white = go.GetComponent<BallForces>() != null || IsWhite(source.sharedMaterial);
            var visual = new GameObject("_SlimeSurface") { hideFlags = HideFlags.DontSave };
            visual.transform.SetParent(go.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = cube;
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            source.forceRenderingOff = true;
            pieces.Add(new Piece { source = source, collider = col, surface = renderer, white = white });
            EnsureInteraction(go, white);
        }
        nextDiscovery = Time.realtimeSinceStartup + 1f;
    }

    private void EnsureInteraction(GameObject go, bool white)
    {
        if (!Application.isPlaying) return;
        var body = go.GetComponent<SlimeBody>();
        if (body == null) body = go.AddComponent<SlimeBody>();
        body.stage = this;
        body.white = white;
    }

    private static bool IsWhite(Material mat)
    {
        if (mat == null) return false;
        Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") :
            mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.black;
        return c.grayscale > 0.55f;
    }

    private void LateUpdate()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
        if (material == null) OnEnable();
        if (material == null) return;
        if (Time.realtimeSinceStartup >= nextDiscovery) Discover();
        pieces.RemoveAll(p => p.source == null || p.surface == null || p.collider == null);
        foreach (Piece p in pieces)
        {
            p.center = p.collider.bounds.center;
            p.radius = Mathf.Max(p.collider.bounds.extents.x, p.collider.bounds.extents.y);
        }
        foreach (Piece p in pieces)
        {
            bool visible = p.source.gameObject.activeInHierarchy && p.collider.enabled && p.radius > 0.01f;
            p.surface.enabled = visible;
            if (!visible) continue;
            p.source.forceRenderingOff = true;
            // Use world-sized proxy cubes even when the imported mesh is scaled by 40.
            Transform t = p.surface.transform;
            t.SetPositionAndRotation(p.center, Quaternion.identity);
            Vector3 scale = p.source.transform.lossyScale;
            float diameter = p.radius * (2.8f + blend);
            t.localScale = new Vector3(diameter / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                diameter / Mathf.Max(0.001f, Mathf.Abs(scale.y)), diameter / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            neighbors[0] = new Vector4(p.center.x, p.center.y, p.center.z, p.radius);
            int count = 1;
            foreach (Piece other in pieces)
            {
                if (other == p || other.white != p.white || other.radius <= 0.01f || !other.source.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(p.center, other.center) > (p.radius + other.radius) * 1.7f) continue;
                if (count == neighbors.Length) break;
                neighbors[count++] = new Vector4(other.center.x, other.center.y, other.center.z, other.radius);
            }
            p.properties.SetVectorArray("_Blobs", neighbors);
            p.properties.SetInt("_BlobCount", count);
            p.properties.SetColor("_Color", p.white ? whiteSlime : blackSlime);
            p.properties.SetFloat("_Radius", p.radius);
            p.properties.SetFloat("_Thickness", thickness);
            p.properties.SetFloat("_Blend", p.radius * blend);
            p.properties.SetFloat("_Wobble", wobble);
            p.surface.SetPropertyBlock(p.properties);
        }
    }

    public void Stick(SlimeBody a, SlimeBody b)
    {
        if (!stickyContact) return;
        Rigidbody first = a.GetComponent<Rigidbody>(), second = b.GetComponent<Rigidbody>();
        if (first == null || second == null || (first.isKinematic && second.isKinematic)) return;
        if (bonds.Contains((a, b)) || bonds.Contains((b, a))) return;
        bonds.Add((a, b));
        if (first.isKinematic) { Rigidbody swap = first; first = second; second = swap; }
        FixedJoint joint = first.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = second;
        joint.enableCollision = false;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
        joints.Add(joint);
    }

    private void OnDisable()
    {
        foreach (Piece p in pieces)
        {
            if (p.source != null) p.source.forceRenderingOff = false;
            if (p.surface != null) Release(p.surface.gameObject);
        }
        foreach (FixedJoint joint in joints) if (joint != null) Release(joint);
        pieces.Clear(); joints.Clear(); bonds.Clear();
        if (material != null) Release(material);
        material = null;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
