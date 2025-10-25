using UnityEngine;

public class Pickup : MonoBehaviour
{
    public PickupType type; // Tipo de pickup: Maize, Jade, Cacao, Jaguar, Lava, Serpiente

    // Opcional: tiempo para reaparecer si quieres spawn repetido
    public float respawnTime = 0f;

    // ✅ Referencia al spawn point que usó este pickup
    [System.NonSerialized] public Transform spawnPointUsed;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.CollectPickup(type);

                // ✅ Notificar al spawner que fue recogido
                PickupsSpawner spawner = FindObjectOfType<PickupsSpawner>();
                if (spawner != null)
                    spawner.OnPickupCollected(gameObject, spawnPointUsed);

                gameObject.SetActive(false);

                // Si quieres que reaparezca automáticamente
                if (respawnTime > 0f)
                {
                    Invoke(nameof(Respawn), respawnTime);
                }
            }
        }
    }

    void Respawn()
    {
        gameObject.SetActive(true);
    }
}