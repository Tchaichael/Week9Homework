using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene1BarrierSpringSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string cleanup = "Temp/CleanScene1Barrier.request";
            if (File.Exists(cleanup) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Clean();
                File.Delete(cleanup);
            }
            const string request = "Temp/Scene1BarrierSpring.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 1/Arrange Barrier Spring Mountain")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/1.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/1.unity", OpenSceneMode.Additive);
        try
        {
            var barrier = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Single(t => t.name == "barrier");
            var pieces = barrier.GetComponentsInChildren<Rigidbody>(true).OrderBy(b => b.position.x).ToArray();
            Require(pieces.Length >= 3, "at least three barrier pieces");
            float minX = pieces.Min(p => p.position.x), maxX = pieces.Max(p => p.position.x);
            float baseY = pieces.Min(p => p.position.y), peakY = pieces.Max(p => p.position.y);
            float centerX = (minX + maxX) * .5f;
            float depth = pieces.Average(p => p.position.z);
            Require(maxX > minX && peakY > baseY, "existing mountain bounds");
            // Remove old links owned by this barrier, including any stray external connections.
            var joints = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Joint>(true)).ToArray();
            int removed = 0;
            foreach (var joint in joints)
            {
                if (!joint.transform.IsChildOf(barrier) && !pieces.Contains(joint.connectedBody)) continue;
                Undo.DestroyObjectImmediate(joint);
                removed++;
            }
            foreach (var link in barrier.GetComponentsInChildren<JointLinkVisual>(true))
            {
                if (!link.GetComponent<Rigidbody>()) Undo.DestroyObjectImmediate(link.gameObject);
                else
                {
                    var line = link.GetComponent<LineRenderer>();
                    Undo.DestroyObjectImmediate(link);
                    if (line) Undo.DestroyObjectImmediate(line);
                }
            }
            var shape = barrier.GetComponent<SpringMountain>();
            if (!shape) shape = Undo.AddComponent<SpringMountain>(barrier.gameObject);
            Undo.RecordObject(shape, "Keep barrier mountain shape");
            int peak = pieces.Length / 2;
            for (int i = 0; i < pieces.Length; i++)
            {
                Rigidbody piece = pieces[i];
                Undo.RecordObjects(new UnityEngine.Object[] { piece, piece.transform }, "Arrange barrier mountain");
                Vector3 position = i <= peak
                    ? Vector3.Lerp(new Vector3(minX, baseY, depth), new Vector3(centerX, peakY, depth), i / (float)peak)
                    : Vector3.Lerp(new Vector3(centerX, peakY, depth), new Vector3(maxX, baseY, depth),
                        (i - peak) / (float)(pieces.Length - 1 - peak));
                piece.transform.position = position;
                piece.transform.SetSiblingIndex(i);
                piece.useGravity = false;
                piece.isKinematic = i == 0 || i == pieces.Length - 1;
                piece.linearDamping = 1f;
                piece.angularDamping = 2f;
                piece.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                piece.interpolation = RigidbodyInterpolation.Interpolate;
                piece.solverIterations = 16;
                piece.solverVelocityIterations = 8;
                if (i > 0)
                {
                    Rigidbody previous = pieces[i - 1];
                    var joint = Undo.AddComponent<SpringJoint>(piece.gameObject);
                    joint.connectedBody = previous;
                    joint.autoConfigureConnectedAnchor = false;
                    joint.anchor = Center(piece);
                    joint.connectedAnchor = Center(previous);
                    float distance = Vector3.Distance(piece.transform.TransformPoint(joint.anchor),
                        previous.transform.TransformPoint(joint.connectedAnchor));
                    joint.minDistance = distance;
                    joint.maxDistance = distance;
                    joint.spring = 80f;
                    joint.damper = 12f;
                    joint.tolerance = .02f;
                    joint.enableCollision = false;
                    joint.breakForce = Mathf.Infinity;
                    joint.breakTorque = Mathf.Infinity;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(piece);
                PrefabUtility.RecordPrefabInstancePropertyModifications(piece.transform);
            }
            Physics.SyncTransforms();
            shape.CaptureShape(pieces);
            EditorUtility.SetDirty(shape);
            Validate(barrier, pieces);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "scene saved");
            var report = new StringBuilder("PASS: regular mountain chain, neighboring spring connections, anchored ends, no gravity, restoring force.\n");
            report.AppendLine("Old links removed=" + removed + "; pieces=" + pieces.Length + "; springs=" + (pieces.Length - 1));
            foreach (var piece in pieces)
                report.AppendLine(piece.name + " position=" + piece.position.ToString("F3") + " fixed=" + piece.isKinematic);
            File.WriteAllText("Temp/Scene1BarrierSpringReport.txt", report.ToString());
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            var grid = original.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChessBackgroundSettings>()).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }

    private static Vector3 Center(Rigidbody body)
    {
        var box = body.GetComponent<BoxCollider>();
        return box ? box.center : Vector3.zero;
    }

    [MenuItem("Tools/Scene 1/Remove Barrier Spring Visuals And Extra Joints")]
    public static void Clean()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/1.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/1.unity", OpenSceneMode.Additive);
        try
        {
            var barrier = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Single(t => t.name == "barrier");
            var pieces = barrier.GetComponentsInChildren<Rigidbody>(true).OrderBy(p => p.position.x).ToArray();
            int removedVisuals = 0, removedJoints = 0;
            foreach (var visual in barrier.GetComponentsInChildren<JointLinkVisual>(true))
            {
                if (visual.GetComponent<Rigidbody>())
                {
                    var line = visual.GetComponent<LineRenderer>();
                    Undo.DestroyObjectImmediate(visual);
                    if (line) Undo.DestroyObjectImmediate(line);
                }
                else Undo.DestroyObjectImmediate(visual.gameObject);
                removedVisuals++;
            }
            for (int i = 0; i < pieces.Length; i++)
            {
                var joints = pieces[i].GetComponents<Joint>();
                SpringJoint keep = i == 0 ? null : joints.OfType<SpringJoint>()
                    .FirstOrDefault(j => j.connectedBody == pieces[i - 1]);
                Require(i == 0 || keep, "required neighboring spring exists");
                foreach (var joint in joints)
                {
                    if (joint == keep) continue;
                    Undo.DestroyObjectImmediate(joint);
                    removedJoints++;
                }
            }
            foreach (var joint in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Joint>(true)).ToArray())
            {
                if (joint.transform.IsChildOf(barrier) || !pieces.Contains(joint.connectedBody)) continue;
                Undo.DestroyObjectImmediate(joint);
                removedJoints++;
            }
            Require(barrier.GetComponentsInChildren<Joint>(true).Length == pieces.Length - 1,
                "only necessary neighbor joints remain");
            Require(barrier.GetComponentsInChildren<JointLinkVisual>(true).Length == 0, "no spring visuals remain");
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "cleanup saved");
            File.WriteAllText("Temp/CleanScene1BarrierReport.txt", "PASS: removed visual links=" + removedVisuals +
                "; removed extra joints=" + removedJoints + "; retained neighbor SpringJoints=" + (pieces.Length - 1) +
                "; mountain positions and restoring behavior unchanged.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Validate(Transform barrier, Rigidbody[] pieces)
    {
        Require(barrier.GetComponentsInChildren<Joint>().Length == pieces.Length - 1, "one link per neighboring pair");
        Require(pieces.First().isKinematic && pieces.Last().isKinematic, "two fixed endpoints");
        for (int i = 0; i < pieces.Length; i++)
        {
            var links = pieces[i].GetComponents<SpringJoint>();
            Require(links.Length == (i == 0 ? 0 : 1), "no duplicate links");
            if (i > 0) Require(links[0].connectedBody == pieces[i - 1], "links follow spatial order");
            if (i > 0) Require(pieces[i].position.x > pieces[i - 1].position.x, "left-to-right order");
            Require(!pieces[i].useGravity, "no gravity collapse");
        }
        Require(Mathf.Abs(pieces.First().position.y - pieces.Last().position.y) < .01f, "level mountain feet");
        Vector3 force = SpringMountain.RestoreAcceleration(Vector3.left * 2f, Vector3.zero, 30f, 8f);
        Require(force.x < 0f && force.z == 0f, "restoring force points back toward shape");
    }

    private static void Require(bool result, string check)
    {
        if (!result) throw new Exception("Barrier spring setup failed: " + check);
    }
}
