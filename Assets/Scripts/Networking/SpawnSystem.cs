using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class SpawnSystem : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefabs (asignar por Inspector)")]
    public GameObject pfBeatriz;
    public GameObject pfIxquic;

    [Header("Spawn points (asignar por Inspector)")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // 🔹 Firmas actualizadas de Fusion 2
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        foreach (var p in runner.ActivePlayers)
        {
            Transform chosen = (p.RawEncoded % 2 == 0) ? spawnPoint1 : spawnPoint2;

            int charId = PlayerPrefs.GetInt("AssignedCharacter", -1);
            GameObject prefab = charId == 0 ? pfBeatriz : pfIxquic;

            if (prefab == null)
            {
                Debug.LogError("SpawnSystem: prefab no asignado.");
                continue;
            }

            runner.Spawn(prefab, chosen.position, chosen.rotation, p);
            Debug.Log($"SpawnSystem → Spawned {prefab.name} for player {p}");
        }
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
}