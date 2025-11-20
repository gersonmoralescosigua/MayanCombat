using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
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
        // Si entramos a esta escena sin SessionManager (pruebas), salimos
        if (SessionManager.Instance == null) return;

        int myTeam = SessionManager.Instance.currentTeam;     // 0 o 1
        int winnerTeam = SessionManager.Instance.FinalWinnerTeam; // Quién ganó el torneo

        Debug.Log($"🎬 Iniciando Video. Soy Team {myTeam}. Ganó Team {winnerTeam}.");

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
            
            // Ir al ranking o menú cuando termine el video
            StartCoroutine(WaitAndExit((float)clipToPlay.length));
        }
    }

    IEnumerator WaitAndExit(float duration)
    {
        // Esperamos la duración del video + 1 segundo de margen
        yield return new WaitForSeconds(duration + 1f);
        
        // Cargar siguiente escena (Ranking o Menu)
        // Asegúrate de que esta escena exista en Build Settings
        SceneManager.LoadScene("Menu"); 
    }
}