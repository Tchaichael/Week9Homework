using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene3LaunchSetup
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
        const string request = "Temp/Scene3Launch.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Apply();
        File.Delete(request);
    }

    [MenuItem("Tools/Scene 3/Launch From Current Cat Position")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/3.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/3.unity", OpenSceneMode.Additive);
        try
        {
            var cat = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BallForces>(true))
                .Single(g => g.name == "ball");
            Vector3 position = cat.transform.position;
            Undo.RecordObject(cat, "Center drag bounds on current spawn");
            cat.dragBoundsFollowSpawn = true;
            float range = Mathf.Abs(cat.maxMouseDrag * cat.dragSensitivity);
            cat.minX = position.x - range;
            cat.maxX = position.x + range;
            cat.minY = position.y - range;
            cat.maxY = position.y + range;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cat);
            EditorUtility.SetDirty(cat);
            Validate(position, cat);
            if (cat.transform.position != position) throw new System.Exception("Cat spawn unexpectedly moved.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/Scene3LaunchReport.txt", "PASS: scene 3 spawn preserved at " + position.ToString("F3") +
                "; drag bounds follow spawn; zero-drag stays at spawn, drag remains local, release queues launch force and enables click-dash.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            var grid = original.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChessBackgroundSettings>()).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }

    private static void Validate(Vector3 position, BallForces source)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        preview.name = "3";
        try
        {
            var go = new GameObject("Scene 3 launch validation");
            SceneManager.MoveGameObjectToScene(go, preview);
            go.transform.position = position;
            var rb = go.AddComponent<Rigidbody>();
            go.AddComponent<SphereCollider>();
            var controller = go.AddComponent<BallForces>();
            controller.dragBoundsFollowSpawn = true;
            controller.maxMouseDrag = source.maxMouseDrag;
            controller.dragSensitivity = source.dragSensitivity;
            controller.minLaunchForce = source.minLaunchForce;
            controller.maxLaunchForce = source.maxLaunchForce;
            var dash = go.AddComponent<CatClickDash>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BallForces).GetMethod("Start", flags).Invoke(controller, null);
            Require(Vector3.Distance(controller.GetDragPosition(Vector2.zero), position) < .001f,
                "drag must not jump to old Y bounds");
            Vector3 pulled = controller.GetDragPosition(new Vector2(-100f, 100f));
            Require(pulled.z == position.z && pulled.y > position.y && pulled.x < position.x,
                "drag stays around new spawn on XY plane");
            go.transform.position = pulled;
            typeof(BallForces).GetMethod("LaunchBall", flags).Invoke(controller, null);
            Require(!rb.isKinematic && dash.IsArmed, "release starts dynamic launch and click-dash");
            Require(rb.GetAccumulatedForce().sqrMagnitude > 0f, "launch force queued");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void Require(bool condition, string check)
    {
        if (!condition) throw new System.Exception("Scene 3 launch validation failed: " + check);
    }
}
