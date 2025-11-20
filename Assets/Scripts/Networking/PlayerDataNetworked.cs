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
            {
                myNick = SessionManager.Instance.playerNickname;
            }
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
                PlayerPrefs.SetInt("AssignedCharacter", CharacterID);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(string name)
    {
        PlayerName = name;
        Debug.Log($"🏷️ Servidor guardando nombre: {name}");
        
        // --- IMPORTANTE: Guardamos en el diccionario del Handler ---
        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.RegisterPlayerName(Object.InputAuthority, name);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateMatchResults(string message, bool isFinal)
    {
        Debug.Log($"📩 Mensaje UI: {message}");
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = message;
            SessionManager.Instance.IsFinalMatch = isFinal;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GoToVideoScene(string sceneName)
    {
        Debug.Log($"🎬 Orden Video: {sceneName}");
        if (SessionManager.Instance != null) SessionManager.Instance.VideoSceneToLoad = sceneName;
        
        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.ExecuteVideoTransition();
        }
    }
}