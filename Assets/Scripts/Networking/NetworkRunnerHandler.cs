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

    // En NetworkRunnerHandler.cs

// Variables nuevas para controlar el flujo
private bool _joining = false;

public void StartMatchmaking()
{
    Debug.Log("🔗 Conectando al Lobby para buscar partida...");
    _joining = true;
    
    if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
    
    // Paso 1: Unirse al Lobby para ver las listas de salas
    _runner.JoinSessionLobby(SessionLobby.ClientServer);
}

// ESTE MÉTODO ES NUEVO: Fusion lo llama cuando recibe la lista de salas del Lobby
public async void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
{
    if (!_joining) return; // Solo actuamos si estamos buscando partida

    Debug.Log($"📋 Lista de sesiones recibida. Total: {sessionList.Count}");

    // Buscamos una sesión que tenga espacio (menos de 2 jugadores) y esté abierta
    SessionInfo availableSession = null;
    foreach (var session in sessionList)
    {
        if (session.PlayerCount < maxPlayers && session.IsOpen)
        {
            availableSession = session;
            break;
        }
    }

    // Configuramos la escena
    var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
    _joining = false; // Dejamos de buscar para no spamear uniones

    if (availableSession != null)
    {
        Debug.Log($"✅ Sala encontrada: {availableSession.Name}. Uniéndose...");
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client, // Nos unimos como cliente
            SessionName = availableSession.Name,
            SceneManager = sceneManager,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex)
        });
    }
    else
    {
        Debug.Log("⚠️ No hay salas disponibles. Creando una NUEVA sala Host...");
        // Creamos una sala con nombre único (Guid) para que nadie más se meta por error
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host, // Somos el Host de la nueva sala
            SessionName = System.Guid.NewGuid().ToString(), // Nombre único aleatorio
            PlayerCount = maxPlayers, // Límite estricto de 2
            SceneManager = sceneManager,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex)
        });
    }
}

// --- Asegúrate de mantener el resto del código (OnPlayerJoined, SpawnPlayer corregido, etc.) ---

public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    Debug.Log($"👤 Player joined: {player}");

    if (runner.IsServer)
    {
        int connected = runner.ActivePlayers.Count();
        Debug.Log($"🧩 Jugadores conectados: {connected}/{maxPlayers}");

        if (connected == maxPlayers)
        {
            AssignTeams(); // Envía los RPCs
            // Inicia una corrutina para esperar 1s antes de cargar la escena
            // Esto da tiempo a que los RPCs lleguen a los clientes y SessionManager se actualice
            StartCoroutine(DelayLoadLevel(runner)); 
        }
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
Debug.Log($"🎯 Eres {(team == 0 ? "Español" : "Maya")} - Personaje {charId}");        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"✅ Escena cargada: {currentScene}");

        if (currentScene == loadingSceneName && runner.IsServer)
        {
            if (_autoStartTimer != null) StopCoroutine(_autoStartTimer);
            _autoStartTimer = StartCoroutine(AutoStartAfterDelay(10f));
        }

        // IMPORTANT: spawn SOLO en servidor
        if (currentScene == mapSceneName && runner.IsServer)
        {
            foreach (var p in runner.ActivePlayers)
                SpawnPlayer(runner, p);
        }
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

// En NetworkRunnerHandler.cs -> SpawnPlayer
// CAMBIO: Invertir la lógica ternaria. Si es 0 (Maya), carga ixquic. Si es 1, beatriz.
string prefabPath = $"Prefabs/Characters/pf_{(charId == 0 ? "ixquic" : "beatriz")}";
var prefab = Resources.Load<NetworkObject>(prefabPath); // Cambiar a NetworkObject
    
    Debug.Log($"🎯 Spawneando: Player {player}, Team {team}, Char {charId}, Prefab {prefabPath}");

    Vector3 spawnPos = team == 0 ? new Vector3(-0.39f, -0.382f, 0f) : new Vector3(1.3f, -0.4f, 0f);

    var spawned = runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
    
    if (spawned != null)
    {
        Debug.Log($"✅ Spawn exitoso - InputAuthority: {spawned.InputAuthority}, StateAuthority: {spawned.HasStateAuthority}");
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
    
    // Movimiento horizontal (usa la estructura que ya tienes)
    data.Move.x = 0f;
    if (Input.GetKey(KeyCode.A)) data.Move.x -= 1f;
    if (Input.GetKey(KeyCode.D)) data.Move.x += 1f;
    data.Move.y = 0f; // No hay movimiento vertical con teclas
    
    // Botones (usa NetworkBool como ya lo tienes)
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


    // Añade esta corrutina al NetworkRunnerHandler.cs:
IEnumerator DelayLoadLevel(NetworkRunner runner)
{
    yield return new WaitForSeconds(1.0f); // Espera de seguridad para sync de datos
    int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity");
    if (sceneIndex >= 0) runner.LoadScene(SceneRef.FromIndex(sceneIndex));
}
}