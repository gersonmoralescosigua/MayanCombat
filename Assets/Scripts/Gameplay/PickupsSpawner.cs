// Assets/Scripts/Gameplay/PickupsSpawner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PickupsSpawner : NetworkBehaviour
{
    public GameObject[] pickupPrefabs;      // asignar desde inspector (prefabs deben estar en Resources y en NetworkProjectConfig)
    public Transform[] spawnPoints;
    public float spawnMin = 3f;
    public float spawnMax = 7f;
    public float overlapRadius = 1.5f;
    public int maxSimultaneousPickups = 3;
    public float autoDestroyTime = 12f;

    private List<NetworkObject> active = new List<NetworkObject>();
    private Dictionary<Transform, bool> occupied = new Dictionary<Transform, bool>();

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            foreach (var pt in spawnPoints) occupied[pt] = false;
            StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnMin, spawnMax));

            Cleanup();
            if (!Runner.IsServer) yield break;
            if (active.Count >= maxSimultaneousPickups) continue;

            TrySpawnOne();
        }
    }

    void TrySpawnOne()
    {
        if (!Runner.IsServer) return;

        List<Transform> available = new List<Transform>();

        foreach (var pt in spawnPoints)
        {
            if (occupied.ContainsKey(pt) && occupied[pt]) continue;
            Collider2D[] hits = Physics2D.OverlapCircleAll(pt.position, overlapRadius);
            bool hasPickup = false;
            foreach (var h in hits) if (h.CompareTag("Pickup")) { hasPickup = true; break; }
            if (!hasPickup) available.Add(pt);
        }

        if (available.Count == 0) return;

        var chosen = available[Random.Range(0, available.Count)];
        var prefab = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
        var obj = Runner.Spawn(prefab, chosen.position, Quaternion.identity, null);

        if (obj != null)
        {
            active.Add(obj);
            occupied[chosen] = true;
            StartCoroutine(AutoDestroy(obj, chosen));
        }
    }

    IEnumerator AutoDestroy(NetworkObject obj, Transform pt)
    {
        yield return new WaitForSeconds(autoDestroyTime);
        if (obj != null)
        {
            active.Remove(obj);
            if (Runner != null) Runner.Despawn(obj);
        }
        if (pt != null && occupied.ContainsKey(pt)) occupied[pt] = false;
    }

    public void OnPickupCollected(NetworkObject obj, Transform usedPoint)
    {
        if (active.Contains(obj)) active.Remove(obj);
        if (Runner != null && obj != null) Runner.Despawn(obj);
        if (usedPoint != null && occupied.ContainsKey(usedPoint)) occupied[usedPoint] = false;
    }

    void Cleanup()
    {
        active.RemoveAll(x => x == null || !x.gameObject.activeInHierarchy);
    }
}