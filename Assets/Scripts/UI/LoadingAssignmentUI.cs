using UnityEngine;
using TMPro;
using System.Collections; // Necesario para corrutinas

public class LoadingAssignmentUI : MonoBehaviour
{
    public TMP_Text infoText;

    void Start()
    {
        infoText.text = "⏳ Asignando equipo sagrado...";
        StartCoroutine(WaitForTeamAssignment());
    }

    IEnumerator WaitForTeamAssignment()
    {
        // Esperamos hasta que SessionManager tenga un equipo válido (0 o 1)
        // -1 es el valor por defecto que pusimos en SessionManager
        while (SessionManager.Instance == null || SessionManager.Instance.currentTeam == -1)
        {
            yield return null; // Esperar al siguiente frame
        }

        // ¡Datos listos!
        int team = SessionManager.Instance.currentTeam;
        // Corrección de textos según tu lógica: 0=Maya(Ixquic), 1=Español(Beatriz)
        string role = team == 0 ? "Maya (Ixquic)" : "Español (Beatriz)";
        
        infoText.text = $"Eres {role}\nLa partida comenzará en breve...";
    }
}