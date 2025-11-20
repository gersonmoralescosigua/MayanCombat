using UnityEngine;
using TMPro;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;

    void Update()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            // Actualizamos siempre, así atrapamos el mensaje cuando llegue
            if (!string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage) && 
                SessionManager.Instance.GameOverMessage != "Cargando resultados...")
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
            else
            {
                winnerText.text = "Esperando datos del árbitro...\n(No cierres el juego)";
            }
        }
    }
}