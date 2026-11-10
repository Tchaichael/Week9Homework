using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayButton : MonoBehaviour
{
    public void Replay()
    {
        SceneManager.LoadScene("Game 7 Bowling");
    }
}