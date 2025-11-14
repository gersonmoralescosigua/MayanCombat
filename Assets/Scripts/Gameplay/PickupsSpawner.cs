using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PickupsSpawner : MonoBehaviour
{
    private NetworkRunner runner;

    [Header("Prefabs de pickups")]
    public GameObject[] pickupPrefabs;

    [Header("Puntos de spawn")]
    public Transform[] spawnPoints;

    [Header("Control de spawns")]
    public float spawnIntervalMin = 3f;
    public float spawnIntervalMax = 7f;
    public int maxSimultaneousPickups = 3;
    public int initialSpawnCount = 2;

    [Header("Control de superposición y tiempo")]
    public float overlapCheckRadius = 1.5f;
    public float autoDestroyTime = 10f;

    private readonly List<NetworkObject> activePickups = new List<NetworkObject>();
    private readonly Dictionary<Transform, bool> spawnPointOccupied = new Dictionary<Transform, bool>();

    void Start()
    {
        runner = NetworkRunnerHandler.Instance?.Runner;
        if (runner == null)
        {
            Debug.LogError("[PickupsSpawner] Runner no encontrado.");
            return;
        }

        if (pickupPrefabs == null || pickupPrefabs.Length == 0)
        {
            Debug.LogWarning("[PickupsSpawner] No hay pickupPrefabs asignados.");
            return;
        }

        foreach (Transform pt in spawnPoints)
            spawnPointOccupied[pt] = false;

        int spawnInicial = Mathf.Clamp(initialSpawnCount, 0, maxSimultaneousPickups);
        for (int i = 0; i < spawnInicial; i++)
            TrySpawnOne();

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnIntervalMin, spawnIntervalMax));
            CleanupList();

            if (activePickups.Count >= maxSimultaneousPickups) continue;

            TrySpawnOne();
        }
    }

    void TrySpawnOne()
    {
        if (runner == null) return;
        if (!runner.IsServer) return; // importantísimo

        List<Transform> available = new List<Transform>();

        foreach (var pt in spawnPoints)
        {
            if (spawnPointOccupied[pt]) continue;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(pt.position, overlapCheckRadius);
            bool nearby = false;
            foreach (var c in overlaps)
            {
                if (c.CompareTag("Pickup"))
                {
                    nearby = true; break;
                }
            }

            if (!nearby) available.Add(pt);
        }

        if (available.Count == 0) return;

        Transform chosen = available[Random.Range(0, available.Count)];
        GameObject prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
        Vector3 pos = chosen.position;

        NetworkObject obj = runner.Spawn(prefab, pos, Quaternion.identity, null);
        if (obj != null)
        {
            activePickups.Add(obj);
            spawnPointOccupied[chosen] = true;
            StartCoroutine(AutoDestroyPickup(obj, chosen));
        }
    }

    IEnumerator AutoDestroyPickup(NetworkObject obj, Transform spawnPoint)
    {
        yield return new WaitForSeconds(autoDestroyTime);

        if (obj != null)
        {
            activePickups.Remove(obj);
            runner.Despawn(obj);
        }

        if (spawnPoint != null && spawnPointOccupied.ContainsKey(spawnPoint))
            spawnPointOccupied[spawnPoint] = false;
    }

    // llamado por Pickup cuando alguien lo recoge
    public void OnPickupCollected(NetworkObject obj, Transform spawnPoint)
    {
        if (activePickups.Contains(obj)) activePickups.Remove(obj);

        if (runner != null && obj != null) runner.Despawn(obj);

        if (spawnPoint != null && spawnPointOccupied.ContainsKey(spawnPoint))
            spawnPointOccupied[spawnPoint] = false;
    }

    void CleanupList()
    {
        activePickups.RemoveAll(x => x == null || !x.gameObject.activeInHierarchy);
    }
}
