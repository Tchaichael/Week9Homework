using UnityEngine;

[ExecuteAlways]
public class ChessBackgroundSettings : MonoBehaviour
{
    public Material backgroundMaterial;
    [InspectorName("格子尺寸（世界单位）"), Min(0.1f)]
    public float cellSize = 9f;
    [InspectorName("格子位置偏移（X / Y）")]
    public Vector2 gridOffset = Vector2.zero;
    [InspectorName("对齐平面 Z"), Tooltip("应与小猫、棋子的游戏平面接近，避免移动时出现视差。")]
    public float projectionDepth = -79f;

    private void OnEnable() { Apply(); }
    private void OnValidate() { Apply(); }

    public void Apply()
    {
        if (!backgroundMaterial) return;
        backgroundMaterial.SetFloat("_CellSize", Mathf.Max(.1f, cellSize));
        backgroundMaterial.SetVector("_GridOffset", new Vector4(gridOffset.x, gridOffset.y, 0f, 0f));
        backgroundMaterial.SetFloat("_BoardZ", projectionDepth);
        RenderSettings.skybox = backgroundMaterial;
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(backgroundMaterial);
        UnityEditor.SceneView.RepaintAll();
#endif
    }
}
