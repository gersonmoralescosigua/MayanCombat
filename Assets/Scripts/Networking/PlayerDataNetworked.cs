using Fusion;
using UnityEngine;

public class PlayerDataNetworked : NetworkBehaviour
{
    [Networked] public int TeamID { get; set; } = -1;
    [Networked] public int CharacterID { get; set; } = -1;
    
    // Variable mágica: -1 = Nadie gana aún. 0 = Gana Maya. 1 = Gana Español.
    [Networked] public int WinnerTeamID { get; set; } = -1; 

    private int _lastTeamID = -1;
    private int _lastWinnerID = -1;

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            // Detectar asignación de equipo (Inicio de partida)
            if (TeamID != _lastTeamID)
            {
                _lastTeamID = TeamID;
                if (TeamID != -1)
                {
                    SessionManager.Instance?.SetTeam(TeamID);
                    PlayerPrefs.SetInt("AssignedCharacter", CharacterID);
                }
            }

            // Detectar FINAL DE PARTIDA (Cuando alguien muere)
            if (WinnerTeamID != -1 && WinnerTeamID != _lastWinnerID)
            {
                _lastWinnerID = WinnerTeamID;
                
                if (SessionManager.Instance != null)
                {
                    // 0 = Maya, 1 = Español
                    string winnerRole = (WinnerTeamID == 0) ? "IMPERIO MAYA" : "ESPAÑOLES";
                    
                    // Mensaje personalizado para todos
                    SessionManager.Instance.GameOverMessage = $"¡VICTORIA PARA {winnerRole}!\n\n(El oponente ha caído)";
                    Debug.Log($"🏆 Ganador recibido: {winnerRole}");
                }
            }
        }
    }
}