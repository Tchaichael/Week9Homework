using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2CatSplitSetup
{
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/SetupScene2CatSplit.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 2/Set Both Split Cats To Scale X 18")]
    public static void Apply()
    {
        EditorSceneManager.SaveOpenScenes();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/2.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/2.unity");
        var ball = GameObject.Find("ball");
        var split = ball.GetComponent<CollisionSplit>();
        if (!split || !ball.transform.Find("Burger Cat Visual")) throw new Exception("Scene 2 cat player missing.");
        Undo.RecordObject(split, "Set split cat size");
        split.splitPieceScaleX = 18f;
        EditorUtility.SetDirty(split);
        PrefabUtility.RecordPrefabInstancePropertyModifications(split);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate(ball);
        Selection.activeGameObject = ball;
    }

    static void Validate(GameObject ball)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var test = new GameObject("Split validation");
            SceneManager.MoveGameObjectToScene(test, preview);
            test.transform.position = new Vector3(10000,10000,10000);
            test.transform.rotation = ball.transform.rotation;
            test.transform.localScale = ball.transform.localScale;
            Vector3 before = test.transform.localScale;
            var body = test.AddComponent<Rigidbody>();
            body.useGravity = false;
            test.AddComponent<SphereCollider>();
            test.AddComponent<MeshFilter>().sharedMesh = ball.GetComponent<MeshFilter>().sharedMesh;
            test.AddComponent<MeshRenderer>().enabled = false;
            var holder = UnityEngine.Object.Instantiate(ball.transform.Find("Burger Cat Visual"), test.transform, false);
            holder.name = "Burger Cat Visual";
            var spin = test.AddComponent<CatLaunchSpin>();
            spin.visualTarget = holder.GetChild(0);
            spin.BeginSpin();
            var split = test.AddComponent<CollisionSplit>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CollisionSplit).GetField("body", flags).SetValue(split, body);
            typeof(CollisionSplit).GetField("incomingVelocity", flags).SetValue(split, new Vector3(5,2,0));
            typeof(CollisionSplit).GetMethod("Split", flags).Invoke(split, new object[] { Vector3.up });
            var pieces = preview.GetRootGameObjects().Where(g => g.GetComponent<CollisionSplit>()).ToArray();
            Require(pieces.Length == 2, "exactly two pieces");
            foreach (var piece in pieces)
            {
                Vector3 scale = piece.transform.localScale;
                Require(Mathf.Abs(scale.x - 18f) < .001f, "X scale 18");
                Require(Mathf.Abs(scale.y / scale.x - before.y / before.x) < .001f &&
                    Mathf.Abs(scale.z / scale.x - before.z / before.x) < .001f, "proportional Y/Z");
                Require(!piece.GetComponent<MeshRenderer>().enabled, "no white sphere");
                var visual = piece.transform.Find("Burger Cat Visual");
                Require(visual && visual.GetComponentsInChildren<MeshRenderer>().Length == holder.GetComponentsInChildren<MeshRenderer>().Length,
                    "complete cat hierarchy");
                var catSpin = piece.GetComponent<CatLaunchSpin>();
                Require(catSpin && catSpin.visualTarget && catSpin.IsSpinning, "cat spin retained");
                Require(piece.GetComponent<Collider>() && piece.GetComponent<Rigidbody>(), "physics retained");
            }
            File.WriteAllText("Temp/Scene2CatSplitValidation.txt", "PASS: actual Split() produced two cat burgers.\n" +
                string.Join("\n", pieces.Select(g => g.name + " scale=" + g.transform.localScale.ToString("F5"))) +
                "\nY/Z proportions match original. Both hidden sphere renderers remain disabled.\n" +
                "Both complete cat hierarchies, colliders, rigidbodies and launch spin retained.\nScene 2 saved.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
    static void Require(bool result, string check)
    {
        if (!result) throw new Exception("Split cat validation failed: " + check);
    }
}
