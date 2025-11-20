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
    public string resultsSceneName = "MatchResults"; 

    [Header("Matchmaking")]
    public int maxPlayers = 2;
    public NetworkObject playerDataPrefab; 

    // Diccionarios del Servidor
    private Dictionary<PlayerRef, int> _playerTeams = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, int> _playerCharacters = new Dictionary<PlayerRef, int>();

    private bool _joining = false;
    private Coroutine _autoStartTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- MATCHMAKING ---
    public void StartMatchmaking()
    {
        _joining = true;
        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public async void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!_joining) return;

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
            await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Client, SessionName = availableSession.Name, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
        }
        else
        {
            await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Host, SessionName = System.Guid.NewGuid().ToString(), PlayerCount = maxPlayers, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
        }
    }

    // --- JUGADORES & EQUIPOS ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (playerDataPrefab != null)
            {
                var playerObj = runner.Spawn(playerDataPrefab, Vector3.zero, Quaternion.identity, player);
                runner.SetPlayerObject(player, playerObj);
            }
            if (runner.ActivePlayers.Count() == maxPlayers) StartCoroutine(AssignTeamsRoutine());
        }
    }

    IEnumerator AssignTeamsRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        AssignTeams();
        yield return new WaitForSeconds(1.0f);
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity");
        if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
    }

    private void AssignTeams()
    {
        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2) return;
        
        players = players.OrderBy(x => UnityEngine.Random.value).ToList();

        _playerTeams[players[0]] = 0; // Maya
        _playerCharacters[players[0]] = 0;
        SetPlayerData(players[0], 0, 0);

        _playerTeams[players[1]] = 1; // Español
        _playerCharacters[players[1]] = 1;
        SetPlayerData(players[1], 1, 1);
    }

    private void SetPlayerData(PlayerRef player, int team, int charId)
    {
        if (_runner.TryGetPlayerObject(player, out var obj))
        {
            var data = obj.GetComponent<PlayerDataNetworked>();
            if (data != null) { data.CharacterID = charId; data.TeamID = team; }
        }
    }

    // --- SPAWN Y ESCENAS ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == loadingSceneName && runner.IsServer)
        {
            if (_autoStartTimer != null) StopCoroutine(_autoStartTimer);
            _autoStartTimer = StartCoroutine(AutoStartAfterDelay(8f));
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
        int charId = 0; int team = 0;
        if (_playerCharacters.ContainsKey(player)) { charId = _playerCharacters[player]; team = _playerTeams[player]; }
        else { team = player.RawEncoded % 2; charId = team; }

        string prefabPath = $"Prefabs/Characters/pf_{(charId == 0 ? "ixquic" : "beatriz")}";
        Vector3 spawnPos = team == 0 ? new Vector3(-0.39f, -0.382f, 0f) : new Vector3(1.3f, -0.4f, 0f);
        runner.Spawn(Resources.Load<NetworkObject>(prefabPath), spawnPos, Quaternion.identity, player);
    }

    // --- LÓGICA DE MUERTE ---
    public void OnPlayerFellToDeath(GameObject deadPlayerObj)
    {
        if (!_runner.IsServer) return;

        NetworkObject netObj = deadPlayerObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        PlayerRef deadPlayerRef = netObj.InputAuthority;
        int losingTeam = -1;

        if (_playerTeams.ContainsKey(deadPlayerRef)) losingTeam = _playerTeams[deadPlayerRef];

        int winningTeam = (losingTeam == 0) ? 1 : 0;

        Debug.Log($"💀 Fin de partida. Ganador: {winningTeam}");

        // BUSCAR CUALQUIER PLAYERDATA PARA ENVIAR EL RPC A TODOS
        foreach(var player in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(player, out var pObj))
            {
                var dataScript = pObj.GetComponent<PlayerDataNetworked>();
                if (dataScript != null)
                {
                    // Llama al RPC para que TODOS (incluido el ganador) reciban el mensaje
                    dataScript.RPC_GameFinished(winningTeam);
                    break; 
                }
            }
        }

        StartCoroutine(FinishMatchRoutine());
    }

    IEnumerator FinishMatchRoutine()
    {
        // Damos tiempo para que el RPC llegue y se muestre
        yield return new WaitForSeconds(3.0f); 
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{resultsSceneName}.unity");
        if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
    }

    // Callbacks
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { _playerTeams.Remove(player); _playerCharacters.Remove(player); }
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