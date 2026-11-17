using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Scene1BurgerCatSetup
{
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/SetupScene1Cat.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 1/Replace Ball With Burger Cat")]
    public static void Apply()
    {
        // Save the current editor changes before switching to the requested scene.
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/1.unity");
        var ball = GameObject.Find("ball");
        if (!ball.transform.Find("Burger Cat Visual")) BallCatAppearance.Apply();
        CatLaunchSpinSetup.Apply();
        File.Copy("Temp/BallCatReplacement.txt", "Temp/Scene1CatReplacement.txt", true);
        File.Copy("Temp/CatSpinValidation.txt", "Temp/Scene1CatSpinValidation.txt", true);
        Selection.activeGameObject = ball;
        EditorSceneManager.SaveScene(scene);
    }
}
