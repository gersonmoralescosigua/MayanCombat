using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MatchResultsUI : MonoBehaviour
{
    public TMP_Text resultsText;
    public GameObject loadingIcon;
    public Button menuButton;
    public Button rankingButton;

    void Start()
    {
        Debug.Log($"🎯 MatchResults iniciado - Buscando datos en SessionManager");
        
        // Mostrar mensaje
        if (SessionManager.Instance != null)
        {
            Debug.Log($"🎯 SessionManager encontrado - GameOverMessage: '{SessionManager.Instance.GameOverMessage}', IsFinalMatch: {SessionManager.Instance.IsFinalMatch}");
            
            resultsText.text = SessionManager.Instance.GameOverMessage;
            
            // Si es la final, mostrar botones. Si no, mostrar loading
            if (SessionManager.Instance.IsFinalMatch)
            {
                loadingIcon.SetActive(false);
                menuButton.gameObject.SetActive(true);
                rankingButton.gameObject.SetActive(true);
                Debug.Log($"🎯 MOSTRANDO BOTONES FINALES - Partida terminada");
            }
            else
            {
                loadingIcon.SetActive(true);
                menuButton.gameObject.SetActive(false);
                rankingButton.gameObject.SetActive(false);
                Debug.Log($"🎯 MOSTRANDO LOADING - Aún no es final");
            }
        }
        else
        {
            Debug.LogError($"❌ MatchResults: SessionManager es NULL");
        }

        // Configurar botones
        menuButton.onClick.AddListener(() => {
            Debug.Log("🔙 Saliendo al menú...");
            SceneManager.LoadScene("Menu");
        });
        rankingButton.onClick.AddListener(() => {
            Debug.Log("📊 Yendo a ranking...");
            SceneManager.LoadScene("Ranking");
        });
    }
}