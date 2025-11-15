// Assets/Scripts/Networking/NetworkRunnerHandler.cs
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

    [Header("Scenes (asegúrate en Build Settings)")]
    public string matchmakingSceneName = "Matchmaking";
    public string loadingSceneName = "LoadingAssignment";
    public string mapSceneName = "Map_Tikal_Base";
    public string menuSceneName = "Menu";

    [Header("Matchmaking")]
    public int maxPlayers = 2;

    private readonly Dictionary<PlayerRef, int> _playerTeams = new();
    private readonly Dictionary<PlayerRef, int> _playerCharacters = new();

    private Coroutine _autoStartTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void StartMatchmaking()
    {
        Debug.Log("🔗 Iniciando matchmaking...");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        const string sessionName = "MayanQuickMatch";

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = sessionName,
            SceneManager = sceneManager,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex)
        });

        if (!result.Ok)
        {
            Debug.LogError($"❌ Error al conectar: {result.ShutdownReason}");
            return;
        }

        Debug.Log("✅ Conectado a Fusion. Esperando jugador...");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");

        // Solo el server asigna equipos
        if (runner.IsServer)
        {
            int connected = runner.ActivePlayers.Count();
            Debug.Log($"🧩 Jugadores conectados: {connected}/{maxPlayers}");

            if (connected == maxPlayers)
            {
                AssignTeams();

                runner.LoadScene(SceneRef.FromIndex(
                    SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity")
                ));
            }
        }

        // Si ya estamos en la escena del juego, el server debe spawnear al jugador AHORA
        string currentScene = SceneManager.GetActiveScene().name;
        if (runner.IsServer && currentScene == mapSceneName)
        {
            SpawnPlayer(runner, player);
        }
    }

    private void AssignTeams()
    {
        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2) return;
        players = players.OrderBy(x => UnityEngine.Random.value).ToList();

        _playerTeams[players[0]] = 0;
        _playerTeams[players[1]] = 1;
        _playerCharacters[players[0]] = 0;
        _playerCharacters[players[1]] = 1;

        foreach (var p in players)
            RPC_AssignRole(p, _playerTeams[p], _playerCharacters[p]);

        Debug.Log($"✅ Equipos asignados: {players[0]}=Español, {players[1]}=Maya");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AssignRole(PlayerRef target, int team, int charId, RpcInfo info = default)
    {
        if (_runner.LocalPlayer == target)
        {
            SessionManager.Instance?.SetTeam(team);
            PlayerPrefs.SetInt("AssignedCharacter", charId);
            Debug.Log($"🎯 Eres {(team == 0 ? "Español" : "Maya")} - Personaje {charId}");
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"✅ Escena cargada: {currentScene}");

        if (runner.IsServer && currentScene == mapSceneName)
        {
            foreach (var p in runner.ActivePlayers)
            {
                if (runner.GetPlayerObject(p) == null)
                {
                    SpawnPlayer(runner, p);
                }
            }
        }
    }

    private bool PlayerAlreadyHasCharacter(NetworkRunner runner, PlayerRef player)
    {
        return runner.GetPlayerObject(player) != null;
    }

    private IEnumerator AutoStartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("⏰ Tiempo terminado. Iniciando partida...");

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/Tikal/{mapSceneName}.unity");
        if (sceneIndex >= 0)
            _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        else
            Debug.LogError($"❌ Escena {mapSceneName} no está en Build Settings.");
    }

    // ---- SpawnPlayer: solo servidor debe llamar a esto ----
    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int team = _playerTeams.ContainsKey(player) ? _playerTeams[player] : 0;
        int charId = _playerCharacters.ContainsKey(player) ? _playerCharacters[player] : 0;

        string prefabPath = $"Prefabs/Characters/pf_{(charId == 0 ? "beatriz" : "ixquic")}";
        var prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"❌ Prefab no encontrado: {prefabPath}");
            return;
        }

        Vector3 spawnPos = team == 0 ? new Vector3(-0.39f, -0.382f, 0f) : new Vector3(1.3f, -0.4f, 0f);

        try
        {
            // servidor spawnea y asigna input authority al player
            var spawned = runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
            if (spawned == null)
                Debug.LogError("❌ runner.Spawn devolvió null");
            else
                Debug.Log($"✅ Spawn {prefab.name} ({(team == 0 ? "Español" : "Maya")}) at {spawnPos}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Excepción al spawnear: {ex}");
        }
    }

    // ---- Callbacks vacíos requeridos por la interfaz ----
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🚪 Player left: {player}");
        _playerTeams.Remove(player);
        _playerCharacters.Remove(player);
    }
    public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("🌐 Connected to server");
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => Debug.LogWarning($"❌ Disconnected: {reason}");
    public void OnSceneLoadStart(NetworkRunner runner) => Debug.Log("📥 Fusion comenzó a cargar escena...");
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        // USAMOS WASD+J en todos los clientes (decisión A)
        data.Move.x = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
        data.Move.y = 0f;
        data.JumpPressed = Input.GetKey(KeyCode.W);
        data.AttackPressed = Input.GetKey(KeyCode.J);
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}