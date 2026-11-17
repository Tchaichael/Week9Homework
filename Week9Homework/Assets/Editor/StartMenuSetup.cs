using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StartMenuSetup
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
        const string request = "Temp/StartMenuSetup.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Apply();
        File.Delete(request);
    }

    [MenuItem("Tools/Start/Build Start Screen")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Start.unity");
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Start.unity", OpenSceneMode.Additive);
        try
        {
            var cat = Components<BallForces>(scene).Single(c => c.name == "ball").transform;
            var white = Components<Transform>(scene).Single(t => t.name == "wincondition");
            int catVisuals = cat.GetComponentsInChildren<MeshRenderer>(true).Length;
            int whiteVisuals = white.GetComponentsInChildren<MeshRenderer>(true).Length;
            foreach (var root in scene.GetRootGameObjects())
                if (PrefabUtility.IsAnyPrefabInstanceRoot(root))
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            foreach (var tutorial in Components<CatTutorialHints>(scene)) Undo.DestroyObjectImmediate(tutorial);
            foreach (var t in Components<Transform>(scene).Where(t => t.name.StartsWith("Scene 0 - ")).ToArray())
                if (t) Undo.DestroyObjectImmediate(t.gameObject);
            var removed = 0;
            foreach (var renderer in Components<MeshRenderer>(scene).OrderByDescending(r => Depth(r.transform)).ToArray())
            {
                if (!renderer) continue;
                Transform t = renderer.transform;
                if (t.IsChildOf(cat) || cat.IsChildOf(t) || t.IsChildOf(white) || white.IsChildOf(t)) continue;
                // The board background is a skybox. Other world meshes here are the
                // old level's black pieces, rails and finite board geometry.
                Undo.DestroyObjectImmediate(renderer.gameObject);
                removed++;
            }
            foreach (var body in Components<Rigidbody>(scene))
            {
                Undo.RecordObject(body, "Freeze start-screen decoration");
                body.isKinematic = true;
                body.useGravity = false;
            }
            foreach (var behaviour in Components<MonoBehaviour>(scene))
            {
                if (!behaviour) continue;
                if (behaviour is BallForces || behaviour is BallGameManager || behaviour is CatClickDash ||
                    behaviour is CatLaunchSpin || behaviour is CollisionSplit || behaviour is SplitBall ||
                    behaviour is GoPieceChecker || behaviour is MovingObstacle || behaviour is RotatingObstacle ||
                    behaviour is RollBall || behaviour is BowlingController)
                {
                    Undo.RecordObject(behaviour, "Disable level gameplay on start screen");
                    behaviour.enabled = false;
                }
            }
            foreach (var canvas in Components<Canvas>(scene))
                if (canvas.name != "Start Menu Canvas") canvas.gameObject.SetActive(false);
            var oldMenu = Components<StartMenuController>(scene).FirstOrDefault();
            if (oldMenu) Undo.DestroyObjectImmediate(oldMenu.gameObject);
            var rootUI = new GameObject("Start Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(rootUI, scene);
            Undo.RegisterCreatedObjectUndo(rootUI, "Create start menu");
            var menuCanvas = rootUI.GetComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 100;
            var scaler = rootUI.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            var labelObject = new GameObject("Left Click To Start", typeof(RectTransform));
            labelObject.transform.SetParent(rootUI.transform, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Start Cute SDF.asset");
            bool cuteFont = label.font;
            if (!label.font) label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            label.text = "Left Click to Start";
            label.fontSize = 56f;
            label.fontStyle = cuteFont ? FontStyles.Normal : FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
            label.rectTransform.pivot = new Vector2(.5f, .5f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(1400f, 120f);
            var menu = rootUI.AddComponent<StartMenuController>();
            menu.prompt = label;
            menu.InitializePrompt();
            var camera = Components<Camera>(scene).Single(c => c.CompareTag("MainCamera"));
            var whiteRenderers = white.GetComponentsInChildren<MeshRenderer>();
            Bounds whiteBounds = whiteRenderers[0].bounds;
            foreach (var renderer in whiteRenderers) whiteBounds.Encapsulate(renderer.bounds);
            Ray whiteRay = camera.ViewportPointToRay(new Vector3(.78f, .5f, 0f));
            var whitePlane = new Plane(Vector3.forward, whiteBounds.center);
            if (whitePlane.Raycast(whiteRay, out float whiteDistance))
            {
                Undo.RecordObject(white, "Place white piece beside start prompt");
                white.position += whiteRay.GetPoint(whiteDistance) - whiteBounds.center;
            }
            var entries = EditorBuildSettings.scenes.Where(s => s.path != scene.path).ToList();
            entries.Insert(0, new EditorBuildSettingsScene(scene.path, true));
            if (!entries.Any(s => s.path == "Assets/Scenes/0.unity"))
                entries.Add(new EditorBuildSettingsScene("Assets/Scenes/0.unity", true));
            foreach (var entry in entries) if (entry.path == "Assets/Scenes/0.unity") entry.enabled = true;
            EditorBuildSettings.scenes = entries.ToArray();
            Require(cat && white && catVisuals == cat.GetComponentsInChildren<MeshRenderer>(true).Length &&
                whiteVisuals == white.GetComponentsInChildren<MeshRenderer>(true).Length, "cat and white piece retained");
            Require(!Components<MeshRenderer>(scene).Any(r => !r.transform.IsChildOf(cat) &&
                !r.transform.IsChildOf(white) && !cat.IsChildOf(r.transform) && !white.IsChildOf(r.transform)),
                "black level meshes removed");
            Require(label.font && label.rectTransform.anchoredPosition == Vector2.zero, "font and centered prompt");
            menu.AnimatePrompt(menu.animationPeriod * .25f);
            Require(label.rectTransform.anchoredPosition.y > 0f && label.rectTransform.localScale.x > 1f,
                "prompt bounces and scales");
            menu.AnimatePrompt(0f);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "start scene saved");
            File.WriteAllText("Temp/StartMenuReport.txt", "PASS: Start is first enabled build scene; left click loads Assets/Scenes/0.unity; centered English prompt animates; cat and white piece preserved; removed old level meshes=" + removed + "; board background preserved.");
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            if (opened) EditorSceneManager.CloseScene(scene, true);
            var grid = Components<ChessBackgroundSettings>(original).FirstOrDefault();
            if (grid) grid.Apply();
        }
    }

    private static T[] Components<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

    private static int Depth(Transform t)
    {
        int depth = 0;
        while (t.parent) { t = t.parent; depth++; }
        return depth;
    }

    private static void Require(bool result, string check)
    {
        if (!result) throw new Exception("Start menu setup failed: " + check);
    }
}
