using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene3CheeseTriggerRepair
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
        const string request = "Temp/Scene3CheeseTriggerRepair.request";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/3.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BallForces>())
            .Single(g => g.name == "ball");
        var foods = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CheesePickup>(true)).ToArray();
        foreach (var food in foods)
        {
            Undo.RecordObject(food, "Align cheese pickup with visual");
            food.autoFitTrigger = true;
            food.gameplayDepth = player.transform.position.z;
            var collider = food.GetComponent<BoxCollider>();
            Undo.RecordObject(collider, "Repair cheese trigger position");
            collider.enabled = true;
            food.RefreshPickupBounds();
            var body = food.GetComponent<Rigidbody>();
            Undo.RecordObject(body, "Enable cheese pickup contacts");
            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = true;
            foreach (var childCollider in food.GetComponentsInChildren<Collider>(true))
            {
                if (childCollider == collider) continue;
                Undo.RecordObject(childCollider, "Use one aligned cheese trigger");
                childCollider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(childCollider);
            }
            EditorUtility.SetDirty(food);
            EditorUtility.SetDirty(collider);
            EditorUtility.SetDirty(body);
            PrefabUtility.RecordPrefabInstancePropertyModifications(food);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/Scene3CheeseTriggerRepairReport.txt", "Aligned " + foods.Length +
            " cheese triggers to their visible meshes; audio references retained.");
        File.Delete(request);
        File.WriteAllText("Temp/Scene3CheesePhysics.request", "Retest physical pickup after visual/trigger alignment.");
    }
}
