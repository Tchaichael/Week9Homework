using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2CheesePickupSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += ApplyPending;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ApplyPending;
        };
    }

    private static void ApplyPending()
    {
        const string request = "Temp/Scene2CheesePickup.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Apply();
        File.Delete(request);
    }

    [MenuItem("Tools/Scene 2/Configure Cheese Pickups")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/2.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/2.unity", OpenSceneMode.Additive);
        try
        {
            var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => (t.name.Equals("Cheese", StringComparison.OrdinalIgnoreCase) ||
                    t.name.StartsWith("Cheese (", StringComparison.OrdinalIgnoreCase)) &&
                    !t.GetComponentInParent<CheesePickup>()).ToArray();
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BallForces>(true))
                .Single(g => g.name == "ball");
            var report = new StringBuilder();
            foreach (Transform visual in objects)
            {
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                Require(renderers.Length > 0, visual.name + " has visible mesh");
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                string name = visual.name;
                var root = new GameObject(name);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.layer = visual.gameObject.layer;
                root.transform.SetParent(visual.parent, true);
                root.transform.position = bounds.center;
                Undo.RegisterCreatedObjectUndo(root, "Configure cheese pickup");
                Undo.SetTransformParent(visual, root.transform, "Keep original cheese visual");
                Undo.RecordObject(visual.gameObject, "Name cheese visual");
                visual.name = "Cheese Visual";
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                {
                    Undo.RecordObject(collider, "Disable solid cheese collider");
                    collider.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
                }
                foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
                {
                    Undo.RecordObject(body, "Keep cheese visual anchored");
                    body.useGravity = false;
                    body.isKinematic = true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(body);
                }
                var pickup = root.AddComponent<CheesePickup>();
                pickup.visualTarget = visual;
                pickup.SetVisualPivot(bounds.center);
                var trigger = root.GetComponent<BoxCollider>();
                trigger.isTrigger = true;
                Vector3 min = bounds.min, max = bounds.max;
                // Collectibles are visual 3D assets; extend only their trigger depth
                // through the XY gameplay plane while preserving the placed appearance.
                min.z = Mathf.Min(min.z, player.transform.position.z - 4f);
                max.z = Mathf.Max(max.z, player.transform.position.z + 4f);
                var pickupBounds = new Bounds((min + max) * .5f, max - min);
                trigger.center = root.transform.InverseTransformPoint(pickupBounds.center);
                trigger.size = root.transform.InverseTransformVector(pickupBounds.size) * 1.08f;
                var rb = root.GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
                Require(trigger.isTrigger && pickup.visualTarget && !pickup.pickupSound,
                    name + " has trigger, visual and empty sound slot");
                report.AppendLine(name + " trigger=" + trigger.size.ToString("F2") + " audio=None; pulse=4%; rotation=18 deg/s");
            }
            Require(objects.Length > 0 || scene.GetRootGameObjects().Any(g => g.GetComponent<CheesePickup>()), "cheese objects found");
            Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "scene saved");
            File.WriteAllText("Temp/Scene2CheesePickupReport.txt", "PASS: " + objects.Length +
                " cheese pickups configured. Player/split-piece collection, one-time pickup, environment rejection and centered idle animation validated.\n" + report);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            var grid = original.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChessBackgroundSettings>()).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }

    private static void Validate()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        scene.name = "2";
        try
        {
            var cheese = Create("Test cheese", scene).AddComponent<CheesePickup>();
            var visual = Create("Test visual", scene).transform;
            visual.SetParent(cheese.transform);
            cheese.visualTarget = visual;
            cheese.SetVisualPivot(visual.TransformPoint(new Vector3(.5f, .2f, 0f)));
            Vector3 center = visual.TransformPoint(new Vector3(.5f, .2f, 0f));
            cheese.ApplyAnimation(.3f);
            Require(Vector3.Distance(center, visual.TransformPoint(new Vector3(.5f, .2f, 0f))) < .001f, "animation center stable");
            Require(visual.localScale.x >= .96f && visual.localScale.x <= 1.04f &&
                Quaternion.Angle(visual.localRotation, Quaternion.identity) > 0f, "gentle scale and rotation");
            var other = Create("Other rigidbody", scene);
            other.AddComponent<Rigidbody>();
            var collider = other.AddComponent<SphereCollider>();
            Require(!cheese.TryCollect(collider), "non-cat cannot collect");
            other.AddComponent<BallForces>();
            Require(cheese.TryCollect(collider) && cheese.IsCollected && !cheese.gameObject.activeSelf,
                "original cat collects cheese");
            Require(!cheese.TryCollect(collider), "pickup only once");
            var second = Create("Second cheese", scene).AddComponent<CheesePickup>();
            var split = Create("Split piece", scene);
            split.AddComponent<Rigidbody>();
            split.AddComponent<CollisionSplit>();
            Require(second.TryCollect(split.AddComponent<SphereCollider>()), "split cat collects cheese");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static GameObject Create(string name, Scene scene)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static void Require(bool value, string check)
    {
        if (!value) throw new Exception("Cheese setup failed: " + check);
    }
}
