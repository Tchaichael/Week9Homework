using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2BackgroundSettingsSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/Scene2BackgroundSettings.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 2/Expose Background Grid Settings")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/2.unity") return;
        var settings = Object.FindFirstObjectByType<ChessBackgroundSettings>();
        if (!settings)
        {
            var go = new GameObject("棋盘背景设置 (Chess Background Settings)");
            Undo.RegisterCreatedObjectUndo(go, "Add chess background settings");
            settings = Undo.AddComponent<ChessBackgroundSettings>(go);
        }
        settings.backgroundMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/RouteMaterials/Infinite Chess Background.mat");
        settings.Apply();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings.backgroundMaterial);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = settings.gameObject;
        File.WriteAllText("Temp/Scene2BackgroundSettingsReport.txt",
            "Grid size=" + settings.cellSize + "; offset=" + settings.gridOffset +
            "; depth=" + settings.projectionDepth + "; shader errors=" +
            ShaderUtil.ShaderHasError(settings.backgroundMaterial.shader));
    }
}
