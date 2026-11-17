using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene3TrapDropSetup
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
        const string request = "Temp/Scene3TrapDrop.request";
        const string second = "Temp/Scene3Trap2Drop.request";
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(request)) { Apply(); File.Delete(request); }
        if (File.Exists(second)) { ApplySecond(); File.Delete(second); }
    }

    [MenuItem("Tools/Scene 3/Configure Trap Drop")]
    public static void Apply()
    {
        Configure("trap", "Prince (1)", -80f, "Temp/Scene3TrapDropReport.txt");
    }

    [MenuItem("Tools/Scene 3/Configure Trap 2 Drop")]
    public static void ApplySecond()
    {
        Configure("trap 2", "Prince (2)", 218f, "Temp/Scene3Trap2DropReport.txt");
    }

    private static void Configure(string trapName, string targetName, float destinationY, string reportPath)
    {
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == "Assets/Scenes/3.unity", "open scene 3");
        var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var trap = transforms.Single(t => t.name.Equals(trapName, StringComparison.OrdinalIgnoreCase) ||
            (trapName == "trap 2" && (t.name.Equals("trap2", StringComparison.OrdinalIgnoreCase) ||
                t.name.Equals("trap (2)", StringComparison.OrdinalIgnoreCase))));
        var target = transforms.Single(t => t.name == targetName);
        Vector3 before = target.position;
        var box = trap.GetComponent<BoxCollider>();
        if (!box) box = Undo.AddComponent<BoxCollider>(trap.gameObject);
        Undo.RecordObject(box, "Make trap a trigger region");
        box.isTrigger = true;
        box.enabled = true;
        var body = trap.GetComponent<Rigidbody>();
        if (body)
        {
            Undo.RecordObject(body, "Keep trap region stationary");
            body.isKinematic = true;
            body.useGravity = false;
        }
        var drop = trap.GetComponent<TrapDropTrigger>();
        if (!drop) drop = Undo.AddComponent<TrapDropTrigger>(trap.gameObject);
        Undo.RecordObject(drop, "Bind Prince drop target");
        drop.dropTarget = target;
        drop.targetY = destinationY;
        drop.dropSpeed = 600f;
        EditorUtility.SetDirty(drop);
        PrefabUtility.RecordPrefabInstancePropertyModifications(drop);
        PrefabUtility.RecordPrefabInstancePropertyModifications(box);
        Validate(destinationY);
        Require(target.position == before, "target remains in place until the trap triggers");
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "scene saved");
        File.WriteAllText(reportPath, "PASS: " + trap.name + " bound to " + targetName +
            "; world target Y=" + destinationY + "; speed=600. Cat-only one-time trigger, whole-obstacle movement, exact landing and X/Z preservation verified in Unity. Initial target=" + before);
    }

    private static void Validate(float destinationY)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var target = Create("Drop target", preview);
            target.transform.position = new Vector3(30f, 300f, -79f);
            var piece = Create("Target jointed piece", preview);
            piece.transform.SetParent(target.transform, false);
            var pieceBody = piece.AddComponent<Rigidbody>();
            var trap = Create("Test trap", preview).AddComponent<TrapDropTrigger>();
            trap.dropTarget = target.transform;
            trap.targetY = destinationY;
            var actor = Create("Actor", preview);
            actor.AddComponent<Rigidbody>();
            var collider = actor.AddComponent<SphereCollider>();
            Require(!trap.TryActivate(collider), "non-cat does not trigger");
            actor.AddComponent<BallForces>();
            Require(trap.TryActivate(collider) && !trap.TryActivate(collider), "cat activates only once");
            trap.AdvanceDrop(.1f);
            Require(target.transform.position.y < 300f && target.transform.position.y > destinationY, "fast drop starts");
            trap.AdvanceDrop(10f);
            Require(target.transform.position == new Vector3(30f, destinationY, -79f) && trap.HasLanded,
                "exact world destination without overshoot");
            Require(pieceBody.isKinematic && piece.transform.position.y == destinationY, "entire target moves together");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static GameObject Create(string name, Scene scene)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static void Require(bool result, string check)
    {
        if (!result) throw new Exception("Trap setup failed: " + check);
    }
}
