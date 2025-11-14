using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

[System.Serializable]
public class SlotRef
{
    public GameObject root;
    public string currentNick = "";
    [HideInInspector] public Text uiText;
#if TMP_PRESENT
    [HideInInspector] public TextMeshProUGUI tmpText;
#endif
    [HideInInspector] public Button btnAction;
}

public class CreateTeamAutoWire : MonoBehaviour
{
    [Header("Hierarchy")]
    [Tooltip("Drag the TeamGrid GameObject (parent that contains the 4 slot panels).")]
    public Transform teamGrid; // set in inspector

    [Header("Buttons")]
    public Button btnSearchEnemies;
    public Button btnCancel;

    [Header("Settings")]
    public string matchmakingSceneName = "Matchmaking";

    public List<SlotRef> slots = new List<SlotRef>();

    void Awake()
    {
        if (teamGrid == null)
        {
            Debug.LogError("[CreateTeamAutoWire] teamGrid not assigned. Please assign the TeamGrid transform.");
            return;
        }

        AutoWireSlots();
        BindTopButtons();
        UpdateAllSlotsUI();
        UpdateSearchButton();
    }

    void AutoWireSlots()
    {
        slots.Clear();
        // iterate children of teamGrid and try to find text & button inside each child
        foreach (Transform child in teamGrid)
        {
            var rootGO = child.gameObject;
            var slot = new SlotRef { root = rootGO, currentNick = "" };

            // Try find Button in this child (or deeper)
            Button b = rootGO.GetComponentInChildren<Button>(true);
            if (b != null) slot.btnAction = b;

            // Try find Text (UI) or TMP
            Text t = rootGO.GetComponentInChildren<Text>(true);
#if TMP_PRESENT
            TextMeshProUGUI tt = rootGO.GetComponentInChildren<TextMeshProUGUI>(true);
#else
            TextMeshProUGUI tt = null;
#endif

            slot.uiText = t;
#if TMP_PRESENT
            slot.tmpText = tt;
#endif

            // only accept slots that have at least a button and a text (or TMP)
            if (slot.btnAction == null)
            {
                Debug.LogWarning($"[CreateTeamAutoWire] Slot '{rootGO.name}' has no Button child. Skipping.");
                continue;
            }
            if (slot.uiText == null
#if TMP_PRESENT
                && slot.tmpText == null
#endif
                )
            {
                Debug.LogWarning($"[CreateTeamAutoWire] Slot '{rootGO.name}' has no Text/TMP child. Skipping.");
                continue;
            }

            // add and wire listener
            slots.Add(slot);
        }

        // Debug
        Debug.Log($"[CreateTeamAutoWire] AutoWired {slots.Count} slots from TeamGrid.");
        // add listeners (capture index safely)
        for (int i = 0; i < slots.Count; i++)
        {
            int idx = i;
            slots[idx].btnAction.onClick.RemoveAllListeners();
            slots[idx].btnAction.onClick.AddListener(() => OnSlotActionClicked(idx));
        }
    }

    void BindTopButtons()
    {
        if (btnSearchEnemies != null)
        {
            btnSearchEnemies.onClick.RemoveAllListeners();
            btnSearchEnemies.onClick.AddListener(OnSearchEnemiesClicked);
        }
        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(OnCancelClicked);
        }
    }

    void UpdateAllSlotsUI()
    {
        for (int i = 0; i < slots.Count; i++) UpdateSlotUI(i);
    }

    void UpdateSlotUI(int i)
    {
        var s = slots[i];
        string display = string.IsNullOrEmpty(s.currentNick) ? "VACÍO" : s.currentNick;

        // set text (TMP preferred if present)
#if TMP_PRESENT
        if (s.tmpText != null) s.tmpText.text = display;
        else if (s.uiText != null) s.uiText.text = display;
#else
        if (s.uiText != null) s.uiText.text = display;
#endif

        // set button label: try TMP inside button first or Text
        Text btnText = s.btnAction.GetComponentInChildren<Text>();
#if TMP_PRESENT
        TextMeshProUGUI btnTmpText = s.btnAction.GetComponentInChildren<TextMeshProUGUI>();
        if (btnTmpText != null) btnTmpText.text = string.IsNullOrEmpty(s.currentNick) ? "INVITAR AMIGO" : "EXPULSAR";
        else if (btnText != null) btnText.text = string.IsNullOrEmpty(s.currentNick) ? "INVITAR AMIGO" : "EXPULSAR";
#else
        if (btnText != null) btnText.text = string.IsNullOrEmpty(s.currentNick) ? "INVITAR AMIGO" : "EXPULSAR";
#endif
    }

    void OnSlotActionClicked(int index)
    {
        Debug.Log($"[CreateTeamAutoWire] Slot action clicked: {index}");
        if (index < 0 || index >= slots.Count) return;

        var s = slots[index];
        if (string.IsNullOrEmpty(s.currentNick))
        {
            // simulate invite — generate placeholder unique
            s.currentNick = "Jugador_" + Random.Range(100, 999);
        }
        else
        {
            // expel
            s.currentNick = "";
        }

        UpdateSlotUI(index);
        UpdateSearchButton();
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
            Debug.LogError($"[CreateTeamAutoWire] Scene '{matchmakingSceneName}' not in Build Settings.");
    }

    void OnCancelClicked()
    {
        SceneManager.LoadScene("Menu");
    }
}
