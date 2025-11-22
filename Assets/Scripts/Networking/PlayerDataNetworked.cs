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

    // --- RPC MEJORADO CON CONFIRMACIÓN ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetUIMessage(string exactMessage, bool isFinal, int finalWinnerID, RpcInfo info = default)
    {
        Debug.Log($"📩 CLIENTE {Object.InputAuthority}: Datos recibidos - {exactMessage}");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = exactMessage;
            SessionManager.Instance.IsFinalMatch = isFinal;
            if (isFinal) SessionManager.Instance.FinalWinnerTeam = finalWinnerID;
            
            // CONFIRMAR RECEPCIÓN
            Debug.Log($"✅ CLIENTE {Object.InputAuthority}: Datos guardados en SessionManager");
        }
        else
        {
            Debug.LogError($"❌ CLIENTE {Object.InputAuthority}: SessionManager es NULL");
        }

        // Enviar confirmación al servidor (opcional)
        if (Object.HasStateAuthority) // Solo el servidor procesa confirmaciones
        {
            RPC_ConfirmMessageReceived(info.Source);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConfirmMessageReceived(PlayerRef player)
    {
        Debug.Log($"✅ Confirmación recibida del jugador: {player}");
    }
}