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
    Debug.Log($"🎬 CLIENTE: Iniciando SelectCorrectVideo()");
    
    if (SessionManager.Instance != null)
    {
        Debug.Log($"🎬 CLIENTE: SessionManager encontrado - FinalWinnerTeam: {SessionManager.Instance.FinalWinnerTeam}, currentTeam: {SessionManager.Instance.currentTeam}");
        
        if (SessionManager.Instance.FinalWinnerTeam != -1)
        {
            if (SessionManager.Instance.FinalWinnerTeam == 0) // Maya gana
            {
                if (videoMayaWins != null)
                {
                    vp.clip = videoMayaWins;
                    Debug.Log($"🏆 CLIENTE: Reproduciendo video MAYA GANA - Soy team {SessionManager.Instance.currentTeam}");
                }
            }
            else // Español gana
            {
                if (videoSpanishWins != null)
                {
                    vp.clip = videoSpanishWins;
                    Debug.Log($"🏆 CLIENTE: Reproduciendo video ESPAÑOL GANA - Soy team {SessionManager.Instance.currentTeam}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ CLIENTE: FinalWinnerTeam es -1 - No se pudo determinar ganador");
        }
    }
    else
    {
        Debug.LogError($"❌ CLIENTE: SessionManager es NULL - No se puede seleccionar video");
    }

    // DEBUG FINAL
    Debug.Log($"🎬 CLIENTE: Video seleccionado: {(vp.clip != null ? vp.clip.name : "NULL")}");
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