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
        // Obtener team asignado en el login/session
        myTeam = SessionManager.Instance.currentTeam;

        panelMaya.SetActive(myTeam == 0);
        panelEspanol.SetActive(myTeam == 1);

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