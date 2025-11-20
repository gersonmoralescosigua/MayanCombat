using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    [Networked] public int TeamID { get; set; } = -1;
    [Networked] public int CharacterID { get; set; } = -1;
    
    private int _lastTeamID = -1;

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            if (TeamID != _lastTeamID)
            {
                _lastTeamID = TeamID;
                if (TeamID != -1)
                {
                    SessionManager.Instance?.SetTeam(TeamID);
                    PlayerPrefs.SetInt("AssignedCharacter", CharacterID);
                }
            }
        }
    }

    // --- RPC CRUCIAL: RECIBE EL MENSAJE DE GANADOR ---
    // [Rpc(RpcSources.StateAuthority, RpcTargets.All)] significa:
    // "El Servidor (StateAuthority) lo llama, y se ejecuta en TODOS (All) los clientes".
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GameFinished(int winningTeamID)
    {
        string winnerRole = (winningTeamID == 0) ? "IMPERIO MAYA" : "ESPAÑOLES";
        string mensaje = $"¡VICTORIA PARA {winnerRole}!\n\n(El oponente ha caído)";
        
        Debug.Log($"🏆 RPC Recibido en cliente: {mensaje}");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = mensaje;
        }
    }
}