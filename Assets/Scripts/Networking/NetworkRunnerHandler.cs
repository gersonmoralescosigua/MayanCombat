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

    [Header("Nombres EXACTOS de las Escenas (Tal cual en Build Settings)")]
    public string matchmakingSceneName = "Matchmaking";
    public string loadingSceneName = "LoadingAssignment";
    public string resultsSceneName = "MatchResults";
    public string winnersSceneName = "Winners";
    public string menuSceneName = "Menu";

    // Asegúrate de que estos nombres coincidan con tus escenas de mapas
    public string[] mapRotation = new string[] { "Map_Tikal_Base", "Map_Atitlan_Base", "Map_Volcan_Base" };

    [Header("Configuración")]
    public int maxPlayers = 2;
    public NetworkObject playerDataPrefab;

    // Estado interno
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

        if (availableSession != null)
            await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Client, SessionName = availableSession.Name, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
        else
            await runner.StartGame(new StartGameArgs() { GameMode = GameMode.Host, SessionName = System.Guid.NewGuid().ToString(), PlayerCount = maxPlayers, SceneManager = sceneManager, Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex) });
    }

    // --- CONTROL DE JUGADORES ---
    public void RegisterPlayerName(PlayerRef player, string nickname)
    {
        if (_playerNames.ContainsKey(player)) _playerNames[player] = nickname;
        else _playerNames.Add(player, nickname);
    }

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
        SafeLoadScene(loadingSceneName); // USAMOS LA FUNCIÓN SEGURA
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

    // --- LÓGICA DE MUERTE Y PUNTUACIÓN ---
    public void OnPlayerFellToDeath(GameObject deadPlayerObj)
    {
        if (!_runner.IsServer || !_roundIsActive) return;

        // Bloqueamos inmediatamente para que si el otro cae 0.1s después, no cuente doble
        _roundIsActive = false;

        NetworkObject netObj = deadPlayerObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        int losingTeam = _playerTeams.ContainsKey(netObj.InputAuthority) ? _playerTeams[netObj.InputAuthority] : -1;
        int winningTeam = (losingTeam == 0) ? 1 : 0; // Si pierde 0, gana 1. Si pierde 1, gana 0.

        if (winningTeam == 0) _mayaWins++; else _spanishWins++;

        // Analizamos estado del torneo
        bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length - 1);
        string winnerName = (_mayaWins > _spanishWins) ? "IMPERIO MAYA" : "ESPAÑOLES";

        // Mensaje para UI
        string msg = "";
        string roundWinner = (winningTeam == 0) ? "Maya" : "Español";

        if (matchEnded) msg = $"👑 ¡FIN DEL TORNEO!\n\nGanador Global: {winnerName}\nMarcador: Maya {_mayaWins} - {_spanishWins} Español";
        else msg = $"Ronda Terminada\nGanador Ronda: {roundWinner}\n\nGlobal: Maya {_mayaWins} - {_spanishWins} Español";

        Debug.Log($"📝 ENVIANDO RESULTADOS: {msg}");

        // 1. ENVIAR RPC (CRÍTICO: HACERLO ANTES DE CAMBIAR ESCENA)
        foreach (var p in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(p, out var obj))
            {
                var pd = obj.GetComponent<PlayerDataNetworked>();
                if (pd != null) pd.RPC_SetUIMessage(msg, matchEnded, matchEnded ? winningTeam : -1);
            }
        }

        // 2. FIREBASE
        if (matchEnded) SaveToFirebaseCorrectly(winningTeam == 0 ? "Maya" : "Español");
        else _currentMapIndex++;

        // 3. CAMBIO DE ESCENA SEGURO
        if (matchEnded) StartCoroutine(LoadSceneDelayed(winnersSceneName, 3f));
        else StartCoroutine(LoadSceneDelayed(resultsSceneName, 3f));
    }

    // --- NUEVO SISTEMA DE CARGA DE ESCENAS (EVITA EL ERROR ROJO) ---

    private void SafeLoadScene(string sceneName)
    {
        if (!_runner.IsServer) return;

        int buildIndex = GetSceneIndex(sceneName);
        if (buildIndex != -1)
        {
            Debug.Log($"🔄 Cargando escena: {sceneName} (Index: {buildIndex})");
            _runner.LoadScene(SceneRef.FromIndex(buildIndex));
        }
        else
        {
            Debug.LogError($"⛔ ERROR FATAL: La escena '{sceneName}' no está en Build Settings o está mal escrito el nombre. El juego no puede continuar.");
        }
    }

    private int GetSceneIndex(string sceneName)
    {
        // Intentamos buscar por ruta (lo más seguro si está en build settings)
        int index = SceneUtility.GetBuildIndexByScenePath(sceneName);

        // Si devuelve -1, intentamos buscar si está en alguna subcarpeta común
        if (index == -1) index = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{sceneName}.unity");
        if (index == -1) index = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{GetFolder(sceneName)}/{sceneName}.unity");

        return index;
    }

    IEnumerator LoadSceneDelayed(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SafeLoadScene(sceneName);
    }

    // --- CONTROL DE FLUJO AL LLEGAR A UNA ESCENA ---
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"📍 Escena cargada: {currentScene}");

        if (currentScene == loadingSceneName)
        {
            StartCoroutine(AutoStartRound(5f));
        }
        else if (mapRotation.Contains(currentScene))
        {
            _roundIsActive = true;
            foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
            if (!_mapsPlayedHistory.Contains(currentScene)) _mapsPlayedHistory.Add(currentScene);
        }
        else if (currentScene == winnersSceneName)
        {
            // Esperar 10s de video y luego ir a resultados finales
            StartCoroutine(WaitVideoAndLoadFinalResults(10f));
        }
        else if (currentScene == resultsSceneName)
        {
            // Si llegamos a resultados, verificamos si hay que seguir jugando
            bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length);

            if (!matchEnded)
            {
                // Si NO terminó, cargar siguiente mapa en 5s
                StartCoroutine(AutoLoadNextMap(5f));
            }
        }
    }

    IEnumerator AutoStartRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadCurrentMap();
    }

    IEnumerator AutoLoadNextMap(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadCurrentMap();
    }

    IEnumerator WaitVideoAndLoadFinalResults(float delay)
    {
        yield return new WaitForSeconds(delay);
        SafeLoadScene(resultsSceneName);
    }

    private void LoadCurrentMap()
    {
        if (_currentMapIndex < mapRotation.Length)
        {
            string mapToLoad = mapRotation[_currentMapIndex];
            SafeLoadScene(mapToLoad);
        }
    }

    private string GetFolder(string map) { if (map.Contains("Tikal")) return "Tikal"; if (map.Contains("Atitlan")) return "Atitlan"; return "Volcan"; }

    // --- UTILS ---
    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int team = _playerTeams.ContainsKey(player) ? _playerTeams[player] : player.RawEncoded % 2;
        string prefabPath = $"Prefabs/Characters/pf_{(team == 0 ? "ixquic" : "beatriz")}";
        Vector3 spawnPos = (team == 0) ? new Vector3(-2, 2, 0) : new Vector3(2, 2, 0);

        // Intentar buscar punto de spawn
        GameObject pt = GameObject.Find(team == 0 ? "Spawn_Maya" : "Spawn_Spanish");
        if (pt != null) spawnPos = pt.transform.position;

        runner.Spawn(Resources.Load<NetworkObject>(prefabPath), spawnPos, Quaternion.identity, player);
    }

    private void SaveToFirebaseCorrectly(string winnerTeam)
    {
        string winnerNick = "Desconocido", loserNick = "Desconocido";
        foreach (var kvp in _playerTeams)
        {
            string name = _playerNames.ContainsKey(kvp.Key) ? _playerNames[kvp.Key] : "SinNombre";
            bool pIsWinner = (winnerTeam == "Maya" && kvp.Value == 0) || (winnerTeam == "Español" && kvp.Value == 1);
            if (pIsWinner) winnerNick = name; else loserNick = name;
        }
        string loserTeam = (winnerTeam == "Maya") ? "Español" : "Maya";
        MatchHistoryLogger.SaveMatch(winnerTeam, loserTeam, winnerNick, loserNick, 20, 0, _mapsPlayedHistory);
    }

    public void ShutdownAndMenu()
    {
        StartCoroutine(ShutdownRoutine());
    }

    IEnumerator ShutdownRoutine()
    {
        if (_runner != null) _runner.Shutdown();
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(menuSceneName);
    }

    // Callbacks vacíos...
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { _playerTeams.Remove(player); _playerNames.Remove(player); }
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