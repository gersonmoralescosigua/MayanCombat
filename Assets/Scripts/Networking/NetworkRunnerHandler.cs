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

    private bool _isConnecting = false;
    private Coroutine selectionTimer;

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

    // 🔹 INICIO DE MATCHMAKING
    public async void StartMatchmaking()
    {
        if (_isConnecting) return;
        _isConnecting = true;
        Debug.Log("🔗 Buscando o creando sesión en Fusion...");

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "MayanCombatRoom",
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError($"❌ Error al conectar: {result.ShutdownReason}");
            _isConnecting = false;
            return;
        }

        Debug.Log("✅ Conectado a Fusion. Esperando jugadores...");
        _isConnecting = false;
    }

    // 🔹 CUANDO UN JUGADOR ENTRA
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");

        if (runner.IsServer)
        {
            int connected = runner.ActivePlayers.Count();
            Debug.Log($"🧩 Jugadores conectados: {connected}/{maxPlayers}");

            // Asigna equipos (0 = Español, 1 = Maya)
            int team = (connected % 2 == 1) ? 0 : 1;
            AssignTeamRPC(player, team);

            // Cuando estén los 2, carga CharacterSelectWrapper
            if (connected == maxPlayers)
            {
                Debug.Log("✅ Ambos jugadores conectados. Cargando CharacterSelectWrapper...");
                runner.LoadScene(characterSelectScene);
            }
        }
    }

    // 🔹 RPC: Asignar equipo a jugador
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void AssignTeamRPC(PlayerRef target, int team, RpcInfo info = default)
    {
        if (Runner.LocalPlayer == target)
        {
            SessionManager.Instance?.SetTeam(team);
            Debug.Log($"[NetworkRunnerHandler] 🎯 Asignado equipo {(team == 0 ? "Español" : "Maya")} al jugador local");
        }
    }

    // 🔹 CUANDO SE CARGA UNA ESCENA
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"✅ Escena cargada (Fusion): {sceneName}");

        if (sceneName == characterSelectScene)
        {
            if (runner.IsServer)
            {
                Debug.Log("⏳ Dando 10 segundos para seleccionar personaje...");
                if (selectionTimer != null) StopCoroutine(selectionTimer);
                selectionTimer = StartCoroutine(SelectionCountdown());
            }
        }

        if (sceneName == mapSceneName)
        {
            foreach (var p in runner.ActivePlayers)
                SpawnPlayer(runner, p);
        }
    }

    // 🔹 TEMPORIZADOR DE 10 SEGUNDOS PARA ELECCIÓN
    private IEnumerator SelectionCountdown()
    {
        yield return new WaitForSeconds(10f);
        Debug.Log("⏰ Tiempo de selección terminado. Asignando personajes automáticamente...");
        AutoAssignCharacters();
        TryStartGame();
    }

    // 🔹 AUTO ASIGNACIÓN SI NADIE ELIGE
    private void AutoAssignCharacters()
    {
        foreach (var player in _runner.ActivePlayers)
        {
            if (!SelectedCharacters.ContainsKey(player))
            {
                int autoChar = SessionManager.Instance != null && SessionManager.Instance.currentTeam == 0 ? 0 : 1;
                SelectedCharacters[player] = autoChar;
                Debug.Log($"⚙️ Auto-asignado personaje {autoChar} para {player}");
            }
        }
    }

    // 🔹 REGISTRA LA ELECCIÓN DE PERSONAJE
    public void SetPlayerCharacter(PlayerRef player, int characterId)
    {
        if (!SelectedCharacters.ContainsKey(player))
            SelectedCharacters.Add(player, characterId);
        else
            SelectedCharacters[player] = characterId;

        Debug.Log($"✅ Player {player} eligió personaje {characterId}");
    }

    // 🔹 INICIA EL JUEGO
    public void TryStartGame()
    {
        if (SelectedCharacters.Count < maxPlayers)
        {
            Debug.Log("⌛ Esperando a que ambos elijan personaje...");
            return;
        }

        if (_runner.IsServer)
        {
            Debug.Log($"🗺 Cargando mapa {mapSceneName}...");
            _runner.LoadScene(mapSceneName);
        }
    }

    // 🔹 SPAWN DE PREFABS
    public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (!SelectedCharacters.ContainsKey(player))
        {
            Debug.LogError("❌ Player no ha seleccionado personaje.");
            return;
        }

        int charID = SelectedCharacters[player];
        string prefabPath = $"Prefabs/Characters/pf_{(charID == 0 ? "beatriz" : "ixquic")}";

        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"❌ Prefab no encontrado en {prefabPath}");
            return;
        }

        Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-2f, 2f), 1f, 0f);
        runner.Spawn(prefab, spawnPos, Quaternion.identity, player);
        Debug.Log($"✅ Spawn player {player} ({(charID == 0 ? "Beatriz" : "Ixquic")})");
    }

    // 🔹 CIERRA SESIÓN
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

    // ---------- CALLBACKS ----------
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"🚪 Player left: {player}");
        if (SelectedCharacters.ContainsKey(player))
            SelectedCharacters.Remove(player);
    }

    public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("🌐 Connected to server");
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) => Debug.LogWarning($"❌ Disconnected: {reason}");
    public void OnSceneLoadStart(NetworkRunner runner) => Debug.Log("📥 Fusion comenzó a cargar una escena...");
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.move.x = Input.GetAxis("Horizontal");
        data.move.y = Input.GetAxis("Vertical");
        data.jumpPressed = Input.GetKey(KeyCode.Space);
        data.attackPressed = Input.GetKey(KeyCode.J);
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
