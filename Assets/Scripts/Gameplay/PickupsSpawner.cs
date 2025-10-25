using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupsSpawner : MonoBehaviour
{
    [Header("Prefabs de pickups")]
    public GameObject[] pickupPrefabs;

    [Header("Puntos de spawn")]
    public Transform[] spawnPoints;

    [Header("Control de spawns")]
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 7f;
    public int maxSimultaneousPickups = 3;
    public int initialSpawnCount = 2;
    public bool randomizeWithinSpawnPoint = false;

    [Header("Control de superposición y tiempo")]
    [Tooltip("Radio para verificar si ya hay un pickup cerca")]
    public float overlapCheckRadius = 1.5f;
    [Tooltip("Tiempo en segundos antes de que un pickup desaparezca automáticamente")]
    public float autoDestroyTime = 10f;

    // Track active pickups
    private readonly List<GameObject> activePickups = new List<GameObject>();
    // Track spawn points ocupados temporalmente
    private readonly Dictionary<Transform, bool> spawnPointOccupied = new Dictionary<Transform, bool>();

    void Start()
    {
        if (pickupPrefabs == null || pickupPrefabs.Length == 0)
        {
            Debug.LogWarning("[PickupsSpawner] No hay pickupPrefabs asignados.");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[PickupsSpawner] No hay spawnPoints asignados.");
            return;
        }

        // Inicializar diccionario de spawn points
        foreach (Transform point in spawnPoints)
        {
            spawnPointOccupied[point] = false;
        }

        // Spawn inicial
        int spawnInicial = Mathf.Clamp(initialSpawnCount, 0, maxSimultaneousPickups);
        for (int i = 0; i < spawnInicial; i++)
            TrySpawnOne();

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(wait);

            CleanupList();
            if (activePickups.Count >= maxSimultaneousPickups) continue;

            TrySpawnOne();
        }
    }

    void TrySpawnOne()
    {
        // Buscar spawn point disponible (no ocupado y sin pickups cerca)
        List<Transform> availablePoints = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            // Verificar si el spawn point está marcado como ocupado
            if (spawnPointOccupied.ContainsKey(point) && spawnPointOccupied[point])
                continue;

            // Verificar si hay pickups cerca físicamente
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(point.position, overlapCheckRadius);
            bool hasPickupNearby = false;
            foreach (Collider2D col in overlaps)
            {
                if (col.CompareTag("Pickup") && col.gameObject.activeInHierarchy)
                {
                    hasPickupNearby = true;
                    break;
                }
            }

            if (!hasPickupNearby)
                availablePoints.Add(point);
        }

        if (availablePoints.Count == 0)
        {
            // Debug.Log("No hay spawn points disponibles");
            return;
        }

        // Elegir spawn point aleatorio de los disponibles
        Transform spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
        GameObject prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];

        Vector3 pos = spawnPoint.position;
        if (randomizeWithinSpawnPoint)
        {
            Vector3 ext = spawnPoint.localScale * 0.5f;
            pos.x += Random.Range(-ext.x, ext.x);
            pos.y += Random.Range(-ext.y, ext.y);
        }

        GameObject inst = Instantiate(prefab, pos, Quaternion.identity, transform);
        activePickups.Add(inst);

        // ✅ Marcar spawn point como ocupado temporalmente
        spawnPointOccupied[spawnPoint] = true;

        // ✅ Auto-destrucción después de tiempo
        StartCoroutine(AutoDestroyPickup(inst, spawnPoint, autoDestroyTime));
    }

    IEnumerator AutoDestroyPickup(GameObject pickup, Transform usedSpawnPoint, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pickup != null && pickup.activeInHierarchy)
        {
            activePickups.Remove(pickup);

            // ✅ Liberar el spawn point
            if (spawnPointOccupied.ContainsKey(usedSpawnPoint))
                spawnPointOccupied[usedSpawnPoint] = false;

            Destroy(pickup);
        }
        else if (pickup == null)
        {
            // ✅ Si el pickup fue destruido/recogido, liberar spawn point
            if (spawnPointOccupied.ContainsKey(usedSpawnPoint))
                spawnPointOccupied[usedSpawnPoint] = false;
        }
    }

    // ✅ Nuevo método para cuando un pickup es recogido (llamado desde Pickup.cs)
    public void OnPickupCollected(GameObject pickup, Transform spawnPointUsed)
    {
        activePickups.Remove(pickup);

        if (spawnPointUsed != null && spawnPointOccupied.ContainsKey(spawnPointUsed))
            spawnPointOccupied[spawnPointUsed] = false;
    }

    void CleanupList()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            if (activePickups[i] == null)
                activePickups.RemoveAt(i);
            else if (!activePickups[i].activeInHierarchy)
                activePickups.RemoveAt(i);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.yellow;
        foreach (Transform t in spawnPoints)
        {
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, 0.25f);

            // ✅ Dibujar radio de verificación de superposición
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(t.position, overlapCheckRadius);
            Gizmos.color = Color.yellow;
        }
    }
}
