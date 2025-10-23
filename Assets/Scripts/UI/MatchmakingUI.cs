using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MatchmakingUI : MonoBehaviour
{
    [Header("UI Elements (TMP)")]
    public TMP_Text txtPlayersAssigned;
    public Button btnCancel;

    [Header("Settings")]
    public int requiredPlayers = 2;
    public float simulateIntervalSeconds = 1.0f;
    public string mapSceneToLoad = "Map_Tikal_Base";

    int currentAssigned = 0;
    Coroutine simCoroutine;

    void Start()
    {
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);
        UpdatePlayersText();
        simCoroutine = StartCoroutine(SimulatePlayerJoin());
    }

    IEnumerator SimulatePlayerJoin()
    {
        yield return new WaitForSeconds(0.5f);
        while (currentAssigned < requiredPlayers)
        {
            currentAssigned++;
            UpdatePlayersText();

            if (currentAssigned >= requiredPlayers)
            {
                yield return new WaitForSeconds(0.5f);
                LoadMapIfAvailable();
                yield break;
            }

            yield return new WaitForSeconds(simulateIntervalSeconds);
        }
    }

    void UpdatePlayersText()
    {
        if (txtPlayersAssigned != null)
            txtPlayersAssigned.text = $"Jugadores asignados: {currentAssigned} / {requiredPlayers}";
    }

    public void OnCancelClicked()
    {
        if (simCoroutine != null) StopCoroutine(simCoroutine);
        simCoroutine = null;

        if (Application.CanStreamedLevelBeLoaded("Menu"))
            SceneManager.LoadScene("Menu");
        else
            Debug.LogError("MatchmakingUI_TMP: 'Menu' no está en Build Settings.");
    }

    void LoadMapIfAvailable()
    {
        if (Application.CanStreamedLevelBeLoaded(mapSceneToLoad))
            SceneManager.LoadScene(mapSceneToLoad);
        else
            Debug.LogError($"MatchmakingUI_TMP: La escena '{mapSceneToLoad}' no está en Build Settings.");
    }

    [ContextMenu("ResetSimulation")]
    public void ResetSimulation()
    {
        currentAssigned = 0;
        UpdatePlayersText();
        if (simCoroutine != null) StopCoroutine(simCoroutine);
        simCoroutine = StartCoroutine(SimulatePlayerJoin());
    }
}
