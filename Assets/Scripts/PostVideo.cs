using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class PostVideo : MonoBehaviour
{
    public VideoPlayer vp;
    public string nombreDeLaSiguienteEscena = "ranking";

    void Start()
    {
        // Suscribe un método para ser llamado cuando el video termine
        vp.loopPointReached += OnVideoEnd;
        vp.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video ha terminado, cargando el juego...");
        // Carga la siguiente escena.
        SceneManager.LoadScene(nombreDeLaSiguienteEscena);
    }
}