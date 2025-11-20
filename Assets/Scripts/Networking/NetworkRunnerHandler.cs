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
    public string menuSceneName = "Menu";

    [Header("Videos")]
    public string videoGanaMaya = "GanaMaya";
    public string videoPierdeMaya = "PierdeMaya";
    public string videoGanaSpanish = "GanaSpanish";
    public string videoPierdeSpanish = "PierdeSpanish";

    public string[] mapRotation = new string[] { "Map_Tikal_Base", "Map_Atitlan_Base", "Map_Volcan_Base" };

    [Header("Configuración")]
    public int maxPlayers = 2;
    public NetworkObject playerDataPrefab; 

    // Estado
    private int _mayaWins = 0;
    private int _spanishWins = 0;
    private int _currentMapIndex = 0;
    private bool _roundIsActive = true; // SEMÁFORO IMPORTANTE
    private List<string> _mapsPlayedHistory = new List<string>();

    // Diccionarios
    private Dictionary<PlayerRef, int> _playerTeams = new Dictionary<PlayerRef, int>();
    private Dictionary<PlayerRef, int> _playerCharacters = new Dictionary<PlayerRef, int>();

    private bool _joining = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ... (StartMatchmaking y OnSessionListUpdated IGUAL QUE ANTES) ...
    public void StartMatchmaking()
    {
        _mayaWins = 0; _spanishWins = 0; _currentMapIndex = 0; _mapsPlayedHistory.Clear();
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
    // ... (Fin Matchmaking) ...

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
        // Cargar Loading
        _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{loadingSceneName}.unity")));
    }

    private void AssignTeams()
    {
        var players = _runner.ActivePlayers.ToList();
        if (players.Count < 2) return;
        players = players.OrderBy(x => UnityEngine.Random.value).ToList();

        _playerTeams[players[0]] = 0; _playerCharacters[players[0]] = 0; 
        _playerTeams[players[1]] = 1; _playerCharacters[players[1]] = 1; 
        
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

        if (runner.IsServer)
        {
            if (currentScene == loadingSceneName)
            {
                StartCoroutine(LoadMapDelay(5f));
            }
            else if (currentScene == resultsSceneName)
            {
                // Aquí decidimos: ¿Siguiente mapa o Video Final?
                StartCoroutine(ProcessMatchResultsLogic(8f));
            }
            else if (mapRotation.Contains(currentScene))
            {
                // Estamos en un mapa de juego
                _roundIsActive = true; // ACTIVAMOS EL SEMÁFORO DE LA RONDA
                foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
                if (!_mapsPlayedHistory.Contains(currentScene)) _mapsPlayedHistory.Add(currentScene);
            }
        }
    }

    IEnumerator LoadMapDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadCurrentMap();
    }

    private void LoadCurrentMap()
    {
        if (_currentMapIndex < mapRotation.Length)
        {
            string mapName = mapRotation[_currentMapIndex];
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{GetMapFolder(mapName)}/{mapName}.unity")));
        }
    }

    private string GetMapFolder(string mapName)
    {
        if (mapName.Contains("Tikal")) return "Tikal";
        if (mapName.Contains("Atitlan")) return "Atitlan";
        return "Volcan";
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int team = _playerTeams.ContainsKey(player) ? _playerTeams[player] : player.RawEncoded % 2;
        string prefabPath = $"Prefabs/Characters/pf_{(team == 0 ? "ixquic" : "beatriz")}";
        
        // Spawn Dinámico por nombre de objeto
        Vector3 spawnPos = Vector3.zero;
        string spawnName = (team == 0) ? "Spawn_Maya" : "Spawn_Spanish";
        GameObject point = GameObject.Find(spawnName);
        
        if (point != null) spawnPos = point.transform.position;
        else spawnPos = (team == 0) ? new Vector3(-2, 2, 0) : new Vector3(2, 2, 0);

        runner.Spawn(Resources.Load<NetworkObject>(prefabPath), spawnPos, Quaternion.identity, player);
    }

    // --- LÓGICA DE MUERTE (CORREGIDA) ---
    public void OnPlayerFellToDeath(GameObject deadPlayerObj)
    {
        // Si no soy servidor O la ronda ya terminó, ignoramos
        if (!_runner.IsServer || !_roundIsActive) return;

        _roundIsActive = false; // BLOQUEAMOS INMEDIATAMENTE PARA EVITAR DOBLES MUERTES

        NetworkObject netObj = deadPlayerObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        int losingTeam = _playerTeams.ContainsKey(netObj.InputAuthority) ? _playerTeams[netObj.InputAuthority] : -1;
        int winningTeam = (losingTeam == 0) ? 1 : 0;

        // Sumar puntos
        if (winningTeam == 0) _mayaWins++; else _spanishWins++;

        // Verificar si terminó el torneo
        bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length - 1);
        string winnerName = (_mayaWins > _spanishWins) ? "IMPERIO MAYA" : "ESPAÑOLES";

        string msg = "";
        if (matchEnded)
            msg = $"👑 ¡FIN DEL TORNEO!\n\nGanador Global: {winnerName}\nMarcador: Maya {_mayaWins} - {_spanishWins} Español";
        else
            msg = $"Ronda Terminada\nGanador Ronda: {(winningTeam == 0 ? "Maya" : "Español")}\n\nGlobal: Maya {_mayaWins} - {_spanishWins} Español";

        Debug.Log(msg);

        // ENVIAR RPC Y ESPERAR
        SendGlobalRPC(msg, matchEnded);

        // Si terminó, guardamos en Firebase AHORA
        if (matchEnded)
        {
            SaveToFirebase(winningTeam == 0 ? "Maya" : "Español");
        }

        StartCoroutine(GoToResultsScene(3f));
    }

    private void SendGlobalRPC(string msg, bool isFinal)
    {
        foreach(var p in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(p, out var obj))
            {
                var pd = obj.GetComponent<PlayerDataNetworked>();
                if (pd != null) pd.RPC_UpdateMatchResults(msg, isFinal);
            }
        }
    }

    private void SaveToFirebase(string winnerTeam)
    {
        // Buscar nicknames
        string winnerNick = "CPU", loserNick = "CPU";
        // Lógica simplificada de búsqueda de nombres...
        // ... (Usa tu lógica actual de MatchHistoryLogger) ...
        
        string loserTeam = (winnerTeam == "Maya") ? "Español" : "Maya";
        MatchHistoryLogger.SaveMatch(winnerTeam, loserTeam, winnerNick, loserNick, 20, 0, _mapsPlayedHistory);
    }

    IEnumerator GoToResultsScene(float delay)
    {
        yield return new WaitForSeconds(delay); // Esperar que el mensaje se lea
        _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{resultsSceneName}.unity")));
    }

    // --- PROCESO EN PANTALLA DE RESULTADOS ---
    IEnumerator ProcessMatchResultsLogic(float delay)
    {
        yield return new WaitForSeconds(delay);

        bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length);

        if (matchEnded)
        {
            // ORDENAR TRANSICIÓN A VIDEO
            string wTeam = (_mayaWins > _spanishWins) ? "Maya" : "Español";
            Debug.Log($"🎬 Ordenando transición a video. Ganó: {wTeam}");
            
            // Enviar RPC a todos para que carguen su video correspondiente
            foreach(var p in _runner.ActivePlayers)
            {
                if (_runner.TryGetPlayerObject(p, out var obj))
                {
                    var pd = obj.GetComponent<PlayerDataNetworked>();
                    if (pd != null)
                    {
                        // Determinamos qué video le toca a cada uno
                        int pTeam = _playerTeams[p];
                        bool pWon = (pTeam == 0 && wTeam == "Maya") || (pTeam == 1 && wTeam == "Español");
                        
                        string videoScene = "";
                        if (pTeam == 0) videoScene = pWon ? videoGanaMaya : videoPierdeMaya;
                        else videoScene = pWon ? videoGanaSpanish : videoPierdeSpanish;

                        pd.RPC_GoToVideoScene(videoScene);
                    }
                }
            }
        }
        else
        {
            // SIGUIENTE MAPA
            _currentMapIndex++;
            LoadCurrentMap();
        }
    }

    // Ejecutado localmente por el RPC
    public void ExecuteVideoTransition()
    {
        StartCoroutine(DisconnectAndLoadVideo());
    }

    IEnumerator DisconnectAndLoadVideo()
    {
        string sceneToLoad = SessionManager.Instance.VideoSceneToLoad;
        Debug.Log($"👋 Desconectando Fusion... Próxima parada: {sceneToLoad}");
        
        // Desconexión limpia
        if (_runner != null) _runner.Shutdown();
        
        yield return new WaitForSeconds(1.0f); // Esperar desconexión
        
        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
        else
            SceneManager.LoadScene(menuSceneName);
    }

    // Callbacks vacíos...
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { _playerTeams.Remove(player); _playerCharacters.Remove(player); }
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