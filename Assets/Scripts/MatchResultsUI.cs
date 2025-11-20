using UnityEngine;
using TMPro;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;

    void Start()
    {
        // Forzamos actualización del texto
        UpdateText();
    }

    void Update()
    {
        // Opcional: Actualizar en tiempo real por si el RPC llega un poco tarde
        // (Solo si el texto sigue siendo el default)
        if (winnerText != null && winnerText.text.Contains("Esperando"))
        {
            UpdateText();
        }
    }

    void UpdateText()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            if (!string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
            else
            {
                winnerText.text = "Esperando resultados del árbitro...";
            }
        }
    }
}


