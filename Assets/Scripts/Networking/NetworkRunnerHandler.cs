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

    // LISTA DE MAPAS EN ORDEN DE RONDAS
    public string[] mapRotation = new string[] { 
        "Map_Tikal_Base", 
        "Map_Atitlan_Base", 
        "Map_Volcan_Base" 
    };

    [Header("Matchmaking")]
    public int maxPlayers = 2;
    public NetworkObject playerDataPrefab; 

    // --- ESTADO DEL TORNEO (Solo vive en el Servidor) ---
    private int _mayaWins = 0;
    private int _spanishWins = 0;
    private int _currentMapIndex = 0; // 0 = Tikal, 1 = Atitlan, 2 = Volcan
    private List<string> _mapsPlayedHistory = new List<string>();

    // Diccionarios
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

    // --- MATCHMAKING (Igual que antes) ---
    public void StartMatchmaking()
    {
        // RESETEAMOS EL TORNEO AL EMPEZAR A BUSCAR
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

    // --- JUGADORES & EQUIPOS ---
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

        // Asignar
        _playerTeams[players[0]] = 0; _playerCharacters[players[0]] = 0; // Maya
        _playerTeams[players[1]] = 1; _playerCharacters[players[1]] = 1; // Español
        
        // Sincronizar UI
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
        
        // Si estamos en Loading, esperamos y lanzamos el mapa actual
        if (currentScene == loadingSceneName && runner.IsServer)
        {
            StartCoroutine(AutoStartRound(8f));
        }

        // Si estamos en un mapa, spawneamos
        if (IsMapScene(currentScene) && runner.IsServer)
        {
            foreach (var p in runner.ActivePlayers) SpawnPlayer(runner, p);
            
            // Registramos el mapa en el historial
            if (!_mapsPlayedHistory.Contains(currentScene)) _mapsPlayedHistory.Add(currentScene);
        }
    }

    private bool IsMapScene(string sceneName)
    {
        return mapRotation.Contains(sceneName);
    }

    private IEnumerator AutoStartRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Cargamos el mapa según el índice actual (0, 1 o 2)
        string mapToLoad = mapRotation[_currentMapIndex];
        Debug.Log($"🗺️ Cargando Ronda {_currentMapIndex + 1}: {mapToLoad}");
        
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{(_currentMapIndex == 0 ? "Tikal" : (_currentMapIndex == 1 ? "Atitlan" : "Volcan"))}/{mapToLoad}.unity");
        
        // Fallback por si las carpetas son diferentes, intenta buscar solo por nombre si falla la ruta exacta
        if (sceneIndex < 0)
        {
             Debug.LogWarning("⚠️ Ruta exacta falló, buscando por nombre...");
             // Nota: Esto requiere que las escenas estén en Build Settings
             _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(mapToLoad))); 
        }
        else
        {
            _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
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

    // --- LÓGICA DE MUERTE Y PUNTUACIÓN ---
    public void OnPlayerFellToDeath(GameObject deadPlayerObj)
    {
        if (!_runner.IsServer) return;

        NetworkObject netObj = deadPlayerObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        PlayerRef deadPlayerRef = netObj.InputAuthority;
        int losingTeam = -1;
        if (_playerTeams.ContainsKey(deadPlayerRef)) losingTeam = _playerTeams[deadPlayerRef];

        // 0=Maya, 1=Español
        int winningTeam = (losingTeam == 0) ? 1 : 0;

        // --- ACTUALIZAR PUNTUACIÓN ---
        if (winningTeam == 0) _mayaWins++;
        else _spanishWins++;

        Debug.Log($"💀 Ronda terminada. Ganador Ronda: {winningTeam}. Score: Maya {_mayaWins} - {_spanishWins} Español");

        // --- VERIFICAR CONDICIÓN DE VICTORIA ---
        bool matchEnded = false;
        string finalWinnerName = "";
        
        // Gana el mejor de 3 (quien llegue a 2)
        if (_mayaWins >= 2)
        {
            matchEnded = true;
            finalWinnerName = "IMPERIO MAYA";
        }
        else if (_spanishWins >= 2)
        {
            matchEnded = true;
            finalWinnerName = "ESPAÑOLES";
        }
        else if (_currentMapIndex >= mapRotation.Length - 1)
        {
            // Se acabaron los mapas (empate técnico o gana por puntos)
            matchEnded = true;
            if (_mayaWins > _spanishWins) finalWinnerName = "IMPERIO MAYA";
            else if (_spanishWins > _mayaWins) finalWinnerName = "ESPAÑOLES";
            else finalWinnerName = "EMPATE";
        }

        // --- ENVIAR MENSAJE A TODOS ---
        foreach(var player in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(player, out var pObj))
            {
                var dataScript = pObj.GetComponent<PlayerDataNetworked>();
                if (dataScript != null)
                {
                    // Llamar RPC con info detallada
                    dataScript.RPC_RoundFinished(winningTeam, _mayaWins, _spanishWins, matchEnded, finalWinnerName);
                }
            }
        }

        // --- GUARDAR EN FIREBASE SI TERMINÓ ---
        if (matchEnded)
        {
            string w = (_mayaWins > _spanishWins) ? "Maya" : "Español";
            string l = (w == "Maya") ? "Español" : "Maya";
            MatchHistoryLogger.SaveMatch(w, l, Mathf.Max(_mayaWins, _spanishWins), Mathf.Min(_mayaWins, _spanishWins), _mapsPlayedHistory);
        }

        StartCoroutine(FinishRoundRoutine(matchEnded));
    }

    IEnumerator FinishRoundRoutine(bool matchEnded)
    {
        yield return new WaitForSeconds(3.0f); // Esperar para leer mensaje

        if (matchEnded)
        {
            // Si terminó el juego, cargamos MatchResults pero configurado para FIN
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{resultsSceneName}.unity");
            if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));
            
            // Después de esto, el MatchResultsUI mandará al Menu
        }
        else
        {
            // Si NO terminó, cargamos MatchResults intermedio
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/UI/{resultsSceneName}.unity");
            if (sceneIndex >= 0) _runner.LoadScene(SceneRef.FromIndex(sceneIndex));

            // Y preparamos el siguiente mapa
            _currentMapIndex++; // Avanzar al siguiente mapa (Atitlan o Volcan)
            StartCoroutine(GoToNextMapDelay(8f)); // Esperar en pantalla de resultados 8s
        }
    }

    IEnumerator GoToNextMapDelay(float delay)
    {
        // Esperamos en la pantalla de resultados
        yield return new WaitForSeconds(delay);
        
        // Cargamos el siguiente mapa
        string nextMap = mapRotation[_currentMapIndex];
        Debug.Log($"🔜 Avanzando al mapa: {nextMap}");
        
        // Lógica de carga robusta
        int buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{(_currentMapIndex == 1 ? "Atitlan" : "Volcan")}/{nextMap}.unity");
        if (buildIndex < 0) 
        {
             // Fallback búsqueda por nombre
             _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(nextMap)));
        }
        else
        {
             _runner.LoadScene(SceneRef.FromIndex(buildIndex));
        }
    }

    // Callbacks vacíos requeridos
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