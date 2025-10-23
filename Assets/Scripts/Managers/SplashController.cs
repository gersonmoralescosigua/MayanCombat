using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashController : MonoBehaviour
{
    public float delay = 10f; // segundos antes de pasar a Login

    void Start()
    {
        Invoke("LoadLoginScene", delay);
    }

    void LoadLoginScene()
    {
        SceneManager.LoadScene("Login");
    }
}
