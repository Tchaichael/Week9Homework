using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CatLaunchSpinSetup
{
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/SetupCatSpin.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/3.unity") return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 3/Set Up Cat Launch Spin")]
    public static void Apply()
    {
        var ball = GameObject.Find("ball");
        var holder = ball.transform.Find("Burger Cat Visual");
        if (!holder || holder.childCount == 0) throw new Exception("Ball cat visual missing.");
        var spin = ball.GetComponent<CatLaunchSpin>();
        if (!spin) spin = Undo.AddComponent<CatLaunchSpin>(ball);
        Undo.RecordObject(spin, "Configure launch spin");
        spin.visualTarget = holder.GetChild(0);
        spin.initialSpeed = 60f;
        spin.maximumSpeed = 1440f;
        spin.accelerationTime = 1.5f;
        EditorUtility.SetDirty(spin);
        EditorSceneManager.MarkSceneDirty(ball.scene);
        EditorSceneManager.SaveScene(ball.scene);
        Validate();
        File.WriteAllText("Temp/CatSpinValidation.txt",
            "PASS: " + ball.scene.path + " saved; visual target assigned.\n" +
            "Idle remains still; launch starts at 60 deg/s; reaches 1440 deg/s in 1.5 s; " +
            "speed capped; only Y rotation added; X/Z held still; drag/reset restores pose.\n" +
            "Large-frame and small-frame integration agree. Player physics untouched.");
    }

    static void Validate()
    {
        var test = new GameObject("Cat spin validation");
        try
        {
            var target = new GameObject("Visual").transform;
            target.SetParent(test.transform);
            target.localRotation = Quaternion.Euler(0, -19, 0);
            Quaternion original = target.rotation;
            var spin = test.AddComponent<CatLaunchSpin>();
            spin.visualTarget = target;
            var advance = typeof(CatLaunchSpin).GetMethod("AdvanceSpin", BindingFlags.NonPublic | BindingFlags.Instance);
            Action<float> step = dt => advance.Invoke(spin, new object[] { dt });
            step(.5f);
            Require(!spin.IsSpinning && Quaternion.Angle(original, target.rotation) < .001f, "idle");
            spin.BeginSpin();
            Require(spin.IsSpinning && spin.CurrentSpeed == 60f, "launch trigger");
            step(.75f);
            Require(Mathf.Abs(spin.CurrentSpeed - 750f) < .01f, "acceleration midpoint");
            Quaternion expected = Quaternion.Euler(0, 303.75f, 0) * original;
            Require(Quaternion.Angle(expected, target.rotation) < .05f, "Y only; X/Z fixed");
            step(.75f);
            Require(spin.CurrentSpeed == 1440f, "maximum speed");
            step(.25f);
            Quaternion smallFrames = target.rotation;
            spin.StopAndReset();
            Require(!spin.IsSpinning && Quaternion.Angle(original, target.rotation) < .001f, "drag reset");
            spin.BeginSpin();
            step(1.75f);
            Require(Quaternion.Angle(smallFrames, target.rotation) < .05f && spin.CurrentSpeed == 1440f, "frame independence");
            spin.StopAndReset();
        }
        finally { UnityEngine.Object.DestroyImmediate(test); }
    }
    static void Require(bool condition, string check)
    {
        if (!condition) throw new Exception("Cat spin validation failed: " + check);
    }
}


