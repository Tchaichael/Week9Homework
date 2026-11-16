using UnityEngine;
using UnityEngine.SceneManagement;

public class Retry : MonoBehaviour
{
    public void Start()
    {
        gameObject.SetActive(false);
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void ShowRetry()
    {
        gameObject.SetActive(true);
    }
}