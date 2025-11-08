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

    public async void StartMatchmaking()
    {
        if (_isConnecting) return;
        _isConnecting = true;
        Debug.Log("🔗 Conectando con Fusion...");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "MayanCombatRoom",
            Scene = SceneRef.None,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers
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

    public void LoadSelectCharacterScene()
    {
        SceneManager.LoadScene(characterSelectScene);
    }

    public void SetPlayerCharacter(PlayerRef player, int characterId)
    {
        SelectedCharacters[player] = characterId;
        Debug.Log($"✅ Player {player} eligió personaje {characterId}");
    }

    public void TryStartGame()
    {
        if (SelectedCharacters.Count < maxPlayers) return;
        if (_runner.IsServer) StartGameOnServer();
    }

    private async void StartGameOnServer()
    {
        Debug.Log($"🗺 Cargando mapa {mapSceneName}...");
        await _runner.LoadScene(mapSceneName);
        Debug.Log("✅ Mapa cargado");
    }

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
            // intentar rutas alternativas (compatibilidad con tu estructura)
            prefab = Resources.Load<GameObject>($"Prefabs/Characters/pf_{(charID == 0 ? "ixquic" : "beatriz")}");
        }

        if (prefab == null)
        {
            Debug.LogError($"❌ Prefab no encontrado para char {charID}");
            return;
        }

        Vector3 spawnPos = new Vector3(Random.Range(-2f, 2f), 1f, 0f);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
        Debug.Log($"✅ Spawn player {player} con personaje {charID}");
    }

    // ---------- INetworkRunnerCallbacks ----------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");

        // si soy server, asegurar MatchController o usar SelectedCharacters
        if (runner.IsServer)
        {
            var mc = FindObjectOfType<MatchController>();
            if (mc == null)
            {
                var mcPrefab = Resources.Load<GameObject>("Network/MatchController");
                if (mcPrefab != null)
                    runner.Spawn(mcPrefab, Vector3.zero, Quaternion.identity, PlayerRef.None);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🚪 Player left: {player}");
        if (SelectedCharacters.ContainsKey(player)) SelectedCharacters.Remove(player);
    }

    public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("🌐 Connected to server");
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => Debug.LogWarning($"❌ Disconnected: {reason}");

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("✅ Escena cargada (Fusion)");

        if (SceneManager.GetActiveScene().name == mapSceneName)
        {
            foreach (var p in runner.ActivePlayers)
                SpawnPlayer(runner, p);
        }
    }
    public void OnSceneLoadStart(NetworkRunner runner) => Debug.Log("📥 Fusion comenzó a cargar una escena...");

    // Métodos obligatorios vacíos
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // Input (envía Dirección + salto)
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.move.x = Input.GetAxis("Horizontal");
        data.move.y = Input.GetAxis("Vertical");
        data.jumpPressed = Input.GetKey(KeyCode.Space);
        data.attackPressed = Input.GetKey(KeyCode.J);
        input.Set(data);
    }

    // Shutdown helper
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