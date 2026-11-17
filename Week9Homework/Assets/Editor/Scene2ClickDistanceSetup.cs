using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2ClickDistanceSetup
{
    private const string Request = "Temp/Scene2ClickDistance.request";

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(Request);
        };
    }

    [MenuItem("Tools/Scene 2/Apply Click Distance And Infinite Background")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/2.unity") return;
        EditorApplication.isPaused = false;
        var skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/RouteMaterials/Infinite Chess Background.mat");
        if (!skybox) throw new System.InvalidOperationException("Infinite background material missing.");
        RenderSettings.skybox = skybox;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("board")) continue;
                Undo.RecordObject(renderer, "Hide finite chess board visuals");
                renderer.enabled = false;
            }
            if (root.name == "Infinite Chess Background")
            {
                // Remove only the obsolete finite visual, leaving user edits elsewhere intact.
                Undo.DestroyObjectImmediate(root);
            }
        }
        foreach (CatClickDash dash in Object.FindObjectsByType<CatClickDash>())
        {
            if (dash.gameObject.scene != scene) continue;
            Undo.RecordObject(dash, "Set click dash distance");
            PrefabUtility.RecordPrefabInstancePropertyModifications(dash);
        }
        foreach (CatLaunchSpin spin in Object.FindObjectsByType<CatLaunchSpin>())
        {
            if (spin.gameObject.scene != scene) continue;
            Undo.RecordObject(spin, "Set click spin duration");
            spin.burstAndDecay = true;
            spin.EnableClickControl();
            spin.spinDownTime = .5f;
            spin.maximumSpeed = 2160f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(spin);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SceneView.RepaintAll();
        var messages = ShaderUtil.GetShaderMessages(skybox.shader);
        ValidateClickSpin();
        ValidateGridProjection();
        File.WriteAllText("Temp/Scene2ClickDistanceReport.txt", "PASS: click/hold/release spin and camera-relative grid projection validated in Unity.\n" +
            "Scene 2 updated without reloading user edits. Grid projection Z=" + skybox.GetFloat("_BoardZ") + "\n" +
            "Skybox=" + RenderSettings.skybox.name + "\n" +
            string.Join("\n", messages.Select(m => m.severity + ": " + m.message)));
    }

    private static void ValidateClickSpin()
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var cat = new GameObject("Click spin validation");
            SceneManager.MoveGameObjectToScene(cat, preview);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(cat.transform);
            var spin = cat.AddComponent<CatLaunchSpin>();
            spin.visualTarget = visual.transform;
            spin.maximumSpeed = 2160f;
            spin.spinDownTime = .5f;
            var advance = typeof(CatLaunchSpin).GetMethod("AdvanceSpin",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            spin.EnableClickControl();
            spin.BeginSpin();
            if (spin.CurrentSpeed != 0f) throw new System.Exception("Launch unexpectedly spins.");
            spin.PressSpin();
            if (spin.CurrentSpeed < 2160f) throw new System.Exception("Click failed to boost spin.");
            advance.Invoke(spin, new object[] { .25f });
            if (spin.CurrentSpeed < 2160f) throw new System.Exception("Held click lost spin speed.");
            spin.PressSpin();
            if (spin.CurrentSpeed <= 0f) throw new System.Exception("Second click reset speed.");
            spin.ReleaseSpin();
            advance.Invoke(spin, new object[] { .1f });
            if (spin.CurrentSpeed <= 0f || spin.CurrentSpeed >= 2160f)
                throw new System.Exception("Release failed to decelerate.");
            advance.Invoke(spin, new object[] { .4f });
            if (spin.CurrentSpeed != 0f || spin.IsSpinning) throw new System.Exception("Spin failed to stop after release.");
            spin.PressSpin();
            if (spin.CurrentSpeed < 2160f) throw new System.Exception("Spin failed to restart.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void ValidateGridProjection()
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject("Grid projection validation");
            SceneManager.MoveGameObjectToScene(go, preview);
            var camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 60f;
            camera.aspect = 16f / 9f;
            var point = new Vector3(25f, 285f, -79f);
            foreach (Vector3 position in new[] {
                new Vector3(0f, 250f, -185f), new Vector3(70f, 320f, -220f) })
            {
                camera.transform.SetPositionAndRotation(position, Quaternion.identity);
                Vector3 viewport = camera.WorldToViewportPoint(point);
                Ray ray = camera.ViewportPointToRay(viewport);
                var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, -79f));
                if (!plane.Raycast(ray, out float distance) ||
                    Vector3.Distance(ray.GetPoint(distance), point) > .01f)
                    throw new System.Exception("Grid projection mismatch: " + Vector3.Distance(ray.GetPoint(distance), point) +
                        " viewport=" + viewport + " hit=" + ray.GetPoint(distance) + " expected=" + point);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
