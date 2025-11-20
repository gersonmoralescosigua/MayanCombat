using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;
    public Button btnMenu;

    void Start()
    {
        if (btnMenu != null) btnMenu.onClick.AddListener(OnMenuClicked);

        if (SessionManager.Instance != null && winnerText != null)
        {
            if (string.IsNullOrEmpty(SessionManager.Instance.GameOverMessage))
            {
                winnerText.text = "JUEGO TERMINADO";
            }
            else
            {
                winnerText.text = SessionManager.Instance.GameOverMessage;
            }
        }
    }

    void OnMenuClicked()
    {
        if (SessionManager.Instance != null) SessionManager.Instance.GameOverMessage = "";
        
        // Cerrar conexión de red
        if (NetworkRunnerHandler.Instance != null && NetworkRunnerHandler.Instance.Runner != null)
        {
            NetworkRunnerHandler.Instance.Runner.Shutdown();
        }
        
        SceneManager.LoadScene("Menu");
    }
}