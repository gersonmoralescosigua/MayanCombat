using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    [Networked] public int TeamID { get; set; } = -1;
    [Networked] public int CharacterID { get; set; } = -1;
    [Networked] public NetworkString<_16> PlayerName { get; set; } // Nuevo: Nickname sincronizado

    private int _lastTeamID = -1;

    public override void Spawned()
    {
        // Al nacer, si soy yo (InputAuthority), envío mi nombre al servidor
        if (Object.HasInputAuthority)
        {
            string myNick = "Jugador";
            if (SessionManager.Instance != null) myNick = SessionManager.Instance.playerNickname;
            RPC_SetNickname(myNick);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(string name)
    {
        PlayerName = name;
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            if (TeamID != _lastTeamID && TeamID != -1)
            {
                _lastTeamID = TeamID;
                SessionManager.Instance?.SetTeam(TeamID);
                PlayerPrefs.SetInt("AssignedCharacter", CharacterID);
            }
        }
    }

    // --- RPC PARA RONDA TERMINADA (Intermedia) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RoundFinished(string message)
    {
        Debug.Log($"🏆 Info Ronda: {message}");
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = message;
            SessionManager.Instance.IsFinalMatch = false; // Es ronda intermedia
        }
    }

    // --- RPC PARA FINAL DEL JUEGO (Transición a Videos) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FinalMatchResult(int winningTeamID, string message)
    {
        Debug.Log("🎬 Fin del Juego recibido. Calculando escena de video...");
        
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = message;
            SessionManager.Instance.IsFinalMatch = true;
            
            // Llamamos al Handler para que maneje la desconexión y el video
            if (NetworkRunnerHandler.Instance != null)
            {
                NetworkRunnerHandler.Instance.HandleFinalVideoTransition(winningTeamID);
            }
        }
    }
}