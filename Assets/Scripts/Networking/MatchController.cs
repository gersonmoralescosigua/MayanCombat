using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class MatchController : NetworkBehaviour
{
    // Mapea PlayerRef -> team (0 = Maya, 1 = Espanol)
    private Dictionary<PlayerRef, int> teams = new Dictionary<PlayerRef, int>();
    // Mapea PlayerRef -> characterId (sprite id or index)
    private Dictionary<PlayerRef, int> selections = new Dictionary<PlayerRef, int>();

    // Numero de jugadores que queremos reunir (2)
    public int requiredPlayers = 2;

    // ID de la escena (usa nombre y Build Settings)
    public string mapSceneName = "Map_Tikal_Base";

    // Called on server/host when a player joins: we'll track them from NetworkRunnerHandler by calling RegisterPlayer
    public void RegisterPlayer(PlayerRef player)
    {
        if (!teams.ContainsKey(player))
        {
            teams[player] = -1; // no asignado aún
            selections[player] = -1;
            Debug.Log($"[MatchController] Registrado jugador {player}");
        }

        TryStartAssignment();
    }

    void TryStartAssignment()
    {
        if (!Runner.IsServer) return;

        if (teams.Count >= requiredPlayers && teams.Values.All(v => v == -1))
        {
            // asigna equipos aleatoriamente entre los jugadores registrados
            var players = teams.Keys.ToList();
            // shuffle
            for (int i = 0; i < players.Count; i++)
            {
                int j = Random.Range(i, players.Count);
                var tmp = players[i];
                players[i] = players[j];
                players[j] = tmp;
            }

            // asigna 0 (Maya) al primero y 1 (Español) al segundo (para 2 players)
            for (int i = 0; i < players.Count; i++)
            {
                int team = (i % 2 == 0) ? 0 : 1;
                teams[players[i]] = team;
                // notifica al jugador su equipo (RPC targetado)
                RPC_AssignTeam(players[i], team);
            }
        }
    }

    // RPC: se ejecuta en el target client para decirle su equipo (0=Maya,1=Español)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_AssignTeam(PlayerRef target, int team, RpcInfo info = default)
    {
        if (Object.HasInputAuthority) // solo ejecuta en el jugador correspondiente
        {
            Debug.Log($"[RPC] Asignado team {team} en cliente local.");
            SessionManager.Instance?.SetTeam(team);

            if (team == 0)
                SceneManager.LoadScene("CharacterSelectMaya");
            else
                SceneManager.LoadScene("CharacterSelectEspañoles");
        }
    }

    // RPC desde cliente al host cuando selecciona personaje (characterId = int identificador)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_PlayerSelected(int characterId, RpcInfo info = default)
    {
        PlayerRef sender = info.Source;
        if (!Runner.IsServer) return;

        Debug.Log($"[MatchController] Host recibió selección del {sender}: char {characterId}");
        if (selections.ContainsKey(sender))
            selections[sender] = characterId;
        else
            selections.Add(sender, characterId);

        TryStartGameIfReady();
    }

    void TryStartGameIfReady()
    {
        if (!Runner.IsServer) return;

        if (selections.Count >= requiredPlayers && selections.Values.All(v => v >= 0))
        {
            Debug.Log("[MatchController] Todos seleccionaron. Lanzando mapa...");

            var sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/Maps/{mapSceneName}.unity");
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex >= 0 ? sceneIndex : 0), LoadSceneMode.Single);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!Runner.IsServer) return;

        foreach (var kv in selections)
        {
            PlayerRef p = kv.Key;
            int charId = kv.Value;

            Vector3 spawnPos = new Vector3(Random.Range(-2, 2), 1, Random.Range(-2, 2));
            string prefabPath = charId == 0
                ? "Prefabs/Characters/pf_ixquic"
                : "Prefabs/Characters/pf_beatriz";

            var prefab = Resources.Load<GameObject>(prefabPath);

            var spawned = Runner.Spawn(prefab, spawnPos, Quaternion.identity, p);

            var pn = spawned.GetComponent<PlayerNetwork>();
            if (pn != null)
            {
                pn.SetCharacterId_Server(charId);
            }
        }
    }
}
