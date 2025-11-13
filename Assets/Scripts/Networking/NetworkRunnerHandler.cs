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

    [Header("Scenes (asegúrate de que están en Build Settings)")]
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 🔹 INICIA MATCHMAKING
    public async void StartMatchmaking()
    {
        Debug.Log("🔗 Iniciando matchmaking...");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Usar SIEMPRE el mismo nombre de sesión
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

    // 🔹 CUANDO UN JUGADOR ENTRA
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");

        if (runner.IsServer)
        {
            int connected = runner.ActivePlayers.Count();
            Debug.Log($"🧩 Jugadores conectados: {connected}/{maxPlayers}");

            if (connected == maxPlayers)
            {
                AssignTeams();

                // ✅ Cambiar a la escena intermedia (pantalla de asignación)
                int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity");
                if (sceneIndex >= 0)
                {
                    Debug.Log($"📥 Cargando escena: {loadingSceneName}");
                    runner.LoadScene(SceneRef.FromIndex(sceneIndex));
                }
                else
                {
                    Debug.LogError($"❌ Escena {loadingSceneName} no está en Build Settings.");
                }
            }
        }
    }

    // 🔹 Asignar equipos aleatoriamente
    private void AssignTeams()
    {
        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2) return;

        players = players.OrderBy(x => UnityEngine.Random.value).ToList();

        _playerTeams[players[0]] = 0; // Español
        _playerTeams[players[1]] = 1; // Maya

        _playerCharacters[players[0]] = 0; // Beatriz
        _playerCharacters[players[1]] = 1; // Ixquic

        foreach (var p in players)
            RPC_AssignRole(p, _playerTeams[p], _playerCharacters[p]);

        Debug.Log($"✅ Equipos asignados: {players[0]}=Español, {players[1]}=Maya");
    }

    // 🔹 RPC: Envia al cliente su rol y personaje
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

    // 🔹 ESCENA CARGADA
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"✅ Escena cargada: {currentScene}");

        if (currentScene == loadingSceneName && runner.IsServer)
        {
            if (_autoStartTimer != null) StopCoroutine(_autoStartTimer);
            _autoStartTimer = StartCoroutine(AutoStartAfterDelay(10f));
        }

        if (currentScene == mapSceneName)
        {
            foreach (var p in runner.ActivePlayers)
                SpawnPlayer(runner, p);
        }
    }

    // 🔹 ESPERA 10 SEGUNDOS Y LANZA PARTIDA
    private IEnumerator AutoStartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("⏰ Tiempo terminado. Iniciando partida...");

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/Tikal/{mapSceneName}.unity");
        if (sceneIndex >= 0)
        {
            _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        else
        {
            Debug.LogError($"❌ Escena {mapSceneName} no está en Build Settings.");
        }
    }

    // 🔹 SPAWN DE JUGADORES
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

        Vector3 spawnPos = team == 0 ? new Vector3(-2, 1, 0) : new Vector3(2, 1, 0);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
        Debug.Log($"✅ Spawn {prefab.name} ({(team == 0 ? "Español" : "Maya")})");
    }

    // 🔹 CALLBACKS REQUERIDOS
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
        data.move.x = Input.GetAxis("Horizontal");
        data.move.y = Input.GetAxis("Vertical");
        data.jumpPressed = Input.GetKey(KeyCode.Space);
        data.attackPressed = Input.GetKey(KeyCode.J);
        input.Set(data);
    }

    // No utilizados pero necesarios
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