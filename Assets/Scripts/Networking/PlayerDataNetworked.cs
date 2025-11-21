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
            string myNick = "Jugador";
            if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.playerNickname))
                myNick = SessionManager.Instance.playerNickname;

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
    public void RPC_SetNickname(string name)
    {
        PlayerName = name;
        if (NetworkRunnerHandler.Instance != null) NetworkRunnerHandler.Instance.RegisterPlayerName(Object.InputAuthority, name);
    }

    // --- ESTA ES LA CLAVE PARA EL PROBLEMA DE UI ---
    // Recibe el texto del servidor y lo guarda en el SessionManager local.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetUIMessage(string exactMessage, bool isFinal, int finalWinnerID)
    {
        Debug.Log($"📩 Datos de partida recibidos: {exactMessage}");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = exactMessage;
            SessionManager.Instance.IsFinalMatch = isFinal;
            if (isFinal) SessionManager.Instance.FinalWinnerTeam = finalWinnerID;
        }
    }
}