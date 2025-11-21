using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class VideoResultManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    [Header("Clips de Video")]
    public VideoClip ganaMaya;
    public VideoClip pierdeMaya;
    public VideoClip ganaSpanish;
    public VideoClip pierdeSpanish;

    void Start()
    {
        // Si el SessionManager aún no tiene datos, es un error grave, 
        // pero con la pausa de 1.5s en el Handler, esto YA NO debería pasar.
        if (SessionManager.Instance == null) return;

        int myTeam = SessionManager.Instance.currentTeam;
        int winnerTeam = SessionManager.Instance.FinalWinnerTeam; // Ahora esto SÍ tendrá valor

        Debug.Log($"🎬 PLAY VIDEO: Soy Team {myTeam}. Ganó {winnerTeam}.");

        VideoClip clipToPlay = null;
        bool iWon = (myTeam == winnerTeam);

        if (myTeam == 0) // Soy Maya
        {
            clipToPlay = iWon ? ganaMaya : pierdeMaya;
        }
        else // Soy Español
        {
            clipToPlay = iWon ? ganaSpanish : pierdeSpanish;
        }

        if (videoPlayer != null && clipToPlay != null)
        {
            videoPlayer.clip = clipToPlay;
            videoPlayer.Play();
            // IMPORTANTE: Eliminé la línea que cargaba el menú aquí.
            // El NetworkRunnerHandler (Servidor) se encargará de cambiarnos de escena
            // cuando acabe el tiempo del video.
        }
    }
}