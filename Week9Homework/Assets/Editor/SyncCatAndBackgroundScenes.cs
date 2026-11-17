using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SyncCatAndBackgroundScenes
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/SyncCatAndBackgroundScenes.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scenes/Sync Scene 2 Cat And Background To 0 And 1")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene source = SceneManager.GetSceneByPath("Assets/Scenes/2.unity");
        bool openedSource = !source.IsValid() || !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene("Assets/Scenes/2.unity", OpenSceneMode.Additive);
        try
        {
            var sourceCat = Player(source);
            var sourceGrid = Components<ChessBackgroundSettings>(source).Single();
            var sourceSpin = sourceCat.GetComponent<CatLaunchSpin>();
            var sourceDash = sourceCat.GetComponent<CatClickDash>();
            var sourceTrail = sourceCat.GetComponent<CatStarTrail>();
            Require(sourceSpin && sourceDash && sourceTrail && sourceGrid.backgroundMaterial,
                "Source cat/background components missing.");
            if (source.isDirty) EditorSceneManager.SaveScene(source);
            var report = new StringBuilder();
            foreach (string path in new[] { "Assets/Scenes/0.unity", "Assets/Scenes/1.unity" })
            {
                Scene target = SceneManager.GetSceneByPath(path);
                bool opened = !target.IsValid() || !target.isLoaded;
                if (opened) target = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(target);
                    var cat = Player(target);
                    var spin = cat.GetComponent<CatLaunchSpin>();
                    Require(spin && spin.visualTarget, path + ": existing cat visual missing.");
                    Transform visual = spin.visualTarget;
                    Undo.RecordObject(spin, "Sync cat spin settings");
                    EditorUtility.CopySerialized(sourceSpin, spin);
                    spin.visualTarget = visual;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(spin);
                    var dash = cat.GetComponent<CatClickDash>();
                    if (!dash) dash = Undo.AddComponent<CatClickDash>(cat.gameObject);
                    Undo.RecordObject(dash, "Sync click dash settings");
                    EditorUtility.CopySerialized(sourceDash, dash);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(dash);
                    var trail = cat.GetComponent<CatStarTrail>();
                    if (!trail) trail = Undo.AddComponent<CatStarTrail>(cat.gameObject);
                    Undo.RecordObject(trail, "Sync star trail settings");
                    EditorUtility.CopySerialized(sourceTrail, trail);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(trail);
                    var grid = Components<ChessBackgroundSettings>(target).SingleOrDefault();
                    if (!grid)
                    {
                        var go = new GameObject(sourceGrid.gameObject.name);
                        SceneManager.MoveGameObjectToScene(go, target);
                        Undo.RegisterCreatedObjectUndo(go, "Add infinite background settings");
                        grid = go.AddComponent<ChessBackgroundSettings>();
                    }
                    Undo.RecordObject(grid, "Sync background settings");
                    EditorUtility.CopySerialized(sourceGrid, grid);
                    grid.Apply();
                    foreach (var renderer in Components<MeshRenderer>(target))
                    {
                        if (!renderer.name.StartsWith("board", StringComparison.Ordinal)) continue;
                        Undo.RecordObject(renderer, "Hide finite board visuals");
                        renderer.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                    Require(spin.visualTarget && spin.visualTarget.gameObject.scene == target,
                        path + ": visual reference points outside its scene.");
                    Require(spin.clickControlled && spin.spinDownTime == sourceSpin.spinDownTime &&
                        dash.dashDistance == sourceDash.dashDistance && grid.cellSize == sourceGrid.cellSize &&
                        grid.gridOffset == sourceGrid.gridOffset && grid.projectionDepth == sourceGrid.projectionDepth,
                        path + ": copied settings mismatch.");
                    EditorSceneManager.MarkSceneDirty(target);
                    Require(EditorSceneManager.SaveScene(target), path + ": save failed.");
                    report.AppendLine("PASS " + path + ": grid=" + grid.cellSize + " offset=" + grid.gridOffset +
                        " depth=" + grid.projectionDepth + " dash=" + dash.dashDistance +
                        " spin=" + spin.maximumSpeed + " release=" + spin.spinDownTime +
                        "; local cat visual and star trail retained.");
                }
                finally
                {
                    SceneManager.SetActiveScene(source);
                    if (opened) EditorSceneManager.CloseScene(target, true);
                }
            }
            File.WriteAllText("Temp/SyncCatAndBackgroundScenesReport.txt", report.ToString());
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (openedSource) EditorSceneManager.CloseScene(source, true);
            var grid = Components<ChessBackgroundSettings>(original).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }

    private static T[] Components<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    private static BallForces Player(Scene scene) =>
        Components<BallForces>(scene).Single(cat => cat.name == "ball");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
