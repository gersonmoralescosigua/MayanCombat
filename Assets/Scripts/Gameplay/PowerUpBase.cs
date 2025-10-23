using UnityEngine;

public abstract class PowerUpBase : MonoBehaviour
{
    public float duration = 5f;          // duración por defecto
    public Sprite icon;                  // icono para HUD
    public string powerUpName = "PowerUp";

    // Cuando el jugador entra en el trigger
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            var player = col.GetComponent<PlayerController>(); // tu script de jugador
            if (player != null)
            {
                ApplyTo(player);
                // Destruye pickup o desactiva para respawn
                Destroy(gameObject);
            }
        }
    }

    // Implementar en cada derivado
    public abstract void ApplyTo(PlayerController player);
}