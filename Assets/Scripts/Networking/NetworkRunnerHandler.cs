using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance;
    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    [Header("Scenes")]
    public string matchmakingSceneName = "Matchmaking";
    public string loadingSceneName = "LoadingAssignment";
    public string mapSceneName = "Map_Tikal_Base";
    public string menuSceneName = "Menu";

    [Header("Matchmaking")]
    public int maxPlayers = 2;
    // ARRASTRA AQUÍ EL PREFAB QUE CREASTE EN EL PASO 2
    public NetworkObject playerDataPrefab; 

    private bool _joining = false;
    private Coroutine _autoStartTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartMatchmaking()
    {
        Debug.Log("🔗 Conectando al Lobby...");
        _joining = true;
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public async void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!_joining) return;

        Debug.Log($"📋 Sesiones encontradas: {sessionList.Count}");
        SessionInfo availableSession = null;
        
        foreach (var session in sessionList)
        {
            if (session.PlayerCount < maxPlayers && session.IsOpen)
            {
                availableSession = session;
                break;
            }
        }

        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
        _joining = false;

        if (availableSession != null)
        {
            Debug.Log($"✅ Uniéndose a sala: {availableSession.Name}");
            await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = availableSession.Name,
                SceneManager = sceneManager,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex)
            });
        }
        else
        {
            Debug.Log("⚠️ Creando NUEVA sala Host...");
            await runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = System.Guid.NewGuid().ToString(),
                PlayerCount = maxPlayers,
                SceneManager = sceneManager,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex)
            });
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");

        if (runner.IsServer)
        {
            // 1. SPAWN DE LA FICHA DE JUGADOR (Esto arregla la comunicación)
            // Creamos el objeto Data para este jugador y le damos InputAuthority
            if (playerDataPrefab != null)
            {
                var playerObj = runner.Spawn(playerDataPrefab, Vector3.zero, Quaternion.identity, player);
                runner.SetPlayerObject(player, playerObj);
                Debug.Log($"📄 Ficha de datos creada para {player}");
            }
            else
            {
                Debug.LogError("❌ ¡FALTA ASIGNAR EL PREFAB PLAYERDATA EN EL INSPECTOR!");
            }

            // 2. Verificar si estamos listos para asignar equipos
            int connected = runner.ActivePlayers.Count();
            if (connected == maxPlayers)
            {
                StartCoroutine(AssignTeamsRoutine());
            }
        }
    }

    // Usamos corrutina para dar un micro-segundo a que los objetos terminen de spawnear
    IEnumerator AssignTeamsRoutine()
    {
        yield return new WaitForSeconds(0.5f); 
        AssignTeams();
        yield return new WaitForSeconds(1.0f);
        
        // Carga de escena
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity");
        if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
    }

    private void AssignTeams()
    {
        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2) return;
        
        // Mezclar aleatoriamente
        players = players.OrderBy(x => UnityEngine.Random.value).ToList();

        // Asignar datos DIRECTAMENTE en los objetos de red (Sin RPCs)
        SetPlayerData(players[0], 0, 0); // 0 = Maya / Ixquic
        SetPlayerData(players[1], 1, 1); // 1 = Español / Beatriz

        Debug.Log($"✅ Equipos asignados y sincronizados vía NetworkVariables");
    }

    private void SetPlayerData(PlayerRef player, int team, int charId)
    {
        if (_runner.TryGetPlayerObject(player, out var obj))
        {
            var data = obj.GetComponent<PlayerDataNetworked>();
            if (data != null)
            {
                data.CharacterID = charId; // Seteamos primero
                data.TeamID = team;        // Seteamos Team al final para disparar el OnChanged
            }
        }
        else
        {
            Debug.LogError($"❌ No se encontró objeto para player {player}");
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == loadingSceneName && runner.IsServer)
        {
            if (_autoStartTimer != null) StopCoroutine(_autoStartTimer);
            _autoStartTimer = StartCoroutine(AutoStartAfterDelay(10f));
        }

        if (currentScene == mapSceneName && runner.IsServer)
        {
            foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
        }
    }

    private IEnumerator AutoStartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/Tikal/{mapSceneName}.unity");
        if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        // Leemos los datos desde el objeto de red seguro
        int team = 0;
        int charId = 0;

        if (runner.TryGetPlayerObject(player, out var obj))
        {
            var data = obj.GetComponent<PlayerDataNetworked>();
            team = data.TeamID;
            charId = data.CharacterID;
        }

        // Lógica invertida corregida: 0=Ixquic, 1=Beatriz
        string prefabPath = $"Prefabs/Characters/pf_{(charId == 0 ? "ixquic" : "beatriz")}";
        var prefab = Resources.Load<NetworkObject>(prefabPath);

        Vector3 spawnPos = team == 0 ? new Vector3(-0.39f, -0.382f, 0f) : new Vector3(1.3f, -0.4f, 0f);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
    }

    // --- Callbacks vacíos ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        var data = new NetworkInputData();
        data.Move.x = 0f;
        if (Input.GetKey(KeyCode.A)) data.Move.x -= 1f;
        if (Input.GetKey(KeyCode.D)) data.Move.x += 1f;
        data.JumpPressed = Input.GetKey(KeyCode.W);
        data.AttackPressed = Input.GetKey(KeyCode.J);
        input.Set(data);
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}