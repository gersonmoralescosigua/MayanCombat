using UnityEngine;

/// <summary>
/// Minimal HUDManager para evitar errores de compilación.
/// Amplía estos métodos para que actualicen realmente los elementos del HUD.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Métodos mínimos usados desde PlayerController
    public void UpdateJadeCount(int count)
    {
        Debug.Log($"[HUDManager] UpdateJadeCount -> {count}");
        // TODO: actualizar el prefab HUD: Icon_Jade / JadeCountText
    }

    public void ShowPowerupIcon(string id, float duration)
    {
        Debug.Log($"[HUDManager] ShowPowerupIcon {id} duration:{duration}");
        // TODO: mostrar icono en ActivePowerupsRow y temporizador
    }

    public void HidePowerupIcon(string id)
    {
        Debug.Log($"[HUDManager] HidePowerupIcon {id}");
        // TODO: ocultar icono cuando termine
    }

    // Auxiliares que puedes necesitar:
    public void UpdateAllPlayersJade(int value)
    {
        Debug.Log($"[HUDManager] UpdateAllPlayersJade -> {value}");
    }
}
