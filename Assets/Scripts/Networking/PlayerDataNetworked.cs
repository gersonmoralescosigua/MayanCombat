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
            if (TeamID != _lastTeamID && TeamID != -1)
            {
                _lastTeamID = TeamID;
                SessionManager.Instance?.SetTeam(TeamID);
                PlayerPrefs.SetInt("AssignedCharacter", CharacterID);
            }
        }
    }

    // --- RPC MEJORADO ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RoundFinished(int winningTeamID, int mayaScore, int spanishScore, bool isFinalMatch, string finalWinnerName)
    {
        string roundWinner = (winningTeamID == 0) ? "Maya" : "Español";
        string mensaje = "";

        if (isFinalMatch)
        {
            mensaje = $"👑 ¡GRAN VICTORIA FINAL!\n\nGanador: {finalWinnerName}\nMarcador Final: Maya {mayaScore} - {spanishScore} Español";
        }
        else
        {
            mensaje = $"Ronda Terminada\nGanador: {roundWinner}\n\nMarcador: Maya {mayaScore} - {spanishScore} Español\n(Siguiente mapa en breve...)";
        }
        
        Debug.Log($"🏆 RPC Info: {mensaje}");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = mensaje;
            SessionManager.Instance.IsFinalMatch = isFinalMatch;
        }
    }
}