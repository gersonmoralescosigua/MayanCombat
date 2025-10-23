using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class TeamSlotUI
{
    public TextMeshProUGUI txtNickname;      // muestra nickname o "VACÍO"
    public Button btnAction;      // INVITAR / EXPULSAR
    public string currentNick = ""; // estado local
}

public class CreateTeamUI : MonoBehaviour
{
    [Header("Slots")]
    public List<TeamSlotUI> slots = new List<TeamSlotUI>(); // tamaño 4

    [Header("Acciones")]
    public Button btnSearchEnemies;
    public Button btnCancel;

    [Header("Settings")]
    public string matchmakingSceneName = "Matchmaking";

    void Start()
    {
        // Bind acciones
        for (int i = 0; i < slots.Count; i++)
        {
            int idx = i;
            if (slots[idx].btnAction != null)
            {
                slots[idx].btnAction.onClick.AddListener(() => OnSlotActionClicked(idx));
            }
            UpdateSlotUI(idx);
        }

        if (btnSearchEnemies != null) btnSearchEnemies.onClick.AddListener(OnSearchEnemiesClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);

        UpdateSearchButton();
    }

    void OnSlotActionClicked(int index)
    {
        // Si slot vacío -> INVITAR (simulamos agregando un placeholder nickname)
        if (string.IsNullOrEmpty(slots[index].currentNick))
        {
            // Simulación: abrir ventana para escribir nickname o auto-fill
            // Aquí hacemos auto-fill placeholder con ejemplo único
            string placeholder = "Jugador_" + Random.Range(100, 999);
            AddPlayerToSlot(index, placeholder);
        }
        else // ya ocupado -> expulsar
        {
            RemovePlayerFromSlot(index);
        }
    }

    void AddPlayerToSlot(int index, string nickname)
    {
        slots[index].currentNick = nickname;
        UpdateSlotUI(index);
        UpdateSearchButton();
    }

    void RemovePlayerFromSlot(int index)
    {
        slots[index].currentNick = "";
        UpdateSlotUI(index);
        UpdateSearchButton();
    }

    void UpdateSlotUI(int index)
    {
        var s = slots[index];
        if (s.txtNickname != null)
            s.txtNickname.text = string.IsNullOrEmpty(s.currentNick) ? "VACÍO" : s.currentNick;
        if (s.btnAction != null)
        {
            var txt = s.btnAction.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = string.IsNullOrEmpty(s.currentNick) ? "INVITAR AMIGO" : "EXPULSAR";
        }
    }

    void UpdateSearchButton()
    {
        bool allFilled = true;
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.currentNick)) { allFilled = false; break; }
        }
        if (btnSearchEnemies != null) btnSearchEnemies.interactable = allFilled;
    }

    void OnSearchEnemiesClicked()
    {
        if (Application.CanStreamedLevelBeLoaded(matchmakingSceneName))
            SceneManager.LoadScene(matchmakingSceneName);
        else
            Debug.LogError("CreateTeamUI: Matchmaking no agregado en Build Settings.");
    }

    void OnCancelClicked()
    {
        SceneManager.LoadScene("Menu");
    }

    // Helpers para debug / tests
    [ContextMenu("FillAllSlotsSample")]
    public void FillAllSlotsSample()
    {
        for (int i = 0; i < slots.Count; i++)
            AddPlayerToSlot(i, "Jugador" + (i + 1));
    }
}
