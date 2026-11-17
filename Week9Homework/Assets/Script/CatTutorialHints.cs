using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class CatTutorialHints : MonoBehaviour
{
    [Header("发射后：跟随小猫的提示")]
    [TextArea] public string followText = "Click to dash, Meow";
    [Min(1f)] public float followFontSize = 44f;
    [Tooltip("相对小猫的世界坐标偏移，X 左右、Y 上下、Z 前后。")]
    public Vector3 followOffset = new Vector3(0f, 15f, -12f);

    [Header("起点：固定位置的提示")]
    [TextArea] public string spawnText = "drag me";
    [Min(1f)] public float spawnFontSize = 48f;
    [Tooltip("相对发射起点的世界坐标偏移；不会跟随小猫移动。")]
    public Vector3 spawnOffset = new Vector3(0f, 23f, -12f);

    [Header("drag me 左边的箭头")]
    public Sprite arrowSprite;
    [Tooltip("相对 drag me 文字的位置偏移。")]
    public Vector3 arrowOffset = new Vector3(-16f, -6f, 0f);
    public Vector2 arrowSize = new Vector2(18f, 12f);
    public float arrowRotation;
    [Header("编辑预览")]
    [Tooltip("在编辑模式也显示跟随提示，方便拖动位置；运行时仍只在发射后显示。")]
    public bool previewFollowInEditor;

    [SerializeField, HideInInspector] private Vector3 launchPoint;
    [SerializeField, HideInInspector] private TextMeshPro followLabel;
    [SerializeField, HideInInspector] private TextMeshPro spawnLabel;
    [SerializeField, HideInInspector] private SpriteRenderer arrowLabel;
    [SerializeField, HideInInspector] private Vector3 lastFollowPosition, lastSpawnPosition, lastArrowPosition;
    [SerializeField, HideInInspector] private bool positionsInitialized;
    private Camera viewCamera;
    public TextMeshPro FollowLabel => followLabel;
    public TextMeshPro SpawnLabel => spawnLabel;
    public SpriteRenderer ArrowLabel => arrowLabel;

    private void Awake()
    {
        if (Application.isPlaying) CaptureLaunchPoint();
    }

    public void CaptureLaunchPoint() { launchPoint = transform.position; }

    private void LateUpdate()
    {
        var dash = GetComponent<CatClickDash>();
        RefreshHints(Application.isPlaying ? dash && dash.IsArmed : previewFollowInEditor);
    }

    public void RefreshHints(bool launched)
    {
        if (gameObject.scene.name != "0")
        {
            SetVisible(false, false);
            return;
        }
        if (!followLabel) followLabel = CreateLabel("Scene 0 - Following Cat Hint");
        if (!spawnLabel) spawnLabel = CreateLabel("Scene 0 - Fixed Drag Hint");
        if (arrowSprite && !arrowLabel)
        {
            var go = new GameObject("Scene 0 - Drag Hint Arrow");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            arrowLabel = go.AddComponent<SpriteRenderer>();
        }
        // Scene handles/Transform edits are authoritative in edit mode. Convert those
        // moves back into the public offsets rather than snapping them back each frame.
        if (!Application.isPlaying && positionsInitialized)
        {
            bool changed = false;
            if (followLabel.transform.position != lastFollowPosition)
            { followOffset = followLabel.transform.position - transform.position; changed = true; }
            if (spawnLabel.transform.position != lastSpawnPosition)
            { spawnOffset = spawnLabel.transform.position - launchPoint; changed = true; }
            if (arrowLabel && arrowLabel.transform.position != lastArrowPosition)
            { arrowOffset = arrowLabel.transform.position - (launchPoint + spawnOffset); changed = true; }
#if UNITY_EDITOR
            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
#endif
        }
        if (!viewCamera)
        {
            foreach (Camera candidate in Object.FindObjectsByType<Camera>())
                if (candidate.gameObject.scene == gameObject.scene && candidate.CompareTag("MainCamera"))
                { viewCamera = candidate; break; }
        }
        ApplyLabel(followLabel, followText, followFontSize, transform.position + followOffset);
        ApplyLabel(spawnLabel, spawnText, spawnFontSize, launchPoint + spawnOffset);
        if (arrowLabel && arrowSprite)
        {
            arrowLabel.sprite = arrowSprite;
            arrowLabel.color = Color.white;
            arrowLabel.transform.position = spawnLabel.transform.position + arrowOffset;
            arrowLabel.transform.rotation = spawnLabel.transform.rotation * Quaternion.Euler(0f, 0f, arrowRotation);
            Vector3 size = arrowSprite.bounds.size;
            arrowLabel.transform.localScale = new Vector3(Mathf.Max(.1f, arrowSize.x) / Mathf.Max(.001f, size.x),
                Mathf.Max(.1f, arrowSize.y) / Mathf.Max(.001f, size.y), 1f);
            lastArrowPosition = arrowLabel.transform.position;
        }
        lastFollowPosition = followLabel.transform.position;
        lastSpawnPosition = spawnLabel.transform.position;
        positionsInitialized = true;
        SetVisible(launched, true);
    }

    private TextMeshPro CreateLabel(string labelName)
    {
        // Independent roots avoid inheriting the cat's spin or non-uniform scale.
        var go = new GameObject(labelName);
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        var label = go.AddComponent<TextMeshPro>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = false;
        label.richText = false;
        label.rectTransform.sizeDelta = new Vector2(140f, 16f);
        var renderer = label.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return label;
    }

    private void ApplyLabel(TextMeshPro label, string content, float size, Vector3 position)
    {
        label.text = content;
        label.fontSize = Mathf.Max(1f, size);
        label.color = Color.white;
        label.transform.position = position;
        label.transform.rotation = viewCamera ? viewCamera.transform.rotation : Quaternion.identity;
        label.transform.localScale = Vector3.one;
    }

    private void SetVisible(bool follow, bool spawn)
    {
        if (followLabel && followLabel.gameObject.activeSelf != follow) followLabel.gameObject.SetActive(follow);
        if (spawnLabel && spawnLabel.gameObject.activeSelf != spawn) spawnLabel.gameObject.SetActive(spawn);
        if (arrowLabel) arrowLabel.gameObject.SetActive(spawn && arrowSprite);
    }

    private void OnDisable() { SetVisible(false, false); }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (followLabel) Destroy(followLabel.gameObject);
        if (spawnLabel) Destroy(spawnLabel.gameObject);
        if (arrowLabel) Destroy(arrowLabel.gameObject);
    }
}
