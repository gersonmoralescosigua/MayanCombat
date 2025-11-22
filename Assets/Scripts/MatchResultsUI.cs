using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MatchResults : MonoBehaviour
{
    public TMP_Text resultsText;
    public GameObject loadingIcon;
    public Button menuButton;

    void Start()
    {
        Debug.Log("🎯 MatchResults INICIADO - Configurando botones...");
        
        // Mostrar mensaje
        if (SessionManager.Instance != null)
        {
            resultsText.text = SessionManager.Instance.GameOverMessage;
            
            Debug.Log($"🔍 SessionManager - IsFinalMatch: {SessionManager.Instance.IsFinalMatch}, GameOverMessage: {SessionManager.Instance.GameOverMessage}");
            
            // --- LÓGICA CRÍTICA DE BOTONES ---
            if (SessionManager.Instance.IsFinalMatch)
            {
                // ✅ ES FINAL: Mostrar botones, ocultar loading
                loadingIcon.SetActive(false);
                menuButton.gameObject.SetActive(true);
                Debug.Log("🎯 MOSTRANDO BOTONES - Es partida final");
            }
            else
            {
                // ❌ NO ES FINAL: Mostrar loading, ocultar botones  
                loadingIcon.SetActive(true);
                menuButton.gameObject.SetActive(false);
                Debug.Log("🎯 MOSTRANDO LOADING - No es partida final");
            }
        }
        else
        {
            Debug.LogError("❌ SessionManager es NULL - Usando configuración por defecto");
            // Por defecto: mostrar loading, ocultar botones
            loadingIcon.SetActive(true);
            menuButton.gameObject.SetActive(false);
        }

        // Configurar botones (SIEMPRE, aunque estén ocultos)
        menuButton.onClick.AddListener(() => {
            Debug.Log("🔙 Botón Menú presionado");
            SceneManager.LoadScene("Menu");
        });
        
        
        Debug.Log("✅ MatchResults configurado correctamente");
    }
}