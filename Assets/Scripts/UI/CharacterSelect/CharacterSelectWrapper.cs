using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class CharacterSelectWrapper : MonoBehaviour
{
    [Header("UI para cada Team")]
    public GameObject panelMaya;     // Team 0
    public GameObject panelEspanol;  // Team 1

    [Header("Botones")]
    public Button btnSelectMaya;
    public Button btnSelectEspanol;
    private int myTeam;

    void Start()
    {
        panelMaya.SetActive(false);
        panelEspanol.SetActive(false);
        Debug.Log("[CharacterSelectWrapper] Iniciando selección de personaje...");

        if (SessionManager.Instance == null)
        {
            Debug.LogWarning("[CharacterSelectWrapper] ⚠️ SessionManager no existe. Activando ambos paneles temporalmente para debug.");
            panelMaya.SetActive(true);
            panelEspanol.SetActive(true);
            return;
        }

        myTeam = SessionManager.Instance.currentTeam;
        Debug.Log($"[CharacterSelectWrapper] Mi equipo: {myTeam}");

        if (myTeam == -1)
        {
            Debug.LogWarning("[CharacterSelectWrapper] ⚠️ No se asignó equipo. Mostrando ambos paneles.");
            panelMaya.SetActive(true);
            panelEspanol.SetActive(true);
        }
        else
        {
            panelMaya.SetActive(myTeam == 0);
            panelEspanol.SetActive(myTeam == 1);
        }

        btnSelectMaya?.onClick.AddListener(() => SelectCharacter(0));
        btnSelectEspanol?.onClick.AddListener(() => SelectCharacter(1));
    }

    private void SelectCharacter(int charId)
    {
        Debug.Log($"[CharacterSelectWrapper] Elegiste personaje {charId}");
        var runner = NetworkRunnerHandler.Instance.Runner;
        var localPlayer = runner.LocalPlayer;
        // Guardar selección
        NetworkRunnerHandler.Instance.SetPlayerCharacter(localPlayer, charId);
        // Desactivar botones para evitar doble selección
        btnSelectMaya.interactable = false;
        btnSelectEspanol.interactable = false;
        // Intentar iniciar partida
        NetworkRunnerHandler.Instance.TryStartGame();
    }
}