using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Scene3CheesePhysicsCheck
{
    private const string Request = "Temp/Scene3CheesePhysics.request";
    private static int warmup;
    private static bool busy;

    static Scene3CheesePhysicsCheck() { EditorApplication.update += Update; }

    private static void Update()
    {
        if (busy || !File.Exists(Request) || EditorApplication.isCompiling) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/3.unity") return;
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
            SessionState.SetBool("CheeseCheckReturnToEdit", true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (EditorApplication.isPaused || ++warmup < 4) return;
        busy = true;
        var report = new StringBuilder();
        Scene testScene = default;
        try
        {
            var source = SceneManager.GetSceneByPath("Assets/Scenes/3.unity");
            var player = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BallForces>())
                .Single(g => g.name == "ball");
            var pickups = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CheesePickup>()).ToArray();
            testScene = SceneManager.CreateScene("Cheese physical contact check", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var physics = testScene.GetPhysicsScene();
            foreach (var original in pickups)
            {
                var food = UnityEngine.Object.Instantiate(original.gameObject).GetComponent<CheesePickup>();
                SceneManager.MoveGameObjectToScene(food.gameObject, testScene);
                food.pickupSound = null;
                var renderers = food.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                Bounds visible = renderers[0].bounds;
                foreach (var renderer in renderers) visible.Encapsulate(renderer.bounds);
                var box = food.GetComponent<BoxCollider>();
                var cat = new GameObject("Actual cat collider check");
                SceneManager.MoveGameObjectToScene(cat, testScene);
                cat.layer = player.gameObject.layer;
                cat.transform.rotation = player.transform.rotation;
                cat.transform.localScale = player.transform.lossyScale;
                var sphere = cat.AddComponent<SphereCollider>();
                var sourceSphere = player.GetComponent<SphereCollider>();
                sphere.radius = sourceSphere.radius;
                sphere.center = sourceSphere.center;
                float radius = sphere.radius * Mathf.Max(cat.transform.lossyScale.x, cat.transform.lossyScale.z);
                Vector3 target = new Vector3(visible.center.x, visible.center.y, player.transform.position.z);
                cat.transform.position = target - Vector3.right * (visible.extents.x + radius + 4f);
                var body = cat.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
                var marker = cat.AddComponent<BallForces>();
                marker.enabled = false;
                body.linearVelocity = Vector3.right * 30f;
                Physics.SyncTransforms();
                string boundsInfo = " visible=" + visible + " trigger=" + box.bounds + " playerZ=" + target.z +
                    " layerBlocked=" + Physics.GetIgnoreLayerCollision(cat.layer, food.gameObject.layer);
                bool overlapped = false;
                for (int step = 0; step < 200 && !food.IsCollected; step++)
                {
                    physics.Simulate(.02f);
                    if (Physics.ComputePenetration(sphere, sphere.transform.position, sphere.transform.rotation,
                        box, box.transform.position, box.transform.rotation, out _, out _)) overlapped = true;
                }
                report.AppendLine((food.IsCollected ? "PASS " : "FAIL ") + original.name + boundsInfo +
                    " physicalOverlap=" + overlapped + " finalCat=" + body.position + " velocity=" + body.linearVelocity +
                    " dynamic=" + !body.isKinematic + " detects=" + body.detectCollisions);
                UnityEngine.Object.Destroy(cat);
                if (food) UnityEngine.Object.Destroy(food.gameObject);
            }
            if (pickups.Length == 0) report.AppendLine("FAIL: no active pickups in scene 3");
        }
        catch (Exception exception) { report.AppendLine("ERROR: " + exception); }
        finally
        {
            File.WriteAllText("Temp/Scene3CheesePhysicsReport.txt", report.ToString());
            File.Delete(Request);
            if (testScene.IsValid()) SceneManager.UnloadSceneAsync(testScene);
            if (SessionState.GetBool("CheeseCheckReturnToEdit", false))
            {
                SessionState.SetBool("CheeseCheckReturnToEdit", false);
                EditorApplication.isPlaying = false;
            }
            busy = false;
        }
    }
}
