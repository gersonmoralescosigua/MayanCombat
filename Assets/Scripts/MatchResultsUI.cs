
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necesario aunque no usemos botones

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;
    // public Button btnMenu; // YA NO LO NECESITAMOS
        public GameObject loadingSpinner; // Opcional, si quieres poner algo que gire


    void Start()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            // Mostrar el mensaje que llegó por RPC
            if (string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = "Esperando resultados...";
            }
            else
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
        }
    }
}