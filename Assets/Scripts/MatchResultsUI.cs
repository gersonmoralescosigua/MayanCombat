

using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necesario aunque no usemos botones


public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;
            public GameObject loadingSpinner; // Opcional, si quieres poner algo que gire


    void Start()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            // Si el mensaje está vacío, ponemos uno por defecto
            if (string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = "Procesando resultados de la ronda...";
            }
            else
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
        }
    }
}