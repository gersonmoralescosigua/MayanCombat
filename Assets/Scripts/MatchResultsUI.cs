using UnityEngine;
using TMPro;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;

    void Update()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            // Muestra lo que SessionManager tenga guardado
            if (!string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
            else
            {
                winnerText.text = "Recibiendo datos del árbitro...";
            }
        }
    }
}