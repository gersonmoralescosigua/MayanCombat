using UnityEngine;

/// <summary>
/// PowerUpBase ahora envía la acción al PlayerNetwork (cliente objetivo)
/// El Host (authority) es quien debe detectar colisión en pickups networked y llamar ApplyTo
/// </summary>
public abstract class PowerUpBase : MonoBehaviour
{
    public float duration = 5f;
    public Sprite icon;
    public string powerUpName = "PowerUp";

    // Nota: este OnTrigger debe ejecutarse en la instancia que tiene autoridad sobre el pickup.
    // Si los pickups son instanciados por el host/fusion runner, el host manejará esto.
    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        var pn = col.GetComponent<PlayerNetwork>();
        if (pn != null)
        {
            ApplyTo(pn);
            Destroy(gameObject);
        }
    }

    public abstract void ApplyTo(PlayerNetwork player);
}