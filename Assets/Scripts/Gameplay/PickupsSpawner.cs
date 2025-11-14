using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Collections;

public class PickupsSpawner : NetworkBehaviour
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
    public bool randomizeWithinSpawnPoint = false;

    [Header("Control de superposición y tiempo")]
    public float overlapCheckRadius = 1.5f;
    public float autoDestroyTime = 10f;

    private readonly List<NetworkObject> activePickups = new List<NetworkObject>();
    private readonly Dictionary<Transform, bool> spawnPointOccupied = new Dictionary<Transform, bool>();


    public override void Spawned()
    {
        runner = Runner;

        if (!runner.IsServer)
        {
            // ❌ IMPORTANTE: Los clientes NO hacen nada
            return;
        }

        if (pickupPrefabs.Length == 0)
        {
            Debug.LogWarning("[PickupsSpawner] No hay pickupPrefabs asignados.");
            return;
        }

        foreach (Transform point in spawnPoints)
            spawnPointOccupied[point] = false;

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

            if (activePickups.Count >= maxSimultaneousPickups)
                continue;

            TrySpawnOne();
        }
    }


    void TrySpawnOne()
    {
        // ❌ Clientes JAMÁS deben entrar a esta función
        if (!runner.IsServer) return;

        List<Transform> availablePoints = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            if (spawnPointOccupied[point]) continue;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(point.position, overlapCheckRadius);
            bool hasNearbyPickup = false;

            foreach (Collider2D col in overlaps)
            {
                if (col.CompareTag("Pickup"))
                {
                    hasNearbyPickup = true;
                    break;
                }
            }

            if (!hasNearbyPickup)
                availablePoints.Add(point);
        }

        if (availablePoints.Count == 0) return;

        Transform spawnPoint = availablePoints[Random.Range(0, availablePoints.Count)];
        GameObject prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];

        Vector3 pos = spawnPoint.position;

        NetworkObject obj = runner.Spawn(prefab, pos, Quaternion.identity);
        activePickups.Add(obj);

        spawnPointOccupied[spawnPoint] = true;

        StartCoroutine(AutoDestroyPickup(obj, spawnPoint));
    }


    IEnumerator AutoDestroyPickup(NetworkObject obj, Transform spawnPoint)
    {
        // ❌ Clientes NO destruyen pickups
        if (!runner.IsServer) yield break;

        yield return new WaitForSeconds(autoDestroyTime);

        if (obj != null)
        {
            activePickups.Remove(obj);
            runner.Despawn(obj);
        }

        spawnPointOccupied[spawnPoint] = false;
    }


    // Llamado desde Pickup.cs cuando un jugador lo recoge
    public void OnPickupCollected(NetworkObject obj, Transform spawnPointUsed)
    {
        if (!runner.IsServer) return;

        if (activePickups.Contains(obj))
            activePickups.Remove(obj);

        if (obj != null)
            runner.Despawn(obj);

        if (spawnPointUsed != null && spawnPointOccupied.ContainsKey(spawnPointUsed))
            spawnPointOccupied[spawnPointUsed] = false;
    }


    void CleanupList()
    {
        activePickups.RemoveAll(o => o == null || !o.gameObject.activeInHierarchy);
    }
}