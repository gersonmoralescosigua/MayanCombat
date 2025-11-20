using UnityEngine;
using TMPro;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;

    void Start()
    {
        // Intento inicial
        RefreshText();
    }

    void Update()
    {
        // Si el texto sigue diciendo "Esperando...", seguimos consultando a SessionManager
        // Esto arregla el problema de sincronización si el RPC llega después de cargar la escena
        if (winnerText != null && winnerText.text.Contains("Esperando"))
        {
            RefreshText();
        }
    }

    void RefreshText()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            if (!string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
            else
            {
                winnerText.text = "Esperando resultados del árbitro...\n(Calculando)";
            }
        }
    }
}