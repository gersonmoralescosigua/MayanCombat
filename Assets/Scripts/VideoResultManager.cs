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
        // Validación de seguridad
        if (SessionManager.Instance == null) return;

        int myTeam = SessionManager.Instance.currentTeam;     // 0 o 1
        int winnerTeam = SessionManager.Instance.FinalWinnerTeam; // Quién ganó

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
            StartCoroutine(WaitAndExit((float)clipToPlay.length));
        }
    }

    IEnumerator WaitAndExit(float duration)
    {
        yield return new WaitForSeconds(duration + 1f);
        // Regresar al menú o ranking
        SceneManager.LoadScene("Menu"); 
    }
}