using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class CharacterSelectWrapper : MonoBehaviour
{
    [Header("UI para cada Team")]
    public GameObject panelMaya;       // Team 0
    public GameObject panelEspanol;    // Team 1

    [Header("Botones")]
    public Button btnSelectMaya;
    public Button btnSelectEspanol;

    private int myTeam;
    private MatchController match;

    void Start()
    {
        // Obtener el MatchController instanciado por Fusion
        match = FindFirstObjectByType<MatchController>();

        // Obtener el team asignado en SessionManager (0 = Maya, 1 = Español)
        myTeam = SessionManager.Instance.currentTeam;

        // Activar solo el panel del equipo correspondiente
        panelMaya.SetActive(myTeam == 0);
        panelEspanol.SetActive(myTeam == 1);

        // Asignar callbacks
        if (btnSelectMaya != null)
            btnSelectMaya.onClick.AddListener(() => SelectCharacter(0));

        if (btnSelectEspanol != null)
            btnSelectEspanol.onClick.AddListener(() => SelectCharacter(1));
    }

    private void SelectCharacter(int charId)
    {
        Debug.Log($"[CharacterSelectWrapper] Seleccionaste personaje ID {charId}");

        // Enviar selección al host
        match.RPC_PlayerSelected(charId);

        // Desactivar botones después de elegir
        if (btnSelectMaya != null) btnSelectMaya.interactable = false;
        if (btnSelectEspanol != null) btnSelectEspanol.interactable = false;
    }
}