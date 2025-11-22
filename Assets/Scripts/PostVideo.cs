using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PostVideo : MonoBehaviour
{
    public VideoPlayer vp;
    public VideoClip videoMayaWins;
    public VideoClip videoSpanishWins;
    public string nombreDeLaSiguienteEscena = "MatchResults";

    void Start()
    {
        // Suscribe un método para ser llamado cuando el video termine
        vp.loopPointReached += OnVideoEnd;
        
        // SELECCIONAR VIDEO CORRECTO BASADO EN EL GANADOR
        SelectCorrectVideo();
        
        vp.Play();
        Debug.Log("🎬 Reproduciendo video: " + vp.clip.name);
    }

    void SelectCorrectVideo()
    {
        if (SessionManager.Instance != null && SessionManager.Instance.FinalWinnerTeam != -1)
        {
            if (SessionManager.Instance.FinalWinnerTeam == 0) // Maya gana
            {
                if (videoMayaWins != null)
                {
                    vp.clip = videoMayaWins;
                    Debug.Log("🏆 Reproduciendo video: MAYA GANA");
                }
            }
            else // Español gana
            {
                if (videoSpanishWins != null)
                {
                    vp.clip = videoSpanishWins;
                    Debug.Log("🏆 Reproduciendo video: ESPAÑOL GANA");
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No se pudo determinar el ganador, usando video por defecto");
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("✅ Video ha terminado, cargando resultados finales...");
        SceneManager.LoadScene(nombreDeLaSiguienteEscena);
    }

    // Botón de skip para testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("⏭️ Saltando video...");
            SceneManager.LoadScene(nombreDeLaSiguienteEscena);
        }
    }
}