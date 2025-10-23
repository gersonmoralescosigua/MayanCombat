using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundTimer : MonoBehaviour
{
    public float roundDuration = 60f;

    private void Start()
    {
        Invoke(nameof(EndMatch), roundDuration);
    }

    void EndMatch()
    {
        Debug.Log("🏁 Tiempo terminado — finalizando partida.");
        SceneManager.LoadScene("Menu");
    }
}
