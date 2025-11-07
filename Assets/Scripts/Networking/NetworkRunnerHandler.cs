using UnityEngine;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance;

    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    private bool _isConnecting = false;

    [Header("Scenes")]
    public string characterSelectScene = "CharacterSelectWrapper";
    public string mapSceneName = "Map_Tikal_Base";

    [Header("Players")]
    public int maxPlayers = 2;

    public Dictionary<PlayerRef, int> SelectedCharacters = new();

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

    // ✅ Matchmaking sin cargar mapa
    public async void StartMatchmaking()
    {
        if (_isConnecting)
            return;

        _isConnecting = true;
        Debug.Log("🔗 Conectando con Fusion...");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "MayanCombatRoom",
            Scene = SceneRef.None,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
        {
            Debug.LogError($"❌ Error al conectar: {result.ShutdownReason}");
            _isConnecting = false;
            return;
        }

        Debug.Log("✅ Conectado a Fusion");
        LoadSelectCharacterScene();

        _isConnecting = false;
    }

    // ✅ Cargar pantalla de selección
    public void LoadSelectCharacterScene()
    {
        SceneManager.LoadScene(characterSelectScene);
    }

    // ✅ Guardar selección
    public void SetPlayerCharacter(PlayerRef player, int characterId)
    {
        SelectedCharacters[player] = characterId;
        Debug.Log($"✅ Player {player} eligió personaje {characterId}");
    }

    // ✅ Ver si ya se puede iniciar partida
    public void TryStartGame()
    {
        if (SelectedCharacters.Count < maxPlayers)
            return;

        if (_runner.IsServer)
            StartGameOnServer();
    }

    // ✅ Cargar el mapa — LoadScene NO retorna nada en tu versión
    private async void StartGameOnServer()
    {
        Debug.Log($"🗺 Cargando mapa {mapSceneName}...");

        await _runner.LoadScene(mapSceneName);

        Debug.Log("✅ Mapa cargado");
    }

    // ✅ Spawn del personaje seleccionado
    public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (!SelectedCharacters.ContainsKey(player))
        {
            Debug.LogError("❌ Player no ha seleccionado personaje.");
            return;
        }

        int charID = SelectedCharacters[player];
        GameObject prefab = Resources.Load<GameObject>($"Characters/Character_{charID}");

        if (prefab == null)
        {
            Debug.LogError($"❌ Prefab no encontrado: Characters/Character_{charID}");
            return;
        }

        Vector3 spawnPos = new Vector3(Random.Range(-2f, 2f), 0, 0);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, player);

        Debug.Log($"✅ Spawn player {player} con personaje {charID}");
    }

    // ✅ Fusion callbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🚪 Player left: {player}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("🌐 Connected to server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"❌ Disconnected: {reason}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("✅ Escena cargada (Fusion)");

        if (SceneManager.GetActiveScene().name == mapSceneName)
        {
            foreach (var p in runner.ActivePlayers)
                SpawnPlayer(runner, p);
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("📥 Fusion comenzó a cargar una escena...");
    }

    // ✅ Métodos obligatorios vacíos
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // ✅ Inputs
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.direction.x = Input.GetAxis("Horizontal");
        data.direction.y = Input.GetAxis("Vertical");
        data.jump = Input.GetKey(KeyCode.Space);
        input.Set(data);
    }

    // ✅ Método que faltaba (para MatchmakingUI)
    public void Shutdown()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            Destroy(_runner);
            _runner = null;
            Debug.Log("🔴 Fusion apagado.");
        }
    }
}