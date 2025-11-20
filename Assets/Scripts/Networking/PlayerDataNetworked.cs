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
        // Guardar en el Handler del Servidor
        if (NetworkRunnerHandler.Instance != null) NetworkRunnerHandler.Instance.RegisterPlayerName(Object.InputAuthority, name);
    }

    // --- RPC 1: RECIBIR RESULTADOS Y CONSTRUIR TEXTO ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncResults(int roundWinnerID, int mayaScore, int spanishScore, bool isFinal)
    {
        // Construimos el mensaje LOCALMENTE para evitar errores de red
        string wName = (roundWinnerID == 0) ? "Maya" : "Español";
        string msg = "";

        if (isFinal)
        {
            string globalW = (mayaScore > spanishScore) ? "IMPERIO MAYA" : "ESPAÑOLES";
            msg = $"👑 ¡FIN DEL TORNEO!\n\nGanador Global: {globalW}\nMarcador: Maya {mayaScore} - {spanishScore} Español";
        }
        else
        {
            msg = $"Ronda Terminada\nGanador Ronda: {wName}\n\nGlobal: Maya {mayaScore} - {spanishScore} Español";
        }

        Debug.Log($"📝 Mensaje Construido Localmente: {msg}");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GameOverMessage = msg;
            SessionManager.Instance.IsFinalMatch = isFinal;
        }
    }

    // --- RPC 2: PREPARAR VIDEO ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PrepareForVideo(int finalWinnerID)
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.FinalWinnerTeam = finalWinnerID;
        }
    }

    // --- RPC 3: DESCONECTAR Y CARGAR ESCENA ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DisconnectAndLoadVideo()
    {
        Debug.Log("👋 RPC Recibido: Desconectar e ir a Winners.");
        
        // Apagar Fusion
        if (Runner != null) Runner.Shutdown();

        // Decirle al SessionManager que cargue la escena
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.GoToVideoScene();
        }
    }
}