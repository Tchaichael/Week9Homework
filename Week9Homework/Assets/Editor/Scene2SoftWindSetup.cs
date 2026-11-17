using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2SoftWindSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/Scene2SoftWind.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 2/Use Soft Fading Wind Curves")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/2.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/2.unity", OpenSceneMode.Additive);
        try
        {
            var winds = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WindZone>(true)).ToArray();
            foreach (var wind in winds)
            {
                Undo.RecordObject(wind, "Soften wind visuals");
                wind.softCurves = true;
                wind.waveSpeed = 2.2f;
                wind.secondaryWaveStrength = .35f;
                wind.swayAmplitude = 1.3f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(wind);
                EditorUtility.SetDirty(wind);
            }
            if (WindZone.FadeEnvelope(0f, .25f) != 0f || WindZone.FadeEnvelope(1f, .25f) != 0f ||
                WindZone.FadeEnvelope(.5f, .25f) != 1f ||
                WindZone.FadeEnvelope(.05f, .25f) >= WindZone.FadeEnvelope(.2f, .25f))
                throw new System.Exception("Wind fade envelope validation failed.");
            var test = new GameObject("Wind deformation validation");
            try
            {
                var wind = test.AddComponent<WindZone>();
                float firstChange = wind.SampleCurveOffset(-8f, .25f, .6f, 1.1f) -
                    wind.SampleCurveOffset(-8f, .25f, 0f, 1.1f);
                float secondChange = wind.SampleCurveOffset(8f, .75f, .6f, 1.1f) -
                    wind.SampleCurveOffset(8f, .75f, 0f, 1.1f);
                if (Mathf.Abs(firstChange - secondChange) < .01f)
                    throw new System.Exception("Wind curve is translating without deforming.");
            }
            finally { Object.DestroyImmediate(test); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/Scene2SoftWindReport.txt", "PASS: " + winds.Length +
                " wind zones use animated, deforming curves with two traveling waves, breathing amplitude, independent phases and gentle sway. Non-rigid deformation and fade checks passed; wind force unchanged.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            var grid = original.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChessBackgroundSettings>()).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }
}
