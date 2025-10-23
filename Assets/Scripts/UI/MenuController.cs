using UnityEngine;
using UnityEngine.SceneManagement; // <- importante

public class MenuController : MonoBehaviour
{
    // Nombre de la escena que quieres cargar (configurable en el Inspector)
    public string sceneToLoad = "Menu";

    // Método público sin parámetros (aparecerá en OnClick)
    public void StartGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Juego cerrado");
    }
}