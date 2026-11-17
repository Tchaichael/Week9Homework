using System;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2ClickedCatCameraValidation
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/Scene2ClickedCatCamera.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Validate();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 2/Validate Clicked Cat Camera")]
    public static void Validate()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "2";
        try
        {
            var first = Create("First cat", scene).AddComponent<Rigidbody>();
            var second = Create("Second cat", scene).AddComponent<Rigidbody>();
            var unrelated = Create("Unrelated object", scene).AddComponent<Rigidbody>();
            var camera = Create("Cat camera", scene).AddComponent<CinemachineCamera>();
            camera.Follow = first.transform;
            var dash = first.gameObject.AddComponent<CatClickDash>();
            dash.RegisterPiece(first);
            dash.FollowClickedCat(first);
            Require(camera.Follow == first.transform, "single cat retains original camera");
            dash.RegisterPiece(second);
            dash.FollowClickedCat(second);
            Require(camera.Follow == second.transform, "click second cat switches follow target");
            dash.FollowClickedCat(first);
            Require(camera.Follow == first.transform, "click original cat switches back");
            dash.FollowClickedCat(unrelated);
            Require(camera.Follow == first.transform, "unregistered objects cannot capture camera");
            camera.Target.CustomLookAtTarget = true;
            camera.LookAt = first.transform;
            dash.FollowClickedCat(second);
            Require(camera.Follow == second.transform && camera.LookAt == second.transform, "custom look-at follows selection");
            scene.name = "0";
            dash.FollowClickedCat(first);
            Require(camera.Follow == second.transform, "scene 0 camera behavior unchanged");
            File.WriteAllText("Temp/Scene2ClickedCatCameraReport.txt",
                "PASS: second cat selection, switch back, unrelated click, custom look-at, and scene 0 isolation.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject Create(string name, Scene scene)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static void Require(bool result, string check)
    {
        if (!result) throw new Exception("Clicked cat camera validation failed: " + check);
    }
}
