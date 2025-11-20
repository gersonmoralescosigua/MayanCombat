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

    [Header("Escenas")]
    public string matchmakingSceneName = "Matchmaking";
    public string loadingSceneName = "LoadingAssignment";
    public string resultsSceneName = "MatchResults"; 
    public string winnersSceneName = "Winners"; 
    public string menuSceneName = "Menu";

    public string[] mapRotation = new string[] { "Map_Tikal_Base", "Map_Atitlan_Base", "Map_Volcan_Base" };

    [Header("Configuración")]
    public int maxPlayers = 2;
    public NetworkObject playerDataPrefab; 

    // Estado
    private int _mayaWins = 0;
    private int _spanishWins = 0;
    private int _currentMapIndex = 0;
    private bool _roundIsActive = true;
    private List<string> _mapsPlayedHistory = new List<string>();

    private Dictionary<PlayerRef, int> _playerTeams = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, string> _playerNames = new Dictionary<PlayerRef, string>();

    private bool _joining = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- MATCHMAKING ---
    public void StartMatchmaking()
    {
        _mayaWins = 0; _spanishWins = 0; _currentMapIndex = 0; 
        _mapsPlayedHistory.Clear(); _playerNames.Clear();
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
        if (availableSession != null) await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Client, SessionName = availableSession.Name, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
        else await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Host, SessionName = System.Guid.NewGuid().ToString(), PlayerCount = maxPlayers, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
    }

    // --- NOMBRES ---
    public void RegisterPlayerName(PlayerRef player, string nickname)
    {
        if (_playerNames.ContainsKey(player)) _playerNames[player] = nickname;
        else _playerNames.Add(player, nickname);
    }

    // --- SPAWN ---
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
        _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity")));
    }

    private void AssignTeams()
    {
        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2) return;
        players = players.OrderBy(x => UnityEngine.Random.value).ToList();

        _playerTeams[players[0]] = 0; _playerTeams[players[1]] = 1;
        
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

    // --- CONTROL DE ESCENAS ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == loadingSceneName && runner.IsServer) StartCoroutine(AutoStartRound(5f));
        
        // Si llegamos a Resultados, esperamos 8 seg y decidimos el destino
        if (currentScene == resultsSceneName && runner.IsServer) StartCoroutine(ProcessMatchResultsLogic(8f));

        if (mapRotation.Contains(currentScene) && runner.IsServer)
        {
            _roundIsActive = true; 
            foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
            if (!_mapsPlayedHistory.Contains(currentScene)) _mapsPlayedHistory.Add(currentScene);
        }
    }

    IEnumerator AutoStartRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadCurrentMap();
    }

    private void LoadCurrentMap()
    {
        if (_currentMapIndex < mapRotation.Length)
        {
            string mapToLoad = mapRotation[_currentMapIndex];
            int idx = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{GetFolder(mapToLoad)}/{mapToLoad}.unity");
            if (idx < 0) idx = SceneUtility.GetBuildIndexByScenePath(mapToLoad); 
            if (idx >= 0) _runner.LoadScene(SceneRef.FromIndex(idx));
        }
    }
    private string GetFolder(string map) { if (map.Contains("Tikal")) return "Tikal"; if (map.Contains("Atitlan")) return "Atitlan"; return "Volcan"; }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int team = _playerTeams.ContainsKey(player) ? _playerTeams[player] : player.RawEncoded % 2;
        string prefabPath = $"Prefabs/Characters/pf_{(team == 0 ? "ixquic" : "beatriz")}";
        Vector3 spawnPos = Vector3.zero;
        string sPoint = (team == 0) ? "Spawn_Maya" : "Spawn_Spanish";
        GameObject pt = GameObject.Find(sPoint);
        if (pt != null) spawnPos = pt.transform.position;
        else spawnPos = (team == 0) ? new Vector3(-2, 2, 0) : new Vector3(2, 2, 0);

        runner.Spawn(Resources.Load<NetworkObject>(prefabPath), spawnPos, Quaternion.identity, player);
    }

    // --- LÓGICA DE MUERTE OPTIMIZADA ---
    public void OnPlayerFellToDeath(GameObject deadPlayerObj)
    {
        if (!_runner.IsServer || !_roundIsActive) return;
        _roundIsActive = false; 

        NetworkObject netObj = deadPlayerObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        int losingTeam = _playerTeams.ContainsKey(netObj.InputAuthority) ? _playerTeams[netObj.InputAuthority] : -1;
        int winningTeam = (losingTeam == 0) ? 1 : 0;

        if (winningTeam == 0) _mayaWins++; else _spanishWins++;

        bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length - 1);
        string winnerName = (_mayaWins > _spanishWins) ? "IMPERIO MAYA" : "ESPAÑOLES";

        // CONSTRUCCIÓN DEL MENSAJE (Tal cual sale en tu consola)
        string msg = "";
        string roundWinner = (winningTeam == 0) ? "Maya" : "Español";

        if (matchEnded)
        {
            msg = $"👑 ¡FIN DEL TORNEO!\n\nGanador Global: {winnerName}\nMarcador: Maya {_mayaWins} - {_spanishWins} Español";
        }
        else
        {
            msg = $"Ronda Terminada\nGanador Ronda: {roundWinner}\n\nGlobal: Maya {_mayaWins} - {_spanishWins} Español";
        }

        Debug.Log($"📝 ENVIANDO MENSAJE: {msg}");

        // ENVIAR A TODOS (RPC)
        foreach(var p in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(p, out var obj))
            {
                var pd = obj.GetComponent<PlayerDataNetworked>();
                if (pd != null) 
                {
                    // Enviamos el string exacto a la pantalla
                    pd.RPC_SetUIMessage(msg, matchEnded, matchEnded ? winningTeam : -1);
                }
            }
        }

        // FIREBASE (TU LÓGICA INTACTA)
        if (matchEnded)
        {
            SaveToFirebaseCorrectly(winningTeam == 0 ? "Maya" : "Español");
        }
        else
        {
            _currentMapIndex++;
        }

        StartCoroutine(GoToResultsScene(3f));
    }

    private void SaveToFirebaseCorrectly(string winnerTeam)
    {
        string winnerNick = "Desconocido", loserNick = "Desconocido";
        foreach(var kvp in _playerTeams)
        {
            string name = _playerNames.ContainsKey(kvp.Key) ? _playerNames[kvp.Key] : "SinNombre";
            bool pIsWinner = (winnerTeam == "Maya" && kvp.Value == 0) || (winnerTeam == "Español" && kvp.Value == 1);
            if (pIsWinner) winnerNick = name; else loserNick = name;
        }
        string loserTeam = (winnerTeam == "Maya") ? "Español" : "Maya";
        MatchHistoryLogger.SaveMatch(winnerTeam, loserTeam, winnerNick, loserNick, 20, 0, _mapsPlayedHistory);
    }

    IEnumerator GoToResultsScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{resultsSceneName}.unity")));
    }

    // --- TRANSICIÓN A VIDEO (WINNERS) ---
    IEnumerator ProcessMatchResultsLogic(float delay)
    {
        yield return new WaitForSeconds(delay);

        bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length);

        if (matchEnded)
        {
            Debug.Log("🎬 Ordenando ir a WINNERS (Videos)...");
            
            // Enviamos la orden a todos los clientes
            foreach(var p in _runner.ActivePlayers)
            {
                if (_runner.TryGetPlayerObject(p, out var obj))
                {
                    var pd = obj.GetComponent<PlayerDataNetworked>();
                    if (pd != null) pd.RPC_OrderVideoTransition();
                }
            }
        }
        else
        {
            LoadCurrentMap();
        }
    }

    // Se llama localmente desde el RPC
    public void ExecuteLocalVideoTransition()
    {
        if (_runner != null) _runner.Shutdown();
        SessionManager.Instance.LoadFinalVideoScene();
    }

    // Callbacks vacíos...
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { _playerTeams.Remove(player); _playerNames.Remove(player); }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { 
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