using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BallCatAppearance
{
    [InitializeOnLoadMethod] static void OnLoad(){EditorApplication.delayCall += () => {if(File.Exists("Temp/ApplyBallCat.request")){File.Delete("Temp/ApplyBallCat.request");Apply();}};}
    [MenuItem("Tools/Scene 3/Replace Ball With Assets Burger Cat")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/3.unity" || EditorApplication.isPlaying)
            throw new Exception("Open scene 3 in edit mode first.");
        var ball = GameObject.Find("ball");
        if (!ball || ball.transform.Find("Burger Cat Visual"))
            throw new Exception("Ball missing or already has cat visual.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Burger Cat.prefab");
        if (!source) throw new Exception("Assets/Burger Cat.prefab is missing.");
        var originalRenderer = ball.GetComponent<MeshRenderer>();
        var originalBounds = originalRenderer.bounds;
        var components = ball.GetComponents<Component>().Where(c => !(c is Transform) && c != originalRenderer).ToArray();
        var before = components.Select(c => EditorJsonUtility.ToJson(c)).ToArray();
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace ball appearance with Burger Cat");
        int group = Undo.GetCurrentGroup();
        // Cancel only the initial sphere's rotation/nonuniform scale. The visual then
        // follows its parent throughout dragging, launching, resetting and motion.
        var holder = new GameObject("Burger Cat Visual");
        Undo.RegisterCreatedObjectUndo(holder, "Create cat visual");
        holder.transform.rotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;
        holder.transform.position = ball.transform.position;
        holder.transform.SetParent(ball.transform, true);
        var cat = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
        Undo.RegisterCreatedObjectUndo(cat, "Add Burger Cat model");
        cat.transform.SetParent(holder.transform, false);
        cat.transform.localPosition = Vector3.zero;
        cat.transform.localRotation = source.transform.localRotation;
        cat.transform.localScale = Vector3.one;
        // Keep raycasts and all contacts on the original ball collider.
        foreach (var collider in cat.GetComponentsInChildren<Collider>(true))
            Undo.DestroyObjectImmediate(collider);
        foreach (var body in cat.GetComponentsInChildren<Rigidbody>(true))
            Undo.DestroyObjectImmediate(body);
        Bounds bounds = BoundsOf(cat);
        float width = Mathf.Max(originalBounds.size.x, originalBounds.size.y);
        cat.transform.localScale *= width / bounds.size.x;
        bounds = BoundsOf(cat);
        cat.transform.position += originalBounds.center - bounds.center;
        Undo.RecordObject(originalRenderer, "Hide original sphere mesh");
        originalRenderer.enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(originalRenderer);
        for (int i = 0; i < components.Length; i++)
            if (before[i] != EditorJsonUtility.ToJson(components[i]))
                throw new Exception("Unexpected change to player component " + components[i].GetType().Name);
        if (cat.GetComponentsInChildren<Collider>().Length != 0 || !ball.GetComponent<Collider>().enabled)
            throw new Exception("Collider validation failed.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = ball;
        var view = SceneView.lastActiveSceneView;
        view.sceneLighting = false;
        view.sceneViewState.showImageEffects = false;
        view.LookAt(originalBounds.center, Quaternion.Euler(8, -12, 0), width * 1.4f);
        File.WriteAllText("Temp/BallCatReplacement.txt", "PASS: scene 3 saved\nSource: Assets/Burger Cat.prefab\nOriginal player components unchanged: " +
            string.Join(", ", components.Select(c => c.GetType().Name)) +
            "\nOriginal sphere renderer disabled; original collider and rigidbody retained.\nVisual bounds: " + BoundsOf(cat));
    }
    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }
}



