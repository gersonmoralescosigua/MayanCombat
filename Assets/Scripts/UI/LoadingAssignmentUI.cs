using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingAssignmentUI : MonoBehaviour
{
    public TMP_Text infoText;

    void Start()
    {
        int team = SessionManager.Instance != null ? SessionManager.Instance.currentTeam : 0;
        string role = team == 0 ? "Español (Beatriz)" : "Maya (Ixquic)";
        infoText.text = $"Eres {role}\nLa partida comenzará en 10 segundos...";
    }
}

