using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    [Networked] public int TeamID { get; set; } = -1;
    [Networked] public int CharacterID { get; set; } = -1;
    // Capacidad para 16 caracteres. OnChanged asegura que se sincronice.
    [Networked] public NetworkString<_16> PlayerName { get; set; } 

    private int _lastTeamID = -1;

    public override void Spawned()
    {
        // Si soy el dueño de este objeto (mi jugador local)
        if (Object.HasInputAuthority)
        {
            string myNick = "Jugador";
            if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.playerNickname))
            {
                myNick = SessionManager.Instance.playerNickname;
            }
            
            // Enviamos el nombre al Servidor inmediatamente
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

    // RPC para que el Servidor guarde mi nombre en la variable Networked
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(string name)
    {
        PlayerName = name;
        Debug.Log($"🏷️ Servidor recibió nombre: {name} para Player {Object.InputAuthority}");
    }

    // --- RPC 1: ACTUALIZAR UI DE RESULTADOS ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateMatchResults(string message, bool isFinal)
    {
        Debug.Log($"📩 RPC Mensaje Recibido: {message}");
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = message;
            SessionManager.Instance.IsFinalMatch = isFinal;
        }
    }

    // --- RPC 2: TRANSICIÓN A VIDEO ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GoToVideoScene(string sceneName)
    {
        Debug.Log($"🎬 RPC Orden de Video: Ir a {sceneName}");
        
        // Guardamos a dónde queremos ir
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.VideoSceneToLoad = sceneName;
        }
        
        // Le decimos al Handler local que ejecute la salida
        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.ExecuteVideoTransition();
        }
    }
}