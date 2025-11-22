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

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_SetUIMessage(string exactMessage, bool isFinal, int finalWinnerID, RpcInfo info = default)
{
    // DEBUG DETALLADO - INCLUIR EL PlayerRef DEL REMITENTE
    Debug.Log($"📩 [{Object.InputAuthority}] RPC recibido de [{info.Source}] - Message: '{exactMessage}', IsFinal: {isFinal}, WinnerID: {finalWinnerID}");

    if (SessionManager.Instance != null)
    {
        // VERIFICAR SI LOS DATOS SON DIFERENTES A LOS ACTUALES
        bool messageChanged = (SessionManager.Instance.GameOverMessage != exactMessage);
        bool finalChanged = (SessionManager.Instance.IsFinalMatch != isFinal);
        bool winnerChanged = (SessionManager.Instance.FinalWinnerTeam != finalWinnerID);
        
        SessionManager.Instance.GameOverMessage = exactMessage;
        SessionManager.Instance.IsFinalMatch = isFinal;
        if (isFinal) 
        {
            SessionManager.Instance.FinalWinnerTeam = finalWinnerID;
            Debug.Log($"✅ [{Object.InputAuthority}] FINAL WinnerTeam guardado: {SessionManager.Instance.FinalWinnerTeam} (cambiado: {winnerChanged})");
        }
        else
        {
            Debug.Log($"✅ [{Object.InputAuthority}] Datos de ronda guardados (no es final)");
        }
        
        Debug.Log($"💾 [{Object.InputAuthority}] SessionManager actualizado - MessageChanged: {messageChanged}, FinalChanged: {finalChanged}");
    }
    else
    {
        Debug.LogError($"❌ [{Object.InputAuthority}] SessionManager es NULL - NO se guardaron datos");
    }
}

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConfirmMessageReceived(PlayerRef player)
    {
        Debug.Log($"✅ Confirmación recibida del jugador: {player}");
    }
}