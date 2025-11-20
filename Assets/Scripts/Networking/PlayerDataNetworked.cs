using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    [Networked] public int TeamID { get; set; } = -1;
    [Networked] public int CharacterID { get; set; } = -1;
    
    // Variable auxiliar para detectar cambios locales de equipo al inicio
    private int _lastTeamID = -1;

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            // Detectar si me asignaron equipo al inicio
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

    // --- RPC PARA FINALIZAR PARTIDA ---
    // Esto se ejecuta en TODOS los clientes inmediatamente cuando el Host lo llama.
    // Garantiza que el mensaje llegue antes del cambio de escena.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GameFinished(int winningTeamID)
    {
        string winnerRole = (winningTeamID == 0) ? "IMPERIO MAYA" : "ESPAÑOLES";
        string mensaje = $"¡VICTORIA PARA {winnerRole}!\n\n(El oponente ha caído)";
        
        Debug.Log($"🏆 RPC Recibido: {mensaje}");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = mensaje;
        }
    }
}