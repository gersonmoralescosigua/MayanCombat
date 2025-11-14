using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetworkRunnerHandler — Matchmaking robusto con escaneo de sesiones seguro.
/// - Usa un runner temporal aislado (TempSessionScanner) para pedir lista de sesiones.
/// - Luego inicia el runner "real" en este GameObject (si hay sala disponible se une, si no la crea).
/// - Evita DisconnectByClientLogic causado por mezclar callbacks entre runners.
/// </summary>
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance;

    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    // Lista actualizada por TempSessionScanner
    private List<SessionInfo> _latestSessions = null;

    [Header("Scenes")]
    public string loadingSceneName = "LoadingAssignment";
    public string mapSceneName = "Map_Tikal_Base";

    [Header("Matchmaking")]
    public int maxPlayers = 2;

    [Header("Character Prefabs (Inspector)")]
    public GameObject pfBeatriz;
    public GameObject pfIxquic;

    private readonly Dictionary<PlayerRef, int> _teams = new();
    private readonly Dictionary<PlayerRef, int> _characters = new();

    private Coroutine _autoStartTimer;

    // Protección para no arrancar matchmaking dos veces
    private bool _isMatchmaking = false;

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

    // ---------------------------------------------------------
    // START MATCHMAKING (seguro)
    // ---------------------------------------------------------
    public async void StartMatchmaking()
    {
        if (_isMatchmaking)
        {
            Debug.LogWarning("[NetworkRunnerHandler] Matchmaking ya en progreso.");
            return;
        }

        _isMatchmaking = true;
        Debug.Log("[NetworkRunnerHandler] Buscando sesiones existentes...");

        // ---------------------------------------------------------
        // 1) Runner temporal (en GameObject separado) con scanner aislado
        // ---------------------------------------------------------
        GameObject tempGO = null;
        NetworkRunner tempRunner = null;
        NetworkSceneManagerDefault tempSceneManager = null;
        TempSessionScanner scanner = null;

        try
        {
            tempGO = new GameObject("TempRunnerScanner");
            // Asegurar que no se destruya inmediatamente por alguna otra cosa
            DontDestroyOnLoad(tempGO);

            tempRunner = tempGO.AddComponent<NetworkRunner>();
            tempRunner.ProvideInput = false; // no necesitamos input en el escaneo

            // Añadimos un componente scanner que implementa INetworkRunnerCallbacks pero SOLO
            // para OnSessionListUpdated (aislado del handler principal).
            scanner = tempGO.AddComponent<TempSessionScanner>();
            scanner.Init(this); // le pasamos referencia al handler para que escriba _latestSessions

            tempRunner.AddCallbacks(scanner);

            tempSceneManager = tempGO.AddComponent<NetworkSceneManagerDefault>();

            StartGameArgs tempArgs = new StartGameArgs()
            {
                GameMode = GameMode.Shared, // Shared mode permite obtener lista
                SessionName = "TEMP_SCAN",
                SceneManager = tempSceneManager
            };

            var tempResult = await tempRunner.StartGame(tempArgs);

            if (!tempResult.Ok)
            {
                Debug.LogWarning($"[NetworkRunnerHandler] Falló tempRunner.StartGame(): {tempResult.ShutdownReason}. Continuamos y crearemos host si es necesario.");
            }
            else
            {
                // Esperamos a que el scanner llene _latestSessions (con timeout)
                float timeout = 2.5f;
                float pollInterval = 0.1f;
                while (_latestSessions == null && timeout > 0f)
                {
                    await Task.Delay((int)(pollInterval * 1000));
                    timeout -= pollInterval;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[NetworkRunnerHandler] Error durante escaneo: {ex.Message}");
        }
        finally
        {
            // Shutdown y destruir el runner temporal de forma segura
            if (tempRunner != null)
            {
                try
                {
                    // Shutdown devuelve una tarea; await aquí es correcto dentro del finally async
                    await tempRunner.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[NetworkRunnerHandler] Error al cerrar tempRunner: " + ex.Message);
                }
            }

            if (tempGO != null)
            {
                Destroy(tempGO);
            }
        }

        // ---------------------------------------------------------
        // 2) Evaluar sesiones obtenidas
        // ---------------------------------------------------------
        var sessions = _latestSessions ?? new List<SessionInfo>();
        SessionInfo joinable = sessions.FirstOrDefault(s => s.PlayerCount < maxPlayers && s.IsOpen);

        // ---------------------------------------------------------
        // 3) Crear/usar runner "real" en este GameObject
        // ---------------------------------------------------------
        if (_runner == null)
        {
            // Si no existe, obtener componente (por si existe en inspector) o añadir
            _runner = gameObject.GetComponent<NetworkRunner>();
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }
        }
        else
        {
            // quitamos callbacks previos para evitar duplicados
            try { _runner.RemoveCallbacks(this); } catch { }
        }

        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        // Asegurar SceneManager para el runner real
        NetworkSceneManagerDefault realSceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (realSceneManager == null)
            realSceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // ---------------------------------------------------------
        // 4) Preparar StartGameArgs y unirse o crear
        // ---------------------------------------------------------
        StartGameArgs args = new StartGameArgs()
        {
            SceneManager = realSceneManager
        };

        if (joinable != null)
        {
            Debug.Log("[NetworkRunnerHandler] Sesión encontrada, entrando: " + joinable.Name);
            args.GameMode = GameMode.Client;
            args.SessionName = joinable.Name;
        }
        else
        {
            string newName = "Room_" + UnityEngine.Random.Range(1000, 9999);
            Debug.Log("[NetworkRunnerHandler] No hay sesiones disponibles. Creando: " + newName);
            args.GameMode = GameMode.Host;
            args.SessionName = newName;
        }

        // ---------------------------------------------------------
        // 5) Iniciar la sesión "real"
        // ---------------------------------------------------------
        var result = await _runner.StartGame(args);

        if (!result.Ok)
        {
            Debug.LogError("[NetworkRunnerHandler] Error al iniciar runner real: " + result.ShutdownReason);
            _isMatchmaking = false;
            return;
        }

        Debug.Log("[NetworkRunnerHandler] Runner real iniciado correctamente. SessionName: " + args.SessionName);

        // _runner ya está listo para usarse
        _isMatchmaking = false;
    }

    // ---------------------------------------------------------
    // PLAYER JOIN
    // ---------------------------------------------------------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Jugador conectado: " + player);

        // solo el servidor controla la asignación
        if (!runner.IsServer) return;

        int count = runner.ActivePlayers.Count();

        if (count == maxPlayers)
        {
            AssignTeams();
            runner.LoadScene(loadingSceneName);
        }
    }

    // ---------------------------------------------------------
    // Asigna equipos y personajes
    // ---------------------------------------------------------
    private void AssignTeams()
    {
        if (_runner == null)
        {
            Debug.LogWarning("AssignTeams: runner nulo");
            return;
        }

        var players = _runner.ActivePlayers.OrderBy(x => UnityEngine.Random.value).ToList();

        // seguridad: verificar que haya 2 players
        if (players.Count < 2)
        {
            Debug.LogWarning("AssignTeams: jugadores insuficientes.");
            return;
        }

        _teams[players[0]] = 0; // Español
        _teams[players[1]] = 1; // Maya

        _characters[players[0]] = 0; // Beatriz
        _characters[players[1]] = 1; // Ixquic

        foreach (var p in players)
            RPC_AssignRole(p, _teams[p], _characters[p]);

        Debug.Log("Equipos asignados correctamente.");
    }

    // ---------------------------------------------------------
    // RPC envía datos al cliente
    // ---------------------------------------------------------
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AssignRole(PlayerRef target, int team, int charId, RpcInfo info = default)
    {
        if (_runner != null && _runner.LocalPlayer == target)
        {
            SessionManager.Instance.SetTeam(team);
            PlayerPrefs.SetInt("AssignedCharacter", charId);
        }
    }

    // ---------------------------------------------------------
    // ESCENA CARGADA
    // ---------------------------------------------------------
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string scene = SceneManager.GetActiveScene().name;
        Debug.Log("Escena cargada: " + scene);

        if (scene == loadingSceneName && runner.IsServer)
        {
            _autoStartTimer = StartCoroutine(AutoStartAfterDelay(5f));
        }

        if (scene == mapSceneName)
        {
            foreach (var p in runner.ActivePlayers)
                SpawnPlayer(runner, p);
        }
    }

    private IEnumerator AutoStartAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_runner != null && _runner.IsRunning)
            _runner.LoadScene(mapSceneName);
    }

    // ---------------------------------------------------------
    // SPAWNEA JUGADOR
    // ---------------------------------------------------------
    private void SpawnPlayer(NetworkRunner runner, PlayerRef p)
    {
        if (!_teams.ContainsKey(p) || !_characters.ContainsKey(p))
        {
            Debug.LogWarning("SpawnPlayer: player no tiene equipo o personaje asignado.");
            return;
        }

        int team = _teams[p];
        int charId = _characters[p];

        GameObject prefab = charId == 0 ? pfBeatriz : pfIxquic;

        Vector3 pos = team == 0 ? new Vector3(-4, 1, 0) : new Vector3(4, 1, 0);

        runner.Spawn(prefab, pos, Quaternion.identity, p);
        Debug.Log($"Spawn: {prefab.name} en {pos}");
    }

    // ---------------------------------------------------------
    // CALLBACK: se actualiza la lista de sesiones recibida por Fusion
    // (este método puede ser llamado tanto por tempRunner->scanner como por runner real;
    //  scanner es quien pone la lista en _latestSessions durante el escaneo)
    // ---------------------------------------------------------
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("[NetworkRunnerHandler] Lista de sesiones recibida. Cantidad: " + sessionList.Count);
        _latestSessions = sessionList;
    }

    // ---------------------------------------------------------
    // Otros callbacks (implementaciones vacías o manejadas)
    // ---------------------------------------------------------
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _teams.Remove(player);
        _characters.Remove(player);
    }

    public void OnInput(NetworkRunner r, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.move.x = Input.GetAxis("Horizontal");
        data.move.y = Input.GetAxis("Vertical");
        data.jumpPressed = Input.GetKey(KeyCode.Space);
        data.attackPressed = Input.GetKey(KeyCode.J);
        input.Set(data);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // Logging útil para debugging
        Debug.Log($"[NetworkRunnerHandler] OnDisconnectedFromServer: {reason}");
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput data) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log($"[NetworkRunnerHandler] OnShutdown: {reason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remote, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"[NetworkRunnerHandler] OnConnectFailed: {reason}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr ptr) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef p, ReliableKey k, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef p, ReliableKey k, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }

    // ---------------------------------------------------------
    // Limpieza al destruir este handler: asegúrate de apagar runner
    // ---------------------------------------------------------
    private void OnDestroy()
    {
        if (_runner != null)
        {
            try
            {
                _runner.RemoveCallbacks(this);
                // Shutdown es async; no await en OnDestroy. Llamamos sin bloquear.
                if (_runner.IsRunning)
                {
                    _runner.Shutdown().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NetworkRunnerHandler] OnDestroy error: " + ex.Message);
            }
        }
    }

    // ---------------------------------------------------------
    // Clase auxiliar (componente) que solo escucha OnSessionListUpdated
    // para el runner temporal. Esto evita mezclar callbacks con el handler principal.
    // ---------------------------------------------------------
    private class TempSessionScanner : MonoBehaviour, INetworkRunnerCallbacks
    {
        private NetworkRunnerHandler _owner;

        public void Init(NetworkRunnerHandler owner)
        {
            _owner = owner;
        }

        // Solo usamos OnSessionListUpdated para pasar la lista al owner.
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log("[TempSessionScanner] Lista sesiones recibida: " + sessionList.Count);
            if (_owner != null)
                _owner._latestSessions = sessionList;
        }

        // Implementaciones vacías para evitar que Fusion intente llamar otras lógicas
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput data) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remote, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr ptr) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }   // ← FALTA ESTE
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef p, ReliableKey k, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef p, ReliableKey k, float progress) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject o, PlayerRef p) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject o, PlayerRef p) { }

    }
}