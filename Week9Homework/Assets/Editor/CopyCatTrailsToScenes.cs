using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CopyCatTrailsToScenes
{
    [InitializeOnLoadMethod] static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request="Temp/CopyCatTrails.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Cat Trails/Copy Scene 2 Settings To Scenes 0 And 1")]
    public static void Apply()
    {
        EditorSceneManager.SaveOpenScenes();
        string originalPath=SceneManager.GetActiveScene().path;
        var sourceScene=SceneManager.GetSceneByPath("Assets/Scenes/2.unity");
        bool sourceWasLoaded=sourceScene.IsValid() && sourceScene.isLoaded;
        if (!sourceWasLoaded) sourceScene=EditorSceneManager.OpenScene("Assets/Scenes/2.unity",OpenSceneMode.Additive);
        var source=sourceScene.GetRootGameObjects().First(g=>g.name=="ball").GetComponent<CatStarTrail>();
        if (!source) throw new Exception("Scene 2 trail settings missing.");
        var report=new StringBuilder("Copied current scene 2 trail values:\n");
        report.AppendLine($"size={source.starSize}; length={source.maximumTrailLength}; rate={source.starsPerSecond}; backward={source.backwardSpeed}; downward={source.downwardSpeed}; fade={source.fadeOutTime}");
        foreach(string name in new[]{"0","1"})
        {
            string path="Assets/Scenes/"+name+".unity";
            var scene=SceneManager.GetSceneByPath(path);
            bool wasLoaded=scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var ball=scene.GetRootGameObjects().First(g=>g.name=="ball");
            var preserved=ball.GetComponents<Component>().Where(c=>!(c is CatStarTrail)).ToArray();
            var before=preserved.Select(c=>EditorJsonUtility.ToJson(c)).ToArray();
            var trail=ball.GetComponent<CatStarTrail>();
            if (!trail) trail=Undo.AddComponent<CatStarTrail>(ball);
            Undo.RecordObject(trail,"Copy scene 2 star trail settings");
            source.CopySettingsTo(trail);
            if (trail.starMaterial!=source.starMaterial || trail.starSize!=source.starSize ||
                trail.maximumTrailLength!=source.maximumTrailLength || trail.starsPerSecond!=source.starsPerSecond ||
                trail.backwardSpeed!=source.backwardSpeed || trail.downwardSpeed!=source.downwardSpeed ||
                trail.fadeOutTime!=source.fadeOutTime) throw new Exception("Trail settings mismatch: "+path);
            for(int i=0;i<preserved.Length;i++)
                if(before[i]!=EditorJsonUtility.ToJson(preserved[i])) throw new Exception("Player component changed: "+path);
            EditorUtility.SetDirty(trail);
            PrefabUtility.RecordPrefabInstancePropertyModifications(trail);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed: "+path);
            report.AppendLine("PASS: "+path+" saved. All seven trail settings match; original player components unchanged.");
            if (!wasLoaded) EditorSceneManager.CloseScene(scene,true);
        }
        if (!sourceWasLoaded) EditorSceneManager.CloseScene(sourceScene,true);
        var original=SceneManager.GetSceneByPath(originalPath);
        if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        File.WriteAllText("Temp/CopyCatTrailsValidation.txt",report.ToString());
    }
}
