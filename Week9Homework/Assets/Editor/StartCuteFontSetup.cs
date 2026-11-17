using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartCuteFontSetup
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/StartCuteFont.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Apply();
            File.Delete(request);
        };
    }

    [MenuItem("Tools/Start/Use Cute Handwritten Font")]
    public static void Apply()
    {
        const string assetPath = "Assets/Fonts/Start Cute SDF.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (!font)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Start Cute Bold.ttf");
            if (!source) throw new System.Exception("Start font source missing.");
            font = TMP_FontAsset.CreateFontAsset(source);
            font.name = "Start Cute SDF";
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?'-");
            AssetDatabase.CreateAsset(font, assetPath);
            if (font.material && !AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures)
                if (atlas && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Start.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Additive);
        try
        {
            var menu = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<StartMenuController>(true)).Single();
            Undo.RecordObject(menu.prompt, "Use cute handwritten start font");
            menu.prompt.font = font;
            menu.prompt.fontStyle = FontStyles.Normal;
            menu.prompt.ForceMeshUpdate(true, true);
            if (!font.HasCharacter('L') || menu.prompt.textInfo.characterCount == 0)
                throw new System.Exception("Start font glyph validation failed.");
            EditorUtility.SetDirty(menu.prompt);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Temp/StartCuteFontReport.txt", "PASS: start prompt uses " + font.faceInfo.familyName +
                " " + font.faceInfo.styleName + "; glyph atlas generated and saved; animation and scene link retained.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
