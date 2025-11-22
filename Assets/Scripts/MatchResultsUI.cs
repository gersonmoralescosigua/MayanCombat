using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MatchResults : MonoBehaviour
{
    public TMP_Text resultsText;
    public GameObject loadingIcon;
    public Button menuButton;
    public Button rankingButton;

    void Start()
    {
        // Mostrar mensaje
        if (SessionManager.Instance != null)
        {
            resultsText.text = SessionManager.Instance.GameOverMessage;
            
            // Si es la final, mostrar botones. Si no, mostrar loading
            if (SessionManager.Instance.IsFinalMatch)
            {
                loadingIcon.SetActive(false);
                menuButton.gameObject.SetActive(true);
                rankingButton.gameObject.SetActive(true);
            }
            else
            {
                loadingIcon.SetActive(true);
                menuButton.gameObject.SetActive(false);
                rankingButton.gameObject.SetActive(false);
            }
        }

        // Configurar botones
        menuButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
        rankingButton.onClick.AddListener(() => SceneManager.LoadScene("Ranking"));
    }
}