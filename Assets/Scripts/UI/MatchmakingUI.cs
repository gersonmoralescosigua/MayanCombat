using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;

public class MatchmakingUI : MonoBehaviour
{
    public TMP_Text txtStatus;
    public Button btnCancel;

    void Start()
    {
        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnCancelClicked);

        StartCoroutine(StartFusionConnection());
    }

    IEnumerator StartFusionConnection()
    {
        yield return new WaitForSeconds(0.5f);
        txtStatus?.SetText("🔗 Conectando con Photon Fusion...");

        if (NetworkRunnerHandler.Instance == null)
        {
            var go = new GameObject("NetworkRunnerHandler");
            go.AddComponent<NetworkRunnerHandler>();
        }

        NetworkRunnerHandler.Instance.StartMatchmaking();

        yield return new WaitForSeconds(2f);
        txtStatus?.SetText("⌛ Esperando jugadores...");
    }

    void OnCancelClicked()
    {
        Debug.Log("❌ Cancelando matchmaking...");
        NetworkRunnerHandler.Instance?.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
