using UnityEngine;
using UnityEngine.SceneManagement;

public class GoPieceChecker : MonoBehaviour
{
    [Header("四个方向的独立距离阈值")]
    [Tooltip("最小有效距离")]
    public float minOffset = 0.15f;
    [Tooltip("最大有效距离（根据你之前报的 0.25~0.28 差值，设为 0.35 绝对能覆盖）")]
    public float maxOffset = 0.35f;

    private bool isEliminated = false;

    void Update()
    {
        if (isEliminated) return;
        CheckSurroundedGlobal();
    }

    void CheckSurroundedGlobal()
    {
        bool hasUp = false;
        bool hasDown = false;
        bool hasLeft = false;
        bool hasRight = false;

        Vector3 myPos = transform.position;

        // 【关键改动】：不再依赖父物体！直接以自己为中心，搜索周围半径 2 米内的所有 Collider
        Collider[] allColliders = Physics.OverlapSphere(myPos, 2.0f);

        foreach (Collider col in allColliders)
        {
            // 跳过自己
            if (col.gameObject == gameObject) continue;

            // 计算相对坐标偏移
            Vector3 offset = col.transform.position - myPos;
            float absX = Mathf.Abs(offset.x);
            float absY = Mathf.Abs(offset.y);

            // 1. 判断是否在【垂直方向】（上方或下方）：X 偏移很小，Y 偏移在范围内
            if (absX < 0.12f && absY >= minOffset && absY <= maxOffset)
            {
                if (offset.y > 0)
                {
                    hasUp = true;
                    // Debug.Log($"[全局] 上方发现棋子: {col.name}, 偏移: {offset}");
                }
                else
                {
                    hasDown = true;
                    // Debug.Log($"[全局] 下方发现棋子: {col.name}, 偏移: {offset}");
                }
            }

            // 2. 判断是否在【水平方向】（左方或右方）：Y 偏移很小，X 偏移在范围内
            if (absY < 0.12f && absX >= minOffset && absX <= maxOffset)
            {
                if (offset.x > 0)
                {
                    hasRight = true;
                    // Debug.Log($"[全局] 右方发现棋子: {col.name}, 偏移: {offset}");
                }
                else
                {
                    hasLeft = true;
                    // Debug.Log($"[全局] 左方发现棋子: {col.name}, 偏移: {offset}");
                }
            }
        }

        int count = (hasUp ? 1 : 0) + (hasDown ? 1 : 0) + (hasLeft ? 1 : 0) + (hasRight ? 1 : 0);

        // 实时打印状态
        //Debug.Log(gameObject.name + $"【全局扫描】上:{hasUp} 下:{hasDown} 左:{hasLeft} 右:{hasRight} (总计: {count}/4)");

        if (count >= 4)
        {
            isEliminated = true;
            OnEliminated();
        }
    }

    void OnEliminated()
    {
        Debug.Log("【围棋消除】目标棋子四周被全景扫描锁定，触发消除！");
        Destroy(gameObject);
        Invoke("LoadNextScene", 0.5f);
    }

    void LoadNextScene()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}