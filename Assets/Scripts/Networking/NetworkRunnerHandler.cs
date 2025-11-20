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

    [Header("Configuración de Escenas")]
    public string matchmakingSceneName = "Matchmaking";
    public string loadingSceneName = "LoadingAssignment";
    public string resultsSceneName = "MatchResults"; 
    public string menuSceneName = "Menu";

    // ROTACIÓN DE MAPAS
    public string[] mapRotation = new string[] { 
        "Map_Tikal_Base", 
        "Map_Atitlan_Base", 
        "Map_Volcan_Base" 
    };

    [Header("Matchmaking")]
    public int maxPlayers = 2;
    public NetworkObject playerDataPrefab; 

    // ESTADO DEL TORNEO
    private int _mayaWins = 0;
    private int _spanishWins = 0;
    private int _currentMapIndex = 0;
    private List<string> _mapsPlayedHistory = new List<string>();

    // DICCIONARIOS
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
        _mayaWins = 0;
        _spanishWins = 0;
        _currentMapIndex = 0;
        _mapsPlayedHistory.Clear();
        _joining = true;

        if (_runner == null) _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    public async void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!_joining) return;
        SessionInfo availableSession = sessionList.FirstOrDefault(s => s.PlayerCount < maxPlayers && s.IsOpen);
        
        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>();
        _joining = false;

        if (availableSession != null)
            await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Client, SessionName = availableSession.Name, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
        else
            await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Host, SessionName = System.Guid.NewGuid().ToString(), PlayerCount = maxPlayers, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
    }

    // --- EQUIPOS ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (playerDataPrefab != null)
            {
                var pObj = runner.Spawn(playerDataPrefab, Vector3.zero, Quaternion.identity, player);
                runner.SetPlayerObject(player, pObj);
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

        _playerTeams[players[0]] = 0; _playerCharacters[players[0]] = 0; // Maya
        _playerTeams[players[1]] = 1; _playerCharacters[players[1]] = 1; // Español
        
        SetPlayerData(players[0], 0, 0);
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

    // --- CARGA DE ESCENAS Y SPAWN ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        // 1. Estamos en Loading -> Esperamos y vamos al primer mapa
        if (currentScene == loadingSceneName && runner.IsServer)
        {
            StartCoroutine(AutoStartRound(8f));
        }

        // 2. Estamos en MatchResults -> Esperamos 10s y decidimos (Siguiente mapa o Menu)
        if (currentScene == resultsSceneName && runner.IsServer)
        {
            StartCoroutine(ProcessMatchResults(10f));
        }

        // 3. Estamos en un Mapa de Juego -> Spawneamos
        if (IsMapScene(currentScene) && runner.IsServer)
        {
            foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
            if (!_mapsPlayedHistory.Contains(currentScene)) _mapsPlayedHistory.Add(currentScene);
        }
    }

    private bool IsMapScene(string sceneName) => mapRotation.Contains(sceneName);

    private IEnumerator AutoStartRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadCurrentMap();
    }

    private IEnumerator ProcessMatchResults(float delay)
    {
        // Esperamos 10 segundos mostrando el resultado
        yield return new WaitForSeconds(delay);

        // Verificamos si el torneo terminó (2 victorias o sin mapas)
        bool tournamentOver = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length);

        if (tournamentOver)
        {
            Debug.Log("🏁 Torneo finalizado. Volviendo al menú.");
            _runner.Shutdown();
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            // Avanzamos al siguiente mapa
            LoadCurrentMap();
        }
    }

    private void LoadCurrentMap()
    {
        if (_currentMapIndex < mapRotation.Length)
        {
            string mapToLoad = mapRotation[_currentMapIndex];
            Debug.Log($"🗺️ Cargando Mapa: {mapToLoad}");
            
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{(_currentMapIndex == 0 ? "Tikal" : (_currentMapIndex == 1 ? "Atitlan" : "Volcan"))}/{mapToLoad}.unity");
            
            if (sceneIndex < 0) _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(mapToLoad)));
            else _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int charId = 0; int team = 0;
        if (_playerCharacters.ContainsKey(player)) { charId = _playerCharacters[player]; team = _playerTeams[player]; }
        else { team = player.RawEncoded % 2; charId = team; }

        string prefabPath = $"Prefabs/Characters/pf_{(charId == 0 ? "ixquic" : "beatriz")}";
        
        // --- NUEVO SISTEMA DE SPAWN POR NOMBRE ---
        Vector3 spawnPos = Vector3.zero;
        string spawnPointName = (team == 0) ? "Spawn_Maya" : "Spawn_Spanish";
        GameObject point = GameObject.Find(spawnPointName);

        if (point != null) 
        {
            spawnPos = point.transform.position;
        }
        else
        {
            Debug.LogWarning($"⚠️ No se encontró '{spawnPointName}' en la escena. Usando Default.");
            spawnPos = (team == 0) ? new Vector3(-2, 0, 0) : new Vector3(2, 0, 0);
        }
        // -----------------------------------------

        runner.Spawn(Resources.Load<NetworkObject>(prefabPath), spawnPos, Quaternion.identity, player);
    }

    // --- MUERTE Y PUNTUACIÓN ---
    public void OnPlayerFellToDeath(GameObject deadPlayerObj)
    {
        if (!_runner.IsServer) return;

        NetworkObject netObj = deadPlayerObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        PlayerRef deadPlayerRef = netObj.InputAuthority;
        int losingTeam = -1;
        if (_playerTeams.ContainsKey(deadPlayerRef)) losingTeam = _playerTeams[deadPlayerRef];

        int winningTeam = (losingTeam == 0) ? 1 : 0;

        // Actualizar Puntos
        if (winningTeam == 0) _mayaWins++; else _spanishWins++;

        // Lógica de Fin de Torneo
        bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length - 1);
        string winnerName = (_mayaWins > _spanishWins) ? "IMPERIO MAYA" : "ESPAÑOLES";

        // Enviar Notificación (RPC)
        foreach(var player in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(player, out var pObj))
            {
                var data = pObj.GetComponent<PlayerDataNetworked>();
                if (data != null) data.RPC_RoundFinished(winningTeam, _mayaWins, _spanishWins, matchEnded, winnerName);
            }
        }

        // Guardar en Firebase solo si terminó el torneo
        if (matchEnded)
        {
            string w = (_mayaWins > _spanishWins) ? "Maya" : "Español";
            string l = (w == "Maya") ? "Español" : "Maya";
            MatchHistoryLogger.SaveMatch(w, l, Mathf.Max(_mayaWins, _spanishWins), Mathf.Min(_mayaWins, _spanishWins), _mapsPlayedHistory);
        }

        // Avanzar índice de mapa para la próxima carga
        if (!matchEnded) _currentMapIndex++;

        StartCoroutine(GoToResultsRoutine());
    }

    IEnumerator GoToResultsRoutine()
    {
        yield return new WaitForSeconds(2.0f); // 2s para ver que murió
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{resultsSceneName}.unity");
        if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
    }

    // Callbacks vacíos
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