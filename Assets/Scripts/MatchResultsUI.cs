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
        if (btnMenu != null) btnMenu.onClick.AddListener(() => SceneManager.LoadScene("Menu"));

        if (SessionManager.Instance != null && winnerText != null)
        {
            winnerText.text = SessionManager.Instance.GameOverMessage;
        }
    }
}
