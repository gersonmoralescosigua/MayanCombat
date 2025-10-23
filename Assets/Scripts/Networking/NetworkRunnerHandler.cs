using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance;

    private NetworkRunner _runner;
    private bool _isConnecting = false;

    public string mapSceneName = "Map_Tikal_Base";
    public int maxPlayers = 2;

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

    public async void StartMatchmaking()
    {
        if (_isConnecting)
            return;

        _isConnecting = true;
        Debug.Log("🔗 Iniciando conexión con Photon Fusion...");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{mapSceneName}.unity");
        if (sceneIndex < 0)
        {
            Debug.LogWarning($"No se encontró la escena {mapSceneName} en Build Settings, cargando por nombre...");
        }

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "MayanCombatRoom",
            Scene = SceneRef.FromIndex(sceneIndex >= 0 ? sceneIndex : 0),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers
        });

        if (result.Ok)
        {
            Debug.Log("✅ Conectado correctamente a Photon Fusion.");
        }
        else
        {
            Debug.LogError($"❌ Error al conectar: {result.ShutdownReason}");
        }

        _isConnecting = false;
    }

    public void Shutdown()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            Destroy(_runner);
            _runner = null;
            Debug.Log("🔴 Photon Fusion cerrado.");
        }
    }

    // --- Callbacks básicos de Fusion ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Jugador conectado: {player}");

        if (runner.IsServer)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-2, 2), 1, Random.Range(-2, 2));
            runner.Spawn(Resources.Load<GameObject>("Prefabs/Characters/pf_player"), spawnPos, Quaternion.identity, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🚪 Jugador salió: {player}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("🌐 Conectado al servidor de Photon.");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"🔌 Desconectado del servidor: {reason}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.direction.x = Input.GetAxis("Horizontal");
        data.direction.y = Input.GetAxis("Vertical");
        data.jump = Input.GetKey(KeyCode.Space);
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
