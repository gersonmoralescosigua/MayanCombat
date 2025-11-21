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

    [Header("Nombres EXACTOS de las Escenas")]
    public string matchmakingSceneName = "Matchmaking";
    public string loadingSceneName = "LoadingAssignment";
    public string resultsSceneName = "MatchResults";
    public string winnersSceneName = "Winners";
    public string menuSceneName = "Menu";

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
        SafeLoadScene(loadingSceneName);
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

    // --- SOLUCIÓN DEFINITIVA AQUÍ ---
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

        string msg = "";
        string roundWinner = (winningTeam == 0) ? "Maya" : "Español";

        if (matchEnded) msg = $"👑 ¡FIN DEL TORNEO!\n\nGanador Global: {winnerName}\nMarcador: Maya {_mayaWins} - {_spanishWins} Español";
        else msg = $"Ronda Terminada\nGanador Ronda: {roundWinner}\n\nGlobal: Maya {_mayaWins} - {_spanishWins} Español";

        Debug.Log($"📝 DATA ACTUALIZADA: {msg}. GanadorID: {winningTeam}");

        // --- 0. ACTUALIZACIÓN INMEDIATA LOCAL (ESTO ES LO QUE FALTABA) ---
        // El Host actualiza su propio SessionManager AQUI MISMO. 
        // No espera al RPC.
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = msg;
            SessionManager.Instance.IsFinalMatch = matchEnded;
            // Si terminó el match, guardamos el ganador FINAL. Si no, da igual (-1).
            if (matchEnded) SessionManager.Instance.FinalWinnerTeam = winningTeam;
        }

        // 1. ENVIAR EL MENSAJE (RPC) PARA LOS CLIENTES
        foreach (var p in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(p, out var obj))
            {
                var pd = obj.GetComponent<PlayerDataNetworked>();
                // Enviamos mensaje a clientes.
                if (pd != null) pd.RPC_SetUIMessage(msg, matchEnded, matchEnded ? winningTeam : -1);
            }
        }

        // 2. GUARDAR EN FIREBASE
        if (matchEnded) SaveToFirebaseCorrectly(winningTeam == 0 ? "Maya" : "Español");
        else _currentMapIndex++;

        // 3. ESPERAR (Para que los clientes reciban el RPC antes del cambio de escena)
        StartCoroutine(WaitAndSwitchScene(matchEnded));
    }

    IEnumerator WaitAndSwitchScene(bool matchEnded)
    {
        // Esperamos 1.5 segundos.
        // El Host YA tiene los datos (paso 0), así que el video le funcionará seguro.
        // Los Clientes aprovecharán este tiempo para recibir el RPC.
        yield return new WaitForSeconds(1.5f);

        if (matchEnded) SafeLoadScene(winnersSceneName);
        else SafeLoadScene(resultsSceneName);
    }

    // --- CARGA SEGURA ---
    private void SafeLoadScene(string sceneName)
    {
        if (!_runner.IsServer) return;
        int buildIndex = GetSceneIndex(sceneName);
        if (buildIndex != -1) _runner.LoadScene(SceneRef.FromIndex(buildIndex));
        else Debug.LogError($"⛔ ERROR CRÍTICO: La escena '{sceneName}' NO está en el Build Settings.");
    }

    private int GetSceneIndex(string sceneName)
    {
        int index = SceneUtility.GetBuildIndexByScenePath(sceneName);
        if (index == -1) index = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{sceneName}.unity");
        if (index == -1) index = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{GetFolder(sceneName)}/{sceneName}.unity");
        return index;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == loadingSceneName) StartCoroutine(AutoStartRound(5f));
        else if (mapRotation.Contains(currentScene))
        {
            _roundIsActive = true;
            foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
            if (!_mapsPlayedHistory.Contains(currentScene)) _mapsPlayedHistory.Add(currentScene);
        }
        else if (currentScene == winnersSceneName) StartCoroutine(WaitVideoAndLoadFinalResults(12f));
        else if (currentScene == resultsSceneName)
        {
            bool matchEnded = (_mayaWins >= 2 || _spanishWins >= 2 || _currentMapIndex >= mapRotation.Length);
            if (!matchEnded) StartCoroutine(AutoLoadNextMap(5f));
        }
    }

    IEnumerator AutoStartRound(float delay) { yield return new WaitForSeconds(delay); LoadCurrentMap(); }
    IEnumerator AutoLoadNextMap(float delay) { yield return new WaitForSeconds(delay); LoadCurrentMap(); }
    IEnumerator WaitVideoAndLoadFinalResults(float delay) { yield return new WaitForSeconds(delay); SafeLoadScene(resultsSceneName); }

    private void LoadCurrentMap()
    {
        if (_currentMapIndex < mapRotation.Length) SafeLoadScene(mapRotation[_currentMapIndex]);
    }

    private string GetFolder(string map) { if (map.Contains("Tikal")) return "Tikal"; if (map.Contains("Atitlan")) return "Atitlan"; return "Volcan"; }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        int team = _playerTeams.ContainsKey(player) ? _playerTeams[player] : player.RawEncoded % 2;
        string prefabPath = $"Prefabs/Characters/pf_{(team == 0 ? "ixquic" : "beatriz")}";
        Vector3 spawnPos = (team == 0) ? new Vector3(-2, 2, 0) : new Vector3(2, 2, 0);
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

    public void ShutdownAndMenu() { StartCoroutine(ShutdownRoutine()); }
    IEnumerator ShutdownRoutine() { if (_runner != null) _runner.Shutdown(); yield return new WaitForSeconds(1f); SceneManager.LoadScene(menuSceneName); }

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