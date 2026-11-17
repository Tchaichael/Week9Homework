using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene0CatTutorialSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/Scene0CatTutorial.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Scene 0/Add Cat Tutorial Hints")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/0.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/0.unity", OpenSceneMode.Additive);
        try
        {
            var cat = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BallForces>(true))
                .Single(g => g.name == "ball");
            var hints = cat.GetComponent<CatTutorialHints>();
            if (!hints) hints = Undo.AddComponent<CatTutorialHints>(cat.gameObject);
            const string arrowPath = "Assets/BurgerCat/Tutorial Arrow.png";
            var importer = AssetImporter.GetAtPath(arrowPath) as TextureImporter;
            if (importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            hints.arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(arrowPath);
            hints.CaptureLaunchPoint();
            hints.RefreshHints(false);
            Require(hints.FollowLabel && hints.SpawnLabel && hints.FollowLabel.font && hints.SpawnLabel.font,
                "Text labels and font references");
            Require(!hints.FollowLabel.gameObject.activeSelf && hints.SpawnLabel.gameObject.activeSelf,
                "Only fixed drag prompt visible before launch");
            Require(hints.ArrowLabel && hints.ArrowLabel.sprite, "Arrow sprite linked");
            EditorUtility.SetDirty(hints);
            PrefabUtility.RecordPrefabInstancePropertyModifications(hints);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Scene 0 saved");
            Validate();
            File.WriteAllText("Temp/Scene0CatTutorialReport.txt",
                "PASS: scene 0 labels, transparent arrow and fonts saved; direct text/arrow dragging, editable offsets, following movement, fixed spawn position, white text, and scene 1 isolation validated in Unity.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            var grid = original.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ChessBackgroundSettings>()).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }

    private static void Validate()
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        preview.name = "0";
        try
        {
            var cat = new GameObject("Tutorial validation cat");
            SceneManager.MoveGameObjectToScene(cat, preview);
            cat.transform.position = new Vector3(5f, 10f, -79f);
            var hints = cat.AddComponent<CatTutorialHints>();
            hints.arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BurgerCat/Tutorial Arrow.png");
            hints.CaptureLaunchPoint();
            hints.RefreshHints(false);
            Require(!hints.FollowLabel.gameObject.activeSelf && hints.SpawnLabel.gameObject.activeSelf, "prelaunch state");
            Vector3 fixedPosition = hints.SpawnLabel.transform.position;
            hints.RefreshHints(true);
            Require(hints.FollowLabel.gameObject.activeSelf, "follow text shows after launch");
            cat.transform.position += new Vector3(20f, 30f, 0f);
            hints.followText = "Custom hint";
            hints.followFontSize = 60f;
            hints.followOffset = new Vector3(2f, 18f, -10f);
            hints.RefreshHints(true);
            Require(hints.FollowLabel.transform.position == cat.transform.position + hints.followOffset, "follow position");
            Require(hints.SpawnLabel.transform.position == fixedPosition, "fixed spawn position");
            Require(hints.FollowLabel.text == "Custom hint" && hints.FollowLabel.fontSize == 60f, "editable text/size");
            Require(hints.FollowLabel.color == Color.white && hints.SpawnLabel.color == Color.white, "white text");
            hints.SpawnLabel.transform.position += new Vector3(3f, 2f, 0f);
            Vector3 movedSpawn = hints.SpawnLabel.transform.position;
            hints.RefreshHints(true);
            Require(hints.SpawnLabel.transform.position == movedSpawn, "direct Transform drag remains in place");
            hints.spawnOffset += Vector3.right * 4f;
            hints.RefreshHints(true);
            Require(hints.SpawnLabel.transform.position == movedSpawn + Vector3.right * 4f, "public offset still editable");
            Require(hints.ArrowLabel && hints.ArrowLabel.sprite, "arrow created");
            hints.ArrowLabel.transform.position += new Vector3(-2f, 3f, 0f);
            Vector3 movedArrow = hints.ArrowLabel.transform.position;
            hints.RefreshHints(true);
            Require(hints.ArrowLabel.transform.position == movedArrow, "direct arrow dragging remains in place");
            preview.name = "1";
            hints.RefreshHints(true);
            Require(!hints.FollowLabel.gameObject.activeSelf && !hints.SpawnLabel.gameObject.activeSelf, "scene 0 only");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void Require(bool result, string message)
    {
        if (!result) throw new Exception("Tutorial hint validation failed: " + message);
    }
}
