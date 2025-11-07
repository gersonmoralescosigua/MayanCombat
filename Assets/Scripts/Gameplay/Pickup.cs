using UnityEngine;
using Fusion;

public class Pickup : NetworkBehaviour
{
    public PickupType type;   // Tipo de pickup: Maize, Jade, Cacao, Jaguar, Lava, Serpiente

    // Referencia al spawn point asignado por el spawner
    [System.NonSerialized]
    public Transform spawnPointUsed;

    private PickupsSpawner spawner;

    private void Start()
    {
        // ✅ Obtiene el spawner una sola vez (sin FindObjectOfType repetido)
        spawner = FindFirstObjectByType<PickupsSpawner>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Object.HasStateAuthority)
            return;
        // Solo el HOST ejecuta lógica de despawn

        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        // ✅ Aplicar efecto al jugador
        player.CollectPickup(type);

        // ✅ Notificar al spawner
        if (spawner != null)
        {
            NetworkObject obj = GetComponent<NetworkObject>();
            spawner.OnPickupCollected(obj, spawnPointUsed);
        }
    }
}