using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SeesawSetup
{
    static SeesawSetup() { EditorApplication.update += Check; }
    static void Check()
    {
        if (File.Exists("Temp/SeesawSetup.request") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
        {
            File.Delete("Temp/SeesawSetup.request");
            Apply();
        }
        const string request = "Temp/SeesawInspect.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        File.Delete(request);
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/3.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/3.unity", OpenSceneMode.Additive);
        var text = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects().Where(g => g.name == "Mon (2)" || g.name == "Prince (1)" || g.GetComponent<BallForces>() || g.GetComponent<TrapDropTrigger>()))
        {
            text.AppendLine(root.name + " pos=" + root.transform.position);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                text.AppendLine("  " + t.name + " pos=" + t.position + " components=" + string.Join(",", t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));
                foreach (var c in t.GetComponents<Collider>()) text.AppendLine("    collider=" + c.bounds + " trigger=" + c.isTrigger);
                foreach (var r in t.GetComponents<Renderer>()) text.AppendLine("    render=" + r.bounds);
            }
        }
        File.WriteAllText("Temp/SeesawInspect.txt",text.ToString());
        if (opened) EditorSceneManager.CloseScene(scene,true);
    }

    [MenuItem("Tools/Scene 3/Configure Impact Seesaw")]
    static void Apply()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/3.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/3.unity", OpenSceneMode.Additive);
        var roots = scene.GetRootGameObjects();
        var board = roots.Single(g => g.name == "Mon (2)");
        var prince = roots.Single(g => g.name == "Prince (1)");
        var trap = roots.SelectMany(g=>g.GetComponentsInChildren<TrapDropTrigger>(true)).Single(t=>t.dropTarget == prince.transform);
        Undo.RegisterFullObjectHierarchyUndo(board,"Make Mon (2) an impact seesaw");
        var visuals = board.GetComponentsInChildren<MeshRenderer>();
        Bounds bounds = visuals[0].bounds;
        foreach (var r in visuals) bounds.Encapsulate(r.bounds);
        var children = board.transform.Cast<Transform>().ToArray();
        var positions = children.Select(t=>t.position).ToArray();
        board.transform.position = bounds.center;
        for (int i=0;i<children.Length;i++) children[i].position = positions[i];
        foreach (var joint in board.GetComponentsInChildren<Joint>()) Undo.DestroyObjectImmediate(joint);
        foreach (var rb in board.GetComponentsInChildren<Rigidbody>())
            if (rb.gameObject != board) Undo.DestroyObjectImmediate(rb);
        foreach (var motion in board.GetComponentsInChildren<MovingObstacle>()) motion.enabled = false;
        foreach (var checker in board.GetComponentsInChildren<GoPieceChecker>()) checker.enabled = false;
        var body = board.GetComponent<Rigidbody>();
        if (!body) body = Undo.AddComponent<Rigidbody>(board);
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var seesaw = board.GetComponent<ImpactSeesaw>();
        if (!seesaw) seesaw = Undo.AddComponent<ImpactSeesaw>(board);
        seesaw.dropTrigger = trap;
        seesaw.fallingPrince = prince.transform;
        EditorUtility.SetDirty(seesaw);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = board;
        File.WriteAllText("Temp/SeesawSetupReport.txt","PASS: Mon (2) unified with central pivot " + bounds.center + "; existing appearance preserved, old joints and child rigidbodies removed; Prince (1) drop linked.");
        if (opened) EditorSceneManager.CloseScene(scene,true);
    }
}
