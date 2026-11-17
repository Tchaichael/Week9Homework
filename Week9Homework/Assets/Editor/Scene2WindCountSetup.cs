using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2WindCountSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/Scene2WindCount.request";
            if (!File.Exists(request)) return;
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/2.unity");
            if (!scene.IsValid() || !scene.isLoaded) { File.Delete(request); return; }
            var winds = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WindZone>(true)).ToArray();
            foreach (var wind in winds)
            {
                Undo.RecordObject(wind, "Set wind line count to 20");
                wind.lineCount = 20;
                if (EditorApplication.isPlaying && wind.isActiveAndEnabled)
                {
                    wind.enabled = false;
                    wind.enabled = true;
                }
                EditorUtility.SetDirty(wind);
                PrefabUtility.RecordPrefabInstancePropertyModifications(wind);
            }
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            File.WriteAllText("Temp/Scene2WindCountReport.txt", "Set " + winds.Length + " wind zones to 20 lines each.");
            File.Delete(request);
        };
    }
}
