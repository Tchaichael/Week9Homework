using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    public TMP_Text prompt;
    public string promptText = "Left Click to Start";
    public string gameScenePath = "Assets/Scenes/0.unity";
    [Min(0f)] public float bounceHeight = 8f;
    [Range(0f, .2f)] public float pulseAmount = .04f;
    [Min(.1f)] public float animationPeriod = 1.6f;
    private Vector2 restPosition;
    private Vector3 restScale = Vector3.one;
    private float enabledAt;
    private bool loading;

    private void OnEnable()
    {
        enabledAt = Time.unscaledTime;
        InitializePrompt();
    }

    public void InitializePrompt()
    {
        if (!prompt) return;
        prompt.text = promptText;
        restPosition = prompt.rectTransform.anchoredPosition;
        restScale = prompt.rectTransform.localScale;
    }

    private void Update()
    {
        float elapsed = Time.unscaledTime - enabledAt;
        AnimatePrompt(elapsed);
        if (!loading && elapsed > .15f && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            StartGame();
    }

    public void AnimatePrompt(float elapsed)
    {
        if (!prompt) return;
        float wave = Mathf.Sin(elapsed * Mathf.PI * 2f / Mathf.Max(.1f, animationPeriod));
        prompt.rectTransform.anchoredPosition = restPosition + Vector2.up * (wave * bounceHeight);
        prompt.rectTransform.localScale = restScale * (1f + wave * pulseAmount);
    }

    public void StartGame()
    {
        if (loading) return;
        loading = true;
        SceneManager.LoadScene(gameScenePath, LoadSceneMode.Single);
    }
}
