using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CatTutorialHints))]
public class CatTutorialHintsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("可以修改上方 Offset，也可以在 Scene 视图直接拖动两行文字或箭头。勾选 Preview Follow In Editor 可预览跟随文字。", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        if (Application.isPlaying) return;
        var hints = (CatTutorialHints)target;
        Handle(hints, hints.FollowLabel ? hints.FollowLabel.transform : null, "Follow hint", 0);
        Handle(hints, hints.SpawnLabel ? hints.SpawnLabel.transform : null, "drag me", 1);
        Handle(hints, hints.ArrowLabel ? hints.ArrowLabel.transform : null, "Arrow", 2);
    }

    private static void Handle(CatTutorialHints hints, Transform label, string title, int kind)
    {
        if (!label) return;
        Handles.Label(label.position, title);
        EditorGUI.BeginChangeCheck();
        Vector3 position = Handles.PositionHandle(label.position, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(hints, "Move tutorial hint");
        Vector3 delta = position - label.position;
        if (kind == 0) hints.followOffset += delta;
        else if (kind == 1) hints.spawnOffset += delta;
        else hints.arrowOffset += delta;
        EditorUtility.SetDirty(hints);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hints);
        hints.RefreshHints(hints.previewFollowInEditor);
    }
}
