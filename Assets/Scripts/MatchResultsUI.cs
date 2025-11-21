using UnityEngine;
using TMPro;
using UnityEngine.UI; // Necesario para el Botón

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;
    public Button btnBackToMenu; // ASIGNA ESTO EN EL INSPECTOR

    void Start()
    {
        if (btnBackToMenu != null)
        {
            btnBackToMenu.onClick.AddListener(OnBackToMenuClicked);
            // Lo ocultamos por defecto para que no salga en las rondas intermedias
            btnBackToMenu.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            // 1. Mostrar texto (Ahora sí funciona porque el RPC actualizó el SessionManager)
            if (!string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
            else
            {
                winnerText.text = "Recibiendo datos del árbitro...";
            }

            // 2. Mostrar botón SOLO si es la final
            if (SessionManager.Instance.IsFinalMatch && btnBackToMenu != null)
            {
                if (!btnBackToMenu.gameObject.activeSelf)
                {
                    btnBackToMenu.gameObject.SetActive(true);
                }
            }
        }
    }

    void OnBackToMenuClicked()
    {
        Debug.Log("🔙 Volviendo al Menú Principal...");
        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.ShutdownAndMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
    }
}