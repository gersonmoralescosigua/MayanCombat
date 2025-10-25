using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MatchmakingUI : MonoBehaviour
{
    public TMP_Text txtStatus;
    public Button btnCancel;

    private bool _isConnecting = false;

    void Start()
    {
        txtStatus.text = "🔍 Buscando partida...";
        btnCancel.onClick.AddListener(OnCancelClicked);

        Invoke(nameof(StartFusionMatchmaking), 0.3f);
    }

    async void StartFusionMatchmaking()
    {
        if (_isConnecting) return;
        _isConnecting = true;

        if (NetworkRunnerHandler.Instance == null)
        {
            var go = new GameObject("NetworkRunnerHandler");
            go.AddComponent<NetworkRunnerHandler>();
        }

        txtStatus.text = "🔗 Conectando con Photon Fusion...";
        NetworkRunnerHandler.Instance.StartMatchmaking();

        await System.Threading.Tasks.Task.Delay(3000);
        txtStatus.text = "Esperando jugadores...";

        _isConnecting = false;
    }

    void OnCancelClicked()
    {
        NetworkRunnerHandler.Instance?.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
