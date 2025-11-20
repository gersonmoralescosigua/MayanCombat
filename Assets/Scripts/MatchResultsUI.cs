using UnityEngine;
using TMPro;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;

    void Update()
    {
        // Actualizar constantemente para atrapar el mensaje apenas llegue
        if (SessionManager.Instance != null && winnerText != null)
        {
            if (!string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
            else
            {
                winnerText.text = "Calculando resultados del combate...";
            }
        }
    }
}