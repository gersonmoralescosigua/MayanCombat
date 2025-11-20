using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    [Networked] public int TeamID { get; set; } = -1;
    [Networked] public int CharacterID { get; set; } = -1;
    [Networked] public NetworkString<_16> PlayerName { get; set; }

    private int _lastTeamID = -1;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            string myNick = SessionManager.Instance != null ? SessionManager.Instance.playerNickname : "Jugador";
            RPC_SetNickname(myNick);
        }
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            if (TeamID != _lastTeamID && TeamID != -1)
            {
                _lastTeamID = TeamID;
                SessionManager.Instance?.SetTeam(TeamID);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(string name) => PlayerName = name;

    // --- RPC 1: FINAL DE RONDA O PARTIDA (Actualiza textos) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateMatchResults(string message, bool isFinal)
    {
        Debug.Log($"📩 RPC Recibido: {message}");
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = message;
            SessionManager.Instance.IsFinalMatch = isFinal;
        }
    }

    // --- RPC 2: ORDEN DE IR A VIDEO (Desconectar) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GoToVideoScene(string sceneName)
    {
        Debug.Log($"🎬 Orden recibida: Ir a video {sceneName}");
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.VideoSceneToLoad = sceneName;
        }
        
        // Llamamos al Handler local para que ejecute la desconexión
        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.ExecuteVideoTransition();
        }
    }
}