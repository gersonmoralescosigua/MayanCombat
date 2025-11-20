using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text winnerText;
    public Button btnMenu;
    public GameObject loadingSpinner; // Opcional, si quieres poner algo que gire

    void Start()
    {
        if (SessionManager.Instance != null && winnerText != null)
        {
            winnerText.text = SessionManager.Instance.GameOverMessage;
            
            // Si ES la final, mostramos el botón de salir
            if (SessionManager.Instance.IsFinalMatch)
            {
                if (btnMenu != null) 
                {
                    btnMenu.gameObject.SetActive(true);
                    btnMenu.onClick.AddListener(OnMenuClicked);
                }
            }
            else
            {
                // Si NO es la final (es ronda intermedia), ocultamos el botón
                // porque el Host nos moverá automáticamente al siguiente mapa
                if (btnMenu != null) btnMenu.gameObject.SetActive(false);
            }
        }
    }

    void OnMenuClicked()
    {
        if (SessionManager.Instance != null) SessionManager.Instance.GameOverMessage = "";
        
        if (NetworkRunnerHandler.Instance != null && NetworkRunnerHandler.Instance.Runner != null)
        {
            NetworkRunnerHandler.Instance.Runner.Shutdown();
        }
        
        SceneManager.LoadScene("Menu");
    }
}