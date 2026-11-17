using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartMusicSetup
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
        const string request = "Temp/StartMusicSetup.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Apply();
        File.Delete(request);
    }

    [MenuItem("Tools/Start/Add Persistent Background Music")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Start.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Additive);
        try
        {
            var music = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PersistentMusic>(true)).SingleOrDefault();
            if (!music)
            {
                var go = new GameObject("Background Music (Persistent)");
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Add persistent background music");
                music = go.AddComponent<PersistentMusic>();
            }
            var source = music.GetComponent<AudioSource>();
            Undo.RecordObject(source, "Configure looping 2D music");
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = music.volume;
            EditorUtility.SetDirty(source);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = music.gameObject;
            File.WriteAllText("Temp/StartMusicSetupReport.txt",
                "PASS: Start has a single persistent 2D looping music source; Music Clip slot left for user audio.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
