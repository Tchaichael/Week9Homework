using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Scene0BurgerCatSetup
{
    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/SetupScene0Cat.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 0/Replace Ball With Burger Cat")]
    public static void Apply()
    {
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/0.unity");
        var ball = GameObject.Find("ball");
        if (!ball.transform.Find("Burger Cat Visual")) BallCatAppearance.Apply();
        CatLaunchSpinSetup.Apply();
        File.Copy("Temp/BallCatReplacement.txt", "Temp/Scene0CatReplacement.txt", true);
        File.Copy("Temp/CatSpinValidation.txt", "Temp/Scene0CatSpinValidation.txt", true);
        Selection.activeGameObject = ball;
        EditorSceneManager.SaveScene(scene);
    }
}
