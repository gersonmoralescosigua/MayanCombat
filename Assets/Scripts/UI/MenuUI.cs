using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [Header("Botones - Menu Principal")]
    public Button btnUnirseRapida;
    public Button btnCrearEquipo;
    public Button btnAmistades;
    public Button btnRanking;
    public Button btnAjustes;
    public Button btnSalir;

    [Header("Texto de bienvenida")]
    public TextMeshProUGUI txtBienvenida;  // Cambiado a TextMeshProUGUI


    void Start()
    {
        // Asigna listeners (si alguno es null, se ignora)
        if (btnUnirseRapida != null) btnUnirseRapida.onClick.AddListener(() => LoadSceneSafe("Matchmaking"));
        if (btnCrearEquipo != null) btnCrearEquipo.onClick.AddListener(() => LoadSceneSafe("CreateTeam"));
        if (btnAmistades != null) btnAmistades.onClick.AddListener(() => LoadSceneSafe("Friends"));
        if (btnRanking != null) btnRanking.onClick.AddListener(() => LoadSceneSafe("GlobalRanking"));
        if (btnAjustes != null) btnAjustes.onClick.AddListener(() => LoadSceneSafe("Settings"));
        if (btnSalir != null) btnSalir.onClick.AddListener(OnSalirClicked);

        if (SessionManager.Instance != null && txtBienvenida != null)
        {
            string nombre = SessionManager.Instance.playerNickname;
            txtBienvenida.text = $"Bienvenido, {nombre}";
        }
    }

    void LoadSceneSafe(string sceneName)
    {
        // Comprueba que la escena esté en Build Settings
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"MenuUI: La escena '{sceneName}' no está incluida en Build Settings.");
        }
    }

    void OnSalirClicked()
    {
        #if UNITY_EDITOR
                // En editor volvemos a Login para pruebas
                SceneManager.LoadScene("Login");
        #else
                // En build salimos de la aplicación
                Application.Quit();
        #endif
    }
}